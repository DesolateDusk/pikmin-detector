using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.View;
using Microsoft.AspNetCore.Mvc;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Controllers;

[ApiController]
[Route("api/spot-imports")]
public sealed class SpotImportsController(ISpotImportService imports) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SpotImportVM>> Import([FromBody] SpotImportInput request)
    {
        return Ok(await imports.ImportAsync(request));
    }
}
