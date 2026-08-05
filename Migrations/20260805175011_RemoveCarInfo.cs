using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierAuto.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCarInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuingAuthority",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "OwnerAddress",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "SpecialMentions",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "ValidityPeriod",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "VehicleRights",
                table: "ClientCars");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssuingAuthority",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerAddress",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationDate",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialMentions",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidityPeriod",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRights",
                table: "ClientCars",
                type: "text",
                nullable: true);
        }
    }
}
