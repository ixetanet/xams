using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyXProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class migration05 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "System",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "System");
        }
    }
}
