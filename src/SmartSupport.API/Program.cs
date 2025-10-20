using SmartSupport.API.Services.Llm.Interfaces;
using SmartSupport.API.Services.Pdf.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// EF Core ExternalData
builder.Services.AddDbContext<SmartSupport.ExternalData.ExternalDataDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("ExternalData");
    options.UseSqlServer(cs);
});

// HttpClient for ExternalService
builder.Services.AddHttpClient("ExternalService", client =>
{
    var baseUrl = builder.Configuration["ExternalService:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});

// HttpClient for Gemini (Google AI Studio)
builder.Services.AddHttpClient("Gemini", client =>
{
    var baseUrl = builder.Configuration["LLM:BaseUrl"] ?? "https://generativelanguage.googleapis.com";
    client.BaseAddress = new Uri(baseUrl);
});

// App services
builder.Services.AddScoped<IPdfTextExtractor, SmartSupport.API.Services.NaivePdfTextExtractor>();
builder.Services.AddScoped<SmartSupport.API.Services.ISqlRagProvider, SmartSupport.API.Services.SqlRagProvider>();
builder.Services.AddScoped<SmartSupport.API.Services.IApiRagProvider, SmartSupport.API.Services.ApiRagProvider>();
var llmMock = builder.Configuration.GetValue<bool>("LLM:Mock");
if (llmMock)
{
    builder.Services.AddScoped<ILlmClient, SmartSupport.API.Services.MockLlmClient>();
}
else
{
    builder.Services.AddScoped<ILlmClient, SmartSupport.API.Services.GeminiLlmClient>();
}
builder.Services.AddScoped<SmartSupport.API.Services.AssistOrchestrator>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS para WebClient
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClient", policy =>
        policy.WithOrigins("https://localhost:7248", "http://localhost:5052")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowWebClient");

// Migrate DB on startup (demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartSupport.ExternalData.ExternalDataDbContext>();
    // Usar migraciones para evitar inconsistencias del modelo
    db.Database.Migrate();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Assist endpoint (multipart)
app.MapPost("/assist/query", async (
    HttpRequest request,
    SmartSupport.API.Services.AssistOrchestrator orchestrator) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("multipart/form-data requerido");

    var form = await request.ReadFormAsync();
    var prompt = form["prompt"].ToString();
    var orderNumber = form["orderNumber"].ToString();
    var useSql = bool.TryParse(form["useSqlRag"], out var us) && us;
    var useApi = bool.TryParse(form["useApiRag"], out var ua) && ua;
    var file = form.Files.GetFile("file");

    if (string.IsNullOrWhiteSpace(prompt) || file is null)
        return Results.BadRequest("Se requiere prompt y archivo PDF");

    await using var stream = file.OpenReadStream();
    var response = await orchestrator.HandleAsync(prompt, orderNumber, useSql, useApi, stream, file.FileName);
    return Results.Json(response);
})
.WithName("AssistQuery");

// Listar modelos disponibles en Gemini
app.MapGet("/assist/models", async (ILlmClient llm, CancellationToken ct) =>
{
    var json = await llm.ListModelsAsync(ct);
    return Results.Text(json, "application/json");
})
.WithName("AssistModels");

app.Run();

// demo default records removed
