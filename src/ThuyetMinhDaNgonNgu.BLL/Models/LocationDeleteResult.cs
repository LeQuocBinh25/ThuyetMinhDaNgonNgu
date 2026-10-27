namespace ThuyetMinhDaNgonNgu.BLL.Models;

public sealed record LocationDeleteResult(
    ResourceDeleteResult Result,
    IReadOnlyList<string> AudioObjectPaths);
