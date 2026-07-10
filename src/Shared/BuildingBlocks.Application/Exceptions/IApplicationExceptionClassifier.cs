namespace BuildingBlocks.Application;

/// <summary>
/// Classifies application exceptions that are expected control-flow outcomes rather than system failures.
/// </summary>
public interface IApplicationExceptionClassifier
{
    /// <summary>
    /// Returns <see langword="true"/> when the exception should be logged as an expected rejection.
    /// </summary>
    /// <param name="exception">
    /// The exception to classify.
    /// </param>
    bool IsExpected(Exception exception);
}
