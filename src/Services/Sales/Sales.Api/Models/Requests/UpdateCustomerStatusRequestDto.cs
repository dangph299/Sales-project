namespace Sales.Api.Models.Requests;

public sealed class UpdateCustomerStatusRequestDto
{
    public string Status { get; init; } = string.Empty;
}
