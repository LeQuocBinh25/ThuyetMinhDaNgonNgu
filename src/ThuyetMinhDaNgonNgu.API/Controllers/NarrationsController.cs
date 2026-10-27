using Microsoft.AspNetCore.Mvc;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/narrations")]
public sealed class NarrationsController : ControllerBase
{
    private readonly INarrationService _service;

    public NarrationsController(INarrationService service)
    {
        _service = service;
    }

    [HttpGet("{locationId:int}/{languageCode}")]
    [EndpointSummary("Lấy bài thuyết minh hiện hành theo địa điểm và ngôn ngữ.")]
    [ProducesResponseType(typeof(NarrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NarrationResponse>> GetCurrent(
        int locationId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetCurrentAsync(
                locationId,
                languageCode,
                cancellationToken);
            return result is null
                ? NotFound(new { message = "Chưa có bài thuyết minh cho địa điểm và ngôn ngữ đã chọn." })
                : Ok(new NarrationResponse(
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
                    result.IsCached));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
