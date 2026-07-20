namespace Sales.Application.Features.Products.DTOs;

public sealed record ProductColorDto(Guid Id, string Code, string Name, string? HexCode);
