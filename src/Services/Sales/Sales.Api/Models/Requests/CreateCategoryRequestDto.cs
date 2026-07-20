namespace Sales.Api.Models.Requests;

public sealed class CreateCategoryRequestDto
{
    public string CategoryCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? ParentCategoryId { get; init; }

    public int SortOrder { get; init; }
}
