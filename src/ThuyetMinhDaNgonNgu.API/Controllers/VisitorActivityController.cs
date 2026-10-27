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
            var url = _config["Supabase:Url"]!.TrimEnd('/');
            var key = _config["Supabase:ServiceRoleKey"]!;

            var request = new HttpRequestMessage(
                method,
                $"{url}/rest/v1/{path}");

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

            var sessionJson = JsonSerializer.Serialize(new
            {
                visitor_id = id,
                is_active = true
            });

            using var sessionRequest = CreateRequest(
                HttpMethod.Post,
                "visitor_sessions?on_conflict=visitor_id",
                sessionJson);

            sessionRequest.Headers.TryAddWithoutValidation(
                "Prefer",
                "resolution=merge-duplicates,return=minimal");

            using var sessionResponse =
                await _http.SendAsync(sessionRequest);

            if (!sessionResponse.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Không cập nhật được visitor_sessions.");
            }

            var heartbeatJson = JsonSerializer.Serialize(new
            {
                visitor_id = id,
                last_seen = DateTimeOffset.UtcNow
            });

            using var heartbeatRequest = CreateRequest(
                HttpMethod.Post,
                "visitor_heartbeats?on_conflict=visitor_id",
                heartbeatJson);

            heartbeatRequest.Headers.TryAddWithoutValidation(
                "Prefer",
                "resolution=merge-duplicates,return=minimal");

            using var heartbeatResponse =
                await _http.SendAsync(heartbeatRequest);

            if (!heartbeatResponse.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Không cập nhật được visitor_heartbeats.");
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

            // Làm heartbeat cũ hết hạn để không bị tính lại TRUE.
            var oldHeartbeatJson = JsonSerializer.Serialize(new
            {
                last_seen = DateTimeOffset.UtcNow.AddSeconds(-31)
            });

            using var heartbeatRequest = CreateRequest(
                HttpMethod.Patch,
                $"visitor_heartbeats?visitor_id=eq.{Uri.EscapeDataString(id)}",
                oldHeartbeatJson);

            heartbeatRequest.Headers.TryAddWithoutValidation(
                "Prefer",
                "return=minimal");

            using var heartbeatResponse =
                await _http.SendAsync(heartbeatRequest);

            if (!heartbeatResponse.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Không cập nhật được thời gian heartbeat.");
            }

            // Chuyển trạng thái thành FALSE.
            var patchJson = JsonSerializer.Serialize(new
            {
                is_active = false
            });

            using var request = CreateRequest(
                HttpMethod.Patch,
                $"visitor_sessions?visitor_id=eq.{Uri.EscapeDataString(id)}",
                patchJson);

            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "return=minimal");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Không cập nhật được trạng thái.");
            }

            return Ok(new { success = true });
        }

        // Lấy danh sách người truy cập cho dashboard.
        [HttpGet("visitors")]
        public async Task<IActionResult> GetVisitors()
        {
            using var sessionsRequest = CreateRequest(
                HttpMethod.Get,
                "visitor_sessions?select=visitor_id,is_active&order=visitor_id");

            using var sessionsResponse =
                await _http.SendAsync(sessionsRequest);

            if (!sessionsResponse.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Không đọc được visitor_sessions.");
            }

            var sessionsJson =
                await sessionsResponse.Content.ReadAsStringAsync();

            using var heartbeatsRequest = CreateRequest(
                HttpMethod.Get,
                "visitor_heartbeats?select=visitor_id,last_seen");

            using var heartbeatsResponse =
                await _http.SendAsync(heartbeatsRequest);

            if (!heartbeatsResponse.IsSuccessStatusCode)
            {
                return StatusCode(
                    502,
                    "Không đọc được visitor_heartbeats.");
            }

            var heartbeatsJson =
                await heartbeatsResponse.Content.ReadAsStringAsync();

            using var sessionsDoc = JsonDocument.Parse(sessionsJson);
            using var heartbeatsDoc = JsonDocument.Parse(heartbeatsJson);

            var latest = new Dictionary<string, DateTimeOffset>();

            foreach (var item in heartbeatsDoc.RootElement.EnumerateArray())
            {
                var id = item.GetProperty("visitor_id").GetString();
                var timeText = item.GetProperty("last_seen").GetString();

                if (id != null &&
                    DateTimeOffset.TryParse(timeText, out var time))
                {
                    latest[id] = time;
                }
            }

            var now = DateTimeOffset.UtcNow;
            var result = new List<object>();

            foreach (var item in sessionsDoc.RootElement.EnumerateArray())
            {
                var id = item.GetProperty("visitor_id").GetString();

                if (id == null)
                {
                    continue;
                }

                // TRUE nếu heartbeat trong 30 giây gần nhất.
                var active = latest.TryGetValue(id, out var lastSeen)
                    && now - lastSeen <= TimeSpan.FromSeconds(30);

                var oldActive =
                    item.GetProperty("is_active").GetBoolean();

                if (active != oldActive)
                {
                    var patchJson = JsonSerializer.Serialize(new
                    {
                        is_active = active
                    });

                    using var patchRequest = CreateRequest(
                        HttpMethod.Patch,
                        $"visitor_sessions?visitor_id=eq.{Uri.EscapeDataString(id)}",
                        patchJson);

                    patchRequest.Headers.TryAddWithoutValidation(
                        "Prefer",
                        "return=minimal");

                    using var patchResponse =
                        await _http.SendAsync(patchRequest);

                    if (!patchResponse.IsSuccessStatusCode)
                    {
                        return StatusCode(
                            502,
                            "Không cập nhật được is_active trong Supabase.");
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