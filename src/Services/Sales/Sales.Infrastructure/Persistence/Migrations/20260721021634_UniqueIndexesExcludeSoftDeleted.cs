using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexesExcludeSoftDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_ProductCode",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_ProductId_ColorId_SizeId",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_customers_CustomerCode",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_categories_CategoryCode",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_Name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_Name_ParentCategoryId",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "IX_products_ProductCode",
                table: "products",
                column: "ProductCode",
                unique: true,
                filter: "NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId_ColorId_SizeId",
                table: "product_variants",
                columns: new[] { "ProductId", "ColorId", "SizeId" },
                unique: true,
                filter: "NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants",
                column: "Sku",
                unique: true,
                filter: "NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_customers_CustomerCode",
                table: "customers",
                column: "CustomerCode",
                unique: true,
                filter: "NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers",
                column: "NormalizedPhone",
                unique: true,
                filter: "\"NormalizedPhone\" IS NOT NULL AND NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_categories_CategoryCode",
                table: "categories",
                column: "CategoryCode",
                unique: true,
                filter: "NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true,
                filter: "\"ParentCategoryId\" IS NULL AND NOT \"IsDelete\"");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name_ParentCategoryId",
                table: "categories",
                columns: new[] { "Name", "ParentCategoryId" },
                unique: true,
                filter: "NOT \"IsDelete\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_ProductCode",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_ProductId_ColorId_SizeId",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_customers_CustomerCode",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_NormalizedPhone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_categories_CategoryCode",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_Name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_Name_ParentCategoryId",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "IX_products_ProductCode",
                table: "products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId_ColorId_SizeId",
                table: "product_variants",
                columns: new[] { "ProductId", "ColorId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                table: "product_variants",
                column: "Sku",
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
                name: "IX_categories_CategoryCode",
                table: "categories",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true,
                filter: "\"ParentCategoryId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name_ParentCategoryId",
                table: "categories",
                columns: new[] { "Name", "ParentCategoryId" },
                unique: true);
        }
    }
}
