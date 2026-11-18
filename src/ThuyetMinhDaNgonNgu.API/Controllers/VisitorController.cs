//Tạo API ghi nhận lượt truy cập
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuyetMinhDaNgonNgu.DAL.Persistence;

namespace ThuyetMinhDaNgonNgu.API.Controllers;

[ApiController]
[Route("api/visitor")]
public sealed class VisitorController : ControllerBase
{
    private readonly AppDbContext _dbContext; //AppDbContext là đối tượng đại diện cho kết nối và thao tác với cơ sở dữ liệu thông qua Entity Framework Core.

    public VisitorController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Ghi nhận một lượt truy cập: tăng visit_count của đúng visitorId.
    [HttpPost]
    public async Task<IActionResult> Record(
        [FromBody] VisitorRequest request,
        CancellationToken cancellationToken) //cho phép hủy thao tác khi request bị hủy.
    {
        //Kiểm tra mã người truy cập
        if (request == null || request.VisitorId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "visitorId không hợp lệ."
            });
        }
//Thêm dữ liệu vào database
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($""" 
            INSERT INTO public.visitor_tracking
                (visitor_id, visit_count, last_seen, is_active)
            VALUES
                ({request.VisitorId}, 1, {DateTimeOffset.UtcNow}, TRUE)
            ON CONFLICT (visitor_id) DO UPDATE      --on conflict xử lý khi mã người truy cập đã tồn tại,do ppdate cập nhật bản ghi cũ thay vì tạo bản ghi mới
            SET visit_count = COALESCE(visitor_tracking.visit_count, 0) + 1,   --tăng lượt truy cập thêm 1
                last_seen = EXCLUDED.last_seen,
                is_active = TRUE    --thay đổi trạng thái hoạt động
            """, cancellationToken);

        return Ok(new
        {
            message = "Đã ghi nhận lượt truy cập."
        });
    }

    public sealed record VisitorRequest(Guid VisitorId);
}