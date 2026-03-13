using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Tasks");
        }
    }
}
