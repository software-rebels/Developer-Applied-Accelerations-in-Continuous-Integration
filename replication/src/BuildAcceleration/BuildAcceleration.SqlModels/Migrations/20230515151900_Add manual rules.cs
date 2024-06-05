using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    /// <inheritdoc />
    public partial class Addmanualrules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, bool>>(
                name: "HitManualRules",
                table: "AccelerationSamples",
                type: "jsonb",
                nullable: false,
                defaultValue: new Dictionary<string, bool>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HitManualRules",
                table: "AccelerationSamples");
        }
    }
}
