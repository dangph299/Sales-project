using BuildingBlocks.Infrastructure;

namespace AuditLog.Tests;

public sealed class AuditChangeDetectorTests
{
    [Fact]
    public void Updated_records_only_modified_fields()
    {
        var changes = AuditChangeDetector.Updated(
            new { Name = "John", Phone = "0909000000", Status = "Active" },
            new { Name = "John Smith", Phone = "0988111222", Status = "Active" });

        Assert.Equal(["Name", "Phone"], changes.Select(x => x.Field).ToArray());
        Assert.Contains(changes, x => x.Field == "Name" && Equals(x.OldValue, "John") && Equals(x.NewValue, "John Smith"));
        Assert.DoesNotContain(changes, x => x.Field == "Status");
    }

    [Fact]
    public void Created_records_initial_values()
    {
        var changes = AuditChangeDetector.Created(new { Sku = "SKU-1", Name = "TShirt", Price = 120_000m });

        Assert.Equal(["Name", "Price", "Sku"], changes.Select(x => x.Field).Order().ToArray());
        Assert.Contains(changes, x => x.Field == "Price" && x.OldValue is null && Equals(x.NewValue, 120_000m));
    }

    [Fact]
    public void Updated_supports_nullable_and_nested_values()
    {
        var changes = AuditChangeDetector.Updated(
            new { Name = "John", Address = new { City = "HCM", Ward = (string?)null } },
            new { Name = "John", Address = new { City = "Ha Noi", Ward = "Ba Dinh" } });

        Assert.Equal(["Address.City", "Address.Ward"], changes.Select(x => x.Field).ToArray());
        Assert.Contains(changes, x => x.Field == "Address.Ward" && x.OldValue is null && Equals(x.NewValue, "Ba Dinh"));
    }
}
