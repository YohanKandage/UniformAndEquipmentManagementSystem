using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateItemWithSupplierDepartmentAndQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Size",
                table: "Items",
                newName: "Material");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Items",
                newName: "JobRole");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Items",
                newName: "ItemType");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Items",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPersonEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupplierCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContractStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContractEndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_DepartmentId",
                table: "Items",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_SupplierId",
                table: "Items",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Supplier_SupplierId",
                table: "Items",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Supplier_SupplierId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropIndex(
                name: "IX_Items_DepartmentId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_SupplierId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Material",
                table: "Items",
                newName: "Size");

            migrationBuilder.RenameColumn(
                name: "JobRole",
                table: "Items",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ItemType",
                table: "Items",
                newName: "Image");
        }
    }
}
