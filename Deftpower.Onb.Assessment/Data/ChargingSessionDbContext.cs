using Deftpower.Onb.Assessment.Models;
using Microsoft.EntityFrameworkCore;

namespace Deftpower.Onb.Assessment.Data;

public class ChargingSessionDbContext(DbContextOptions<ChargingSessionDbContext> options) : DbContext(options)
{
    public DbSet<ChargingSession> ChargingSessions => Set<ChargingSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChargingSession>(entity =>
        {
            // The primary key already makes SessionId unique and indexed
            entity.HasKey(x => x.SessionId);
            entity.Property(x => x.SessionId).IsRequired().HasMaxLength(36); // Guid string length
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(36);
            entity.Property(x => x.ChargePointId).IsRequired().HasMaxLength(36);
            entity.Property(x => x.StartTime).IsRequired();
            entity.Property(x => x.EndTime).IsRequired(false);
            entity.Property(x => x.EnergyKwh).IsRequired();
            entity.HasIndex(x => x.UserId);
        });
    }
}
