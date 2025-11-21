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
builder.Services.AddControllers();

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

// Mapear controladores
app.MapControllers();

// Migrate DB on startup (demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartSupport.ExternalData.ExternalDataDbContext>();
    // Usar migraciones para evitar inconsistencias del modelo
    db.Database.Migrate();
}

app.Run();

// demo default records removed
