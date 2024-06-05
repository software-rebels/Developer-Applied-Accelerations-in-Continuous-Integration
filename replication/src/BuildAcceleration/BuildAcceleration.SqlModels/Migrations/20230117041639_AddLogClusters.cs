using System.Collections.Generic;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForecastBuildTime.SqlModels.Migrations
{
    /// <inheritdoc />
    public partial class AddLogClusters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<KMeansClusters>>(
                name: "ClusterCentersLog",
                table: "AccelerationSamples",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterCentersLog",
                table: "AccelerationSamples");
        }
    }
}
