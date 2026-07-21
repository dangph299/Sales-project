using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackendAssignedEntityCodeSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "category_code_seq");

            migrationBuilder.CreateSequence(
                name: "customer_code_seq");

            migrationBuilder.CreateSequence(
                name: "product_code_seq");

            // Seed each fresh sequence past the highest code already in use, so a generated code can
            // never collide with an existing one. Every value the pattern accepts counts, including
            // the large timestamp-derived codes the old client-side generator produced (for example
            // PRD772802): those rows still exist and are still protected by the unique index, so
            // seeding below them would hand out a duplicate once the sequence caught up.
            //
            // Codes that are malformed, carry another prefix, or are wider than a bigint are excluded
            // by the pattern before any cast runs, so seeding cannot fail on existing rows. A value
            // too wide for bigint is also unreachable by the sequence, so ignoring it is safe.
            //
            // Duplicate live codes are reported and left alone rather than repaired here.
            migrationBuilder.Sql(
                """
                DO $migration$
                DECLARE
                    code_source record;
                    highest_sequence_number bigint;
                    duplicate_count integer;
                BEGIN
                    FOR code_source IN
                        SELECT *
                        FROM (VALUES
                            ('customers',  'CustomerCode', 'CUS', 'customer_code_seq'),
                            ('products',   'ProductCode',  'PRD', 'product_code_seq'),
                            ('categories', 'CategoryCode', 'CAT', 'category_code_seq')
                        ) AS source(table_name, column_name, code_prefix, sequence_name)
                    LOOP
                        EXECUTE format(
                            'WITH parsed AS MATERIALIZED ('
                            '  SELECT CAST(substring(%I from %s) AS bigint) AS sequence_number'
                            '  FROM %I WHERE %I ~ %L'
                            ') SELECT COALESCE(MAX(sequence_number), 0) FROM parsed',
                            code_source.column_name, length(code_source.code_prefix) + 1,
                            code_source.table_name, code_source.column_name,
                            '^' || code_source.code_prefix || '[0-9]{1,18}$')
                        INTO highest_sequence_number;

                        PERFORM setval(code_source.sequence_name, highest_sequence_number + 1, false);

                        EXECUTE format(
                            'SELECT count(*) FROM (SELECT %I FROM %I WHERE NOT "IsDelete" GROUP BY %I HAVING count(*) > 1) AS duplicates',
                            code_source.column_name, code_source.table_name, code_source.column_name)
                        INTO duplicate_count;

                        IF duplicate_count > 0 THEN
                            RAISE WARNING 'Sales code seeding: % code(s) in %.% are shared by more than one live row. Business data left untouched; resolve manually.',
                                duplicate_count, code_source.table_name, code_source.column_name;
                        END IF;
                    END LOOP;
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "category_code_seq");

            migrationBuilder.DropSequence(
                name: "customer_code_seq");

            migrationBuilder.DropSequence(
                name: "product_code_seq");
        }
    }
}
