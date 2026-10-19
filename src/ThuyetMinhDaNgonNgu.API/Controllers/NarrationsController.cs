using Microsoft.AspNetCore.Mvc;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;

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

    [HttpPost]
    [ProducesResponseType(typeof(NarrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NarrationResponse>> Create(
    [FromBody] CreateNarrationRequest request)
    {
        try
        {
            var result = await _service.CreateNarrationAsync(
    request.LocationId,
    request.LanguageCode);
            var response = new NarrationResponse(
                result.Id,
                result.LocationId,
                result.LocationName,
                result.LanguageCode,
                result.LanguageName,
                result.Text,
                result.AudioUrl,
                result.Status,
                result.CreatedAtUtc);

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
    }
}
