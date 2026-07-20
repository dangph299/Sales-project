namespace Sales.Api.Models.Requests;

public sealed class UpdateCategoryRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? ParentCategoryId { get; init; }

    public int SortOrder { get; init; }

    public string Status { get; init; } = "Draft";
}
