using Microsoft.OpenApi.Models;
using SmartSupport.ExternalService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configurar Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartSupport External Service API",
        Version = "v1",
        Description = "API para servicios externos de SmartSupport (Tracking)"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartSupport External Service API v1");
        c.RoutePrefix = "swagger"; // Swagger UI en /swagger
    });
}

app.UseHttpsRedirection();

// Mapear controladores
app.MapControllers();

app.Run();
