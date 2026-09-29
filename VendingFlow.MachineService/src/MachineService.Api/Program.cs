using MachineService.Application;
using MachineService.Domain;
using MachineService.Infrastructure;
using MediatR;

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

await MachineSeed.EnsureSeededAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.MapHealthChecks("/health");
app.MapControllers();

var api = app.MapGroup("/api/machines");
api.MapGet("/", (IMediator mediator, CancellationToken ct) => mediator.Send(new GetMachinesQuery(), ct));
api.MapGet("/{machineCode}", (string machineCode, IMediator mediator, CancellationToken ct) => mediator.Send(new GetMachineQuery(machineCode), ct));
api.MapPost("/", (RegisterMachineCommand command, IMediator mediator, CancellationToken ct) => mediator.Send(command, ct));
api.MapPut("/{id:guid}", (Guid id, UpdateMachineRequest request, IMediator mediator, CancellationToken ct) =>
    mediator.Send(new UpdateMachineCommand(id, request.Name, request.Location, request.IsActive), ct));
api.MapPut("/{machineCode}/status", (string machineCode, UpdateMachineStatusRequest request, IMediator mediator, CancellationToken ct) =>
    mediator.Send(new UpdateMachineStatusCommand(machineCode, request.Status, request.LastHeartbeatAt), ct));

app.Run();

public record UpdateMachineRequest(string Name, string Location, bool IsActive);
public record UpdateMachineStatusRequest(MachineStatus Status, DateTimeOffset? LastHeartbeatAt);
