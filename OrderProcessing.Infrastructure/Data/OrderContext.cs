using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace OrderProcessing.Infrastructure.Data;

public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.OwnsOne(e => e.Payment, payment =>
            {
                payment.Property(p => p.PaymentId).HasMaxLength(100);
                payment.Property(p => p.PaymentMethod).HasMaxLength(50);
                payment.Property(p => p.Status).HasConversion<string>();
            });

            entity.OwnsOne(e => e.Shipping, shipping =>
            {
                shipping.Property(s => s.TrackingNumber).HasMaxLength(100);
                shipping.Property(s => s.Carrier).HasMaxLength(50);
                shipping.Property(s => s.Status).HasConversion<string>();

                shipping.OwnsOne(s => s.DeliveryAddress, address =>
                {
                    address.Property(a => a.Street).HasMaxLength(200);
                    address.Property(a => a.City).HasMaxLength(100);
                    address.Property(a => a.State).HasMaxLength(50);
                    address.Property(a => a.ZipCode).HasMaxLength(20);
                    address.Property(a => a.Country).HasMaxLength(50);
                });
            });
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(e => e.TotalPrice); // Computed property
        });

        base.OnModelCreating(modelBuilder);
    }
}