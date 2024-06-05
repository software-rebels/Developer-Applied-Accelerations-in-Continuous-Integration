using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Builds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VcsUrl = table.Column<string>(type: "text", nullable: false),
                    BuildUrl = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SumOfBuildTimeInSteps = table.Column<TimeSpan>(type: "interval", nullable: false),
                    JobName = table.Column<string>(type: "text", nullable: false),
                    Parallel = table.Column<int>(type: "integer", nullable: false),
                    SelectedSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    CircleYmlHash = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Builds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircleYmls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sha256 = table.Column<byte[]>(type: "bytea", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleYmls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Builds_BuildUrl",
                table: "Builds",
                column: "BuildUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Builds_VcsUrl",
                table: "Builds",
                column: "VcsUrl");

            migrationBuilder.CreateIndex(
                name: "IX_Builds_VcsUrl_JobName",
                table: "Builds",
                columns: new[] { "VcsUrl", "JobName" });

            migrationBuilder.CreateIndex(
                name: "IX_Builds_VcsUrl_SelectedSuccess",
                table: "Builds",
                columns: new[] { "VcsUrl", "SelectedSuccess" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Builds");

            migrationBuilder.DropTable(
                name: "CircleYmls");
        }
    }
}
