namespace BloodBank.API.Services;

public interface IGenerativeAiService
{
    Task<ContentGenerationResult> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<float>> EmbedContentAsync(string content, CancellationToken cancellationToken);

    Task<IReadOnlyList<IReadOnlyList<float>>> EmbedBatchAsync(
        IEnumerable<string> contents,
        CancellationToken cancellationToken);
}
