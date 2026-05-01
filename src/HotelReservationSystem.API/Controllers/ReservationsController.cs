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
            await _service.CreateReservationAsync(dto);

            var response = new CreateReservationResponse
            {
                RequestId = dto.IdempotencyKey
            };

            return Accepted(response); 
        }
    }
}