using InventoryService.Application;
using InventoryService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(configuration);
builder.Services.AddApplication();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

await InventorySeed.EnsureSeededAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
