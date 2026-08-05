using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierAuto.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTalonFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyNumber",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyStyle",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChassisNumber",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngineCapacity",
                table: "ClientCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IDNV",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuingAuthority",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxMass",
                table: "ClientCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnMass",
                table: "ClientCars",
                type: "integer",
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
                name: "PowerWeightRatio",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationDate",
                table: "ClientCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seats",
                table: "ClientCars",
                type: "integer",
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

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "ClientCars",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyNumber",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "BodyStyle",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "ChassisNumber",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "EngineCapacity",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "IDNV",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "IssuingAuthority",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "MaxMass",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "OwnMass",
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
                name: "PowerWeightRatio",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "ClientCars");

            migrationBuilder.DropColumn(
                name: "Seats",
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

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "ClientCars");
        }
    }
}
