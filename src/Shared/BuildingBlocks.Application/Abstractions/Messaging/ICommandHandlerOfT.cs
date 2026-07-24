using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Handles a command that returns a response.
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
