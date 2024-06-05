using Microsoft.EntityFrameworkCore.Migrations;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class FixWrongIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch",
                table: "Evaluations");

            migrationBuilder.AddColumn<int>(
                name: "BuildCount",
                table: "Evaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch_Why",
                table: "Evaluations",
                columns: new[] { "VcsUrl", "Approach", "Branch", "Why" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch_Why",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "BuildCount",
                table: "Evaluations");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch",
                table: "Evaluations",
                columns: new[] { "VcsUrl", "Approach", "Branch" },
                unique: true);
        }
    }
}
