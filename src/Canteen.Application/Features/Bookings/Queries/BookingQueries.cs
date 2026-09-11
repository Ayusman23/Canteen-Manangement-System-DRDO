using Canteen.Application.Common.Exceptions;
using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Canteen.Application.Features.Bookings.Queries;

public record GetUserBookingsQuery(Guid? UserId, string? UserEmail) : IRequest<List<BookingDto>>;
public record GetBookingByTokenQuery(string Token) : IRequest<BookingDto>;
public record GetAllBookingsQuery(DateOnly? Date = null) : IRequest<List<BookingDto>>;

public class BookingQueriesHandler :
    IRequestHandler<GetUserBookingsQuery, List<BookingDto>>,
    IRequestHandler<GetBookingByTokenQuery, BookingDto>,
    IRequestHandler<GetAllBookingsQuery, List<BookingDto>>
{
    private readonly IApplicationDbContext _context;

    public BookingQueriesHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BookingDto>> Handle(GetUserBookingsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Bookings.AsNoTracking();

        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            query = query.Where(b => b.UserId == request.UserId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(request.UserEmail))
        {
            query = query.Where(b => b.UserEmail.ToLower() == request.UserEmail.ToLower());
        }

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapDto).ToList();
    }

    public async Task<BookingDto> Handle(GetBookingByTokenQuery request, CancellationToken cancellationToken)
    {
        var cleanToken = request.Token.Trim();
        if (cleanToken.Contains('|'))
        {
            cleanToken = cleanToken.Split('|')[0];
        }

        var booking = await _context.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingReference == cleanToken || b.QrCodeHash == cleanToken, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException("Booking Token", request.Token);
        }

        return MapDto(booking);
    }

    public async Task<List<BookingDto>> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Bookings.AsNoTracking();

        if (request.Date.HasValue)
        {
            query = query.Where(b => b.ScheduledMealDate == request.Date.Value);
        }

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapDto).ToList();
    }

    private static BookingDto MapDto(Canteen.Domain.Entities.Booking b) => new(
        Id: b.Id,
        BookingReference: b.BookingReference,
        UserId: b.UserId,
        UserName: b.UserName,
        UserEmail: b.UserEmail,
        EmployeeCode: b.EmployeeCode,
        ScheduleId: b.ScheduleId,
        MealType: b.MealType,
        MealName: b.MealName,
        Price: b.Price,
        ScheduledMealDate: b.ScheduledMealDate,
        Status: b.Status,
        BookingDate: b.BookingDate,
        DispensedAt: b.DispensedAt,
        DispensedByUserId: b.DispensedByUserId,
        QrCodeHash: b.QrCodeHash
    );
}
