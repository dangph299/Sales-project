using FluentValidation;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Orders.Validators;

namespace Sales.Application.Tests;

/// <summary>
/// Pins the order customer snapshot field rules that keep an out-of-range value from reaching the
/// database as a 500 instead of surfacing as a 400.
/// </summary>
public sealed class OrderCustomerValidatorTests
{
    private readonly CreateOrderCustomerValidator _validator = new();

    [Fact]
    public void Phone_longer_than_the_column_is_rejected()
    {
        // Passes the 9-to-15-digit rule (ten digits) yet is far longer than the varchar(32) column,
        // so without the length rule this stored fine in validation and blew up in SaveChangesAsync.
        var result = _validator.Validate(
            new CreateOrderCustomer("0901234567 - please call after five pm", "Nguyen Van A"));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(CreateOrderCustomer.Phone) && error.ErrorMessage.Contains("32"));
    }

    [Fact]
    public void Name_longer_than_the_column_is_rejected()
    {
        var result = _validator.Validate(
            new CreateOrderCustomer("0901234567", new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateOrderCustomer.Name));
    }

    [Fact]
    public void Formatted_phone_within_the_column_with_valid_digits_is_accepted()
    {
        var result = _validator.Validate(new CreateOrderCustomer("090-123-4567", "Nguyen Van A"));

        Assert.True(result.IsValid);
    }
}
