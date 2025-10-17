using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

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


