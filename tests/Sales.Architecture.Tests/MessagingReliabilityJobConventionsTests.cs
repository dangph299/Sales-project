using BuildingBlocks.Contracts;
using Inventory.Infrastructure;
using Sales.Infrastructure;
using InventoryKafkaLagMonitorJobOptions = Inventory.Infrastructure.KafkaLagMonitorJobOptions;
using SalesKafkaLagMonitorJobOptions = Sales.Infrastructure.KafkaLagMonitorJobOptions;

namespace Sales.Architecture.Tests;

public sealed class MessagingReliabilityJobConventionsTests
{
    [Fact]
    public void Sales_and_inventory_messaging_recurring_job_ids_do_not_overlap()
    {
        var salesJobIds = new[]
        {
            SalesRecurringJobIds.ReplayDeadLetter,
            SalesRecurringJobIds.KafkaLagMonitor,
            SalesRecurringJobIds.InboxCleanup,
            SalesRecurringJobIds.FailedOutboxRetry,
            SalesRecurringJobIds.OutboxPendingMonitor
        };
        var inventoryJobIds = new[]
        {
            InventoryRecurringJobIds.ReplayDeadLetter,
            InventoryRecurringJobIds.KafkaLagMonitor,
            InventoryRecurringJobIds.InboxCleanup,
            InventoryRecurringJobIds.FailedOutboxRetry,
            InventoryRecurringJobIds.OutboxPendingMonitor
        };

        Assert.Empty(salesJobIds.Intersect(inventoryJobIds));
        Assert.Equal(salesJobIds.Length, salesJobIds.Distinct().Count());
        Assert.Equal(inventoryJobIds.Length, inventoryJobIds.Distinct().Count());
    }

    [Fact]
    public void Sales_and_inventory_messaging_lock_keys_do_not_overlap()
    {
        var lockKeys = new[]
        {
            SalesMessagingJobLockKeys.ReplayDeadLetter,
            SalesMessagingJobLockKeys.KafkaLagMonitor,
            SalesMessagingJobLockKeys.InboxCleanup,
            SalesMessagingJobLockKeys.FailedOutboxRetry,
            SalesMessagingJobLockKeys.OutboxPendingMonitor,
            InventoryMessagingJobLockKeys.ReplayDeadLetter,
            InventoryMessagingJobLockKeys.KafkaLagMonitor,
            InventoryMessagingJobLockKeys.InboxCleanup,
            InventoryMessagingJobLockKeys.FailedOutboxRetry,
            InventoryMessagingJobLockKeys.OutboxPendingMonitor
        };

        Assert.Equal(lockKeys.Length, lockKeys.Distinct().Count());
    }

    [Fact]
    public void Kafka_lag_monitor_options_default_to_service_owned_groups_and_topics()
    {
        var salesOptions = new SalesKafkaLagMonitorJobOptions();
        var inventoryOptions = new InventoryKafkaLagMonitorJobOptions();

        Assert.Equal(KafkaConsumerGroups.SalesInventoryResults, salesOptions.GroupId);
        Assert.Equal(
            [KafkaTopics.StockReserved, KafkaTopics.StockRejected, KafkaTopics.StockReleased],
            salesOptions.Topics);
        Assert.Equal(KafkaConsumerGroups.InventoryOrders, inventoryOptions.GroupId);
        Assert.Equal(
            [KafkaTopics.OrderConfirmationRequested, KafkaTopics.OrderUndoConfirmationRequested],
            inventoryOptions.Topics);
    }
}
