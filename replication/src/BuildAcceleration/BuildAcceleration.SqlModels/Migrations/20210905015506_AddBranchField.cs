using Microsoft.EntityFrameworkCore.Migrations;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddBranchField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "Builds",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branch",
                table: "Builds");
        }
    }
}
