using Microsoft.EntityFrameworkCore;
using Oqtane.Databases.Interfaces;
using GIBS.Module.FBClients.Inventory.Models;

namespace GIBS.Module.FBClients.Repository
{
    internal static class InventoryConfiguration
    {
        internal static void Configure(ModelBuilder builder, IDatabase database)
        {
            var categories = builder.Entity<FBProductCategory>();
            categories.ToTable(database.RewriteName("GIBSFBProductCategory"));
            categories.HasOne<Oqtane.Models.Module>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
            categories.Property(x => x.IsActive).HasDefaultValue(true).ValueGeneratedNever();

            var products = builder.Entity<FBProduct>();
            products.ToTable(database.RewriteName("GIBSFBProducts"));
            products.HasOne<Oqtane.Models.Module>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
            products.HasOne<FBProductCategory>().WithMany().HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.NoAction);
            products.Property(x => x.CasePrice).HasPrecision(19, 4).HasDefaultValue(0m).ValueGeneratedNever();
            products.Property(x => x.CaseCount).HasDefaultValue(0).ValueGeneratedNever();
            products.Property(x => x.CaseWeight).HasPrecision(18, 2);
            products.Property(x => x.IsActive).HasDefaultValue(true).ValueGeneratedNever();
            products.Property(x => x.Limit).HasDefaultValue(1).ValueGeneratedNever();

            var suppliers = builder.Entity<FBSupplier>();
            suppliers.ToTable(database.RewriteName("GIBSFBSuppliers"));
            suppliers.HasOne<Oqtane.Models.Module>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
            suppliers.Property(x => x.GBFB).HasDefaultValue(false).ValueGeneratedNever();
            suppliers.Property(x => x.IsActive).HasDefaultValue(true).ValueGeneratedNever();

            var invoices = builder.Entity<FBInvoice>();
            invoices.ToTable(database.RewriteName("GIBSFBInvoices"));
            invoices.HasOne<Oqtane.Models.Module>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
            invoices.HasOne<FBSupplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.NoAction);
            invoices.Property(x => x.InvoiceNumber).HasDefaultValue("0").ValueGeneratedNever();
            invoices.Property(x => x.Organization).IsUnicode(false);

            var lineItems = builder.Entity<FBLineItem>();
            lineItems.ToTable(database.RewriteName("GIBSFBLineItems"));
            lineItems.HasOne<FBInvoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
            lineItems.Property(x => x.Cases).HasDefaultValue(0).ValueGeneratedNever();
            lineItems.Property(x => x.CountPerCase).HasDefaultValue(0).ValueGeneratedNever();
            lineItems.Property(x => x.PricePerCase).HasPrecision(19, 4).HasDefaultValue(0m).ValueGeneratedNever();
            lineItems.Property(x => x.WeightPerCase).HasPrecision(18, 2);
            lineItems.Property(x => x.GIBSFBLineItems).HasPrecision(18, 2);
            lineItems.Property(x => x.ReportType).IsUnicode(false);
        }
    }
}
