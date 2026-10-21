using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/languages")]
public sealed class LanguagesController : ControllerBase
{
    private readonly ILanguageService _service;

    public LanguagesController(ILanguageService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LanguageResponse>>> GetAll()
    {
        var response = (await _service.GetAllAsync())
            .Select(language => new LanguageResponse(language.Code, language.Name))
            .ToArray();

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LanguageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LanguageResponse>> Create([FromBody] CreateLanguageRequest request)
    {
        try
        {
            var language = await _service.AddAsync(request.Code, request.Name);
            if (language is null)
            {
                return Conflict(new { message = $"Mã ngôn ngữ '{request.Code}' đã tồn tại." });
            }

            var response = new LanguageResponse(language.Code, language.Name);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
