using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ThuyetMinhDaNgonNgu.API.DTOs.Responses;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.API.DTOs.Requests;
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
    public async Task<ActionResult<IReadOnlyList<LocationResponse>>> GetAll()
    {
        var response = (await _service.GetAllAsync())
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
    public async Task<ActionResult<LocationResponse>> GetById(int id)
    {
        var location = await _service.GetByIdAsync(id);
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
    public async Task<ActionResult<LocationResponse>> Create(
        [FromBody] CreateLocationRequest request)
    {
        try
        {
            var location = await _service.AddAsync(
                request.Name,
                request.City,
                request.Description,
                request.Latitude,
                request.Longitude);

            var response = new LocationResponse(
                location.Id,
                location.Name,
                location.City,
                location.Description,
                location.Latitude,
                location.Longitude);

            return CreatedAtAction(
                nameof(GetById),
                new { id = location.Id },
                response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

}
