using HotelReservationSystem.API.Models.Responses;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HotelReservationSystem.API.Controllers
{
    /// <summary>
    /// Handles reservation-related API endpoints such as creating a reservation
    /// and retrieving the status of an existing request.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ReservationsController : ControllerBase
    {
        /// <summary>
        /// Service responsible for reservation business logic operations.
        /// </summary>
        private readonly IReservationService _service;

        /// <summary>
        /// Initializes the controller with required dependencies.
        /// </summary>
        /// <param name="service">Reservation service abstraction.</param>
        public ReservationsController(IReservationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new reservation request.
        /// This endpoint is asynchronous and returns immediately with a tracking identifier.
        /// </summary>
        /// <param name="dto">Reservation creation request payload.</param>
        /// <returns>Accepted response containing request tracking information.</returns>
        [HttpPost]
        public async Task<IActionResult> Create(CreateReservationDto dto)
        {
            // Attach correlation ID from the current request context for traceability across services
            dto.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();

            // Trigger reservation creation process via application service layer
            await _service.CreateReservationAsync(dto);

            // Return 202 Accepted with idempotency-based request identifier for status tracking
            return Accepted(new CreateReservationResponse
            {
                RequestId = dto.IdempotencyKey
            });
        }

        /// <summary>
        /// Retrieves the current status of a reservation request.
        /// </summary>
        /// <param name="requestId">Unique identifier used to track the reservation request.</param>
        /// <returns>Current status of the reservation request.</returns>
        [HttpGet("status/{requestId}")]
        public async Task<IActionResult> GetStatus(string requestId)
        {
            // Fetch reservation status from service layer
            var result = await _service.GetStatusAsync(requestId);

            // Return result directly without additional mapping to preserve service response contract
            return Ok(result);
        }
    }
}