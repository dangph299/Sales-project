using Inventory.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

var app = builder.Build();
app.ConfigureApplication();

await app.RunStartupTasksAsync();
app.Run();

public partial class Program;
