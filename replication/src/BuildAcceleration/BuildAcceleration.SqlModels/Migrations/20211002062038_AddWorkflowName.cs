using Microsoft.EntityFrameworkCore.Migrations;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddWorkflowName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkflowName",
                table: "Builds",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkflowName",
                table: "Builds");
        }
    }
}
