namespace BloodBank.API.Services;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string GenerationModel { get; set; } = "gemini-3.8-flash";
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public int EmbeddingBatchDelayMilliseconds { get; set; } = 1000;
}
