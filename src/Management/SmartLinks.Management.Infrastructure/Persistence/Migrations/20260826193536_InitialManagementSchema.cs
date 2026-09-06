using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartLinks.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "management");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "configuration_changes",
                schema: "management",
                columns: table => new
                {
                    revision = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    smart_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuration = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuration_changes", x => x.revision);
                });

            migrationBuilder.CreateTable(
                name: "published_smart_links",
                schema: "management",
                columns: table => new
                {
                    smart_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    configuration = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_published_smart_links", x => x.smart_link_id);
                });

            migrationBuilder.CreateTable(
                name: "smart_links",
                schema: "management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    default_url = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_smart_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "smart_link_rules",
                schema: "management",
                columns: table => new
                {
                    priority = table.Column<int>(type: "integer", nullable: false),
                    smart_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    target_url = table.Column<string>(type: "text", nullable: false),
                    condition_dsl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_smart_link_rules", x => new { x.smart_link_id, x.priority });
                    table.ForeignKey(
                        name: "FK_smart_link_rules_smart_links_smart_link_id",
                        column: x => x.smart_link_id,
                        principalSchema: "management",
                        principalTable: "smart_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_changes_smart_link_id",
                schema: "management",
                table: "configuration_changes",
                column: "smart_link_id");

            migrationBuilder.CreateIndex(
                name: "ux_published_smart_links_slug",
                schema: "management",
                table: "published_smart_links",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_smart_links_slug",
                schema: "management",
                table: "smart_links",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuration_changes",
                schema: "management");

            migrationBuilder.DropTable(
                name: "published_smart_links",
                schema: "management");

            migrationBuilder.DropTable(
                name: "smart_link_rules",
                schema: "management");

            migrationBuilder.DropTable(
                name: "smart_links",
                schema: "management");
        }
    }
}
