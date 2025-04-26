using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsWithImageAndNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Suppliers_SupplierId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "JobRole",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "EmploymentDate",
                table: "Employees",
                newName: "JoinDate");

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
                name: "FK_Items_Departments_DepartmentId",
                table: "Items",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Departments_DepartmentId1",
                table: "Items",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Suppliers_SupplierId",
                table: "Items",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Suppliers_SupplierId1",
                table: "Items",
                column: "SupplierId1",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Departments_DepartmentId1",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Suppliers_SupplierId",
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

            migrationBuilder.RenameColumn(
                name: "JoinDate",
                table: "Employees",
                newName: "EmploymentDate");

            migrationBuilder.AddColumn<string>(
                name: "JobRole",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Suppliers_SupplierId",
                table: "Items",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
