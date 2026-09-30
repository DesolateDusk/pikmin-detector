using Microsoft.AspNetCore.Mvc;
using PikminDetector.Api.Models;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Controllers;

[ApiController]
[Route("api/spots")]
public sealed class SpotsController : ControllerBase
{
    private readonly ISpotService spots;

    public SpotsController(ISpotService spots)
    {
        this.spots = spots;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpotVM>>> SearchArea([FromQuery] AreaSpotInput input)
    {
        var results = await spots.SearchAreaAsync(input);
        return Ok(results);
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IReadOnlyList<SpotVM>>> FindNearby([FromQuery] NearbySpotInput input)
    {
        var results = await spots.FindNearbyAsync(input);
        return Ok(results);
    }
}
