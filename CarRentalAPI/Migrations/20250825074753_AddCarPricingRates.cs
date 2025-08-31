using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCarPricingRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PricePerHour",
                table: "Cars",
                newName: "PricePerWeek");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDay",
                table: "Cars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMonth",
                table: "Cars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerDay",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "PricePerMonth",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "PricePerWeek",
                table: "Cars",
                newName: "PricePerHour");
        }
    }
}
