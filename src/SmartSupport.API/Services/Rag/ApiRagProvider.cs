using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

public sealed class ApiRagProvider : IApiRagProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ApiRagProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetApiFactsAsync(
        string? trackingNumber, CancellationToken ct = default)
    {
        var facts = new List<string>();
        var cites = new List<AssistCitation>();
        if (string.IsNullOrWhiteSpace(trackingNumber)) return (facts, cites);

        var client = _httpClientFactory.CreateClient("ExternalService");
        var resp = await client.GetAsync($"/tracking/{trackingNumber}", ct);
        if (!resp.IsSuccessStatusCode) return (facts, cites);
        var json = await resp.Content.ReadAsStringAsync(ct);
        facts.Add($"Tracking raw: {json}");
        cites.Add(new AssistCitation { Source = "api", Service = "ExternalService", Id = trackingNumber });
        return (facts, cites);
    }
}


