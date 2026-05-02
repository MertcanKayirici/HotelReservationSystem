using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservationSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId_CheckInDate_CheckOutDate",
                table: "Reservations",
                columns: new[] { "RoomId", "CheckInDate", "CheckOutDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RoomId_CheckInDate_CheckOutDate",
                table: "Reservations");
        }
    }
}
