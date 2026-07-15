using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderExpirationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_orders_Status_UpdatedAt_Id",
                table: "orders",
                columns: new[] { "Status", "UpdatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_Status_UpdatedAt_Id",
                table: "orders");
        }
    }
}
