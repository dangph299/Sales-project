using System.Text.Json;
using AuditLog.Infrastructure;
using MongoDB.Driver;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: AuditProbe <productId> <updatedName>");
    Environment.ExitCode = 2;
    return;
}

var productId = args[0];
var updatedName = args[1];
var connectionString = Environment.GetEnvironmentVariable("AUDIT_MONGO_URL") ?? "mongodb://localhost:27017";
var databaseName = Environment.GetEnvironmentVariable("AUDIT_MONGO_DATABASE") ?? "audit";

var client = new MongoClient(connectionString);
var database = client.GetDatabase(databaseName);
var collection = database.GetCollection<AuditDocument>("events");

var filter = Builders<AuditDocument>.Filter.And(
    Builders<AuditDocument>.Filter.Eq(document => document.EntityType, "Product"),
    Builders<AuditDocument>.Filter.Eq(document => document.EntityId, productId),
    Builders<AuditDocument>.Filter.Eq(document => document.Action, "Updated"));

var auditDocuments = await collection.Find(filter).SortByDescending(document => document.OccurredAt).Limit(20).ToListAsync();
var auditDocument = auditDocuments.FirstOrDefault(document =>
    document.Changes.Any(change => change.PropertyPath == "Name" && ValueText(change.NewValue) == updatedName));
if (auditDocument is null)
{
    Console.WriteLine("null");
    return;
}

var output = new
{
    auditDocument.ServiceName,
    auditDocument.EventType,
    auditDocument.EntityType,
    auditDocument.EntityId,
    auditDocument.Action,
    auditDocument.SchemaVersion,
    Changes = auditDocument.Changes.Select(change => new
    {
        change.PropertyPath,
        change.OldValue,
        change.NewValue
    })
};

Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

static string? ValueText(object? value)
{
    if (value is null)
    {
        return null;
    }

    if (value is JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            _ => jsonElement.ToString()
        };
    }

    return value.ToString();
}
