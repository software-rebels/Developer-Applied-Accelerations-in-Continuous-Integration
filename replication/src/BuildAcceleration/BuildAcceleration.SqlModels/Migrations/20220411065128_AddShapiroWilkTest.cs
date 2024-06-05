using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddShapiroWilkTest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ShapiroWilk_P",
                table: "AccelerationSamples",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ShapiroWilk_W",
                table: "AccelerationSamples",
                type: "double precision",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShapiroWilk_P",
                table: "AccelerationSamples");

            migrationBuilder.DropColumn(
                name: "ShapiroWilk_W",
                table: "AccelerationSamples");
        }
    }
}
