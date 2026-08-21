using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeWorkAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_work_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    assignment_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_work_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_work_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_work_assignments_date",
                table: "employee_work_assignments",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_employee_work_assignments_employee_id",
                table: "employee_work_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_work_assignments_employee_id_date_active",
                table: "employee_work_assignments",
                columns: new[] { "employee_id", "date" },
                unique: true,
                filter: "\"status\" = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_work_assignments");
        }
    }
}
