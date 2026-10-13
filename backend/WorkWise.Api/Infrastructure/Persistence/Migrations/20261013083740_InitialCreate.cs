using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkWise.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "work_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_requests", x => x.id);
                    table.CheckConstraint("ck_work_requests_priority", "priority IN ('Low','Medium','High')");
                    table.CheckConstraint("ck_work_requests_status", "status IN ('New','InProgress','Blocked','Completed')");
                });

            migrationBuilder.CreateTable(
                name: "work_request_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_request_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_request_notes_work_requests_work_request_id",
                        column: x => x.work_request_id,
                        principalTable: "work_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_request_notes_work_request_id_created_at",
                table: "work_request_notes",
                columns: new[] { "work_request_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_work_requests_client_name_trgm",
                table: "work_requests",
                column: "client_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_work_requests_due_date",
                table: "work_requests",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_work_requests_status_due_date",
                table: "work_requests",
                columns: new[] { "status", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_work_requests_title_trgm",
                table: "work_requests",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_request_notes");

            migrationBuilder.DropTable(
                name: "work_requests");
        }
    }
}
