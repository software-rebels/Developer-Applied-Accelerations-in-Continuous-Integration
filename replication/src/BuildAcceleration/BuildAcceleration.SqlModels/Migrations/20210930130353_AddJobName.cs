using Microsoft.EntityFrameworkCore.Migrations;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddJobName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch_Why",
                table: "Evaluations");

            migrationBuilder.AddColumn<string>(
                name: "JobName",
                table: "Evaluations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VcsUrl_JobName_Branch_Why_Approach",
                table: "Evaluations",
                columns: new[] { "VcsUrl", "JobName", "Branch", "Why", "Approach" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VcsUrl_JobName_Branch_Why_Approach",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "JobName",
                table: "Evaluations");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch_Why",
                table: "Evaluations",
                columns: new[] { "VcsUrl", "Approach", "Branch", "Why" },
                unique: true);
        }
    }
}
