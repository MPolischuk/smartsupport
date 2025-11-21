using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using SmartSupport.WebClient.Models;

namespace SmartSupport.WebClient.Services;

public class AssistService : IAssistService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "https://localhost:7086";

    public AssistService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AssistResponseDto?> QueryAsync(
        string prompt,
        IBrowserFile file,
        string? orderNumber = null,
        bool useSql = false,
        bool useApi = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("El prompt es requerido", nameof(prompt));

        if (file is null)
            throw new ArgumentNullException(nameof(file));

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(prompt), "prompt");
        if (!string.IsNullOrWhiteSpace(orderNumber))
            content.Add(new StringContent(orderNumber), "orderNumber");
        content.Add(new StringContent(useSql.ToString()), "useSqlRag");
        content.Add(new StringContent(useApi.ToString()), "useApiRag");

        using var fs = file.OpenReadStream(10 * 1024 * 1024, cancellationToken);
        var streamContent = new StreamContent(fs);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(streamContent, "file", file.Name);

        var response = await _httpClient.PostAsync($"{ApiBaseUrl}/assist/query", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        try
        {
            var dto = JsonSerializer.Deserialize<AssistResponseDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Respuesta no JSON del servidor: {body}");
        }
    }

    public IReadOnlyList<PromptTemplate> GetPromptTemplates()
    {
        return new List<PromptTemplate>
        {
            // Estado de pedido y tracking
            new() { Value = "¿Cuándo llega mi pedido AT-1003? ¿Cuál es el estado actual?", Label = "📦 Estado de pedido AT-1003", OrderNumber = "AT-1003", UseSql = true, UseApi = true },
            new() { Value = "¿Dónde está mi pedido AT-1003? ¿Puedo ver el tracking?", Label = "📍 Tracking de pedido AT-1003", OrderNumber = "AT-1003", UseSql = true, UseApi = true },
            
            // Políticas de envío
            new() { Value = "¿Cuánto tiempo tarda un envío estándar?", Label = "⏱️ Tiempo de envío estándar", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Cuánto tarda un envío express? ¿Cuáles son las condiciones?", Label = "⚡ Envío Express - tiempos y condiciones", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Cuándo llega mi pedido? ¿Cuentan fines de semana?", Label = "📅 Plazos de entrega - días hábiles", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Qué pasa con el cut-off diario? ¿A qué hora se procesan los pedidos?", Label = "🕐 Cut-off diario y procesamiento", OrderNumber = null, UseSql = false, UseApi = false },
            
            // Productos dañados
            new() { Value = "¿Qué hago si mi pedido AT-1003 llega dañado?", Label = "💔 Producto dañado - Pedido AT-1003", OrderNumber = "AT-1003", UseSql = false, UseApi = false },
            new() { Value = "¿Cuántos días tengo para reportar un producto dañado?", Label = "⏰ Plazo para reportar producto dañado", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Qué evidencia necesito si mi pedido llega defectuoso?", Label = "📸 Evidencia para producto defectuoso", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Cuál es el proceso si mi pedido llega dañado? ¿Hay reemplazo o reembolso?", Label = "🔄 Proceso de producto dañado - reemplazo/reembolso", OrderNumber = null, UseSql = false, UseApi = false },
            
            // Devoluciones
            new() { Value = "¿Cuántos días tengo para devolver mi pedido AT-1003?", Label = "↩️ Plazo de devolución - Pedido AT-1003", OrderNumber = "AT-1003", UseSql = false, UseApi = false },
            new() { Value = "¿Cuál es el proceso de devolución? ¿Cómo solicito la etiqueta?", Label = "📋 Proceso de devolución paso a paso", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Cuánto tarda el reembolso después de devolver un producto?", Label = "💰 Tiempo de reembolso después de devolución", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Puedo cambiar mi pedido por otro modelo o color?", Label = "🔄 Cambio por modelo o color", OrderNumber = null, UseSql = false, UseApi = false },
            
            // Cambio de dirección
            new() { Value = "¿Puedo cambiar la dirección de entrega de mi pedido AT-1003?", Label = "🏠 Cambio de dirección - Pedido AT-1003", OrderNumber = "AT-1003", UseSql = true, UseApi = false },
            new() { Value = "¿Qué pasa si quiero cambiar la dirección cuando el pedido ya está en tránsito?", Label = "📍 Cambio de dirección en tránsito", OrderNumber = null, UseSql = false, UseApi = false },
            
            // Reprogramación y entrega
            new() { Value = "¿Puedo reprogramar la entrega de mi pedido?", Label = "📅 Reprogramación de entrega", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Qué pasa si no estoy en mi domicilio cuando intentan entregar?", Label = "🏡 Ausente en domicilio - intentos de entrega", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Cuántos intentos de entrega se realizan?", Label = "🔔 Intentos de entrega", OrderNumber = null, UseSql = false, UseApi = false },
            
            // Pérdida y extravío
            new() { Value = "¿Qué pasa si mi pedido se pierde o se extravía?", Label = "❓ Pedido perdido o extraviado", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Me ofrecen reenvío o reembolso si mi pedido se pierde?", Label = "📦 Reenvío o reembolso por pérdida", OrderNumber = null, UseSql = false, UseApi = false },
            
            // SLA y soporte
            new() { Value = "¿Cuánto tiempo tarda en responder el soporte?", Label = "⏱️ SLA de primera respuesta de soporte", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Qué canales de soporte están disponibles?", Label = "💬 Canales de soporte disponibles", OrderNumber = null, UseSql = false, UseApi = false },
            new() { Value = "¿Cuál es el horario de atención de soporte?", Label = "🕐 Horario de atención de soporte", OrderNumber = null, UseSql = false, UseApi = false },
            
            // Consultas combinadas
            new() { Value = "¿Cuándo llega mi pedido AT-1003 y qué políticas de devolución aplican?", Label = "📦 Estado de pedido + políticas de devolución", OrderNumber = "AT-1003", UseSql = true, UseApi = false },
            new() { Value = "Mi pedido AT-1003 con tracking 1Z999SMART está en tránsito. ¿Puedo cambiar la dirección?", Label = "📍 Tracking en tránsito + cambio de dirección", OrderNumber = "AT-1003", UseSql = true, UseApi = true },
        };
    }
}
