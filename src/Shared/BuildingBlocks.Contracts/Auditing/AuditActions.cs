namespace BuildingBlocks.Contracts;

/// <summary>
/// Standard audit action names shared by all services.
/// </summary>
public static class AuditActions
{
    /// <summary>Entity was created.</summary>
    public const string Created = "Created";

    /// <summary>Entity was updated.</summary>
    public const string Updated = "Updated";

    /// <summary>Entity was deleted.</summary>
    public const string Deleted = "Deleted";
}
