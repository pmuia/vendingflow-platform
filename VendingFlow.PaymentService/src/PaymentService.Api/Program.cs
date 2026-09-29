using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application;
using PaymentService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb") ??
                     "Host=localhost;Port=5432;Database=payment_db;Username=vendingflow;Password=vendingflow"));
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IEventBus, RabbitEventBus>();
builder.Services.AddHostedService<PaymentEventConsumer>();
builder.Services.AddMediatR(typeof(GetRecentPaymentsHandler).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

await PaymentSeed.EnsureCreatedAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/health");

app.MapGet("/api/payments/recent", (IMediator mediator, CancellationToken ct) => mediator.Send(new GetRecentPaymentsQuery(), ct));

app.Run();
