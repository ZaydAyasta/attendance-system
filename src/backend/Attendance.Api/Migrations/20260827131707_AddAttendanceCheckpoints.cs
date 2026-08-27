using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    checkpoint_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkpoints", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_marks_checkpoint_id",
                table: "attendance_marks",
                column: "checkpoint_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkpoints_code",
                table: "checkpoints",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_marks_checkpoints_checkpoint_id",
                table: "attendance_marks",
                column: "checkpoint_id",
                principalTable: "checkpoints",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_marks_checkpoints_checkpoint_id",
                table: "attendance_marks");

            migrationBuilder.DropTable(
                name: "checkpoints");

            migrationBuilder.DropIndex(
                name: "IX_attendance_marks_checkpoint_id",
                table: "attendance_marks");
        }
    }
}
