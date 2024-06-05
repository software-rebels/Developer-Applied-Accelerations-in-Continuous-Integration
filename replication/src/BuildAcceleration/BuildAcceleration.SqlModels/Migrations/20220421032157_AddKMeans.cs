using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddKMeans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_AccelerationSamples_VcsUrl_JobName",
                table: "AccelerationSamples",
                columns: new[] { "VcsUrl", "JobName" });

            migrationBuilder.CreateTable(
                name: "KMeansClusters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VcsUrl = table.Column<string>(type: "text", nullable: false),
                    JobName = table.Column<string>(type: "text", nullable: false),
                    Month = table.Column<string>(type: "text", nullable: false),
                    Higher = table.Column<double>(type: "double precision", nullable: false),
                    Lower = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KMeansClusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KMeansClusters_AccelerationSamples_VcsUrl_JobName",
                        columns: x => new { x.VcsUrl, x.JobName },
                        principalTable: "AccelerationSamples",
                        principalColumns: new[] { "VcsUrl", "JobName" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KMeansClusters_VcsUrl_JobName_Month",
                table: "KMeansClusters",
                columns: new[] { "VcsUrl", "JobName", "Month" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KMeansClusters");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AccelerationSamples_VcsUrl_JobName",
                table: "AccelerationSamples");
        }
    }
}
