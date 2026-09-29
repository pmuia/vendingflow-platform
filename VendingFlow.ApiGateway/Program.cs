var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/health"));

var routes = new Dictionary<string, string>
{
    ["/api/machines"] = builder.Configuration["Routes:Machines"] ?? "http://localhost:5001",
    ["/api/inventory"] = builder.Configuration["Routes:Inventory"] ?? "http://localhost:5002",
    ["/api/payments"] = builder.Configuration["Routes:Payments"] ?? "http://localhost:5003",
    ["/api/telemetry"] = builder.Configuration["Routes:Telemetry"] ?? "http://localhost:8084"
};

app.Map("/{**path}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var path = context.Request.Path.Value ?? "/";
    var match = routes.OrderByDescending(r => r.Key.Length).FirstOrDefault(r => path.StartsWith(r.Key, StringComparison.OrdinalIgnoreCase));
    if (string.IsNullOrEmpty(match.Key)) return Results.NotFound(new { message = "No gateway route matched." });

    var target = new Uri(match.Value + path + context.Request.QueryString);
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
    if (context.Request.ContentLength > 0)
        request.Content = new StreamContent(context.Request.Body);
    foreach (var header in context.Request.Headers)
        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

    var response = await httpClientFactory.CreateClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    foreach (var header in response.Content.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    return Results.Empty;
});

app.Run();
