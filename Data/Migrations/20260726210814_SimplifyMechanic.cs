using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierAuto.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMechanic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Mechanics");

            migrationBuilder.DropColumn(
                name: "ExperienceYears",
                table: "Mechanics");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Mechanics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Mechanics",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceYears",
                table: "Mechanics",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Mechanics",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }
    }
}
