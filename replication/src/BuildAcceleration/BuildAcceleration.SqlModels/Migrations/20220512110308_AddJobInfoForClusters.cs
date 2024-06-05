using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddJobInfoForClusters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VcsUrl = table.Column<string>(type: "text", nullable: false),
                    JobName = table.Column<string>(type: "text", nullable: false),
                    MaxNonZeroBuildCountPerMonth = table.Column<int>(type: "integer", nullable: false),
                    MeetMinimumSampleSize = table.Column<bool>(type: "boolean", nullable: false),
                    SampledToInspect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobInfos_VcsUrl_JobName",
                table: "JobInfos",
                columns: new[] { "VcsUrl", "JobName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobInfos");
        }
    }
}
