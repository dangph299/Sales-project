namespace Sales.Application;

/// <summary>
/// Adds Sales-specific expected application failures to the shared exception classifier.
/// </summary>
public sealed class SalesApplicationExceptionClassifier : IApplicationExceptionClassifier
{
    private readonly DefaultApplicationExceptionClassifier _defaultClassifier = new();

    /// <inheritdoc/>
    public bool IsExpected(Exception exception) =>
        _defaultClassifier.IsExpected(exception) ||
        exception is NotFoundException or ConflictException;
}
