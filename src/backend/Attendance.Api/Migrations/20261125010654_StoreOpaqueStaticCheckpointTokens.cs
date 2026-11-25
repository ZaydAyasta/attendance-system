using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class StoreOpaqueStaticCheckpointTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE checkpoints
                SET static_qr_token = CASE WHEN qr_mode = 'Static'
                    THEN 's2:' || replace(gen_random_uuid()::text, '-', '') ELSE NULL END;
                """);
            migrationBuilder.CreateIndex(
                name: "IX_checkpoints_static_qr_token",
                table: "checkpoints",
                column: "static_qr_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_checkpoints_static_qr_token",
                table: "checkpoints");
            migrationBuilder.Sql("""
                UPDATE checkpoints
                SET static_qr_token = 's1:' || replace(id::text, '-', '')
                WHERE qr_mode = 'Static';
                """);
        }
    }
}
