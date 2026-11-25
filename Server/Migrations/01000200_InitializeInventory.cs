using GIBS.Module.FBClients.Inventory.Migrations.EntityBuilders;
using GIBS.Module.FBClients.Repository;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;

namespace GIBS.Module.FBClients.Migrations
{
    [DbContext(typeof(FBClientsContext))]
    [Migration("GIBS.Module.FBClients.01.00.02.00")]
    public class InitializeInventory(IDatabase database) : MultiDatabaseMigration(database)
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var categories = new FBProductCategoryEntityBuilder(migrationBuilder, ActiveDatabase);
            categories.Create();
            categories.AddIndex("IX_GIBSFBProductCategory_ModuleId", "ModuleId");

            var products = new FBProductEntityBuilder(migrationBuilder, ActiveDatabase);
            products.Create();
            products.AddIndex("IX_GIBSFBProducts_ModuleId", "ModuleId");
            products.AddIndex("IX_GIBSFBProducts_ProductCategoryId", "ProductCategoryId");

            var suppliers = new FBSupplierEntityBuilder(migrationBuilder, ActiveDatabase);
            suppliers.Create();
            suppliers.AddIndex("IX_GIBSFBSuppliers_ModuleId", "ModuleId");

            var invoices = new FBInvoiceEntityBuilder(migrationBuilder, ActiveDatabase);
            invoices.Create();
            invoices.AddIndex("IX_GIBSFBInvoices_ModuleId", "ModuleId");
            invoices.AddIndex("IX_GIBSFBInvoices_SupplierId", "SupplierId");

            var lineItems = new FBLineItemEntityBuilder(migrationBuilder, ActiveDatabase);
            lineItems.Create();
            lineItems.AddIndex("IX_GIBSFBLineItems_InvoiceId", "InvoiceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            new FBLineItemEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBInvoiceEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBSupplierEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBProductEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
            new FBProductCategoryEntityBuilder(migrationBuilder, ActiveDatabase).Drop();
        }
    }
}
