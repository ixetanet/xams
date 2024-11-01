using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyXProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class migration06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "System",
                newName: "DateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateTime",
                table: "System",
                newName: "Date");
        }
    }
}
