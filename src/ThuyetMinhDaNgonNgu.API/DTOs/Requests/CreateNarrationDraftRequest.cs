using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

public sealed class CreateNarrationDraftRequest
{
    [Range(1, int.MaxValue)]
    public int LocationId { get; init; }

    [Required, StringLength(10, MinimumLength = 2)]
    public string LanguageCode { get; init; } = string.Empty;
}
