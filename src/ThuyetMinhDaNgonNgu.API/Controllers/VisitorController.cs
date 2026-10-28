using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/visitor")]
public sealed class VisitorController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public VisitorController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Ghi nhận một lượt truy cập: tăng visit_count của đúng visitorId.
    [HttpPost]
    public async Task<IActionResult> Record(
        [FromBody] VisitorRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.VisitorId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "visitorId không hợp lệ."
            });
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.visitor_tracking
                (visitor_id, visit_count, last_seen, is_active)
            VALUES
                ({request.VisitorId}, 1, {DateTimeOffset.UtcNow}, TRUE)
            ON CONFLICT (visitor_id) DO UPDATE
            SET visit_count = COALESCE(visitor_tracking.visit_count, 0) + 1,
                last_seen = EXCLUDED.last_seen,
                is_active = TRUE
            """, cancellationToken);

        return Ok(new
        {
            message = "Đã ghi nhận lượt truy cập."
        });
    }

    public sealed record VisitorRequest(Guid VisitorId);
}