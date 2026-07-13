using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Query that reads state and returns a response.
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>;
