using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
using ThuyetMinhDaNgonNgu.BLL.Models;
namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationService _service;

    public LocationsController(ILocationService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationResponse>), StatusCodes.Status200OK)]
    [EndpointSummary("Lấy danh sách địa điểm.")]
    public async Task<ActionResult<IReadOnlyList<LocationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = (await _service.GetAllAsync(cancellationToken))
            .Select(location => new LocationResponse(
                location.Id,
                location.Name,
                location.City,
                location.Description,
                location.Latitude,
                location.Longitude))
            .ToArray();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Lấy thông tin một địa điểm.")]
    public async Task<ActionResult<LocationResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var location = await _service.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return NotFound(new { message = $"Không tìm thấy địa điểm có mã {id}." });
        }

        return Ok(new LocationResponse(
            location.Id,
            location.Name,
            location.City,
            location.Description,
            location.Latitude,
            location.Longitude));
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Tạo địa điểm mới.")]
    public async Task<ActionResult<LocationResponse>> Create(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var location = await _service.AddAsync(
                request.Name,
                request.City,
                request.Description,
                request.Latitude,
                request.Longitude,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = location.Id },
                ToResponse(location));
        }
        catch (ArgumentException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Cập nhật địa điểm.")]
    [EndpointDescription("Cập nhật thông tin địa điểm. Yêu cầu vai trò Admin.")]
    public async Task<ActionResult<LocationResponse>> Update(
        int id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var location = await _service.UpdateAsync(
                id,
                request.Name,
                request.City,
                request.Description,
                request.Latitude,
                request.Longitude,
                cancellationToken);

            return location is null
                ? Problem(
                    $"Không tìm thấy địa điểm có mã {id}.",
                    statusCode: StatusCodes.Status404NotFound)
                : Ok(ToResponse(location));
        }
        catch (ArgumentException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Xóa địa điểm.")]
    [EndpointDescription("Từ chối xóa nếu còn bài thuyết minh tham chiếu địa điểm.")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.DeleteAsync(id, cancellationToken) switch
            {
                ResourceDeleteResult.Deleted => NoContent(),
                ResourceDeleteResult.NotFound =>
                    Problem(
                        $"Không tìm thấy địa điểm có mã {id}.",
                        statusCode: StatusCodes.Status404NotFound),
                ResourceDeleteResult.InUse =>
                    Problem(
                        "Không thể xóa địa điểm đang được bài thuyết minh tham chiếu.",
                        statusCode: StatusCodes.Status409Conflict),
                _ => throw new InvalidOperationException("Kết quả xóa địa điểm không hợp lệ.")
            };
        }
        catch (ArgumentException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static LocationResponse ToResponse(TouristLocation location) =>
        new(
            location.Id,
            location.Name,
            location.City,
            location.Description,
            location.Latitude,
            location.Longitude);
}
