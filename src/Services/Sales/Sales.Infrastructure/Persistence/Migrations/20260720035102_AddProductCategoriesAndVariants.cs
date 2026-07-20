using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoriesAndVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_Sku",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_order_lines_OrderId_ProductId",
                table: "order_lines");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "products",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "products",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "order_lines",
                type: "character varying(96)",
                maxLength: 96,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "order_lines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ColorCode",
                table: "order_lines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "order_lines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "order_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SizeCode",
                table: "order_lines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "customers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "customers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "customers",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhone",
                table: "customers",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "customers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteByUser = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "colors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColorCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HexCode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sizes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteByUser = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_colors_ColorId",
                        column: x => x.ColorId,
                        principalTable: "colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_variants_sizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "colors",
                columns: new[] { "Id", "ColorCode", "HexCode", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "BLK", "#000000", "Black" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "WHT", "#FFFFFF", "White" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "RED", "#FF0000", "Red" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "BLU", "#0000FF", "Blue" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "GRN", "#008000", "Green" }
                });

            migrationBuilder.InsertData(
                table: "sizes",
                columns: new[] { "Id", "Code", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "XXS", "Extra Extra Small", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "XS", "Extra Small", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "S", "Small", 30 },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "M", "Medium", 40 },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "L", "Large", 50 },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "XL", "Extra Large", 60 },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "XXL", "Extra Extra Large", 70 },
                    { new Guid("20000000-0000-0000-0000-000000000008"), "XXXL", "Extra Extra Extra Large", 80 }
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CategoryCode", "Name", "Description", "ParentCategoryId", "SortOrder", "Status", "CreatedAt", "CreatedBy", "UpdatedBy", "IsDelete", "DeleteByUser", "DeletedBy", "DeletedAt", "Version", "UpdatedAt" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), "CAT001", "Uncategorized", "Default category for products migrated from the legacy product schema.", null, 0, "Published", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc)), null, null, false, null, null, null, 1L, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc)) });

            migrationBuilder.Sql("""
                WITH numbered_products AS (
                    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "UpdatedAt", "Id") AS sequence_number
                    FROM "products"
                )
                UPDATE "products" AS product
                SET "ProductCode" = 'PRD' || LPAD(numbered_products.sequence_number::text, 6, '0'),
                    "Status" = CASE WHEN product."IsActive" THEN 'Published' ELSE 'Draft' END,
                    "CategoryId" = '30000000-0000-0000-0000-000000000001',
                    "CreatedAt" = COALESCE(product."UpdatedAt", TIMESTAMPTZ '2026-07-20 00:00:00+00')
                FROM numbered_products
                WHERE numbered_products."Id" = product."Id";
                """);

            migrationBuilder.Sql("""
                WITH numbered_customers AS (
                    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "UpdatedAt", "Id") AS sequence_number
                    FROM "customers"
                )
                UPDATE "customers" AS customer
                SET "CustomerCode" = 'CUS' || LPAD(numbered_customers.sequence_number::text, 6, '0'),
                    "NormalizedPhone" = regexp_replace(customer."Phone", '\D', '', 'g'),
                    "Status" = CASE WHEN customer."IsDelete" THEN 'Suspended' ELSE 'Normal' END,
                    "CreatedAt" = COALESCE(customer."UpdatedAt", TIMESTAMPTZ '2026-07-20 00:00:00+00')
                FROM numbered_customers
                WHERE numbered_customers."Id" = customer."Id";
                """);

            migrationBuilder.Sql("""
                INSERT INTO "product_variants" ("Id", "ProductId", "ColorId", "SizeId", "Sku", "Price", "Status", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDelete", "DeleteByUser", "DeletedBy", "DeletedAt", "Version")
                SELECT "Id",
                       "Id",
                       '10000000-0000-0000-0000-000000000001',
                       '20000000-0000-0000-0000-000000000004',
                       UPPER(TRIM("Sku")),
                       "Price",
                       CASE WHEN "IsActive" THEN 'Published' ELSE 'Draft' END,
                       COALESCE("CreatedAt", "UpdatedAt", TIMESTAMPTZ '2026-07-20 00:00:00+00'),
                       NULL,
                       COALESCE("UpdatedAt", TIMESTAMPTZ '2026-07-20 00:00:00+00'),
                       NULL,
                       "IsDelete",
                       "DeleteByUser",
                       "DeletedBy",
                       "DeletedAt",
                       "Version"
                FROM "products";
                """);

            migrationBuilder.Sql("""
                UPDATE "order_lines" AS order_line
                SET "ProductVariantId" = order_line."ProductId",
                    "ProductCode" = product."ProductCode",
                    "ColorCode" = 'BLK',
                    "ColorName" = 'Black',
                    "SizeCode" = 'M'
                FROM "products" AS product
                WHERE product."Id" = order_line."ProductId";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductCode",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerCode",
                table: "customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedPhone",
                table: "customers",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_products_ProductCode",
                table: "products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_Status",
                table: "products",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_OrderId_ProductVariantId",
                table: "order_lines",
                columns: new[] { "OrderId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_CustomerCode",
                table: "customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers",
                column: "NormalizedPhone",
                unique: true,
                filter: "\"NormalizedPhone\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Status",
                table: "customers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_categories_CategoryCode",
                table: "categories",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name_ParentCategoryId",
                table: "categories",
                columns: new[] { "Name", "ParentCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true,
                filter: "\"ParentCategoryId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentCategoryId",
                table: "categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Status",
                table: "categories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_colors_ColorCode",
                table: "colors",
                column: "ColorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_colors_Name",
                table: "colors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ColorId",
                table: "product_variants",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId_ColorId_SizeId",
                table: "product_variants",
                columns: new[] { "ProductId", "ColorId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_SizeId",
                table: "product_variants",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Status",
                table: "product_variants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_sizes_Code",
                table: "sizes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sizes_SortOrder",
                table: "sizes",
                column: "SortOrder",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_CategoryId",
                table: "products",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_CategoryId",
                table: "products");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "colors");

            migrationBuilder.DropTable(
                name: "sizes");

            migrationBuilder.DropIndex(
                name: "IX_products_CategoryId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_ProductCode",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_Status",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_order_lines_OrderId_ProductVariantId",
                table: "order_lines");

            migrationBuilder.DropIndex(
                name: "IX_customers_CustomerCode",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_Status",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "products");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ColorCode",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "SizeCode",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "NormalizedPhone",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "customers");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "products",
                type: "numeric(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "order_lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(96)",
                oldMaxLength: 96);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "order_lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "customers",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_products_Sku",
                table: "products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_OrderId_ProductId",
                table: "order_lines",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);
        }
    }
}
