using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/narrations")]
public sealed class AdminNarrationsController(IAdminNarrationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NarrationResponse>>> GetAll() =>
        Ok((await service.GetAllAsync()).Select(ToResponse).ToArray());

    [HttpGet("{locationId:int}/{languageCode}")]
    public async Task<ActionResult<NarrationResponse>> GetCurrent(int locationId, string languageCode)
    {
        var narration = await service.GetCurrentAsync(locationId, languageCode);
        return narration is null
            ? NotFound(new { message = "Chưa có bản thuyết minh hiện hành cho địa điểm và ngôn ngữ này." })
            : Ok(ToResponse(narration));
    }

    [HttpPost("draft")]
    [EnableRateLimiting("Narration")]
    public async Task<ActionResult<object>> CreateDraft([FromBody] CreateNarrationDraftRequest request)
    {
        try
        {
            string text = await service.CreateDraftAsync(request.LocationId, request.LanguageCode);
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
    }

    [HttpPut("{locationId:int}/{languageCode}")]
    public async Task<ActionResult<NarrationResponse>> Publish(
        int locationId,
        string languageCode,
        [FromBody] PublishNarrationRequest request)
    {
        try
        {
            var narration = await service.PublishAsync(locationId, languageCode, request.Text);
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
