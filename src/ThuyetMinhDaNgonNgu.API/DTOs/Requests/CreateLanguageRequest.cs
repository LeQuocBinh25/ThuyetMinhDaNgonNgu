using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

/// <summary>Mã và tên hiển thị của ngôn ngữ mới.</summary>
public sealed record CreateLanguageRequest
{
    [Required(ErrorMessage = "code là bắt buộc.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "code phải có từ 2 đến 10 ký tự.")]
    [RegularExpression("^[a-zA-Z0-9-]{2,10}$", ErrorMessage = "code chỉ được chứa chữ cái, chữ số và dấu gạch ngang.")]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "name là bắt buộc.")]
    [StringLength(100, ErrorMessage = "name không được vượt quá 100 ký tự.")]
    public string Name { get; init; } = string.Empty;
}
