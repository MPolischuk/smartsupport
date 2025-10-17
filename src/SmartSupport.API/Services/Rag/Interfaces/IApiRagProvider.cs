using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

public interface IApiRagProvider
{
    Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetApiFactsAsync(string? trackingNumber, CancellationToken ct = default);
}


