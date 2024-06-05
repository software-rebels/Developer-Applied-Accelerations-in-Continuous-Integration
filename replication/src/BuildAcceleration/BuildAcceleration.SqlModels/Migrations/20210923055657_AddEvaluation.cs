using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddEvaluation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VcsUrl = table.Column<string>(type: "text", nullable: false),
                    Branch = table.Column<string>(type: "text", nullable: false),
                    Approach = table.Column<string>(type: "text", nullable: false),
                    MMRE = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VcsUrl_Approach_Branch",
                table: "Evaluations",
                columns: new[] { "VcsUrl", "Approach", "Branch" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Evaluations");
        }
    }
}
