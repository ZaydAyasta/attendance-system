using System;
using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance.Api.Migrations
{
    [DbContext(typeof(AttendanceDbContext))]
    [Migration("20260902143000_AddLegacyImportMappings")]
    public partial class AddLegacyImportMappings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legacy_import_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    legacy_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    destination_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_legacy_import_mappings", x => x.id));

            migrationBuilder.CreateIndex(
                name: "IX_legacy_import_mappings_source_system_entity_type_legacy_id",
                table: "legacy_import_mappings",
                columns: new[] { "source_system", "entity_type", "legacy_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DropTable(name: "legacy_import_mappings");
    }
}
