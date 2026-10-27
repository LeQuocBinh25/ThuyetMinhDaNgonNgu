using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/narrations")]
public sealed class NarrationsController : ControllerBase
{
    private readonly INarrationService _service;
    private readonly ILogger<NarrationsController> _logger;

    public NarrationsController(
        INarrationService service,
        ILogger<NarrationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting("Narration")]
    [EndpointSummary("Lấy hoặc tạo thuyết minh theo địa điểm và ngôn ngữ.")]
    [ProducesResponseType(typeof(NarrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NarrationResponse>> Create(
        [FromBody] CreateNarrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateNarrationAsync(
                request.LocationId,
                request.LanguageCode,
                cancellationToken);
            var response = new NarrationResponse(
                result.Id,
                result.LocationId,
                result.LocationName,
                result.LanguageCode,
                result.LanguageName,
                result.Text,
                result.AudioUrl,
                result.Status,
                result.CreatedAtUtc,
                result.UpdatedAtUtc,
                result.IsCached);

            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (ProviderConfigurationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "A narration provider rejected the request.");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Không thể hoàn tất xử lý audio thuyết minh. Nếu vừa cập nhật bản hiện hành, hãy kiểm tra trạng thái và Storage trước khi thử lại."
                });
        }
    }
}
