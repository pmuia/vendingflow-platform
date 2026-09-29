using InventoryService.Application;
using InventoryService.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("InventoryDb") ??
                     "Host=localhost;Port=5432;Database=inventory_db;Username=vendingflow;Password=vendingflow"));
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IEventBus, RabbitEventBus>();
builder.Services.AddHostedService<InventoryEventConsumer>();
builder.Services.AddMediatR(typeof(ReserveProductHandler).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

await InventorySeed.EnsureSeededAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");

var api = app.MapGroup("/api/inventory");
api.MapGet("/products", (IMediator mediator, CancellationToken ct) => mediator.Send(new GetProductsQuery(), ct));
api.MapGet("/machines/{machineId}", (string machineId, IMediator mediator, CancellationToken ct) => mediator.Send(new GetMachineInventoryQuery(machineId), ct));
api.MapGet("/transactions/recent", (IMediator mediator, CancellationToken ct) => mediator.Send(new GetRecentTransactionsQuery(), ct));
api.MapPost("/machines/{machineId}/stock", (string machineId, StockRequest request, IMediator mediator, CancellationToken ct) =>
    mediator.Send(new StockMachineCommand(machineId, request.ProductId, request.Quantity), ct));
api.MapPost("/machines/{machineId}/purchase", (string machineId, PurchaseRequest request, IMediator mediator, CancellationToken ct) =>
    mediator.Send(new ReserveProductCommand(machineId, request.ProductId), ct));

app.Run();

public record StockRequest(Guid ProductId, int Quantity);
public record PurchaseRequest(Guid ProductId);
