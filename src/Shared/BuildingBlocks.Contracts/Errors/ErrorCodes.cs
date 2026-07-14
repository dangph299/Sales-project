namespace BuildingBlocks.Contracts;

/// <summary>
/// Single solution-wide list of public error codes.
/// </summary>
public static class ErrorCodes
{
    // Validation
    public const string Validation = "validation";
    public const string InvalidRequest = "invalid_request";
    public const string InvalidOperation = "invalid_operation";
    public const string InvalidInput = "invalid_input";
    public const string MissingRequiredField = "missing_required_field";
    public const string UnsupportedOperation = "unsupported_operation";

    // Authentication / Authorization
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string AuthenticationFailed = "authentication_failed";
    public const string TokenExpired = "token_expired";
    public const string InvalidToken = "invalid_token";
    public const string PermissionDenied = "permission_denied";

    // Resource
    public const string NotFound = "not_found";
    public const string AlreadyExists = "already_exists";
    public const string Conflict = "conflict";
    public const string Duplicate = "duplicate";
    public const string DuplicateRequest = "duplicate_request";
    public const string DuplicateMessage = "duplicate_message";

    // Concurrency
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string ResourceLocked = "resource_locked";
    public const string ResourceDeleted = "resource_deleted";
    public const string VersionMismatch = "version_mismatch";

    // Business
    public const string BusinessRuleViolation = "business_rule_violation";
    public const string InvalidState = "invalid_state";
    public const string StateTransitionNotAllowed = "state_transition_not_allowed";

    // Inventory
    public const string InsufficientStock = "insufficient_stock";
    public const string ReservationNotFound = "reservation_not_found";
    public const string ReservationExpired = "reservation_expired";
    public const string StaleReservation = "stale_reservation";
    public const string ProductOutOfStock = "product_out_of_stock";

    // Order / Sales
    public const string OrderNotFound = "order_not_found";
    public const string CustomerNotFound = "customer_not_found";
    public const string ProductNotFound = "product_not_found";
    public const string InvalidOrderState = "invalid_order_state";
    public const string PaymentRequired = "payment_required";

    // Database
    public const string DatabaseError = "database_error";
    public const string UniqueViolation = "unique_violation";
    public const string ForeignKeyViolation = "foreign_key_violation";
    public const string SerializationFailure = "serialization_failure";
    public const string TransactionFailed = "transaction_failed";

    // External Services
    public const string ExternalServiceError = "external_service_error";
    public const string ExternalServiceUnavailable = "external_service_unavailable";
    public const string ExternalRequestFailed = "external_request_failed";
    public const string ExternalTimeout = "external_timeout";

    // Messaging
    public const string MessagePublishFailed = "message_publish_failed";
    public const string MessageConsumeFailed = "message_consume_failed";
    public const string MessageProcessingFailed = "message_processing_failed";
    public const string InvalidMessage = "invalid_message";

    // Cache
    public const string CacheError = "cache_error";
    public const string CacheUnavailable = "cache_unavailable";

    // Configuration
    public const string ConfigurationError = "configuration_error";
    public const string FeatureDisabled = "feature_disabled";

    // Network / Availability
    public const string Timeout = "timeout";
    public const string OperationCancelled = "operation_cancelled";
    public const string ServiceUnavailable = "service_unavailable";

    // Internal
    public const string InternalServerError = "internal_server_error";
    public const string UnexpectedError = "unexpected_error";
}
