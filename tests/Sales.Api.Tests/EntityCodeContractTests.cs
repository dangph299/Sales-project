using Sales.Api.Models.Requests;
using Sales.Application.Features.Customers.Commands;
using Sales.Application.Features.Products.Commands;

namespace Sales.Api.Tests;

/// <summary>
/// Guards the deliberate breaking change that removed client-supplied business codes: the create
/// contracts must not expose a code field at all. Accepting one and ignoring it would tell a client
/// its value was used.
/// </summary>
public sealed class EntityCodeContractTests
{
    [Theory]
    [InlineData(typeof(CreateProductCommand), "ProductCode")]
    [InlineData(typeof(CreateCategoryCommand), "CategoryCode")]
    [InlineData(typeof(CreateCategoryRequestDto), "CategoryCode")]
    [InlineData(typeof(CreateCustomerRequestDto), "CustomerCode")]
    [InlineData(typeof(CreateCustomer), "CustomerCode")]
    public void Create_contracts_do_not_accept_a_business_code(Type contractType, string codePropertyName)
    {
        Assert.Null(contractType.GetProperty(codePropertyName));
    }

    [Theory]
    [InlineData(typeof(UpdateProductCommand), "ProductCode")]
    [InlineData(typeof(UpdateCategoryCommand), "CategoryCode")]
    [InlineData(typeof(UpdateCategoryRequestDto), "CategoryCode")]
    [InlineData(typeof(UpdateCustomerRequest), "CustomerCode")]
    [InlineData(typeof(UpdateCustomer), "CustomerCode")]
    public void Update_contracts_cannot_change_a_business_code(Type contractType, string codePropertyName)
    {
        Assert.Null(contractType.GetProperty(codePropertyName));
    }

    [Theory]
    [InlineData(typeof(Sales.Domain.Product))]
    [InlineData(typeof(Sales.Domain.Category))]
    [InlineData(typeof(Sales.Domain.Customer))]
    public void Domain_exposes_no_method_for_rewriting_a_code(Type aggregateType)
    {
        var codeMutators = aggregateType
            .GetMethods()
            .Where(method => method.Name.Contains("Code", StringComparison.Ordinal)
                && (method.Name.StartsWith("Set", StringComparison.Ordinal)
                    || method.Name.StartsWith("Update", StringComparison.Ordinal)
                    || method.Name.StartsWith("Change", StringComparison.Ordinal)))
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(codeMutators);
    }

    [Theory]
    [InlineData(typeof(Sales.Domain.Product), "ProductCode")]
    [InlineData(typeof(Sales.Domain.Category), "CategoryCode")]
    [InlineData(typeof(Sales.Domain.Customer), "CustomerCode")]
    public void Code_property_has_no_public_setter(Type aggregateType, string codePropertyName)
    {
        var codeProperty = aggregateType.GetProperty(codePropertyName);

        Assert.NotNull(codeProperty);
        Assert.False(codeProperty.SetMethod?.IsPublic ?? false);
    }
}
