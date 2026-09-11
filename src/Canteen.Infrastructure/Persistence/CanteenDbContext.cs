using Canteen.Application.Common.Interfaces;
using Canteen.Domain.Entities;
using Canteen.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Infrastructure.Persistence;

public class CanteenDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public CanteenDbContext(DbContextOptions<CanteenDbContext> options) : base(options)
    {
    }

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<DailyMenuSchedule> DailyMenuSchedules => Set<DailyMenuSchedule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TokenAuditLog> TokenAuditLogs => Set<TokenAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // MenuItem configuration
        builder.Entity<MenuItem>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).IsRequired().HasMaxLength(150);
            b.Property(m => m.BasePrice).HasPrecision(18, 2);
        });

        // DailyMenuSchedule configuration
        builder.Entity<DailyMenuSchedule>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Price).HasPrecision(18, 2);
            b.HasOne(s => s.MenuItem)
                .WithMany(m => m.Schedules)
                .HasForeignKey(s => s.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(s => new { s.Date, s.MealType });

            // Optimistic Concurrency Token
            b.Property(s => s.RowVersion).IsRowVersion();
        });

        // Booking configuration
        builder.Entity<Booking>(b =>
        {
            b.HasKey(bk => bk.Id);
            b.Property(bk => bk.BookingReference).IsRequired().HasMaxLength(50);
            b.HasIndex(bk => bk.BookingReference).IsUnique();
            b.HasIndex(bk => bk.QrCodeHash);
            b.Property(bk => bk.Price).HasPrecision(18, 2);

            b.HasOne(bk => bk.Schedule)
                .WithMany(s => s.Bookings)
                .HasForeignKey(bk => bk.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(bk => bk.RowVersion).IsRowVersion();
        });

        // TokenAuditLog configuration
        builder.Entity<TokenAuditLog>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasOne(a => a.Booking)
                .WithMany(bk => bk.AuditLogs)
                .HasForeignKey(a => a.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // For relational providers that support transactions
        if (Database.IsRelational())
        {
            var transaction = await Database.BeginTransactionAsync(cancellationToken);
            return new TransactionWrapper(transaction);
        }

        return new NoOpDisposable();
    }

    private class TransactionWrapper : IDisposable
    {
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _tx;
        private bool _committed = false;

        public TransactionWrapper(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)
        {
            _tx = tx;
        }

        public void Dispose()
        {
            try
            {
                if (!_committed)
                {
                    _tx.Commit();
                    _committed = true;
                }
            }
            catch
            {
                _tx.Rollback();
            }
            finally
            {
                _tx.Dispose();
            }
        }
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
