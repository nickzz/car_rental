using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalAPI.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MessageToCustomer",
                table: "Bookings",
                newName: "DriverLicenseUrl");

            migrationBuilder.RenameColumn(
                name: "EstimatedPrice",
                table: "Bookings",
                newName: "TotalPrice");

            migrationBuilder.AddColumn<string>(
                name: "AdminMessage",
                table: "Bookings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminMessage",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "Bookings",
                newName: "EstimatedPrice");

            migrationBuilder.RenameColumn(
                name: "DriverLicenseUrl",
                table: "Bookings",
                newName: "MessageToCustomer");
        }
    }
}
