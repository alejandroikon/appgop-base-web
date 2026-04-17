using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GOP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWellTransitionHistoryAndUwi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Uwi",
                table: "Wells",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WellTransitionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WellId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PerformedByRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellTransitionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WellTransitionHistory_Wells_WellId",
                        column: x => x.WellId,
                        principalTable: "Wells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wells_Uwi",
                table: "Wells",
                column: "Uwi",
                unique: true,
                filter: "[Uwi] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WellTransitionHistory_WellId",
                table: "WellTransitionHistory",
                column: "WellId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WellTransitionHistory");

            migrationBuilder.DropIndex(
                name: "IX_Wells_Uwi",
                table: "Wells");

            migrationBuilder.DropColumn(
                name: "Uwi",
                table: "Wells");
        }
    }
}
