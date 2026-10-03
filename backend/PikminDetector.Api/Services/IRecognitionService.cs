using PikminDetector.Api.Models.View;

namespace PikminDetector.Api.Services;

public interface IRecognitionService
{
    Task<RecognitionVM> RecognizeAsync(Stream image, string contentType, long length);
}
