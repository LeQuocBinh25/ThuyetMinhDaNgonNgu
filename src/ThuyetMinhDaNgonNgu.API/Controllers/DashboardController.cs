
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThuyetMinhDaNgonNgu.API.Controllers;
//Khai báo Controller
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    //Khai báo các biến và Constructor
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DashboardController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }
//phương thức thực hiện toàn bộ chức năng thống kê
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
//Lấy dữ liệu lượt truy cập
            var visitors = await GetRows<VisitorRow>(
                client,
                supabaseUrl,
                serviceRoleKey,
                "visitor_tracking",
                "visitor_id,visit_count",
                "order=visitor_id");

//Lấy dữ liệu lượt nghe
            var listens = await GetRows<ListenRow>(
                client,
                supabaseUrl,
                serviceRoleKey,
                "audio_listens",
                "listen_id,location_name,language_code,listened_at,visitor_id");

//Thống kê theo địa điểm
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
//Thống kê theo ngôn ngữ
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

//Trả kết quả thống kê
            return Ok(new
            {
                //Tổng lượt truy cập
                totalVisits = visitors.Sum(x => x.VisitCount ?? 0L),
                //Tổng lượt nghe
                totalListens = listens.Count,
                byLocation,
                byLanguage,
                byLocationAndLanguage,
                //Đếm người nghe duy nhất
                uniqueListeners = listens
                .Where(x => x.VisitorId != null)
                .Select(x => x.VisitorId)
                .Distinct()
                .Count(),
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
        //Mỗi lần truy vấn 1.000 bản ghi
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

    public sealed class VisitorRow
    {
        [JsonPropertyName("visitor_id")]
        public Guid VisitorId { get; set; }

        [JsonPropertyName("visit_count")]
        public long? VisitCount { get; set; }
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

        [JsonPropertyName("visitor_id")]
        public Guid? VisitorId { get; set; }
    }
}
