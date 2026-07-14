namespace BuildingBlocks.Contracts;

/// <summary>
/// Resolves public error definitions by code.
/// </summary>
public interface IErrorCatalog
{
    /// <summary>
    /// Gets an error definition for <paramref name="code"/>.
    /// </summary>
    /// <param name="code">Stable error code.</param>
    /// <returns>Resolved error definition.</returns>
    ErrorDefinition Get(string code);
}

/// <summary>
/// Solution-wide default error catalog. Error codes are declared only in <see cref="ErrorCodes"/>.
/// </summary>
public static class ErrorCatalog
{
    public static readonly ErrorDefinition Validation =
        new(ErrorCodes.Validation, "One or more validation errors occurred.");

    public static readonly ErrorDefinition InvalidRequest =
        new(ErrorCodes.InvalidRequest, "The request is invalid.");

    public static readonly ErrorDefinition InvalidOperation =
        new(ErrorCodes.InvalidOperation, "The operation is invalid for the current state.");

    public static readonly ErrorDefinition InvalidInput =
        new(ErrorCodes.InvalidInput, "The input is invalid.");

    public static readonly ErrorDefinition MissingRequiredField =
        new(ErrorCodes.MissingRequiredField, "A required field is missing.");

    public static readonly ErrorDefinition UnsupportedOperation =
        new(ErrorCodes.UnsupportedOperation, "The operation is not supported.");

    public static readonly ErrorDefinition NotFound =
        new(ErrorCodes.NotFound, "The requested resource was not found.");

    public static readonly ErrorDefinition AlreadyExists =
        new(ErrorCodes.AlreadyExists, "The resource already exists.");

    public static readonly ErrorDefinition Conflict =
        new(ErrorCodes.Conflict, "The request conflicts with the current resource state.");

    public static readonly ErrorDefinition Duplicate =
        new(ErrorCodes.Duplicate, "The request contains duplicate data.");

    public static readonly ErrorDefinition DuplicateRequest =
        new(ErrorCodes.DuplicateRequest, "The request has already been processed.");

    public static readonly ErrorDefinition DuplicateMessage =
        new(ErrorCodes.DuplicateMessage, "The message has already been processed.");

    public static readonly ErrorDefinition ConcurrencyConflict =
        new(ErrorCodes.ConcurrencyConflict, "The resource was modified by another request.");

    public static readonly ErrorDefinition Unauthorized =
        new(ErrorCodes.Unauthorized, "Authentication is required to access this resource.");

    public static readonly ErrorDefinition Forbidden =
        new(ErrorCodes.Forbidden, "You do not have permission to access this resource.");

    public static readonly ErrorDefinition AuthenticationFailed =
        new(ErrorCodes.AuthenticationFailed, "Authentication failed.");

    public static readonly ErrorDefinition TokenExpired =
        new(ErrorCodes.TokenExpired, "The authentication token has expired.");

    public static readonly ErrorDefinition InvalidToken =
        new(ErrorCodes.InvalidToken, "The authentication token is invalid.");

    public static readonly ErrorDefinition PermissionDenied =
        new(ErrorCodes.PermissionDenied, "Permission was denied.");

    public static readonly ErrorDefinition ResourceLocked =
        new(ErrorCodes.ResourceLocked, "The resource is currently locked.");

    public static readonly ErrorDefinition ResourceDeleted =
        new(ErrorCodes.ResourceDeleted, "The resource has been deleted.");

    public static readonly ErrorDefinition VersionMismatch =
        new(ErrorCodes.VersionMismatch, "The provided version does not match the current resource version.");

    public static readonly ErrorDefinition BusinessRuleViolation =
        new(ErrorCodes.BusinessRuleViolation, "A business rule was violated.");

    public static readonly ErrorDefinition InvalidState =
        new(ErrorCodes.InvalidState, "The resource is in an invalid state for this operation.");

    public static readonly ErrorDefinition StateTransitionNotAllowed =
        new(ErrorCodes.StateTransitionNotAllowed, "The requested state transition is not allowed.");

    public static readonly ErrorDefinition InsufficientStock =
        new(ErrorCodes.InsufficientStock, "There is not enough stock to complete the request.");

    public static readonly ErrorDefinition ReservationNotFound =
        new(ErrorCodes.ReservationNotFound, "The requested reservation was not found.");

    public static readonly ErrorDefinition ReservationExpired =
        new(ErrorCodes.ReservationExpired, "The reservation has expired.");

    public static readonly ErrorDefinition StaleReservation =
        new(ErrorCodes.StaleReservation, "The reservation event is older than the current state.");

    public static readonly ErrorDefinition ProductOutOfStock =
        new(ErrorCodes.ProductOutOfStock, "The product is out of stock.");

    public static readonly ErrorDefinition OrderNotFound =
        new(ErrorCodes.OrderNotFound, "The requested order was not found.");

    public static readonly ErrorDefinition CustomerNotFound =
        new(ErrorCodes.CustomerNotFound, "The requested customer was not found.");

    public static readonly ErrorDefinition ProductNotFound =
        new(ErrorCodes.ProductNotFound, "The requested product was not found.");

    public static readonly ErrorDefinition InvalidOrderState =
        new(ErrorCodes.InvalidOrderState, "The order is not in a valid state for this operation.");

    public static readonly ErrorDefinition PaymentRequired =
        new(ErrorCodes.PaymentRequired, "Payment is required to complete this operation.");

    public static readonly ErrorDefinition DatabaseError =
        new(ErrorCodes.DatabaseError, "A database error occurred.");

