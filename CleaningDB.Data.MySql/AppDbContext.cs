using Microsoft.EntityFrameworkCore;
using Domain.Cleaning;
using Domain.Statistics;

namespace CleaningDB.Data.SqlServer
{
    public class AppDbContext : DbContext
    {
        public DbSet<City> Cities { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<RequestService> RequestServices { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=localhost;Database=CleaningDB;Trusted_Connection=True;TrustServerCertificate=true;",
                    options => options.EnableRetryOnFailure());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.City)
                .WithMany()
                .HasForeignKey(r => r.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.Cleaner)
                .WithMany()
                .HasForeignKey(r => r.CleanerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.Payment)
                .WithOne(p => p.Request)
                .HasForeignKey<Request>(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Request)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestService>()
                .HasKey(rs => rs.Id);

            modelBuilder.Entity<RequestService>()
                .HasOne(rs => rs.Request)
                .WithMany()
                .HasForeignKey(rs => rs.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestService>()
                .HasOne(rs => rs.Service)
                .WithMany()
                .HasForeignKey(rs => rs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<Request>()
                .HasIndex(r => r.CleaningDate);

            modelBuilder.Entity<Request>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique();
        }
    }
}