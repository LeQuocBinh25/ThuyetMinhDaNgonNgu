using System.ComponentModel.DataAnnotations;

namespace ThuyetMinhDaNgonNgu.API.DTOs.Requests;

public sealed class AdminLoginRequest
{
    [Required, StringLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; init; } = string.Empty;
}
