# Kafka Rules

## Constants

- Topics: `BuildingBlocks.Contracts.KafkaTopics` only. `docker/kafka-init-topics.sh` parses this file to provision topics, so a topic that is not declared there does not exist at runtime (`KAFKA_AUTO_CREATE_TOPICS_ENABLE=false`).
- Consumer groups: `KafkaConsumerGroups`.
- Headers: `MessageHeaders`.
- Contract versions: `ContractVersions`.
- Never inline a topic, group, or header string.

## Producing

- Never call a producer from a handler. Write an outbox row; `OutboxPublisherService` publishes it.
- The only producer path is `KafkaOutboxPublisher`, registered per service with `AddKafkaOutboxPublisher("<service>-outbox")`.
- Message key is `envelope.AggregateId.ToString()` so all events for one aggregate land on one partition and stay ordered.
- The publisher opens a `kafka.publish <topic>` producer span and writes `traceparent`/`tracestate` headers.
- Mark the outbox row processed only after the broker acknowledges.

## Consuming

- Consumers are KafkaFlow typed handlers deriving from `IntegrationEventHandler<THandler>`; the derived class adds nothing but its own type name.
- Business logic goes in an `IIntegrationEventProcessor` implementation resolved per message in a fresh async scope.
- Consumer configuration must set: topics, `WithGroupId(...)`, `WithAutoOffsetReset(AutoOffsetReset.Earliest)`, `WithBufferSize(...)`, `WithWorkersCount(...)`, JSON deserializer, typed handler.
- `AutoOffsetReset.Earliest` is mandatory. The default (`Latest`) silently drops events produced before the consumer first connects.
- **Never rethrow from a consumer handler.** KafkaFlow commits the offset regardless, so a throw loses the message. Record the failure in the inbox and let `InboxRedriveService` retry. The only rethrow is when no `IInboxFailureRecorder` is registered, so the loss is loud.

## Idempotency

- Every consumed event is deduplicated by `EventId` through the inbox before any state change.
- Sales: `SalesInventoryEventProcessor` inserts the inbox row inside an explicit transaction with the order transition.
- Inventory: `InventoryTransactionBehavior` does a non-transactional pre-check, then an authoritative `TryRecordAsync` insert inside a serializable transaction.
- A duplicate returns a "Duplicate" outcome string and touches nothing.

## Bus lifecycle

- API hosts: `KafkaBusLifecycle.StartAsync(app.Services)` in `RunStartupTasksAsync`, stopped on `ApplicationStopping`.
- Worker host: `KafkaBusService` hosted service.
- Never create a bus manually anywhere else.

## Adding a new event end to end

1. Add the contract record in `BuildingBlocks.Contracts/IntegrationEvents/<Context>/`.
2. Add the topic constant to `KafkaTopics`.
3. Producer side: raise a domain event and add a `case` to `DomainEventMapper` (Sales), or call an `IInventoryEventOutbox` method (Inventory).
4. Consumer side: add a `case` in the target `IIntegrationEventProcessor` and a command if state changes.
5. Add the topic to the consumer's `.Topics([...])` list.
6. Restart the stack so `kafka-init` provisions the topic.
7. Add a handler test and, where reliability matters, a `Category=Reliability` test.

## Related

- [event-rule.md](event-rule.md)
- Deep dive: [../../guides/kafka-usage-guide.md](../../guides/kafka-usage-guide.md)
- Reference: [../../tech/kafka-topics-and-schemas.md](../../tech/kafka-topics-and-schemas.md)
