var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Tracking endpoint simulado para la demo
app.MapGet("/tracking/{trackingNumber}", (string trackingNumber, string? mode) =>
{
    var now = DateTime.UtcNow;

    if (string.Equals(trackingNumber, "1Z999SMART", StringComparison.OrdinalIgnoreCase))
    {
        if (string.Equals(mode, "delayed", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new TrackingResponse(
                status: "delayed",
                eta: now.AddDays(2),
                lastScan: new LastScan(now.AddHours(-1), "Centro de distribución", "Demora por logística")
            ));
        }
        else if (string.Equals(mode, "in_transit", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new TrackingResponse(
                status: "in_transit",
                eta: now.AddDays(1),
                lastScan: new LastScan(now.AddHours(-3), "Planta logística", "Clasificado")
            ));
        }
        else
        {
            var eta = DateTime.UtcNow.Date.AddHours(19);
            var lastScan = new LastScan(now.AddHours(-2), "Centro de distribución", "Salida a ruta de reparto");
            return Results.Ok(new TrackingResponse("out_for_delivery", eta, lastScan));
        }
    }

    // Fallback genérico
    return Results.Ok(new TrackingResponse
    (
        status: "in_transit",
        eta: now.AddDays(2),
        lastScan: new LastScan(now.AddHours(-6), "Planta logística", "Clasificado")
    ));
})
.WithName("GetTracking");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal record TrackingResponse(string status, DateTime? eta, LastScan? lastScan);

internal record LastScan(DateTime when, string location, string message);
