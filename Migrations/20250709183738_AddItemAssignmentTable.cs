using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAssignmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Employees_AssignedToId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_AssignedToId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AssignmentDate",
                table: "Items");

            migrationBuilder.CreateTable(
                name: "ItemAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemAssignments_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemAssignments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemAssignments_EmployeeId",
                table: "ItemAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAssignments_ItemId",
                table: "ItemAssignments",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAssignments_RequestId",
                table: "ItemAssignments",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemAssignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "Items",
                type: "datetime2",
                nullable: true);

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
    }
}
