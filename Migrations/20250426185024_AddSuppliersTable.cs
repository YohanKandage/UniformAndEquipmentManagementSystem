using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Supplier_SupplierId",
                table: "Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier");

            migrationBuilder.RenameTable(
                name: "Supplier",
                newName: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "Mobile",
                table: "Employees",
                newName: "Position");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmploymentDate",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NIC",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTerms",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Suppliers_SupplierId",
                table: "Items",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Suppliers_SupplierId",
                table: "Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmploymentDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NIC",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeliveryTerms",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Suppliers");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "Supplier");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "Employees",
                newName: "Mobile");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Supplier_SupplierId",
                table: "Items",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
