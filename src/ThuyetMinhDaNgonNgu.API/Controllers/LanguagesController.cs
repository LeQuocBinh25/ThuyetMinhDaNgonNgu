using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/languages")]
public sealed class LanguagesController : ControllerBase
{
    private readonly ILanguageService _service;
    private readonly ILogger<LanguagesController> _logger;

    public LanguagesController(
        ILanguageService service,
        ILogger<LanguagesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("Lấy danh sách ngôn ngữ.")]
    public async Task<ActionResult<IReadOnlyList<LanguageResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = (await _service.GetAllAsync(cancellationToken))
                .Select(language => new LanguageResponse(language.Code, language.Name))
                .ToArray();

            return Ok(response);
        }
        catch (DbException exception)
        {
            _logger.LogError(exception, "Database query failed while loading supported languages.");
            return Problem(
                "Không thể đọc danh sách ngôn ngữ từ cơ sở dữ liệu. Hãy kiểm tra kết nối database và log máy chủ.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Language database unavailable");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LanguageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Tạo ngôn ngữ mới.")]
    public async Task<ActionResult<LanguageResponse>> Create(
        [FromBody] CreateLanguageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var language = await _service.AddAsync(
                request.Code,
                request.Name,
                cancellationToken);
            if (language is null)
            {
                return Problem(
                    $"Mã ngôn ngữ '{request.Code}' đã tồn tại.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var response = new LanguageResponse(language.Code, language.Name);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (ArgumentException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LanguageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Cập nhật tên ngôn ngữ.")]
    [EndpointDescription("Chỉ cập nhật tên hiển thị; mã ngôn ngữ được giữ nguyên để bảo toàn khóa ngoại.")]
    public async Task<ActionResult<LanguageResponse>> Update(
        string code,
        [FromBody] UpdateLanguageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var language = await _service.UpdateAsync(code, request.Name, cancellationToken);
            return language is null
                ? Problem(
                    $"Không tìm thấy ngôn ngữ '{code}'.",
                    statusCode: StatusCodes.Status404NotFound)
                : Ok(new LanguageResponse(language.Code, language.Name));
        }
        catch (ArgumentException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Xóa ngôn ngữ.")]
    [EndpointDescription("Từ chối xóa nếu còn bài thuyết minh tham chiếu ngôn ngữ.")]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.DeleteAsync(code, cancellationToken) switch
            {
                ResourceDeleteResult.Deleted => NoContent(),
                ResourceDeleteResult.NotFound =>
                    Problem(
                        $"Không tìm thấy ngôn ngữ '{code}'.",
                        statusCode: StatusCodes.Status404NotFound),
                ResourceDeleteResult.InUse =>
                    Problem(
                        "Không thể xóa ngôn ngữ đang được bài thuyết minh tham chiếu.",
                        statusCode: StatusCodes.Status409Conflict),
                _ => throw new InvalidOperationException("Kết quả xóa ngôn ngữ không hợp lệ.")
            };
        }
        catch (ArgumentException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
