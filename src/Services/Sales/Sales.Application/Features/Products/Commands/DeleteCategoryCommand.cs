using MediatR;

namespace Sales.Application.Features.Products.Commands;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand;
