using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DomainAssignedOrderLineIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Metadata-only migration: OrderLine.Id is domain-assigned, so the model snapshot drops
            // ValueGeneratedOnAdd. The database column itself does not need DDL.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No DDL to reverse. Removing this migration restores the previous model snapshot metadata.
        }
    }
}
