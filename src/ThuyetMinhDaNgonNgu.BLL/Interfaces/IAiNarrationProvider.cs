public interface IAiNarrationProvider
{
    Task<string> GenerateNarrationAsync(
        string locationName,
        string description,
        string languageCode,
        CancellationToken cancellationToken = default);
}