using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

public interface ILlmClient
{
    Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default);
    Task<string> ListModelsAsync(CancellationToken ct = default);
}

public sealed class GeminiLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiLlmClient> _logger;
    public GeminiLlmClient(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GeminiLlmClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default)
    {
        var model = _config["LLM:Model"] ?? "gemini-2.5-flash";
        var client = _httpClientFactory.CreateClient("Gemini");
        var apiVersion = _config["LLM:ApiVersion"] ?? "v1"; // permitir alternar a v1beta si es necesario

        var system = "Eres un asistente de soporte de pedidos. Responde SOLO en JSON válido conforme AssistResponse (camelCase). Prioridad de evidencias: API > SQL > PDF.";

        // Limitar tamaño del contexto para evitar 400 por payload demasiado grande
        static string Truncate(string value, int max)
            => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value.Substring(0, max));

        const int maxPdfChars = 100_000; // ~100KB de texto
        const int maxFacts = 50; // limitar cantidad de facts

        var limitedPdf = Truncate(pdfText, maxPdfChars);
        var limitedSqlFacts = sqlFacts.Take(maxFacts);
        var limitedApiFacts = apiFacts.Take(maxFacts);

        var ctx = new StringBuilder();
        ctx.AppendLine("# PDF");
        ctx.AppendLine(limitedPdf);
        if (limitedSqlFacts.Any())
        {
            ctx.AppendLine("\n# SQL facts");
            foreach (var f in limitedSqlFacts) ctx.AppendLine("- " + f);
        }
        if (limitedApiFacts.Any())
        {
            ctx.AppendLine("\n# API facts");
            foreach (var f in limitedApiFacts) ctx.AppendLine("- " + f);
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

        var apiKey = _config["LLM:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
        var keyQuery = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : ("?key=" + Uri.EscapeDataString(apiKey));
        // Simplificar generationConfig para mayor compatibilidad (algunas versiones usan response_mime_type)
        var payload = new { contents, generationConfig = new { temperature = 0.2 } };
        var json = JsonSerializer.Serialize(payload);

        static string NormalizeModelPath(string modelName)
            => modelName.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? modelName : $"models/{modelName}";

        async Task<(bool Ok, string Body, System.Net.HttpStatusCode StatusCode, string? ErrorMessage)> TryCallAsync(string version, string modelName)
        {
            var modelPath = NormalizeModelPath(modelName);
            var reqUriLocal = $"/{version}/{modelPath}:generateContent{keyQuery}";
            using var reqLocal = new HttpRequestMessage(HttpMethod.Post, reqUriLocal)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _logger.LogInformation("[Gemini] POST {Uri} model={Model} payloadChars={Len}", reqUriLocal, modelName, json.Length);

            var resp = await client.SendAsync(reqLocal, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
            {
                return (true, body, resp.StatusCode, null);
            }

            string? errMsg = null;
            try
            {
                using var errDoc = JsonDocument.Parse(body);
                if (errDoc.RootElement.TryGetProperty("error", out var err))
                {
                    errMsg = err.TryGetProperty("message", out var m) ? m.GetString() : err.ToString();
                }
            }
            catch
            {
                // body no es JSON
            }

            _logger.LogWarning("[Gemini] {Status} for {Uri}. msg={Msg}", resp.StatusCode, reqUriLocal, errMsg ?? body);
            return (false, body, resp.StatusCode, errMsg);
        }

        var attempt = await TryCallAsync(apiVersion, model);
        if (!attempt.Ok)
        {
            var shouldRetryBeta = apiVersion != "v1beta" && (
                attempt.StatusCode == System.Net.HttpStatusCode.NotFound ||
                (attempt.ErrorMessage?.Contains("not found for API version", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (attempt.ErrorMessage?.Contains("not supported for generateContent", StringComparison.OrdinalIgnoreCase) ?? false)
            );

            if (shouldRetryBeta)
            {
                _logger.LogInformation("[Gemini] Reintentando con v1beta por {Reason}", attempt.ErrorMessage);
                attempt = await TryCallAsync("v1beta", model);
            }

            if (!attempt.Ok)
            {
                var modelFallbacks = new[] { "gemini-2.5-flash", "gemini-flash-latest", "gemini-2.0-flash" };
                foreach (var candidate in modelFallbacks)
                {
                    _logger.LogInformation("[Gemini] Probando modelo alternativo: {Model}", candidate);
                    var altAttempt = await TryCallAsync("v1beta", candidate);
                    if (altAttempt.Ok)
                    {
                        attempt = altAttempt;
                        break;
                    }
                }

                if (!attempt.Ok)
                {
                    var friendly = $"Gemini devolvió {(int)attempt.StatusCode} {attempt.StatusCode}. Detalle: {attempt.ErrorMessage ?? attempt.Body}";
                    return new AssistResponse { Answer = friendly, Confidence = 0.2, Citations = citations };
                }
            }
        }

        var jsonResp = attempt.Body;

        using var doc = JsonDocument.Parse(jsonResp);
        var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(text))
            return new AssistResponse { Answer = "Respuesta vacía del LLM", Confidence = 0.5, Citations = citations };

        try
        {
            // El modelo puede envolver la respuesta en fences ```json ... ```; limpiarlos
            static string StripCodeFences(string s)
            {
                var trimmed = s.Trim();
                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    // quitar primera línea ```(lang?) y última ```
                    var firstNewLine = trimmed.IndexOf('\n');
                    if (firstNewLine > 0)
                    {
                        trimmed = trimmed.Substring(firstNewLine + 1);
                    }
                    if (trimmed.EndsWith("```", StringComparison.Ordinal))
                    {
                        trimmed = trimmed.Substring(0, trimmed.Length - 3);
                    }
                    return trimmed.Trim();
                }
                return trimmed;
            }

            var cleaned = StripCodeFences(text);
            var resp = JsonSerializer.Deserialize(cleaned, AssistJsonContext.Default.AssistResponse);
            if (resp is null) return new AssistResponse { Answer = text, Confidence = 0.6, Citations = citations };
            return resp with { Citations = citations };
        }
        catch
        {
            // Fallback: si el texto es JSON con campo "answer", extraerlo
            try
            {
                var cleaned = text.Trim();
                if (cleaned.StartsWith("`"))
                {
                    // eliminar backticks sueltos
                    cleaned = cleaned.Trim('`').Trim();
                }
                using var inner = JsonDocument.Parse(cleaned);
                if (inner.RootElement.ValueKind == JsonValueKind.Object && inner.RootElement.TryGetProperty("answer", out var ansProp))
                {
                    var answerVal = ansProp.GetString() ?? text;
                    return new AssistResponse { Answer = answerVal, Confidence = 0.6, Citations = citations };
                }
            }
            catch
            {
                // ignorar y devolver texto bruto
            }
            return new AssistResponse { Answer = text, Confidence = 0.6, Citations = citations };
        }
    }

    public async Task<string> ListModelsAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        var apiVersion = _config["LLM:ApiVersion"] ?? "v1";
        var apiKey = _config["LLM:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
        var keyQuery = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : ("?key=" + Uri.EscapeDataString(apiKey));

        var uri = $"/{apiVersion}/models{keyQuery}";
        _logger.LogInformation("[Gemini] GET {Uri} ListModels", uri);
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("[Gemini] ListModels error {Status} body={Body}", resp.StatusCode, body);
        }
        return body;
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

    public Task<string> ListModelsAsync(CancellationToken ct = default) => Task.FromResult("{\"models\":[]}");
}


