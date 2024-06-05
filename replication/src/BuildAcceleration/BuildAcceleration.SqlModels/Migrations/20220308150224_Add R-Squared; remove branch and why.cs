using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddRSquaredremovebranchandwhy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VcsUrl_JobName_Branch_Why_Approach",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "Branch",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "Why",
                table: "Evaluations");

            migrationBuilder.AddColumn<double>(
                name: "RSquared",
                table: "Evaluations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VcsUrl_JobName_Approach",
                table: "Evaluations",
                columns: new[] { "VcsUrl", "JobName", "Approach" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VcsUrl_JobName_Approach",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "RSquared",
                table: "Evaluations");

            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "Evaluations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why",
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
    }
}
