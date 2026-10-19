using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

public sealed class CreateNarrationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "locationId phải là số nguyên dương.")]
    public int LocationId { get; init; }

    [Required(ErrorMessage = "languageCode là bắt buộc.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "languageCode phải có từ 2 đến 10 ký tự.")]
    public string LanguageCode { get; init; } = string.Empty;
}
