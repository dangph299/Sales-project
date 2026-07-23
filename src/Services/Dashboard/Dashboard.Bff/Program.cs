using Dashboard.Bff.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddDashboardBff();

var app = builder.Build();
app.ConfigureDashboardBff();

await app.RunDashboardStartupTasksAsync();
app.Run();

public partial class Program;
