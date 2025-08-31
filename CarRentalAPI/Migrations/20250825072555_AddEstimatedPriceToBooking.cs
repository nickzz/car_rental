using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedPriceToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageToCustomer",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPrice",
                table: "Bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "MessageToCustomer",
                table: "Bookings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
