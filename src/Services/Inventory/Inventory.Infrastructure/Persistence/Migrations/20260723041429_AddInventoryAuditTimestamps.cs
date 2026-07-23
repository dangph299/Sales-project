using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAuditTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "reservations",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "inventory_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "inventory_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql(
                """
                UPDATE reservations
                SET "UpdatedAt" = "CreatedAt"
                """);

            migrationBuilder.Sql(
                """
                UPDATE inventory_items
                SET "UpdatedAt" = "CreatedAt"
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "inventory_items");
        }
    }
}
