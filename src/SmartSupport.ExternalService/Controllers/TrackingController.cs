using Microsoft.AspNetCore.Mvc;
using SmartSupport.ExternalService.Models;

namespace SmartSupport.ExternalService.Controllers
{
    /// <summary>
    /// Controlador para los endpoints de tracking
    /// </summary>
    [Route("tracking")]
    [ApiController]
    [Produces("application/json")]
    public class TrackingController : ControllerBase
    {
        /// <summary>
        /// Obtiene el estado de seguimiento de un paquete por su número de tracking
        /// </summary>
        /// <param name="trackingNumber">Número de seguimiento del paquete</param>
        /// <param name="mode">Modo opcional para simular diferentes estados (delayed, in_transit, out_for_delivery)</param>
        /// <returns>Información del estado de seguimiento del paquete</returns>
        /// <remarks>
        /// Este es un endpoint simulado para la demo. 
        /// El tracking number "1Z999SMART" tiene comportamientos especiales según el modo.
        /// </remarks>
        [HttpGet("{trackingNumber}")]
        [ProducesResponseType(typeof(TrackingResponse), StatusCodes.Status200OK)]
        public IActionResult GetTracking(string trackingNumber, [FromQuery] string? mode = null)
        {
            var now = DateTime.UtcNow;

            if (string.Equals(trackingNumber, "1Z999SMART", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(mode, "delayed", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new TrackingResponse
                    {
                        Status = "delayed",
                        Eta = now.AddDays(2),
                        LastScan = new LastScan
                        {
                            When = now.AddHours(-1),
                            Location = "Centro de distribución",
                            Message = "Demora por logística"
                        }
                    });
                }
                else if (string.Equals(mode, "in_transit", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new TrackingResponse
                    {
                        Status = "in_transit",
                        Eta = now.AddDays(1),
                        LastScan = new LastScan
                        {
                            When = now.AddHours(-3),
                            Location = "Planta logística",
                            Message = "Clasificado"
                        }
                    });
                }
                else
                {
                    var eta = DateTime.UtcNow.Date.AddHours(19);
                    return Ok(new TrackingResponse
                    {
                        Status = "out_for_delivery",
                        Eta = eta,
                        LastScan = new LastScan
                        {
                            When = now.AddHours(-2),
                            Location = "Centro de distribución",
                            Message = "Salida a ruta de reparto"
                        }
                    });
                }
            }

            // Fallback genérico
            return Ok(new TrackingResponse
            {
                Status = "in_transit",
                Eta = now.AddDays(2),
                LastScan = new LastScan
                {
                    When = now.AddHours(-6),
                    Location = "Planta logística",
                    Message = "Clasificado"
                }
            });
        }
    }
}
