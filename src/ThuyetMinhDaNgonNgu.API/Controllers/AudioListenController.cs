using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/audio-listens")]
public class AudioListenController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AudioListenController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> RecordListen(
        [FromBody] AudioListenRequest request)
    {
        if (request == null || request.ListenId == Guid.Empty)
        {
            return BadRequest(new { message = "listenId không hợp lệ." });
        }

        var supabaseUrl = _configuration["Supabase:Url"];
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];

        if (string.IsNullOrWhiteSpace(supabaseUrl) ||
            string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            return StatusCode(500, new
            {
                message = "Chưa cấu hình Supabase."
            });
        }

        var client = _httpClientFactory.CreateClient();
        var baseUrl = supabaseUrl.TrimEnd('/');

        // Chuẩn hóa: Guid.Empty hoặc null đều coi là không có visitor.
        Guid? visitorId =
            request.VisitorId is { } v && v != Guid.Empty ? v : null;

        // Chuẩn hóa: id địa điểm phải là số dương.
        int? locationId =
            request.LocationId is { } l && l > 0 ? l : null;

        try
        {
            // Bảo đảm dòng visitor_tracking tồn tại để không vi phạm khóa ngoại.
            if (visitorId != null)
            {
                using var ensureRequest = CreateRequest(
                    HttpMethod.Post,
                    $"{baseUrl}/rest/v1/rpc/track_heartbeat",
                    serviceRoleKey);

                ensureRequest.Content = JsonContent.Create(new
                {
                    p_visitor_id = visitorId.Value
                });

                using var ensureResponse =
                    await client.SendAsync(ensureRequest);

                if (!ensureResponse.IsSuccessStatusCode)
                {
                    return StatusCode(502, new
                    {
                        message = "Không thể xác nhận người truy cập."
                    });
                }
            }

            using var httpRequest = CreateRequest(
                HttpMethod.Post,
                $"{baseUrl}/rest/v1/audio_listens?on_conflict=listen_id",
                serviceRoleKey);

            httpRequest.Headers.Add(
                "Prefer",
                "resolution=ignore-duplicates,return=minimal");

            httpRequest.Content = JsonContent.Create(new
            {
                listen_id = request.ListenId,
                visitor_id = visitorId,
                location_id = locationId,
                location_name = request.LocationName,
                language_code = request.LanguageCode
            });

            using var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return StatusCode(502, new
                {
                    message = "Không thể lưu lượt nghe.",
                    detail = error
                });
            }

            return Ok(new { message = "Đã ghi nhận lượt nghe." });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new
            {
                message = "Không thể kết nối Supabase."
            });
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        string serviceRoleKey)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add("apikey", serviceRoleKey);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceRoleKey);

        return request;
    }
}

public record AudioListenRequest(
    Guid ListenId,
    Guid? VisitorId,
    int? LocationId,
    string? LocationName,
    string? LanguageCode);