using System.Text.Json.Serialization;

namespace SmartSupport.API.Models;

public sealed class GeminiResponse
{
    [JsonPropertyName("candidates")] public List<GeminiCandidate> Candidates { get; set; } = new();
    [JsonPropertyName("usageMetadata")] public GeminiUsageMetadata? UsageMetadata { get; set; }
    [JsonPropertyName("modelVersion")] public string? ModelVersion { get; set; }
    [JsonPropertyName("responseId")] public string? ResponseId { get; set; }
}

public sealed class GeminiCandidate
{
    [JsonPropertyName("content")] public GeminiContent? Content { get; set; }
    [JsonPropertyName("finishReason")] public string? FinishReason { get; set; }
    [JsonPropertyName("index")] public int Index { get; set; }
}

public sealed class GeminiContent
{
    [JsonPropertyName("parts")] public List<GeminiPart> Parts { get; set; } = new();
    [JsonPropertyName("role")] public string? Role { get; set; }
}

public sealed class GeminiPart
{
    [JsonPropertyName("text")] public string? Text { get; set; }
}

public sealed class GeminiTextResponse
{
    [JsonPropertyName("responseMessage")] public string? ResponseMessage { get; set; }
}

public sealed class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")] public int PromptTokenCount { get; set; }
    [JsonPropertyName("candidatesTokenCount")] public int CandidatesTokenCount { get; set; }
    [JsonPropertyName("totalTokenCount")] public int TotalTokenCount { get; set; }
}


