using Microsoft.AspNetCore.Mvc;
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
    public ActionResult<IReadOnlyList<LocationResponse>> GetAll()
    {
        var response = _service.GetAll()
            .Select(location => new LocationResponse(
                location.Id,
                location.Name,
                location.City,
                location.Description))
            .ToArray();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<LocationResponse> GetById(int id)
    {
        var location = _service.GetById(id);
        if (location is null)
        {
            return NotFound(new { message = $"Không tìm thấy địa điểm có mã {id}." });
        }

        return Ok(new LocationResponse(
            location.Id,
            location.Name,
            location.City,
            location.Description));
    }
    
[HttpPost]
[ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
public async Task<ActionResult<LocationResponse>> Create(
    [FromBody] CreateLocationRequest request)
{
    var location = await _service.AddAsync(
        request.Name,
        request.City,
        request.Description);

    var response = new LocationResponse(
        location.Id,
        location.Name,
        location.City,
        location.Description);

    return CreatedAtAction(
        nameof(GetById),
        new { id = location.Id },
        response);
}

}
