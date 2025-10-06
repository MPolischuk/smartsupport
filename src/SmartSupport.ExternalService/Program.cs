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
app.MapGet("/tracking/{trackingNumber}", (string trackingNumber) =>
{
    var now = DateTime.UtcNow;

    if (string.Equals(trackingNumber, "1Z999SMART", StringComparison.OrdinalIgnoreCase))
    {
        var eta = DateTime.UtcNow.Date.AddHours(19); // hoy 19:00 UTC
        var lastScan = new LastScan
        (
            when: now.AddHours(-2),
            location: "Centro de distribución",
            message: "Salida a ruta de reparto"
        );

        return Results.Ok(new TrackingResponse
        (
            status: "out_for_delivery",
            eta: eta,
            lastScan: lastScan
        ));
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
