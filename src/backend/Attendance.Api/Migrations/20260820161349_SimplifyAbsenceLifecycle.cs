using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAbsenceLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE absences
                SET status = 'Active'
                WHERE status IN ('Pending', 'Approved');
                """);

            migrationBuilder.Sql(
                """
                UPDATE absences
                SET status = 'Cancelled'
                WHERE status IN ('Rejected', 'Cancelled');
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_absences_status",
                table: "absences",
                sql: "status IN ('Active', 'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_absences_status",
                table: "absences");

            migrationBuilder.Sql(
                """
                UPDATE absences
                SET status = 'Approved'
                WHERE status = 'Active';
                """);
        }
    }
}
