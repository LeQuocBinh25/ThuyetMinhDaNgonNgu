using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

public sealed class PublishNarrationRequest
{
    [Required, StringLength(50000, MinimumLength = 1)]
    public string Text { get; init; } = string.Empty;
}
