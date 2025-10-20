using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartSupport.API.Models;
using SmartSupport.API.Services.Llm.Interfaces;

namespace SmartSupport.API.Services;

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

        var system = "Eres un asistente de soporte de pedidos. Responde SOLO en TEXTO (No en JSON). Prioridad de evidencias: API > SQL > PDF.";

        static string Truncate(string value, int max)
            => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value.Substring(0, max));

        const int maxPdfChars = 100_000;
        const int maxFacts = 50;

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
        var payload = new { contents, generationConfig = new { temperature = 0.2 } };
        var json = JsonSerializer.Serialize(payload);


        var attempt = await TryCallAsync(apiVersion, model, keyQuery, json, ct, client);
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
                attempt = await TryCallAsync("v1beta", model, keyQuery, json, ct, client);
            }

            if (!attempt.Ok)
            {
                var modelFallbacks = new[] { "gemini-2.5-flash", "gemini-flash-latest", "gemini-2.0-flash" };
                foreach (var candidate in modelFallbacks)
                {
                    _logger.LogInformation("[Gemini] Probando modelo alternativo: {Model}", candidate);
                    var altAttempt = await TryCallAsync("v1beta", candidate, keyQuery, json, ct, client);
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

        GeminiResponse? gemini;
        try
        {
            gemini = JsonSerializer.Deserialize<GeminiResponse>(jsonResp, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
        }
        catch
        {
            gemini = null;
        }
        var text = gemini?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
            return new AssistResponse { Answer = "Respuesta vacía del LLM", Confidence = 0.5, Citations = citations };

        try
        {
            var textResponse = ExtractTextResponse(text);
            var parsed = JsonSerializer.Deserialize<GeminiTextResponse>(textResponse); 
            if (parsed != null && !string.IsNullOrEmpty(parsed.ResponseMessage))
            {
                return new AssistResponse { Answer = parsed!.ResponseMessage!, Confidence = 0.6, Citations = citations };
            }
            else
            {
                return new AssistResponse { Answer = textResponse, Confidence = 0.6, Citations = citations };
            }

            
        }
        catch
        {
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

    private string NormalizeModelPath(string modelName)
            => modelName.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? modelName : $"models/{modelName}";

    private async Task<(bool Ok, string Body, System.Net.HttpStatusCode StatusCode, string? ErrorMessage)> TryCallAsync(
        string version, string modelName, string keyQuery, string json, CancellationToken ct, HttpClient client)
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
        }

        _logger.LogWarning("[Gemini] {Status} for {Uri}. msg={Msg}", resp.StatusCode, reqUriLocal, errMsg ?? body);
        return (false, body, resp.StatusCode, errMsg);
    }

    private string ExtractTextResponse(string input)
    {
        int start = input.IndexOf('{');
        int end = input.LastIndexOf('}');
        if (start >= 0 && end >= 0)
        {
            input = input.Substring(start, end - start + 1);
        }
        return input;
    }
}


