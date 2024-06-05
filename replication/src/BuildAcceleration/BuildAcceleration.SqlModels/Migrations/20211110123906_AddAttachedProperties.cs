using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ForecastBuildTime.SqlModels.Migrations
{
    public partial class AddAttachedProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<IDictionary<string, object>>(
                name: "AttachedProperties",
                table: "Builds",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachedProperties",
                table: "Builds");
        }
    }
}
