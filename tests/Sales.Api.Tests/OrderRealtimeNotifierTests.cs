using Microsoft.AspNetCore.SignalR;
using Sales.Api.Realtime;
using Sales.Application.Features.Orders.Realtime;
using Sales.Domain;

namespace Sales.Api.Tests;

public sealed class OrderRealtimeNotifierTests
{
    [Fact]
    public async Task Notifier_sends_status_change_to_order_and_list_groups_without_broadcasting_to_all()
    {
        var clients = new RecordingHubClients();
        var notifier = new SignalROrderRealtimeNotifier(new RecordingHubContext(clients));
        var orderId = Guid.NewGuid();
        var notification = new OrderStatusChangedNotification(
            orderId,
            OrderStatus.PendingInventory,
            OrderStatus.Confirmed,
            DateTimeOffset.UtcNow,
            Version: 2);

        await notifier.NotifyOrderStatusChangedAsync(notification, CancellationToken.None);

        Assert.False(clients.AllAccessed);
        Assert.Collection(
            clients.Sends,
            send =>
            {
                Assert.Equal(OrderRealtimeGroups.ForOrder(orderId), send.GroupName);
                Assert.Equal(OrderRealtimeEvents.StatusChanged, send.Method);
                Assert.Same(notification, Assert.Single(send.Args));
            },
            send =>
            {
                Assert.Equal(OrderRealtimeGroups.OrderList, send.GroupName);
                Assert.Equal(OrderRealtimeEvents.StatusChanged, send.Method);
                Assert.Same(notification, Assert.Single(send.Args));
            });
    }

    private sealed class RecordingHubContext(RecordingHubClients clients) : IHubContext<OrderHub>
    {
        public IHubClients Clients => clients;

        public IGroupManager Groups { get; } = new NoopGroupManager();
    }

    private sealed class RecordingHubClients : IHubClients
    {
        public List<RecordedSend> Sends { get; } = [];

        public bool AllAccessed { get; private set; }

        public IClientProxy All
        {
            get
            {
                AllAccessed = true;
                return new RecordingClientProxy("all", Sends);
            }
        }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            AllAccessed = true;
            return new RecordingClientProxy("all", Sends);
        }

        public IClientProxy Client(string connectionId) => new RecordingClientProxy($"client:{connectionId}", Sends);

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new RecordingClientProxy("clients", Sends);

        public IClientProxy Group(string groupName) => new RecordingClientProxy(groupName, Sends);

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new RecordingClientProxy(groupName, Sends);

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => new RecordingClientProxy(string.Join(",", groupNames), Sends);

        public IClientProxy User(string userId) => new RecordingClientProxy($"user:{userId}", Sends);

        public IClientProxy Users(IReadOnlyList<string> userIds) => new RecordingClientProxy("users", Sends);
    }

    private sealed class RecordingClientProxy(
        string groupName,
        List<RecordedSend> sends) : IClientProxy
    {
        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            sends.Add(new RecordedSend(groupName, method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class NoopGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record RecordedSend(string GroupName, string Method, object?[] Args);
}