    public static readonly ErrorDefinition UniqueViolation =
        new(ErrorCodes.UniqueViolation, "A resource with the same unique value already exists.");

    public static readonly ErrorDefinition ForeignKeyViolation =
        new(ErrorCodes.ForeignKeyViolation, "The request references a related resource that does not exist.");

    public static readonly ErrorDefinition SerializationFailure =
        new(ErrorCodes.SerializationFailure, "The transaction could not be serialized.");

    public static readonly ErrorDefinition TransactionFailed =
        new(ErrorCodes.TransactionFailed, "The transaction failed.");

    public static readonly ErrorDefinition ExternalServiceError =
        new(ErrorCodes.ExternalServiceError, "An external service returned an error.");

    public static readonly ErrorDefinition ExternalServiceUnavailable =
        new(ErrorCodes.ExternalServiceUnavailable, "An external service is unavailable.");

    public static readonly ErrorDefinition ExternalRequestFailed =
        new(ErrorCodes.ExternalRequestFailed, "The external request failed.");

    public static readonly ErrorDefinition ExternalTimeout =
        new(ErrorCodes.ExternalTimeout, "The external request timed out.");

    public static readonly ErrorDefinition MessagePublishFailed =
        new(ErrorCodes.MessagePublishFailed, "The message could not be published.");

    public static readonly ErrorDefinition MessageConsumeFailed =
        new(ErrorCodes.MessageConsumeFailed, "The message could not be consumed.");

    public static readonly ErrorDefinition MessageProcessingFailed =
        new(ErrorCodes.MessageProcessingFailed, "The message could not be processed.");

    public static readonly ErrorDefinition InvalidMessage =
        new(ErrorCodes.InvalidMessage, "The message is invalid.");

    public static readonly ErrorDefinition CacheError =
        new(ErrorCodes.CacheError, "A cache error occurred.");

    public static readonly ErrorDefinition CacheUnavailable =
        new(ErrorCodes.CacheUnavailable, "The cache is unavailable.");

    public static readonly ErrorDefinition ConfigurationError =
        new(ErrorCodes.ConfigurationError, "A configuration error occurred.");

    public static readonly ErrorDefinition FeatureDisabled =
        new(ErrorCodes.FeatureDisabled, "The feature is disabled.");

    public static readonly ErrorDefinition Timeout =
        new(ErrorCodes.Timeout, "The operation timed out.");

    public static readonly ErrorDefinition OperationCancelled =
        new(ErrorCodes.OperationCancelled, "The operation was cancelled.");

    public static readonly ErrorDefinition ServiceUnavailable =
        new(ErrorCodes.ServiceUnavailable, "The service is temporarily unavailable.");

    public static readonly ErrorDefinition InternalServerError =
        new(ErrorCodes.InternalServerError, "An unexpected server error occurred.");

    public static readonly ErrorDefinition UnexpectedError =
        new(ErrorCodes.UnexpectedError, "An unexpected error occurred.");

    private static readonly IReadOnlyDictionary<string, ErrorDefinition> Definitions =
        new[]
        {
            Validation,
            InvalidRequest,
            InvalidOperation,
            InvalidInput,
            MissingRequiredField,
            UnsupportedOperation,
            NotFound,
            AlreadyExists,
            Conflict,
            Duplicate,
            DuplicateRequest,
            DuplicateMessage,
            Unauthorized,
            Forbidden,
            AuthenticationFailed,
            TokenExpired,
            InvalidToken,
            PermissionDenied,
            ConcurrencyConflict,
            ResourceLocked,
            ResourceDeleted,
            VersionMismatch,
            BusinessRuleViolation,
            InvalidState,
            StateTransitionNotAllowed,
            InsufficientStock,
            ReservationNotFound,
            ReservationExpired,
            StaleReservation,
            ProductOutOfStock,
            OrderNotFound,
            CustomerNotFound,
            ProductNotFound,
            InvalidOrderState,
            PaymentRequired,
            DatabaseError,
            UniqueViolation,
            ForeignKeyViolation,
            SerializationFailure,
            TransactionFailed,
            ExternalServiceError,
            ExternalServiceUnavailable,
            ExternalRequestFailed,
            ExternalTimeout,
            MessagePublishFailed,
            MessageConsumeFailed,
            MessageProcessingFailed,
            InvalidMessage,
            CacheError,
            CacheUnavailable,
            ConfigurationError,
            FeatureDisabled,
            Timeout,
            OperationCancelled,
            ServiceUnavailable,
            InternalServerError,
            UnexpectedError
        }.ToDictionary(x => x.Code, StringComparer.Ordinal);

    /// <summary>
    /// Gets a default error definition for a code.
    /// </summary>
    /// <param name="code">Stable error code.</param>
    /// <returns>Default definition, or internal server error when the code is unknown.</returns>
    public static ErrorDefinition Get(string code)
    {
        return Definitions.TryGetValue(code, out var definition) ? definition : InternalServerError;
    }

    /// <summary>
    /// Enumerates all default error definitions.
    /// </summary>
    /// <returns>All known definitions.</returns>
    public static IReadOnlyCollection<ErrorDefinition> All => Definitions.Values.ToArray();
}

/// <summary>
/// Resolves default catalog entries and applies service-specific description overrides.
/// </summary>
/// <param name="messageProvider">Description provider.</param>
public sealed class ErrorCatalogResolver(IErrorMessageProvider messageProvider) : IErrorCatalog
{
    /// <inheritdoc />
    public ErrorDefinition Get(string code)
    {
        var definition = ErrorCatalog.Get(code);
        var description = messageProvider.GetDescription(definition.Code, definition.Description);
        return definition with { Description = description };
    }
}
