using ASC.Model.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ASC.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<MasterDataKey> MasterDataKeys { get; set; }
        public DbSet<MasterDataValue> MasterDataValues { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) // 🔥 bắt buộc
        {
            Database.Migrate();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<MasterDataKey>()
                .HasKey(c => new { c.PartitionKey, c.RowKey });

            builder.Entity<MasterDataValue>()
                .HasKey(c => new { c.PartitionKey, c.RowKey });

            builder.Entity<ServiceRequest>()
                .HasKey(c => new { c.PartitionKey, c.RowKey });

            base.OnModelCreating(builder);
        }
    }
}