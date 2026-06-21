using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailwayReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStationCodeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StationCode",
                table: "TrainStations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TrainStations_TrainId_StationCode",
                table: "TrainStations",
                columns: new[] { "TrainId", "StationCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainStations_TrainId_StationCode",
                table: "TrainStations");

            migrationBuilder.DropColumn(
                name: "StationCode",
                table: "TrainStations");
        }
    }
}
