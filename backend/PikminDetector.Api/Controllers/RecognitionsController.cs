using PikminDetector.Api.Models.View;
using Microsoft.AspNetCore.Mvc;
using PikminDetector.Api.Lib.CustomException;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Controllers;

[ApiController]
[Route("api/recognitions")]
public sealed class RecognitionsController : ControllerBase
{
    private readonly IRecognitionService recognition;

    public RecognitionsController(IRecognitionService recognition)
    {
        this.recognition = recognition;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_500_000)]
    public async Task<ActionResult<RecognitionVM>> Recognize(IFormFile? image)
    {
        if (image is null)
        {
            throw new CommonException(StatusCodes.Status400BadRequest, "image is required.");
        }

        await using var imageStream = image.OpenReadStream();
        var result = await recognition.RecognizeAsync(
            imageStream,
            image.ContentType,
            image.Length);
        return Ok(result);
    }
}
