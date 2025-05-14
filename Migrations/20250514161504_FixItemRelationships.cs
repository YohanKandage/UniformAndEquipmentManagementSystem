using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixItemRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Departments_DepartmentId1",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Suppliers_SupplierId1",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_DepartmentId1",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_SupplierId1",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SupplierId1",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Available",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "Available");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId1",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId1",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_DepartmentId1",
                table: "Items",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Items_SupplierId1",
                table: "Items",
                column: "SupplierId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Departments_DepartmentId1",
                table: "Items",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Suppliers_SupplierId1",
                table: "Items",
                column: "SupplierId1",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}
