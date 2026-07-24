using BuildingBlocks.Contracts;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Customers.Interfaces;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Orders.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Handles <see cref="CreateOrder"/> by resolving or creating the customer behind the requested
/// phone number, then creating a draft <see cref="Order"/> that snapshots the requested details.
/// </summary>
/// <remarks>
/// The customer and the order are written by a single save inside one transaction, so an order can
/// never reference a customer that was not persisted, and a customer created for an order that then
/// fails is rolled back with it.
/// </remarks>
public sealed class CreateOrderHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    ISalesTransactionManager transactionManager,
    ISalesUnitOfWork unitOfWork,
    ICustomerCodeGenerator customerCodeGenerator,
    IOrderCodeGenerator orderCodeGenerator,
    IPersistenceExceptionClassifier persistenceExceptionClassifier,
    ILogger<CreateOrderHandler> logger,
    IMapper mapper)
    : ICommandHandler<CreateOrder, OrderDto>
{
    /// <summary>
    /// Resolves or creates the customer, resolves the requested product lines, creates the order,
    /// and commits both in one transaction.
    /// </summary>
    /// <param name="request">Command describing the customer and the requested lines.</param>
    /// <returns>Newly created draft order, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="Common.Exceptions.NotFoundException">Thrown when one of the requested products does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the phone number resolves to a customer that cannot order, the customer details are invalid, or the requested lines are empty or contain a repeated product.</exception>
    public async Task<OrderDto> Handle(CreateOrder request, CancellationToken cancellationToken)
    {
        var normalizedCustomerPhone = CustomerPhoneNormalizer.Normalize(request.Customer.Phone);
        var orderLineItems = await productRepository.Materialize(request.Lines, cancellationToken);

        try
        {
            return await CreateOrderForCustomerAsync(request, normalizedCustomerPhone, orderLineItems, cancellationToken);
        }
        catch (Exception exception) when (IsUniqueViolation(exception))
        {
            // A concurrent request won the race to insert this customer despite the advisory lock.
            // Rebuild against the row it committed. When no such row exists the conflict came from
            // somewhere else — a customer code collision, say — so rethrow rather than paper over
            // an unrelated database failure.
            unitOfWork.DiscardPendingChanges();
            var concurrentlyCreatedCustomer =
                await customerRepository.FindByNormalizedPhoneAsync(normalizedCustomerPhone, cancellationToken);
            if (concurrentlyCreatedCustomer is null)
            {
                throw;
            }

            logger.LogInformation(
                "Retrying order create after a concurrent customer insert {NormalizedCustomerPhone}",
                normalizedCustomerPhone);
            return await CreateOrderForCustomerAsync(request, normalizedCustomerPhone, orderLineItems, cancellationToken);
        }
    }

    private async Task<OrderDto> CreateOrderForCustomerAsync(
        CreateOrder request,
        string normalizedCustomerPhone,
        IReadOnlyCollection<OrderLineItem> orderLineItems,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            // Taken before the lookup, so a second request for the same new phone number waits here
            // instead of also concluding the customer is missing and inserting a duplicate.
            await customerRepository.AcquireNormalizedPhoneLockAsync(normalizedCustomerPhone, cancellationToken);

            var customerId = await ResolveCustomerIdAsync(request.Customer, normalizedCustomerPhone, cancellationToken);

            // Built from the request, never re-read from the customer row: a caller who types a
            // different name for a known phone number gets that name on this order, while the
            // customer record keeps its own.
            var orderCustomerSnapshot = OrderCustomerSnapshot.Create(
                customerId,
                request.Customer.Name,
                request.Customer.Phone,
                request.Customer.Email,
                request.Customer.Address);

            var orderCode = await orderCodeGenerator.NextCodeAsync(cancellationToken);
            var order = Order.Create(orderCode, orderCustomerSnapshot, orderLineItems);
            await orderRepository.AddAsync(order, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Order created {OrderId} {OrderCode} {CustomerId}",
                order.Id,
                order.OrderCode,
                order.CustomerId);
            return mapper.Map<OrderDto>(order);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Guid> ResolveCustomerIdAsync(
        CreateOrderCustomer requestedCustomer,
        string normalizedCustomerPhone,
        CancellationToken cancellationToken)
    {
        var existingCustomer =
            await customerRepository.FindByNormalizedPhoneAsync(normalizedCustomerPhone, cancellationToken);
        if (existingCustomer is not null)
        {
            if (existingCustomer.Status != ECustomerStatus.Normal)
            {
                throw new DomainException("Only normal customers can create orders.");
            }

            // Deliberately left as it is. The order carries its own snapshot; the customer record is
            // not this request's to edit.
            return existingCustomer.Id;
        }

        var customerCode = await customerCodeGenerator.NextCodeAsync(cancellationToken);
        var newCustomer = Customer.Create(
            customerCode,
            requestedCustomer.Name,
            requestedCustomer.Phone,
            requestedCustomer.Email,
            requestedCustomer.Address);
        await customerRepository.AddAsync(newCustomer, cancellationToken);
        return newCustomer.Id;
    }

    private bool IsUniqueViolation(Exception exception)
    {
        return persistenceExceptionClassifier.Classify(exception)?.Code == ErrorCodes.UniqueViolation;
    }
}
