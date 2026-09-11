using Microsoft.EntityFrameworkCore;
using Canteen.Domain.Entities;

namespace Canteen.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<MenuItem> MenuItems { get; }
    DbSet<DailyMenuSchedule> DailyMenuSchedules { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<TokenAuditLog> TokenAuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
