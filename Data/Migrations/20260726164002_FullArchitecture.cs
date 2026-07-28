using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PremierAuto.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_ClientId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Mechanics_MechanicId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ClientId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_MechanicId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "MechanicId",
                table: "Reviews",
                newName: "AppointmentId");

            migrationBuilder.AddColumn<bool>(
                name: "IsPictureApproved",
                table: "Mechanics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Mechanics",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "ClientProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Appointments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Appointments_AppointmentId",
                table: "Reviews",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Appointments_AppointmentId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsPictureApproved",
                table: "Mechanics");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Mechanics");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "ClientProfiles");

            migrationBuilder.RenameColumn(
                name: "AppointmentId",
                table: "Reviews",
                newName: "MechanicId");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Reviews",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ClientId",
                table: "Reviews",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_MechanicId",
                table: "Reviews",
                column: "MechanicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_ClientId",
                table: "Reviews",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Mechanics_MechanicId",
                table: "Reviews",
                column: "MechanicId",
                principalTable: "Mechanics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
