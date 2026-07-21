# Serialization Rules

## JSON everywhere

`System.Text.Json` is the only serializer. No Newtonsoft in application code (it is a transitive Hangfire dependency only).

## HTTP

- ASP.NET Core defaults: camelCase property names.
- Sales adds `JsonStringEnumConverter` (`options.ConfigureControllers`), so enums are strings on the wire. Inventory does not, but its DTOs already expose status as `string`.
- SignalR uses the same converter via `AddJsonProtocol`.
- `ApiResponse<T>.IsSuccess` is serialized as `success` through `[JsonPropertyName("success")]`. The frontend also tolerates `succeeded` — do not rely on that; emit `success`.

## Kafka

- `EventEnvelope.Data` is a `JsonElement` produced by `JsonSerializer.SerializeToElement(data, data.GetType())` — the **runtime** type, so the concrete payload is serialized, not the static one.
- Envelopes are serialized with `KafkaFlow.Serializer.JsonCore` (default options: PascalCase properties).
- Consumers deserialize with `envelope.Data.Deserialize<TContract>()`.
- Do not change the envelope serializer options; producers and consumers across three services depend on the current shape.

## Persistence

- Outbox/Inbox payloads are `JsonSerializer.Serialize(envelope)` stored in a `jsonb` column.
- Inbox rows keep the envelope so `InboxRedriveService` can replay it — Kafka will not redeliver a committed offset.
- Redis cache entries are `JsonSerializer.Serialize(value)` with default options. A cached DTO must round-trip through default `System.Text.Json`: public parameterless-constructible or a record whose constructor parameters match property names.

## MongoDB

- Audit documents use the MongoDB driver's BSON serializer, not `System.Text.Json`.
- GUIDs are annotated `[BsonGuidRepresentation(GuidRepresentation.Standard)]`.
- `JsonElement` values from the envelope must be normalized to CLR types before writing (`MongoAuditWriter.NormalizeAuditValue`), otherwise Mongo stores driver-internal structure.

## Rules

- Never add a custom `JsonConverter` without checking every consumer of that payload.
- Never rely on property ordering.
- Never serialize a domain aggregate. Serialize a DTO or a contract.
- Additive changes only within a contract version; a removal or rename needs a new topic version.

## Related

- [event-rule.md](event-rule.md)
- [dto-rule.md](dto-rule.md)
