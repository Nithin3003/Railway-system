using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Data
{
    // Inheriting from IdentityDbContext<ApplicationUser> adds 7 Identity tables automatically
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Your Railway Business Tables
        public DbSet<Train> Trains { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }
        public DbSet<TrainStation> TrainStations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANT: Call the base method to configure Identity tables first
            base.OnModelCreating(modelBuilder);

            // One-to-Many: Train has many Bookings
            modelBuilder.Entity<Booking>()
                .HasOne<Train>()
                .WithMany()
                .HasForeignKey(b => b.TrainId);

            modelBuilder.Entity<Booking>()
                .HasOne<Payment>()
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many: Train has many route stations
            modelBuilder.Entity<TrainStation>()
                .HasOne<Train>()
                .WithMany()
                .HasForeignKey(ts => ts.TrainId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique Index for Booking Reference (PNR)
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.PNR)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.OrderReference)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.RazorpayOrderId)
                .IsUnique();

            // Unique Index for Train ID (like "02603")
            modelBuilder.Entity<Train>()
                .HasIndex(t => t.Id)
                .IsUnique();

            // Route guardrails: one stop-order per train and one station name per train
            modelBuilder.Entity<TrainStation>()
                .HasIndex(ts => new { ts.TrainId, ts.StopOrder })
                .IsUnique();

            modelBuilder.Entity<TrainStation>()
                .HasIndex(ts => new { ts.TrainId, ts.StationName })
                .IsUnique();

            modelBuilder.Entity<TrainStation>()
                .HasIndex(ts => new { ts.TrainId, ts.StationCode })
                .IsUnique();

            // Configure decimal precision for Train Fare
            modelBuilder.Entity<Train>()
                .Property(t => t.Fare)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TrainStation>()
                .Property(ts => ts.FareFromStart)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.Fare)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.TotalAmount)
                .HasPrecision(18, 2);
        }
    }
}