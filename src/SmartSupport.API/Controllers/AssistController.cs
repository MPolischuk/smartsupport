using Microsoft.AspNetCore.Mvc;
using SmartSupport.API.Models;
using SmartSupport.API.Services;
using SmartSupport.API.Services.Llm.Interfaces;

namespace SmartSupport.API.Controllers
{
    /// <summary>
    /// Controlador para los endpoints de asistencia inteligente
    /// </summary>
    [Route("assist")]
    [ApiController]
    [Produces("application/json")]
    public class AssistController : ControllerBase
    {
        private readonly AssistOrchestrator _orchestrator;
        private readonly ILlmClient _llmClient;

        public AssistController(AssistOrchestrator orchestrator, ILlmClient llmClient)
        {
            _orchestrator = orchestrator;
            _llmClient = llmClient;
        }

        /// <summary>
        /// Procesa una consulta de asistencia con un archivo PDF
        /// </summary>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Respuesta con la información de asistencia procesada</returns>
        /// <remarks>
        /// Requiere un formulario multipart/form-data con los siguientes campos:
        /// - prompt (string): El texto de la consulta
        /// - file (file): Archivo PDF a procesar
        /// - orderNumber (string, opcional): Número de orden
        /// - useSqlRag (bool, opcional): Usar RAG desde SQL
        /// - useApiRag (bool, opcional): Usar RAG desde API externa
        /// </remarks>
        [HttpPost("query")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(AssistResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Lista los modelos disponibles en el proveedor LLM (Gemini)
        /// </summary>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>JSON con la lista de modelos disponibles</returns>
        [HttpGet("models")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Models(CancellationToken ct = default)
        {
            var json = await _llmClient.ListModelsAsync(ct);
            return Content(json, "application/json");
        }
    }
}
