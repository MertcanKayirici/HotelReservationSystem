using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HotelReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _service;

        public ReservationsController(IReservationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            await _service.CreateReservationAsync(dto);

            return Ok(new
            {
                success = true,
                message = "Reservation created successfully"
            });
        }
    }
}