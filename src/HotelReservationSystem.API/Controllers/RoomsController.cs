using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _roomService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _roomService.GetByIdAsync(id);

            if (room is null)
            {
                return NotFound(new
                {
                    code = "ROOM_NOT_FOUND",
                    message = "Room not found."
                });
            }

            return Ok(room);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            var room = await _roomService.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto)
        {
            var room = await _roomService.UpdateAsync(id, dto);

            if (room is null)
            {
                return NotFound(new
                {
                    code = "ROOM_NOT_FOUND",
                    message = "Room not found."
                });
            }

            return Ok(room);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _roomService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    code = "ROOM_NOT_FOUND",
                    message = "Room not found."
                });
            }

            return NoContent();
        }
    }
}
