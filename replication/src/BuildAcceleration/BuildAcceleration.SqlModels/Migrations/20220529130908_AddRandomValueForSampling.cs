using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddRandomValueForSampling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Random",
                table: "JobInfos",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RandomValueCreated",
                table: "JobInfos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Random",
                table: "JobInfos");

            migrationBuilder.DropColumn(
                name: "RandomValueCreated",
                table: "JobInfos");
        }
    }
}
