namespace Sales.Application.Features.Products.DTOs;

/// <summary>
/// Color published by the reference-data lookup. Separate from <see cref="ProductColorDto"/> so the
/// lookup can carry presentation fields without widening the color projection embedded in every
/// product variant response.
/// </summary>
public sealed record ColorLookupDto(Guid Id, string Code, string Name, string? HexCode);
