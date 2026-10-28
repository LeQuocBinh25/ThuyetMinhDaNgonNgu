using Microsoft.AspNetCore.Mvc;
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

    // Ghi nhận một lượt truy cập: tăng visit_count của đúng visitorId.
    [HttpPost]
    public async Task<IActionResult> Record(
        [FromBody] VisitorRequest request)
    {
        if (request == null || request.VisitorId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "visitorId không hợp lệ."
            });
        }

        string baseUrl = _configuration["Supabase:Url"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Supabase:Url.");

        string key = _configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Supabase:ServiceRoleKey.");

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/rest/v1/rpc/record_visit");

        message.Headers.Add("apikey", key);
        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", key);

        message.Content = JsonContent.Create(new
        {
            p_visitor_id = request.VisitorId
        });

        using var response = await _httpClient.SendAsync(message);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            return StatusCode(502, new
            {
                message = "Không thể lưu lượt truy cập vào Supabase.",
                detail = error
            });
        }

        return Ok(new
        {
            message = "Đã ghi nhận lượt truy cập."
        });
    }

    public sealed record VisitorRequest(Guid VisitorId);
}