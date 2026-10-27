namespace ThuyetMinhDaNgonNgu.DAL.Entities;

public sealed class NarrationEntity
{
    public Guid Id { get; set; }

    public int LocationId { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string AudioObjectPath { get; set; } = string.Empty;

    public string AudioContentType { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public LocationEntity Location { get; set; } = null!;

    public LanguageEntity Language { get; set; } = null!;
}
