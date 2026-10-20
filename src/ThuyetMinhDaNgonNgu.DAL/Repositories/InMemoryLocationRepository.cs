using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.DAL.Repositories;

/// <summary>
/// Kho dữ liệu trong bộ nhớ dùng ở giai đoạn đầu.
/// Dữ liệu sẽ mất khi ứng dụng dừng; sẽ được thay bằng SQLite ở giai đoạn tiếp theo.
/// </summary>
public sealed class InMemoryLocationRepository : ILocationRepository
{
    private static readonly IReadOnlyList<TouristLocation> Locations =
    [
        new TouristLocation(
            1,
            "Hồ Hoàn Kiếm",
            "Hà Nội",
            "Hồ nằm ở trung tâm Hà Nội và gắn với truyền thuyết vua Lê trả gươm thần."),
        new TouristLocation(
            2,
            "Dinh Độc Lập",
            "Thành phố Hồ Chí Minh",
            "Công trình gắn với nhiều sự kiện quan trọng trong lịch sử Việt Nam, đặc biệt là sự kiện ngày 30 tháng 4 năm 1975."),
        new TouristLocation(
            3,
            "Vịnh Hạ Long",
            "Quảng Ninh",
            "Vịnh nổi tiếng với cảnh quan biển và nhiều đảo đá vôi có hình dáng độc đáo."),
        new TouristLocation(
            4,
            "Chùa Một Cột",
            "Hà Nội",
            "Chùa Một Cột nổi tiếng nhất nằm ở quận Ba Đình, thành phố Hà Nội"
        ),
    ];

    public IReadOnlyList<TouristLocation> GetAll() => Locations;

    public TouristLocation? GetById(int id) => Locations.FirstOrDefault(location => location.Id == id);
public Task<TouristLocation> AddAsync(
    string name,
    string city,
    string description)
{
    throw new NotSupportedException(
        "Repository này không hỗ trợ thêm địa điểm.");
}
}
