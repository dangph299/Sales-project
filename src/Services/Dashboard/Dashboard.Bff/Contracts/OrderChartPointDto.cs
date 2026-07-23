namespace Dashboard.Bff.Contracts;

/// <summary>
/// A single data point used to render the order chart.
/// </summary>
/// <param name="CreatedAt">Timestamp the order was created.</param>
/// <param name="Total">Total order amount.</param>
/// <param name="Status">Order status.</param>
public sealed record OrderChartPointDto(
    DateTimeOffset CreatedAt,
    decimal Total,
    string Status);
