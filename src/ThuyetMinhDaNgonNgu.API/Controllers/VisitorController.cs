
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/visitor")]
public sealed class VisitorController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public VisitorController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Record(
        [FromBody] VisitorRequest request)
    {
        if (request.VisitId == Guid.Empty)
            return BadRequest(new { message = "VisitId không hợp lệ." });

        string baseUrl = _configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Chưa cấu hình Supabase:Url.");

        string key = _configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException("Chưa cấu hình Supabase:ServiceRoleKey.");

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/rest/v1/website_visits?on_conflict=visit_id");

        message.Headers.Add("apikey", key);
        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", key);
        message.Headers.Add("Prefer", "resolution=ignore-duplicates,return=minimal");
        message.Content = JsonContent.Create(new
        {
            visit_id = request.VisitId
        });

        using var response = await _httpClient.SendAsync(message);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(502, new
            {
                message = "Không thể lưu lượt truy cập vào Supabase."
            });
        }

        return Ok(new { message = "Đã ghi nhận lượt truy cập." });
    }

    public sealed record VisitorRequest(Guid VisitId);
}
