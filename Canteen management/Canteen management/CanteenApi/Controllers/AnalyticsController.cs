using Canteen.Application.Features.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanteenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly ISender _mediator;

    public AnalyticsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = "CanteenManager")]
    [HttpGet]
    public async Task<IActionResult> GetAnalytics()
    {
        var result = await _mediator.Send(new GetCanteenAnalyticsQuery());
        return Ok(result);
    }
}
