using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/narrations")]
public sealed class AdminNarrationsController(
    IAdminNarrationService service,
    ILogger<AdminNarrationsController> logger) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lấy danh sách bản thuyết minh hiện hành.")]
    public async Task<ActionResult<IReadOnlyList<NarrationResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok((await service.GetAllAsync(cancellationToken)).Select(ToResponse).ToArray());

    [HttpGet("{locationId:int}/{languageCode}")]
    [EndpointSummary("Lấy bản thuyết minh hiện hành theo địa điểm và ngôn ngữ.")]
    public async Task<ActionResult<NarrationResponse>> GetCurrent(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var narration = await service.GetCurrentAsync(locationId, languageCode, cancellationToken);
        return narration is null
            ? NotFound(new { message = "Chưa có bản thuyết minh hiện hành cho địa điểm và ngôn ngữ này." })
            : Ok(ToResponse(narration));
    }

    [HttpPost("draft")]
    [EnableRateLimiting("Narration")]
    [EndpointSummary("Tạo bản nháp thuyết minh bằng AI.")]
    public async Task<ActionResult<object>> CreateDraft(
        [FromBody] CreateNarrationDraftRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            string text = await service.CreateDraftAsync(
                request.LocationId,
                request.LanguageCode,
                cancellationToken);
            return Ok(new { request.LocationId, request.LanguageCode, Text = text });
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
            logger.LogError(exception, "An external provider rejected the admin draft request.");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Không thể tạo bản nháp từ dịch vụ AI. Vui lòng thử lại sau." });
        }
    }

    [HttpPut("{locationId:int}/{languageCode}")]
    [ProducesResponseType(typeof(NarrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NarrationResponse>> Publish(
        int locationId,
        string languageCode,
        [FromBody] PublishNarrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var narration = await service.PublishAsync(
                locationId,
                languageCode,
                request.Text,
                cancellationToken);
            return Ok(ToResponse(narration));
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
            logger.LogError(exception, "An external provider rejected the admin narration publish request.");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Không thể hoàn tất xử lý audio thuyết minh. Nếu vừa cập nhật bản hiện hành, hãy kiểm tra trạng thái và Storage trước khi thử lại."
                });
        }
        catch (ProviderResponseException exception)
        {
            logger.LogWarning(exception, "Gemini TTS returned no usable audio for the admin narration publish request.");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = exception.Message });
        }
    }

    [HttpDelete("{locationId:int}/{languageCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [EndpointSummary("Xóa bản thuyết minh hiện hành.")]
    [EndpointDescription("Xóa audio khỏi Storage khi không còn bản ghi nào tham chiếu.")]
    public async Task<IActionResult> Delete(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteAsync(locationId, languageCode, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Narration {LocationId}/{LanguageCode} was deleted but audio cleanup failed.",
                locationId,
                languageCode);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = exception.Message });
        }
    }

    private static NarrationResponse ToResponse(NarrationResult result) =>
        new(
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
}
