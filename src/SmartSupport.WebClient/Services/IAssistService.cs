using Microsoft.AspNetCore.Components.Forms;
using SmartSupport.WebClient.Models;

namespace SmartSupport.WebClient.Services
{
    public interface IAssistService
    {
        Task<AssistResponseDto?> QueryAsync(
            string prompt,
            IBrowserFile file,
            string? orderNumber = null,
            bool useSql = false,
            bool useApi = false,
            CancellationToken cancellationToken = default);

        IReadOnlyList<PromptTemplate> GetPromptTemplates();
    }
}
