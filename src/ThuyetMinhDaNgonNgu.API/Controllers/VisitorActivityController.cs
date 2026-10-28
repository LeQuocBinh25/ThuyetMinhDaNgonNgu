
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

        // Ghi nhận người truy cập đang hoạt động.
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat(
            [FromBody] HeartbeatRequest data)
        {
            if (data == null || data.VisitorId == Guid.Empty)
            {
                return BadRequest("visitorId không hợp lệ.");
            }

            var id = data.VisitorId.ToString();
            var trackingKey = $"session:{id}";

            var json = JsonSerializer.Serialize(new
            {
                tracking_key = trackingKey,
                record_type = "session",
                visitor_id = id,
                last_seen = DateTimeOffset.UtcNow,
                is_active = true
            });

            using var request = CreateRequest(
                HttpMethod.Post,
                "visitor_tracking?on_conflict=tracking_key",
                json);

            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "resolution=merge-duplicates,return=minimal");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return StatusCode(502, new
                {
                    message = "Không cập nhật được visitor_tracking.",
                    detail = error
                });
            }

            return Ok(new { success = true });
        }

        // Ghi nhận người truy cập rời trang.
        [HttpPost("leave")]
        public async Task<IActionResult> Leave(
            [FromBody] HeartbeatRequest data)
        {
            if (data == null || data.VisitorId == Guid.Empty)
            {
                return BadRequest("visitorId không hợp lệ.");
            }

            var id = data.VisitorId.ToString();
            var trackingKey = Uri.EscapeDataString($"session:{id}");

            var json = JsonSerializer.Serialize(new
            {
                last_seen = DateTimeOffset.UtcNow.AddSeconds(-31),
                is_active = false
            });

            using var request = CreateRequest(
                HttpMethod.Patch,
                $"visitor_tracking?tracking_key=eq.{trackingKey}",
                json);

            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "return=minimal");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return StatusCode(502, new
                {
                    message = "Không cập nhật được trạng thái.",
                    detail = error
                });
            }

            return Ok(new { success = true });
        }

        // Lấy danh sách người truy cập cho dashboard.
        [HttpGet("visitors")]
        public async Task<IActionResult> GetVisitors()
        {
            using var request = CreateRequest(
                HttpMethod.Get,
                "visitor_tracking?record_type=eq.session" +
                "&select=tracking_key,visitor_id,is_active,last_seen" +
                "&order=visitor_id");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return StatusCode(502, new
                {
                    message = "Không đọc được visitor_tracking.",
                    detail = error
                });
            }

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);

            var now = DateTimeOffset.UtcNow;
            var result = new List<object>();

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var id = item.GetProperty("visitor_id").GetString();
                var key = item.GetProperty("tracking_key").GetString();
                var timeText = item.GetProperty("last_seen").GetString();

                if (id == null || key == null ||
                    !DateTimeOffset.TryParse(timeText, out var lastSeen))
                {
                    continue;
                }

                // Giữ nguyên ngưỡng 30 giây.
                var active = now - lastSeen <=
                    TimeSpan.FromSeconds(30);

                var oldActive =
                    item.GetProperty("is_active").GetBoolean();

                // Đồng bộ trạng thái vào database nếu cần.
                if (active != oldActive)
                {
                    var patchJson = JsonSerializer.Serialize(new
                    {
                        is_active = active
                    });

                    using var patchRequest = CreateRequest(
                        HttpMethod.Patch,
                        $"visitor_tracking?tracking_key=eq." +
                        Uri.EscapeDataString(key),
                        patchJson);

                    patchRequest.Headers.TryAddWithoutValidation(
                        "Prefer",
                        "return=minimal");

                    using var patchResponse =
                        await _http.SendAsync(patchRequest);

                    if (!patchResponse.IsSuccessStatusCode)
                    {
                        var error =
                            await patchResponse.Content.ReadAsStringAsync();

                        return StatusCode(502, new
                        {
                            message = "Không cập nhật được trạng thái.",
                            detail = error
                        });
                    }
                }

                result.Add(new
                {
                    visitorId = id,
                    isActive = active
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
