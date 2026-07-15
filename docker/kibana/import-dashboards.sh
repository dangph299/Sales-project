#!/usr/bin/env sh
# Idempotently import the Sales/Inventory Kibana saved objects (data views, visualizations,
# dashboard) once Kibana is healthy. Safe to run repeatedly: uses overwrite=true.
#
# Environment:
#   KIBANA_URL        Kibana base URL (default http://kibana:5601)
#   KIBANA_USERNAME   Optional basic-auth user (only if Kibana security is enabled)
#   KIBANA_PASSWORD   Optional basic-auth password
#   NDJSON_FILE       Saved-objects file (default: alongside this script)
#   WAIT_RETRIES      Health-check attempts before giving up (default 60)
#   WAIT_INTERVAL     Seconds between attempts (default 5)
set -eu

KIBANA_URL="${KIBANA_URL:-http://kibana:5601}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
NDJSON_FILE="${NDJSON_FILE:-$SCRIPT_DIR/exports/sales-management-reliability.ndjson}"
WAIT_RETRIES="${WAIT_RETRIES:-60}"
WAIT_INTERVAL="${WAIT_INTERVAL:-5}"

AUTH=""
if [ -n "${KIBANA_USERNAME:-}" ] && [ -n "${KIBANA_PASSWORD:-}" ]; then
  AUTH="-u ${KIBANA_USERNAME}:${KIBANA_PASSWORD}"
fi

log() { echo "[import-dashboards] $*"; }

if [ ! -f "$NDJSON_FILE" ]; then
  log "ERROR: saved-objects file not found: $NDJSON_FILE"
  exit 1
fi

log "Waiting for Kibana at $KIBANA_URL to become available ..."
i=0
while [ "$i" -lt "$WAIT_RETRIES" ]; do
  # Kibana reports overall_status.level=available when it can serve the Saved Objects API.
  status="$(curl -s $AUTH "$KIBANA_URL/api/status" 2>/dev/null || true)"
  case "$status" in
    *'"level":"available"'*)
      log "Kibana is available."
      break
      ;;
  esac
  i=$((i + 1))
  if [ "$i" -ge "$WAIT_RETRIES" ]; then
    log "ERROR: Kibana did not become available after $((WAIT_RETRIES * WAIT_INTERVAL))s."
    exit 1
  fi
  sleep "$WAIT_INTERVAL"
done

log "Importing saved objects from $NDJSON_FILE ..."
# _import with overwrite=true makes re-runs idempotent (updates existing objects in place).
response="$(curl -s -w '\n%{http_code}' $AUTH \
  -X POST "$KIBANA_URL/api/saved_objects/_import?overwrite=true" \
  -H 'kbn-xsrf: true' \
  --form file=@"$NDJSON_FILE")"

http_code="$(echo "$response" | tail -n1)"
body="$(echo "$response" | sed '$d')"

log "HTTP $http_code"
log "$body"

# The API returns 200 even when individual objects fail, so check the success flag too.
case "$body" in
  *'"success":true'*)
    log "Dashboard import succeeded."
    exit 0
    ;;
  *)
    log "ERROR: dashboard import failed."
    exit 1
    ;;
esac
