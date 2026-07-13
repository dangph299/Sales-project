using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Command that changes state and returns no response body.
/// </summary>
public interface ICommand : IRequest;
