#!/usr/bin/env bash
set -euo pipefail

BOOTSTRAP_SERVER="${KAFKA_BOOTSTRAP_SERVER:-kafka:9092}"
TOPICS_SOURCE="${KAFKA_TOPICS_SOURCE:-/opt/kafka-init/KafkaTopics.cs}"
PARTITIONS="${KAFKA_TOPIC_PARTITIONS:-3}"
REPLICATION_FACTOR="${KAFKA_TOPIC_REPLICATION_FACTOR:-1}"
KAFKA_BIN="${KAFKA_BIN:-/opt/kafka/bin}"

echo "Waiting for Kafka at ${BOOTSTRAP_SERVER}..."
until "${KAFKA_BIN}/kafka-broker-api-versions.sh" --bootstrap-server "${BOOTSTRAP_SERVER}" >/dev/null 2>&1; do
  sleep 2
done

if [ ! -f "${TOPICS_SOURCE}" ]; then
  echo "Kafka topics source not found: ${TOPICS_SOURCE}" >&2
  exit 1
fi

topics="$(sed -n 's/.*const string [A-Za-z0-9_]* = "\([^"]*\)".*/\1/p' "${TOPICS_SOURCE}" | sort -u)"

if [ -z "${topics}" ]; then
  echo "No Kafka topics found in ${TOPICS_SOURCE}" >&2
  exit 1
fi

for topic in ${topics}; do
  echo "Ensuring Kafka topic ${topic} (${PARTITIONS} partitions, replication factor ${REPLICATION_FACTOR})"
  "${KAFKA_BIN}/kafka-topics.sh" \
    --bootstrap-server "${BOOTSTRAP_SERVER}" \
    --create \
    --if-not-exists \
    --topic "${topic}" \
    --partitions "${PARTITIONS}" \
    --replication-factor "${REPLICATION_FACTOR}"
done

echo "Kafka topic initialization completed."
