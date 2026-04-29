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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Yurguen: Configuracion de Supplier.
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.ApiBaseUrl).HasMaxLength(300);
        });

        // Yurguen: Configuracion de Product.
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1500);
            entity.HasIndex(x => x.Sku).IsUnique();
        });

        // Yurguen: Configuracion del mapeo entre producto interno y externo.
        modelBuilder.Entity<ProductSupplierMap>(entity =>
        {
            entity.ToTable("ProductSupplierMaps");
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
            entity.ToTable("PriceRules");
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
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerEmail).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
        });

        // Yurguen: Configuracion de lineas de orden.
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
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
    }
}
