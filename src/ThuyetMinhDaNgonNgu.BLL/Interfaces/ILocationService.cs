using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.BLL.Interfaces;

public interface ILocationService
{
    IReadOnlyList<TouristLocation> GetAll();

    TouristLocation? GetById(int id);
}
