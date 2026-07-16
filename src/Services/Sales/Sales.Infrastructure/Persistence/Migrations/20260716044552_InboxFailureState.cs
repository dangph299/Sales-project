using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InboxFailureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "inbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "inbox_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastExceptionType",
                table: "inbox_messages",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastFailedAt",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalConsumerGroup",
                table: "inbox_messages",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OriginalOffset",
                table: "inbox_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalPartition",
                table: "inbox_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalTopic",
                table: "inbox_messages",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "inbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_Status_DeadLetteredAt",
                table: "inbox_messages",
                columns: new[] { "Status", "DeadLetteredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_Status_DeadLetteredAt",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "LastExceptionType",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "LastFailedAt",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "OriginalConsumerGroup",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "OriginalOffset",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "OriginalPartition",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "OriginalTopic",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "inbox_messages");
        }
    }
}
