using HotelReservationSystem.API.Models.Responses;
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HotelReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _service;

        public ReservationsController(IReservationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReservationDto dto)
        {
            dto.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();

            await _service.CreateReservationAsync(dto);

            return Accepted(new CreateReservationResponse
            {
                RequestId = dto.IdempotencyKey
            });
        }

        [HttpGet("status/{requestId}")]
        public async Task<IActionResult> GetStatus(string requestId)
        {
            var result = await _service.GetStatusAsync(requestId);

            // 🔥 ARTIK MAPPING YAPMA → direkt dön
            return Ok(result);
        }
    }
}