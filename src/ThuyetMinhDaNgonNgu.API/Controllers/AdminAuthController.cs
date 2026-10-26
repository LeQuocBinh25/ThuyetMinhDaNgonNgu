using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("AdminLogin")]
    [ProducesResponseType(typeof(AdminTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AdminTokenResponse> Login([FromBody] AdminLoginRequest request)
    {
        string expectedUsername = configuration["Admin:Username"]
            ?? throw new InvalidOperationException("Chưa cấu hình Admin:Username.");
        string expectedPassword = configuration["Admin:Password"]
            ?? throw new InvalidOperationException("Chưa cấu hình Admin:Password.");
        string signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Chưa cấu hình Jwt:SigningKey.");

        if (!FixedTimeEquals(request.Username, expectedUsername) ||
            !FixedTimeEquals(request.Password, expectedPassword))
        {
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(4);
        string issuer = configuration["Jwt:Issuer"] ?? "VietGuide";
        string audience = configuration["Jwt:Audience"] ?? "VietGuideAdmin";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            new[]
            {
                new Claim(ClaimTypes.Name, expectedUsername),
                new Claim(ClaimTypes.Role, "Admin")
            },
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return Ok(new AdminTokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt));
    }

    private static bool FixedTimeEquals(string provided, string expected)
    {
        byte[] providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}
