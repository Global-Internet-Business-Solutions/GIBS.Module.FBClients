using GIBS.Module.FBClients.Migrations.EntityBuilders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Oqtane.Databases.Interfaces;
using Oqtane.Migrations;
using Oqtane.Migrations.EntityBuilders;

namespace GIBS.Module.FBClients.Inventory.Migrations.EntityBuilders
{
    public class FBProductCategoryEntityBuilder : FBClientsLegacyEntityBuilder<FBProductCategoryEntityBuilder>
    {
        public FBProductCategoryEntityBuilder(MigrationBuilder migration, IDatabase database) : base(migration, database)
        {
            EntityTableName = "GIBSFBProductCategory";
            PrimaryKey = new("PK_GIBSFBProductCategory", x => x.ProductCategoryId);
            ForeignKeys.Add(new("FK_GIBSFBProductCategory_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        public OperationBuilder<AddColumnOperation> ProductCategoryId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }

        protected override FBProductCategoryEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ProductCategoryId = AddAutoIncrementColumn(table, "ProductCategoryId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            AddIntegerColumn(table, "LegacyProductCategoryId", nullable: true);
            AddIntegerColumn(table, "LegacyPortalId", nullable: true);
            AddLegacyAuditColumns(table);
            AddStringColumn(table, "ProductCategory", 50, nullable: true);
            AddBooleanColumn(table, "IsActive", nullable: false, defaultValue: true);
            AddIntegerColumn(table, "SortOrder", nullable: true);
            AddMaxStringColumn(table, "OrderingInstructions", nullable: true);
            return this;
        }
    }

    public class FBProductEntityBuilder : FBClientsLegacyEntityBuilder<FBProductEntityBuilder>
    {
        public FBProductEntityBuilder(MigrationBuilder migration, IDatabase database) : base(migration, database)
        {
            EntityTableName = "GIBSFBProducts";
            PrimaryKey = new("PK_GIBSFBProducts", x => x.ProductId);
            ForeignKeys.Add(new("FK_GIBSFBProducts_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
            ForeignKeys.Add(new("FK_GIBSFBProducts_Category", x => x.ProductCategoryId, "GIBSFBProductCategory", "ProductCategoryId", ReferentialAction.NoAction));
        }

        public OperationBuilder<AddColumnOperation> ProductId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> ProductCategoryId { get; set; }

        protected override FBProductEntityBuilder BuildTable(ColumnsBuilder table)
        {
            ProductId = AddAutoIncrementColumn(table, "ProductId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            AddIntegerColumn(table, "LegacyProductId", nullable: true);
            AddIntegerColumn(table, "LegacyPortalId", nullable: true);
            AddLegacyAuditColumns(table);
            AddStringColumn(table, "ProductName", 100, nullable: true);
            AddDecimalColumn(table, "CasePrice", 19, 4, nullable: true, defaultValue: 0);
            AddIntegerColumn(table, "CaseCount", nullable: true, defaultValue: 0);
            ProductCategoryId = AddIntegerColumn(table, "ProductCategoryId", nullable: true);
            AddDecimalColumn(table, "CaseWeight", 18, 2, nullable: true);
            AddBooleanColumn(table, "IsActive", nullable: false, defaultValue: true);
            AddIntegerColumn(table, "Limit", nullable: true, defaultValue: 1);
            AddStringColumn(table, "LimitQuantities", 50, nullable: true);
            return this;
        }
    }

    public class FBSupplierEntityBuilder : FBClientsLegacyEntityBuilder<FBSupplierEntityBuilder>
    {
        public FBSupplierEntityBuilder(MigrationBuilder migration, IDatabase database) : base(migration, database)
        {
            EntityTableName = "GIBSFBSuppliers";
            PrimaryKey = new("PK_GIBSFBSuppliers", x => x.SupplierId);
            ForeignKeys.Add(new("FK_GIBSFBSuppliers_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
        }

        public OperationBuilder<AddColumnOperation> SupplierId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }

        protected override FBSupplierEntityBuilder BuildTable(ColumnsBuilder table)
        {
            SupplierId = AddAutoIncrementColumn(table, "SupplierId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            AddIntegerColumn(table, "LegacySupplierId", nullable: true);
            AddIntegerColumn(table, "LegacyPortalId", nullable: true);
            AddLegacyAuditColumns(table);
            AddStringColumn(table, "SupplierName", 50, nullable: true);
            AddBooleanColumn(table, "GBFB", nullable: false, defaultValue: false);
            AddStringColumn(table, "Address", 50, nullable: true);
            AddStringColumn(table, "City", 50, nullable: true);
            AddStringColumn(table, "State", 50, nullable: true);
            AddStringColumn(table, "Zip", 50, nullable: true);
            AddStringColumn(table, "SupplierPhone", 50, nullable: true);
            AddStringColumn(table, "Salesman", 50, nullable: true);
            AddStringColumn(table, "SalesmanPhone", 50, nullable: true);
            AddBooleanColumn(table, "IsActive", nullable: false, defaultValue: true);
            return this;
        }
    }

    public class FBInvoiceEntityBuilder : FBClientsLegacyEntityBuilder<FBInvoiceEntityBuilder>
    {
        public FBInvoiceEntityBuilder(MigrationBuilder migration, IDatabase database) : base(migration, database)
        {
            EntityTableName = "GIBSFBInvoices";
            PrimaryKey = new("PK_GIBSFBInvoices", x => x.InvoiceId);
            ForeignKeys.Add(new("FK_GIBSFBInvoices_Module", x => x.ModuleId, "Module", "ModuleId", ReferentialAction.Cascade));
            ForeignKeys.Add(new("FK_GIBSFBInvoices_Supplier", x => x.SupplierId, "GIBSFBSuppliers", "SupplierId", ReferentialAction.NoAction));
        }

        public OperationBuilder<AddColumnOperation> InvoiceId { get; set; }
        public OperationBuilder<AddColumnOperation> ModuleId { get; set; }
        public OperationBuilder<AddColumnOperation> SupplierId { get; set; }

        protected override FBInvoiceEntityBuilder BuildTable(ColumnsBuilder table)
        {
            InvoiceId = AddAutoIncrementColumn(table, "InvoiceId");
            ModuleId = AddIntegerColumn(table, "ModuleId");
            AddIntegerColumn(table, "LegacyInvoiceId", nullable: true);
            AddIntegerColumn(table, "LegacyPortalId", nullable: true);
            AddLegacyAuditColumns(table);
            AddStringColumn(table, "InvoiceNumber", 50, nullable: true, unicode: true, defaultValue: "0");
            AddDateTimeColumn(table, "InvoiceDate", nullable: true);
            SupplierId = AddIntegerColumn(table, "SupplierId", nullable: true);
            AddStringColumn(table, "Organization", 50, nullable: true, unicode: false);
            return this;
        }
    }

    public class FBLineItemEntityBuilder : AuditableBaseEntityBuilder<FBLineItemEntityBuilder>
    {
        public FBLineItemEntityBuilder(MigrationBuilder migration, IDatabase database) : base(migration, database)
        {
            EntityTableName = "GIBSFBLineItems";
            PrimaryKey = new("PK_GIBSFBLineItems", x => x.LineItemId);
            ForeignKeys.Add(new("FK_GIBSFBLineItems_Invoice", x => x.InvoiceId, "GIBSFBInvoices", "InvoiceId", ReferentialAction.NoAction));
        }

        public OperationBuilder<AddColumnOperation> LineItemId { get; set; }
        public OperationBuilder<AddColumnOperation> InvoiceId { get; set; }

        protected override FBLineItemEntityBuilder BuildTable(ColumnsBuilder table)
        {
            LineItemId = AddAutoIncrementColumn(table, "LineItemId");
            AddIntegerColumn(table, "LegacyLineItemId", nullable: true);
            InvoiceId = AddIntegerColumn(table, "InvoiceId");
            AddIntegerColumn(table, "ProductId");
            AddIntegerColumn(table, "Cases", nullable: true, defaultValue: 0);
            AddIntegerColumn(table, "CountPerCase", nullable: true, defaultValue: 0);
            AddDecimalColumn(table, "PricePerCase", 19, 4, nullable: true, defaultValue: 0);
            AddDecimalColumn(table, "WeightPerCase", 18, 2, nullable: true);
            AddDecimalColumn(table, "GIBSFBLineItems", 18, 2, nullable: true);
            AddStringColumn(table, "ReportType", 20, nullable: true, unicode: false);
            AddAuditableColumns(table);
            return this;
        }
    }
}
