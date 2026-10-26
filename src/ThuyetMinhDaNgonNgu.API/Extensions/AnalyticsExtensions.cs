using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ThuyetMinhDaNgonNgu.API.Extensions;

/// <summary>
/// Đăng ký Dependency Injection cho Phân hệ Thống kê & Dữ liệu Sự kiện (AD01 - AD05).
/// Thành viên phụ trách phân hệ Analytics sẽ khai báo các Service / Repository / Policy tại đây
/// để tránh chỉnh sửa trực tiếp và gây xung đột trong Program.cs.
/// </summary>
public static class AnalyticsExtensions
{
    public static IServiceCollection AddAnalyticsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO (Thành viên 2): Đăng ký Repository và Service cho Thống kê & Sự kiện (VisitEvent, PlaybackEvent)
        // Ví dụ:
        // services.AddScoped<IAnalyticsRepository, PostgresAnalyticsRepository>();
        // services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
