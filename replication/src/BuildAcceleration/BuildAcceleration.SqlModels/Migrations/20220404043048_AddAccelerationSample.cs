using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddAccelerationSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccelerationSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VcsUrl = table.Column<string>(type: "text", nullable: false),
                    JobName = table.Column<string>(type: "text", nullable: false),
                    BuildCount = table.Column<int>(type: "integer", nullable: false),
                    MmreAverage = table.Column<double>(type: "double precision", nullable: false),
                    MmreLinearRegression = table.Column<double>(type: "double precision", nullable: false),
                    MmreSlidingWindow = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccelerationSamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccelerationSamples_VcsUrl_JobName",
                table: "AccelerationSamples",
                columns: new[] { "VcsUrl", "JobName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccelerationSamples");
        }
    }
}
