using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCustomerSnapshotAndOrderCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_CustomerPhone",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_ReversedPhone",
                table: "customers");

            migrationBuilder.CreateSequence(
                name: "order_code_seq");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // Every new column starts nullable. The three that end up NOT NULL only get there after
            // the backfill below has populated them from real data: filling them with an empty
            // string first would leave rows the domain itself could never produce.
            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "orders",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCustomerPhone",
                table: "orders",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversedCustomerPhone",
                table: "orders",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderCode",
                table: "orders",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            // Refuse to touch business data the new model could not have produced. Every order
            // written so far went through the phone normalizer, so this should find nothing; when it
            // does find something the operator gets a count and the data is left exactly as it is.
            // Truncating an over-long number would silently merge two different customers under one
            // search value, and blanking an unparseable one would hide the order from every phone
            // search with nobody the wiser.
            migrationBuilder.Sql(
                """
                DO $migration$
                DECLARE
                    invalid_order_count integer;
                    total_order_count bigint;
                BEGIN
                    SELECT count(*) INTO invalid_order_count
                    FROM orders
                    WHERE length(regexp_replace(coalesce("CustomerPhone", ''), '\D', '', 'g')) NOT BETWEEN 9 AND 15;

                    IF invalid_order_count > 0 THEN
                        RAISE EXCEPTION
                            'Cannot backfill order customer phone search columns: % order(s) hold a CustomerPhone that does not normalize to 9-15 digits. Resolve that business data first; this migration will not truncate or blank it.',
                            invalid_order_count;
                    END IF;

                    -- Order codes are ORD- plus seven digits, so ORD-9999999 is the last one that
                    -- exists. More orders than that cannot all be numbered under this format.
                    SELECT count(*) INTO total_order_count FROM orders;

                    IF total_order_count > 9999999 THEN
                        RAISE EXCEPTION
                            'Cannot backfill order codes: % order(s) exceed the ORD-9999999 ceiling of the seven-digit order code format.',
                            total_order_count;
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE orders
                SET "NormalizedCustomerPhone" = regexp_replace("CustomerPhone", '\D', '', 'g');
                """);

            migrationBuilder.Sql(
                """
                UPDATE orders
                SET "ReversedCustomerPhone" = reverse("NormalizedCustomerPhone");
                """);

            // Best effort: what these were when each order was placed was never recorded, so the
            // customer's current details are the closest approximation available. Both columns stay
            // nullable, so an order whose customer row is gone simply keeps NULL.
            migrationBuilder.Sql(
                """
                UPDATE orders AS orders
                SET "CustomerEmail" = customers."Email",
                    "CustomerAddress" = customers."Address"
                FROM customers
                WHERE customers."Id" = orders."CustomerId";
                """);

            // Numbered by creation time so the codes read in the order the orders were placed. Id
            // breaks ties, so two orders created in the same instant are still numbered the same way
            // on every run — which matters because this backfill is what the codes will be forever.
            migrationBuilder.Sql(
                """
                WITH numbered_orders AS (
                    SELECT "Id", row_number() OVER (ORDER BY "CreatedAt", "Id") AS sequence_number
                    FROM orders
                )
                UPDATE orders
                SET "OrderCode" = 'ORD-' || lpad(numbered_orders.sequence_number::text, 7, '0')
                FROM numbered_orders
                WHERE numbered_orders."Id" = orders."Id";
                """);

            // Seed the sequence past the codes just assigned, so a generated code cannot collide
            // with a backfilled one: 532 backfilled orders leave the next generated code at
            // ORD-0000533. The false flag means the value is handed out rather than skipped, so an
            // empty table leaves the first order at ORD-0000001.
            migrationBuilder.Sql(
                """
                SELECT setval('order_code_seq', (SELECT count(*) FROM orders) + 1, false);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedCustomerPhone",
                table: "orders",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReversedCustomerPhone",
                table: "orders",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderCode",
                table: "orders",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_NormalizedCustomerPhone",
                table: "orders",
                column: "NormalizedCustomerPhone")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderCode",
                table: "orders",
                column: "OrderCode",
                unique: true)
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_ReversedCustomerPhone",
                table: "orders",
                column: "ReversedCustomerPhone")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            // Recreated under the same names, now carrying varchar_pattern_ops. The unique index
            // keeps enforcing uniqueness and answering equality lookups, and additionally serves the
            // LIKE 'digits%' scan the customer autocomplete runs, which a default B-tree cannot do
            // under this database's non-C collation.
            migrationBuilder.CreateIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers",
                column: "NormalizedPhone",
                unique: true,
                filter: "\"NormalizedPhone\" IS NOT NULL AND NOT \"IsDelete\"")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_ReversedPhone",
                table: "customers",
                column: "ReversedPhone",
                filter: "\"ReversedPhone\" IS NOT NULL AND NOT \"IsDelete\"")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_NormalizedCustomerPhone",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_OrderCode",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_ReversedCustomerPhone",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_ReversedPhone",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "NormalizedCustomerPhone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReversedCustomerPhone",
                table: "orders");

            migrationBuilder.DropSequence(
                name: "order_code_seq");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_orders_CustomerPhone",
                table: "orders",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers",
                column: "NormalizedPhone",
                unique: true,
                filter: "\"NormalizedPhone\" IS NOT NULL AND NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_customers_ReversedPhone",
                table: "customers",
                column: "ReversedPhone");
        }
    }
}
