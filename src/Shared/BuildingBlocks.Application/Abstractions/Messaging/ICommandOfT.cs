using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Command that changes state and returns a response.
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>;
