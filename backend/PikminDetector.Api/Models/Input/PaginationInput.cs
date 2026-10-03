using System.ComponentModel.DataAnnotations;

namespace PikminDetector.Api.Models.Input;

public abstract class PaginationInput
{
    [Range(1, 100)]
    public int Limit { get; set; } = 50;
}
