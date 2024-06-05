using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddBuildCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NonZeroBuildCount",
                table: "JobInfos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NonZeroBuildCount",
                table: "JobInfos");
        }
    }
}
