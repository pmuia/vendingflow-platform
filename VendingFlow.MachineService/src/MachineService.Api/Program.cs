using MachineService.Application;
using MachineService.Domain;
using MachineService.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MachineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MachineDb") ??
                     "Host=localhost;Port=5432;Database=machine_db;Username=vendingflow;Password=vendingflow"));
builder.Services.AddScoped<IMachineRepository, MachineRepository>();
builder.Services.AddMediatR(typeof(RegisterMachineHandler).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

await MachineSeed.EnsureSeededAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");

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
