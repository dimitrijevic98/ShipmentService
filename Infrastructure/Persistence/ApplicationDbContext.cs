using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentEvent> ShipmentEvents { get; set; }
        public DbSet<ShipmentDocument> ShipmentDocuments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ReferenceNumber).IsUnique();

                entity.Property(e => e.SenderName).IsRequired();
                entity.Property(e => e.RecipientName).IsRequired();

                entity.Property(e => e.State).HasConversion<string>();

                entity.HasMany(e => e.ShipmentEvents)
                    .WithOne(e => e.Shipment)
                    .HasForeignKey(e => e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);


                entity.HasOne(e => e.Document)
                    .WithOne(e => e.Shipment)
                    .HasForeignKey<ShipmentDocument>(d => d.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}