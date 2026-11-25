using Google.GenAI;
using Google.GenAI.Types;

namespace BloodBank.API.Services;

public sealed class GeminiGenerativeAiService(
    Client client,
    GeminiOptions options,
    ISqlExceptionLogger exceptionLogger) : IGenerativeAiService
{
    public async Task<ContentGenerationResult> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        GenerateContentResponse response;
        try
        {
            response = await client.Models.GenerateContentAsync(
                model: options.GenerationModel,
                contents: prompt,
                cancellationToken: cancellationToken);
        }
        catch (ServerError exception)
        {
            await exceptionLogger.LogAsync(exception, "Gemini content generation");
            return new ContentGenerationResult.Unavailable();
        }

        var text = response.Candidates?
            .Select(candidate => candidate.Content?.Parts?
                .FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?.Text)
            .FirstOrDefault(candidateText => !string.IsNullOrWhiteSpace(candidateText));

        return string.IsNullOrWhiteSpace(text)
            ? throw new InvalidOperationException("Gemini returned no text content.")
            : new ContentGenerationResult.Success(text.Trim());
    }

    public async Task<IReadOnlyList<float>> EmbedContentAsync(
        string content,
        CancellationToken cancellationToken)
    {
        // Re-generate any embeddings made with Ollama using this model before reuse; dimensions and vector spaces differ.
        var response = await client.Models.EmbedContentAsync(
            model: options.EmbeddingModel,
            contents: content,
            cancellationToken: cancellationToken);

        var values = response.Embeddings?.FirstOrDefault()?.Values;
        if (values is null || values.Count == 0)
            throw new InvalidOperationException("Gemini returned no embedding values.");

        return values.Select(value => Convert.ToSingle(value)).ToArray();
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> EmbedBatchAsync(
        IEnumerable<string> contents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);

        var embeddings = new List<IReadOnlyList<float>>();
        foreach (var content in contents)
        {
            if (embeddings.Count > 0 && options.EmbeddingBatchDelayMilliseconds > 0)
            {
                await Task.Delay(
                    options.EmbeddingBatchDelayMilliseconds,
                    cancellationToken);
            }

            embeddings.Add(await EmbedContentAsync(content, cancellationToken));
        }

        return embeddings;
    }
}
