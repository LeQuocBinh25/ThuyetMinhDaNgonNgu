
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
        if (request.ListenId == Guid.Empty)
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

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{supabaseUrl.TrimEnd('/')}/rest/v1/audio_listens?on_conflict=listen_id");

        httpRequest.Headers.Add("apikey", serviceRoleKey);
        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceRoleKey);

        httpRequest.Headers.Add(
            "Prefer",
            "resolution=ignore-duplicates,return=minimal");

        httpRequest.Content = JsonContent.Create(new
        {
            listen_id = request.ListenId,
            location_name = request.LocationName,
            language_code = request.LanguageCode
        });

        try
        {
            using var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(502, new
                {
                    message = "Không thể lưu lượt nghe."
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
}

public record AudioListenRequest(
    Guid ListenId,
    string? LocationName,
    string? LanguageCode);
