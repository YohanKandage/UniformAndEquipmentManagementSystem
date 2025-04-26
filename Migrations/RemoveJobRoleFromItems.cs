using Microsoft.EntityFrameworkCore.Migrations;

namespace UniformAndEquipmentManagementSystem.Migrations
{
    public partial class RemoveJobRoleFromItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobRole",
                table: "Items");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobRole",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false);
        }
    }
} 