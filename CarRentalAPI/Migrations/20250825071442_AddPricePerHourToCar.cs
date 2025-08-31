using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPricePerHourToCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isAvailable",
                table: "Cars");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "Cars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerHour",
                table: "Cars");

            migrationBuilder.AddColumn<bool>(
                name: "isAvailable",
                table: "Cars",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
