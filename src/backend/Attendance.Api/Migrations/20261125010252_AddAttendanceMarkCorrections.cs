using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceMarkCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "manual_reason",
                table: "attendance_marks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_mark_id",
                table: "attendance_marks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                table: "attendance_marks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "voided_at",
                table: "attendance_marks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_marks_replaces_mark_id",
                table: "attendance_marks",
                column: "replaces_mark_id");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_marks_attendance_marks_replaces_mark_id",
                table: "attendance_marks",
                column: "replaces_mark_id",
                principalTable: "attendance_marks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_marks_attendance_marks_replaces_mark_id",
                table: "attendance_marks");

            migrationBuilder.DropIndex(
                name: "IX_attendance_marks_replaces_mark_id",
                table: "attendance_marks");

            migrationBuilder.DropColumn(
                name: "manual_reason",
                table: "attendance_marks");

            migrationBuilder.DropColumn(
                name: "replaces_mark_id",
                table: "attendance_marks");

            migrationBuilder.DropColumn(
                name: "void_reason",
                table: "attendance_marks");

            migrationBuilder.DropColumn(
                name: "voided_at",
                table: "attendance_marks");
        }
    }
}
