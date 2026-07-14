namespace BuildingBlocks.Web.Models;

/// <summary>
/// Describes a validation error for a specific input field.
/// Use it for errors produced by model binding, model state, or validation libraries.
/// </summary>
/// <param name="Field">Input field that failed validation.</param>
/// <param name="Message">Client-safe validation message.</param>
/// <param name="Code">Optional machine-readable validation code.</param>
public sealed record ValidationError(string Field, string Message, string? Code = null);
