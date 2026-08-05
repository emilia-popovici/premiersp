using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierAuto.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VIN",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "ClientCars",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VIN",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "ClientCars");
        }
    }
}
