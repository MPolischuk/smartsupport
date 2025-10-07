using System.Text;
using System.Text.Json;
using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

public interface ILlmClient
{
    Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default);
}

public sealed class GeminiLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    public GeminiLlmClient(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default)
    {
        var model = _config["LLM:Model"] ?? "gemini-1.5-flash";
        var client = _httpClientFactory.CreateClient("Gemini");

        var system = "Eres un asistente de soporte de pedidos. Responde SOLO en JSON válido conforme AssistResponse (camelCase). Prioridad de evidencias: API > SQL > PDF.";
        var ctx = new StringBuilder();
        ctx.AppendLine("# PDF");
        ctx.AppendLine(pdfText);
        if (sqlFacts.Count > 0)
        {
            ctx.AppendLine("\n# SQL facts");
            foreach (var f in sqlFacts) ctx.AppendLine("- " + f);
        }
        if (apiFacts.Count > 0)
        {
            ctx.AppendLine("\n# API facts");
            foreach (var f in apiFacts) ctx.AppendLine("- " + f);
        }

        var contents = new[]
        {
            new {
                role = "user",
                parts = new object[]
                {
                    new { text = system },
                    new { text = $"Pregunta: {prompt}\n\nContexto:\n{ctx}" }
                }
            }
        };

        var reqUri = $"/v1/models/{model}:generateContent";
        using var req = new HttpRequestMessage(HttpMethod.Post, reqUri);
        var payload = new { contents, generationConfig = new { temperature = 0.2, responseMimeType = "application/json" } };
        req.Content = new StringContent(JsonSerializer.Serialize(payload));
        req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var httpResp = await client.SendAsync(req, ct);
        httpResp.EnsureSuccessStatusCode();
        var json = await httpResp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(text))
            return new AssistResponse { Answer = "Respuesta vacía del LLM", Confidence = 0.5, Citations = citations };

        try
        {
            var resp = JsonSerializer.Deserialize(text, AssistJsonContext.Default.AssistResponse);
            if (resp is null) return new AssistResponse { Answer = text, Confidence = 0.6, Citations = citations };
            return resp with { Citations = citations };
        }
        catch
        {
            return new AssistResponse { Answer = text, Confidence = 0.6, Citations = citations };
        }
    }
}

public sealed class MockLlmClient : ILlmClient
{
    public Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default)
    {
        var answer = $"[MOCK] {prompt} | PDF:{(pdfText.Length > 48 ? pdfText[..48] + "..." : pdfText)} | SQL:{sqlFacts.Count} | API:{apiFacts.Count}";
        return Task.FromResult(new AssistResponse
        {
            Answer = answer,
            Confidence = 0.8,
            Citations = citations,
            Intent = AssistIntent.OrderStatus,
            Status = AssistStatus.InTransit,
            Eta = DateTimeOffset.UtcNow.AddDays(1),
            Actions = new[] { "track:/tracking" },
            RawContextUsed = true
        });
    }
}


