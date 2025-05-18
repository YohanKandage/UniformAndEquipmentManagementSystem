using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniformAndEquipmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddProofImagesToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofImage1",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofImage2",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofImage3",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            // Remove or comment out the line that alters the Employees.Phone column
            // migrationBuilder.AlterColumn<string>(
            //     name: "Phone",
            //     table: "Employees",
            //     type: "nvarchar(10)",
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofImage1",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ProofImage2",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ProofImage3",
                table: "Requests");

            // Remove or comment out the line that alters the Employees.Phone column
            // migrationBuilder.AlterColumn<string>(
            //     name: "Phone",
            //     table: "Employees",
            //     type: "nvarchar(max)",
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "nvarchar(10)",
            //     oldMaxLength: 10);
        }
    }
}
