using Microsoft.AspNetCore.Mvc;
using PikminDetector.Api.Models;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Controllers;

[ApiController]
[Route("api/spot-imports")]
public sealed class SpotImportsController(SpotImportService imports) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SpotImportVM>> Import([FromBody] SpotImportRequest request)
    {
        return Ok(await imports.ImportAsync(request));
    }
}
