using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ThuyetMinhDaNgonNgu.API.Extensions;

/// <summary>
/// Đăng ký Dependency Injection cho Phân hệ Quản trị CRUD Địa điểm, Ngôn ngữ, Thuyết minh & Storage (AD06 - AD14).
/// Thành viên phụ trách phân hệ Quản trị CRUD sẽ khai báo các Service / Repository / Adapter tại đây
/// để tránh chỉnh sửa trực tiếp và gây xung đột trong Program.cs.
/// </summary>
public static class ManagementExtensions
{
    public static IServiceCollection AddManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO (Thành viên 3): Đăng ký các Service / Repository mở rộng cho CRUD Quản trị
        // (Location CRUD mở rộng, Language CRUD mở rộng, Narration DELETE, Storage cleanup...)
        // Ví dụ:
        // services.AddScoped<ILocationManagementService, LocationManagementService>();

        return services;
    }
}
