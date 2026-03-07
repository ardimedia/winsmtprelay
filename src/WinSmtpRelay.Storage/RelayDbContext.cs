using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Storage;

public class RelayDbContext(DbContextOptions<RelayDbContext> options) : DbContext(options)
{
    public DbSet<QueuedMessage> QueuedMessages => Set<QueuedMessage>();
    public DbSet<DeliveryLog> DeliveryLogs => Set<DeliveryLog>();
    public DbSet<RelayUser> RelayUsers => Set<RelayUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QueuedMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextRetryUtc);
            entity.HasIndex(e => e.MessageId);
            entity.Property(e => e.Sender).HasMaxLength(320);
            entity.Property(e => e.MessageId).HasMaxLength(255);
            entity.Property(e => e.SourceIp).HasMaxLength(45);
            entity.Property(e => e.AuthenticatedUser).HasMaxLength(255);
        });

        modelBuilder.Entity<DeliveryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.QueuedMessageId);
            entity.HasIndex(e => e.TimestampUtc);
            entity.Property(e => e.Recipient).HasMaxLength(320);
            entity.Property(e => e.StatusCode).HasMaxLength(10);
            entity.Property(e => e.RemoteServer).HasMaxLength(255);
        });

        modelBuilder.Entity<RelayUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(255);
        });
    }
}
