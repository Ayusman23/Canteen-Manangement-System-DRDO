using Canteen.Application.Common.Interfaces;
using Canteen.Application.DTOs;
using MediatR;

namespace Canteen.Application.Features.Menu.Queries;

public record GetDailyMenuQuery(DateOnly? Date = null) : IRequest<WeeklyMenuDayDto?>;
public record GetLegacyTodayMenuQuery : IRequest<LegacyMenuDayDto?>;

public class GetDailyMenuQueryHandler : 
    IRequestHandler<GetDailyMenuQuery, WeeklyMenuDayDto?>,
    IRequestHandler<GetLegacyTodayMenuQuery, LegacyMenuDayDto?>
{
    private readonly ISender _mediator;

    public GetDailyMenuQueryHandler(ISender mediator)
    {
        _mediator = mediator;
    }

    public async Task<WeeklyMenuDayDto?> Handle(GetDailyMenuQuery request, CancellationToken cancellationToken)
    {
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var weekly = await _mediator.Send(new GetWeeklyMenuScheduleQuery(targetDate), cancellationToken);

        return weekly.FirstOrDefault(w => w.Date == targetDate || w.Day.Equals(targetDate.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LegacyMenuDayDto?> Handle(GetLegacyTodayMenuQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var legacyWeekly = await _mediator.Send(new GetLegacyWeeklyMenuQuery(), cancellationToken);

        return legacyWeekly.FirstOrDefault(w => w.Day.Equals(today.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}
