using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxReliability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_DeadLetteredAt_NextAttemptAt_OccurredAt",
                table: "outbox_messages",
                columns: new[] { "DeadLetteredAt", "NextAttemptAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_LockId",
                table: "outbox_messages",
                column: "LockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_DeadLetteredAt_NextAttemptAt_OccurredAt",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_LockId",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LockId",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "outbox_messages");
        }
    }
}
