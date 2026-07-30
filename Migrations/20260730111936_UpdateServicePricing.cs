using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierAuto.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServicePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Services");

            migrationBuilder.AddColumn<int>(
                name: "FinalDurationMinutes",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "Appointments",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalDurationMinutes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "Appointments");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Services",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
