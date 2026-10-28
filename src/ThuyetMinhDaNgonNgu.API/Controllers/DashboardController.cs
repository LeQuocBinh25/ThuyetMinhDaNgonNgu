
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DashboardController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
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

        try
        {
            var client = _httpClientFactory.CreateClient();

            var visits = await GetRows<VisitRow>(
                client,
                supabaseUrl,
                serviceRoleKey,
                "visitor_tracking",
                "visit_id",
                "record_type=eq.visit");

            var listens = await GetRows<ListenRow>(
                client,
                supabaseUrl,
                serviceRoleKey,
                "audio_listens",
                "listen_id,location_name,language_code,listened_at");

            var byLocation = listens
                .GroupBy(x => string.IsNullOrWhiteSpace(x.LocationName)
                    ? "Chưa xác định"
                    : x.LocationName)
                .Select(g => new
                {
                    locationName = g.Key,
                    totalListens = g.Count()
                })
                .OrderByDescending(x => x.totalListens)
                .ToList();

            var byLanguage = listens
                .GroupBy(x => string.IsNullOrWhiteSpace(x.LanguageCode)
                    ? "Chưa xác định"
                    : x.LanguageCode)
                .Select(g => new
                {
                    languageCode = g.Key,
                    totalListens = g.Count()
                })
                .OrderByDescending(x => x.totalListens)
                .ToList();

            var byLocationAndLanguage = listens
                .GroupBy(x => new
                {
                    LocationName = string.IsNullOrWhiteSpace(x.LocationName)
                        ? "Chưa xác định"
                        : x.LocationName,
                    LanguageCode = string.IsNullOrWhiteSpace(x.LanguageCode)
                        ? "Chưa xác định"
                        : x.LanguageCode
                })
                .Select(g => new
                {
                    locationName = g.Key.LocationName,
                    languageCode = g.Key.LanguageCode,
                    totalListens = g.Count()
                })
                .OrderByDescending(x => x.totalListens)
                .ToList();

            return Ok(new
            {
                totalVisits = visits.Count,
                totalListens = listens.Count,
                byLocation,
                byLanguage,
                byLocationAndLanguage
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new
            {
                message = "Không thể lấy dữ liệu từ Supabase."
            });
        }
        catch (JsonException)
        {
            return StatusCode(502, new
            {
                message = "Dữ liệu Supabase không đúng định dạng."
            });
        }
    }

    private static async Task<List<T>> GetRows<T>(
        HttpClient client,
        string supabaseUrl,
        string serviceRoleKey,
        string table,
        string columns,
        string? filter = null)
    {
        var result = new List<T>();
        const int pageSize = 1000;
        var offset = 0;

        while (true)
        {
            var query =
                $"{supabaseUrl.TrimEnd('/')}/rest/v1/{table}" +
                $"?select={columns}";

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query += $"&{filter}";
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                query);

            request.Headers.Add("apikey", serviceRoleKey);
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    serviceRoleKey);

            request.Headers.Range = new RangeHeaderValue(
                offset,
                offset + pageSize - 1);

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var rows =
                await response.Content.ReadFromJsonAsync<List<T>>()
                ?? new List<T>();

            result.AddRange(rows);

            if (rows.Count < pageSize)
            {
                break;
            }

            offset += pageSize;
        }

        return result;
    }


    public sealed class VisitRow
    {
        [JsonPropertyName("visit_id")]
        public Guid VisitId { get; set; }
    }

    public sealed class ListenRow
    {
        [JsonPropertyName("listen_id")]
        public Guid ListenId { get; set; }

        [JsonPropertyName("location_name")]
        public string? LocationName { get; set; }

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("listened_at")]
        public DateTimeOffset ListenedAt { get; set; }
    }
}
