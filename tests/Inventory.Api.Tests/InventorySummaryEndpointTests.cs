using System.Reflection;
using Inventory.Api.Controllers;
using Inventory.Api.Extensions;
using Inventory.Api.Models.Requests;
using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Inventory.Api.Tests;

public sealed class InventorySummaryEndpointTests
{
    private static readonly InventorySummary CannedSummary = new(
        TotalItems: 10,
        TotalQuantity: 100,
        InStock: 7,
        LowStock: 2,
        OutOfStock: 1,
        LowStockThreshold: 0);

    [Fact]
    public async Task Summary_uses_explicit_threshold_when_provided()
    {
        var (controller, sender) = CreateController(optionsThreshold: 7);
        var query = new InventorySummaryRequest { LowStockThreshold = 3 };

        var result = await controller.Summary(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var captured = Assert.IsType<GetInventorySummaryQuery>(sender.CapturedRequest);
        Assert.Equal(3, captured.Filter.LowStockThreshold);
    }

    [Fact]
    public async Task Summary_falls_back_to_options_threshold_when_query_omits_it()
    {
        var (controller, sender) = CreateController(optionsThreshold: 7);
        var query = new InventorySummaryRequest { LowStockThreshold = null };

        var result = await controller.Summary(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var captured = Assert.IsType<GetInventorySummaryQuery>(sender.CapturedRequest);
        Assert.Equal(7, captured.Filter.LowStockThreshold);
    }

    [Fact]
    public async Task Summary_passes_through_reserved_filter_fields()
    {
        var (controller, sender) = CreateController(optionsThreshold: 7);
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var query = new InventorySummaryRequest
        {
            LowStockThreshold = 5,
            WarehouseId = warehouseId,
            LocationId = locationId,
            CompanyId = companyId,
        };

        await controller.Summary(query, CancellationToken.None);

        var captured = Assert.IsType<GetInventorySummaryQuery>(sender.CapturedRequest);
        Assert.Equal(warehouseId, captured.Filter.WarehouseId);
        Assert.Equal(locationId, captured.Filter.LocationId);
        Assert.Equal(companyId, captured.Filter.CompanyId);
    }

    [Fact]
    public void InventoryController_requires_authorization_and_summary_has_no_anonymous_override()
    {
        var classAttribute = Assert.Single(
            typeof(InventoryController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .OfType<AuthorizeAttribute>());
        Assert.NotNull(classAttribute);

        var summaryMethod = typeof(InventoryController).GetMethod(nameof(InventoryController.Summary));
        Assert.NotNull(summaryMethod);
        Assert.Empty(summaryMethod!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false));
    }

    private static (InventoryController Controller, FakeSender Sender) CreateController(int optionsThreshold)
    {
        var sender = new FakeSender(CannedSummary);
        var options = Options.Create(new InventorySummaryOptions { LowStockThreshold = optionsThreshold });
        var controller = new InventoryController(sender, options)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        return (controller, sender);
    }

    private sealed class FakeSender(InventorySummary summary) : ISender
    {
        public object? CapturedRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            CapturedRequest = request;
            return Task.FromResult((TResponse)(object)summary);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
