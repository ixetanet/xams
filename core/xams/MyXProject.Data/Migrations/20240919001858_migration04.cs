using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyXProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class migration04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Option",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "Job",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Audit",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    IsCreate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "AuditHistory",
                columns: table => new
                {
                    AuditHistoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", maxLength: 250, nullable: true),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 2147483647, nullable: true),
                    Results = table.Column<string>(type: "TEXT", maxLength: 2147483647, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditHistory", x => x.AuditHistoryId);
                    table.ForeignKey(
                        name: "FK_AuditHistory_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "System",
                columns: table => new
                {
                    SystemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_System", x => x.SystemId);
                });

            migrationBuilder.CreateTable(
                name: "AuditField",
                columns: table => new
                {
                    AuditFieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    AuditId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsCreate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDelete = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditField", x => x.AuditFieldId);
                    table.ForeignKey(
                        name: "FK_AuditField_Audit_AuditId",
                        column: x => x.AuditId,
                        principalTable: "Audit",
                        principalColumn: "AuditId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditHistoryDetail",
                columns: table => new
                {
                    AuditHistoryDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    AuditHistoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    OldValueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    NewValueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditHistoryDetail", x => x.AuditHistoryDetailId);
                    table.ForeignKey(
                        name: "FK_AuditHistoryDetail_AuditHistory_AuditHistoryId",
                        column: x => x.AuditHistoryId,
                        principalTable: "AuditHistory",
                        principalColumn: "AuditHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditField_AuditId",
                table: "AuditField",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_UserId",
                table: "AuditHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistoryDetail_AuditHistoryId",
                table: "AuditHistoryDetail",
                column: "AuditHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditField");

            migrationBuilder.DropTable(
                name: "AuditHistoryDetail");

            migrationBuilder.DropTable(
                name: "System");

            migrationBuilder.DropTable(
                name: "Audit");

            migrationBuilder.DropTable(
                name: "AuditHistory");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Option");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "Job");
        }
    }
}
