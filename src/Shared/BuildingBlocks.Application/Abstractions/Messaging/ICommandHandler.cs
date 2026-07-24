using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Handles a command that returns no response body.
/// </summary>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;
