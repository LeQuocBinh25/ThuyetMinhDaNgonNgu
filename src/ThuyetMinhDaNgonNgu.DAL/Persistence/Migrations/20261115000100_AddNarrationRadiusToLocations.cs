using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

#nullable disable

namespace ThuyetMinhDaNgonNgu.DAL.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261115000100_AddNarrationRadiusToLocations")]
public partial class AddNarrationRadiusToLocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "NarrationRadiusMeters",
            table: "Locations",
            type: "integer",
            nullable: false,
            defaultValue: 50);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NarrationRadiusMeters",
            table: "Locations");
    }
}
