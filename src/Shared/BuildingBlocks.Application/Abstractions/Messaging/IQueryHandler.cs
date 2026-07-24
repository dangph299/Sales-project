using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Handles a query that returns a response.
/// </summary>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
