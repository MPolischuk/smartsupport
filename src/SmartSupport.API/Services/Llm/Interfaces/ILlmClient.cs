using SmartSupport.API.Models;

namespace SmartSupport.API.Services.Llm.Interfaces;

public interface ILlmClient
{
    Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default);
    Task<string> ListModelsAsync(CancellationToken ct = default);
}


