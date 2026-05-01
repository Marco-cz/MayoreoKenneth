using MayoreoKenneth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MayoreoKenneth.Infrastructure.Persistence;

public class MayoreoKennethDbContext : DbContext
{
    public MayoreoKennethDbContext(DbContextOptions<MayoreoKennethDbContext> options) : base(options)
    {
    }

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

        // Yurguen: Configuracion de Supplier.
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers", t => t.HasComment(
                "Proveedores. Retención: no borrar filas salvo baja contractual; auditoría comercial."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.ApiBaseUrl).HasMaxLength(300);

            entity.HasData(new Supplier
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Name = "Proveedor simulado",
                Code = "SIM",
                IsActive = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // Yurguen: Configuracion de Product.
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", t => t.HasComment(
                "Catálogo propio. Retención: productos con borrado lógico (IsCatalogHidden); no purge automático de filas."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1500);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.DisplayPriceWithIva).HasPrecision(18, 2);
            entity.Property(x => x.AdminDiscountPercent).HasPrecision(9, 4);
            entity.HasIndex(x => x.Sku).IsUnique();
        });

        // Yurguen: Configuracion del mapeo entre producto interno y externo.
        modelBuilder.Entity<ProductSupplierMap>(entity =>
        {
            entity.ToTable("ProductSupplierMaps", t => t.HasComment(
                "Mapeo a id/SKU proveedor y último costo sincronizado. Retención: vinculada al producto; sin purge suelta. No persistimos cantidad de stock."));
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

        // Yurguen: Configuracion de reglas de precio por producto.
        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.ToTable("PriceRules", t => t.HasComment(
                "Reglas históricas de precio. Retención: opcional purga >24 meses si política comercial lo permite (job futuro)."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MarkupPercentage).HasPrecision(9, 4);
            entity.Property(x => x.MinimumMarginAmount).HasPrecision(18, 2);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.PriceRules)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Yurguen: Configuracion de ordenes.
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", t => t.HasComment(
                "Órdenes de venta. Retención fiscal CR: conservar años (típico 7+); NO borrar con job sin criterio legal."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerEmail).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
        });

        // Yurguen: Configuracion de lineas de orden.
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", t => t.HasComment(
                "Líneas de orden. Retención: alineada a Orders; sin borrado automático."));
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
                "Configuración global (margen, IVA, días sin stock). Retención: fila única permanente."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DefaultMarkupPercent).HasPrecision(9, 4);
            entity.Property(x => x.IvaPercentOnSale).HasPrecision(9, 4);
            entity.Property(x => x.TimeZoneId).HasMaxLength(80).IsRequired();
            entity.HasData(new StoreSettings());
        });

        modelBuilder.Entity<CatalogSyncLog>(entity =>
        {
            entity.ToTable("CatalogSyncLogs", t => t.HasComment(
                "Historial de sync de catálogo. Retención operativa: 90 días (DataRetentionHostedService)."));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(x => x.StartedAtUtc);
        });
    }
}
