using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

/// <summary>Tên hiển thị mới cho một ngôn ngữ hiện có.</summary>
public sealed record UpdateLanguageRequest
{
    [Required(ErrorMessage = "name là bắt buộc.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "name phải có từ 1 đến 100 ký tự.")]
    public required string Name { get; init; }
}
