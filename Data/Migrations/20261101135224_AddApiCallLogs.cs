using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BuildWorkflowAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiCallLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiCallLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Route = table.Column<string>(type: "text", nullable: false),
                    ApiDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    ApiKeyId = table.Column<int>(type: "integer", nullable: true),
                    KeyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiCallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiCallLogs_ApiDefinitions_ApiDefinitionId",
                        column: x => x.ApiDefinitionId,
                        principalTable: "ApiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApiCallLogs_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_ApiDefinitionId",
                table: "ApiCallLogs",
                column: "ApiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_ApiKeyId",
                table: "ApiCallLogs",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_Route",
                table: "ApiCallLogs",
                column: "Route");

            migrationBuilder.CreateIndex(
                name: "IX_ApiCallLogs_Timestamp",
                table: "ApiCallLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiCallLogs");
        }
    }
}
