using Microsoft.EntityFrameworkCore.Migrations;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VcsRevision",
                table: "Builds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why",
                table: "Builds",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VcsRevision",
                table: "Builds");

            migrationBuilder.DropColumn(
                name: "Why",
                table: "Builds");
        }
    }
}
