namespace BloodBank.API.Services;

public abstract record ContentGenerationResult
{
    public sealed record Success(string Content) : ContentGenerationResult;

    public sealed record Unavailable : ContentGenerationResult;
}
