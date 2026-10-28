using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ThuyetMinhDaNgonNgu.API.Controllers
{
    [ApiController]
    [Route("api/visitor-activity")]
    public class VisitorActivityController : ControllerBase
    {
        // Quá ngưỡng này không có heartbeat thì coi như đã rời đi.
        private const int ActiveWindowSeconds = 30;

        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public VisitorActivityController(
            IHttpClientFactory factory,
            IConfiguration config)
        {
            _http = factory.CreateClient();
            _config = config;
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string path,
            string? body = null)
        {
            var url = _config["Supabase:Url"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Supabase:Url.");

            var key = _config["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Supabase:ServiceRoleKey.");

            var request = new HttpRequestMessage(
                method,
                $"{url.TrimEnd('/')}/rest/v1/{path}");

            request.Headers.Add("apikey", key);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", key);

            if (body != null)
            {
                request.Content = new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json");
            }

            return request;
        }

        private async Task<IActionResult> Fail(
            HttpResponseMessage response,
            string message)
        {
            var error = await response.Content.ReadAsStringAsync();

            return StatusCode(502, new
            {
                message,
                detail = error
            });
        }

        // Heartbeat: cập nhật đúng dòng của visitorId, chưa có thì tạo mới.
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat(
            [FromBody] HeartbeatRequest data)
        {
            if (data == null || data.VisitorId == Guid.Empty)
            {
                return BadRequest("visitorId không hợp lệ.");
            }

            var json = JsonSerializer.Serialize(new
            {
                visitor_id = data.VisitorId,
                last_seen = DateTimeOffset.UtcNow,
                is_active = true
            });

            using var request = CreateRequest(
                HttpMethod.Post,
                "visitor_tracking?on_conflict=visitor_id",
                json);

            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "resolution=merge-duplicates,return=minimal");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return await Fail(
                    response,
                    "Không cập nhật được visitor_tracking.");
            }

            return Ok(new { success = true });
        }

        // Rời trang: đặt is_active = false cho đúng visitorId.
        [HttpPost("leave")]
        public async Task<IActionResult> Leave(
            [FromBody] HeartbeatRequest data)
        {
            if (data == null || data.VisitorId == Guid.Empty)
            {
                return BadRequest("visitorId không hợp lệ.");
            }

            var json = JsonSerializer.Serialize(new
            {
                is_active = false
            });

            using var request = CreateRequest(
                HttpMethod.Patch,
                $"visitor_tracking?visitor_id=eq.{data.VisitorId}",
                json);

            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "return=minimal");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return await Fail(
                    response,
                    "Không cập nhật được trạng thái.");
            }

            return Ok(new { success = true });
        }

        // Dashboard: tắt các visitor quá hạn heartbeat, rồi trả danh sách.
        [HttpGet("visitors")]
        public async Task<IActionResult> GetVisitors()
        {
            // Chỉ chiều true -> false cần xử lý ở đây;
            // chiều false -> true do heartbeat đảm nhiệm.
            var cutoff = Uri.EscapeDataString(
                DateTimeOffset.UtcNow
                    .AddSeconds(-ActiveWindowSeconds)
                    .ToString("o"));

            var expireJson = JsonSerializer.Serialize(new
            {
                is_active = false
            });

            using (var expireRequest = CreateRequest(
                HttpMethod.Patch,
                $"visitor_tracking?is_active=eq.true&last_seen=lt.{cutoff}",
                expireJson))
            {
                expireRequest.Headers.TryAddWithoutValidation(
                    "Prefer",
                    "return=minimal");

                using var expireResponse =
                    await _http.SendAsync(expireRequest);

                if (!expireResponse.IsSuccessStatusCode)
                {
                    return await Fail(
                        expireResponse,
                        "Không cập nhật được trạng thái.");
                }
            }

            using var request = CreateRequest(
                HttpMethod.Get,
                "visitor_tracking?select=visitor_id,is_active" +
                "&order=visitor_id");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return await Fail(
                    response,
                    "Không đọc được visitor_tracking.");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);

            var result = new List<object>();

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var id = item.GetProperty("visitor_id").GetString();

                if (id == null)
                {
                    continue;
                }

                result.Add(new
                {
                    visitorId = id,
                    isActive = item.GetProperty("is_active").GetBoolean()
                });
            }

            return Ok(result);
        }
    }

    public class HeartbeatRequest
    {
        public Guid VisitorId { get; set; }
    }
}