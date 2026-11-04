
using HotelReservationSystem.Application.DTOs;
using HotelReservationSystem.Application.Enums;
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

        [HttpPatch("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var room = await _roomService.ActivateAsync(id);

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

        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var room = await _roomService.DeactivateAsync(id);

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
            var result = await _roomService.DeleteAsync(id);

            switch (result)
            {
                case RoomDeletionResult.Deleted:
                    return NoContent();

                case RoomDeletionResult.NotFound:
                    return NotFound(new
                    {
                        code = "ROOM_NOT_FOUND",
                        message = "Room not found."
                    });

                case RoomDeletionResult.HasDependencies:
                    return Conflict(new
                    {
                        code = "ROOM_HAS_DEPENDENCIES",
                        message = "Room cannot be deleted because related records exist."
                    });

                default:
                    return StatusCode(500, new
                    {
                        code = "ROOM_DELETION_FAILED",
                        message = "An unexpected error occurred while deleting the room."
                    });
            }
        }
    }
}
