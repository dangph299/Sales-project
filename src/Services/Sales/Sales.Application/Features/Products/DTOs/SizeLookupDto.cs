namespace Sales.Application.Features.Products.DTOs;

/// <summary>
/// Size published by the reference-data lookup, including the seeded sort order that ordering-aware
/// clients display.
/// </summary>
public sealed record SizeLookupDto(Guid Id, string Code, string Name, int SortOrder);
