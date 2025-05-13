using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentDate",
                table: "Items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Items_AssignedToId",
                table: "Items",
                column: "AssignedToId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Employees_AssignedToId",
                table: "Items",
                column: "AssignedToId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Employees_AssignedToId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_AssignedToId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AssignmentDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Items");
        }
    }
}
