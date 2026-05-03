using ExhaTechStore.Domain;
using ExhaTechStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Infrastructure.Persistence;

public class ExhaTechStoreDbContext : DbContext
{
    public ExhaTechStoreDbContext(DbContextOptions<ExhaTechStoreDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSupplierMap> ProductSupplierMaps => Set<ProductSupplierMap>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<CatalogSyncLog> CatalogSyncLogs => Set<CatalogSyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Yurguen: Tenant SaaS (slug único para URL).
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants", t => t.HasComment(
                "Clientes tienda SaaS. Retención: registros contractuales; borrado lógico preferido."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();

            entity.HasData(
                new Tenant
                {
                    Id = TenantSeedIds.MayoreoClientTenant,
                    Slug = "mayoreokenneth",
                    DisplayName = "MayoreoKenneth",
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Tenant
                {
                    Id = TenantSeedIds.NinaDesignsTenant,
                    Slug = "ninadesigns",
                    DisplayName = "NinaDesigns",
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers", t => t.HasComment(
                "Proveedores por tenant. Retención: contractual."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ApiBaseUrl).HasMaxLength(300);
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Suppliers)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new Supplier
                {
                    Id = TenantSeedIds.MayoreoSupplier,
                    TenantId = TenantSeedIds.MayoreoClientTenant,
                    Name = "Proveedor simulado",
                    Code = "SIM",
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Supplier
                {
                    Id = TenantSeedIds.NinaSupplier,
                    TenantId = TenantSeedIds.NinaDesignsTenant,
                    Name = "Proveedor simulado",
                    Code = "SIM",
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", t => t.HasComment(
                "Catálogo por tenant. Inventario: SupplierManaged | TenantManaged | Hybrid."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1500);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.DisplayPriceWithIva).HasPrecision(18, 2);
            entity.Property(x => x.AdminDiscountPercent).HasPrecision(9, 4);
            entity.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductSupplierMap>(entity =>
        {
            entity.ToTable("ProductSupplierMaps", t => t.HasComment(
                "Mapeo proveedor. No persistimos cantidad; modo inventario en Product."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SupplierProductId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SupplierSku).HasMaxLength(80);
            entity.Property(x => x.LastKnownCost).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.ProductId, x.SupplierId }).IsUnique();

            entity.HasOne(x => x.Product)
                .WithMany(x => x.SupplierMappings)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Supplier)
                .WithMany(x => x.ProductMappings)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.ToTable("PriceRules", t => t.HasComment(
                "Reglas históricas de precio."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MarkupPercentage).HasPrecision(9, 4);
            entity.Property(x => x.MinimumMarginAmount).HasPrecision(18, 2);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.PriceRules)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", t => t.HasComment("Órdenes por tenant."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerEmail).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", t => t.HasComment("Líneas de orden."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoreSettings>(entity =>
        {
            entity.ToTable("StoreSettings", t => t.HasComment(
                "Config por tenant (margen, IVA, sync). Una fila por TenantId."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DefaultMarkupPercent).HasPrecision(9, 4);
            entity.Property(x => x.IvaPercentOnSale).HasPrecision(9, 4);
            entity.Property(x => x.TimeZoneId).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.TenantId).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.StoreSettingsRows)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new StoreSettings
                {
                    Id = TenantSeedIds.MayoreoStoreSettings,
                    TenantId = TenantSeedIds.MayoreoClientTenant,
                    DefaultMarkupPercent = 40m,
                    IvaPercentOnSale = 13m,
                    UnavailableHideAfterDays = 21,
                    CatalogSyncHourLocal = 2,
                    TimeZoneId = "America/Costa_Rica",
                    UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new StoreSettings
                {
                    Id = TenantSeedIds.NinaStoreSettings,
                    TenantId = TenantSeedIds.NinaDesignsTenant,
                    DefaultMarkupPercent = 40m,
                    IvaPercentOnSale = 13m,
                    UnavailableHideAfterDays = 21,
                    CatalogSyncHourLocal = 3,
                    TimeZoneId = "America/Costa_Rica",
                    UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        });

        modelBuilder.Entity<CatalogSyncLog>(entity =>
        {
            entity.ToTable("CatalogSyncLogs", t => t.HasComment(
                "Historial sync por tenant. Retención: 90 días."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(x => x.StartedAtUtc);
            entity.HasIndex(x => x.TenantId);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
