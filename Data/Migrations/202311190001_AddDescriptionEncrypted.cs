using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSchoolIncidentManagementSystem.Data.Migrations
{
    public partial class AddDescriptionEncrypted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEncrypted",
                table: "Incidents",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEncrypted",
                table: "Incidents");
        }
    }
}
