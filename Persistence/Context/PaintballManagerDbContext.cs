using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class PaintballManagerDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitItem> KitItems => Set<KitItem>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<FactoryOrder> FactoryOrders => Set<FactoryOrder>();
    public DbSet<FactoryOrderItem> FactoryOrderItems => Set<FactoryOrderItem>();

    public PaintballManagerDbContext(DbContextOptions<PaintballManagerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SKU).HasMaxLength(20);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Weight).HasPrecision(10, 3);
            entity.HasIndex(e => e.SKU).IsUnique();
        });

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Document).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(20);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Supplier
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactName).HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Document).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(20);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("OrderId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
        });

        // Purchase
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("PurchaseId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseItem
        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
        });

        // Kit
        modelBuilder.Entity<Kit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("KitId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // KitItem
        modelBuilder.Entity<KitItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // Shipment
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TrackingNumber).HasMaxLength(50);
            entity.Property(e => e.SuperFreteOrderId).HasMaxLength(100);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("ShipmentId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ShipmentItem
        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // InventoryMovement
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Part
        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.HasIndex(e => e.ProductId);
        });

        // FactoryOrder
        modelBuilder.Entity<FactoryOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerContact).HasMaxLength(100);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
            entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SupplierContact).HasMaxLength(100);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalSalePrice).HasPrecision(18, 2);
            entity.Property(e => e.Margin).HasPrecision(6, 2);
            entity.Property(e => e.TrackingCode).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("FactoryOrderId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FactoryOrderItem
        modelBuilder.Entity<FactoryOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.UnitSalePrice).HasPrecision(18, 2);
            entity.Property(e => e.SubtotalCost).HasPrecision(18, 2);
            entity.Property(e => e.SubtotalSalePrice).HasPrecision(18, 2);
        });
    }
}
