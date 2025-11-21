using Microsoft.AspNetCore.Mvc;
using SmartSupport.API.Services;
using SmartSupport.API.Services.Llm.Interfaces;

namespace SmartSupport.API.Controllers
{
    [Route("assist")]
    [ApiController]
    public class AssistController : ControllerBase
    {
        private readonly AssistOrchestrator _orchestrator;
        private readonly ILlmClient _llmClient;

        public AssistController(AssistOrchestrator orchestrator, ILlmClient llmClient)
        {
            _orchestrator = orchestrator;
            _llmClient = llmClient;
        }

        // Assist endpoint (multipart)
        [HttpPost("query")]
        public async Task<IActionResult> Query(CancellationToken ct = default)
        {
            if (!Request.HasFormContentType)
                return BadRequest("multipart/form-data requerido");

            var form = await Request.ReadFormAsync(ct);
            var prompt = form["prompt"].ToString();
            var orderNumber = form["orderNumber"].ToString();
            var useSql = bool.TryParse(form["useSqlRag"], out var us) && us;
            var useApi = bool.TryParse(form["useApiRag"], out var ua) && ua;
            var file = form.Files.GetFile("file");

            if (string.IsNullOrWhiteSpace(prompt) || file is null)
                return BadRequest("Se requiere prompt y archivo PDF");

            await using var stream = file.OpenReadStream();
            var response = await _orchestrator.HandleAsync(prompt, orderNumber, useSql, useApi, stream, file.FileName, ct);
            return Ok(response);
        }

        // Listar modelos disponibles en Gemini
        [HttpGet("models")]
        public async Task<IActionResult> Models(CancellationToken ct = default)
        {
            var json = await _llmClient.ListModelsAsync(ct);
            return Content(json, "application/json");
        }
    }
}
