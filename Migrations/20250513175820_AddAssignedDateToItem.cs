using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedDateToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "Items",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "Items");
        }
    }
}
