using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckpointQrModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "qr_mode",
                table: "checkpoints",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Dynamic");

            migrationBuilder.AddColumn<string>(
                name: "static_qr_token",
                table: "checkpoints",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "qr_mode",
                table: "checkpoints");

            migrationBuilder.DropColumn(
                name: "static_qr_token",
                table: "checkpoints");
        }
    }
}
