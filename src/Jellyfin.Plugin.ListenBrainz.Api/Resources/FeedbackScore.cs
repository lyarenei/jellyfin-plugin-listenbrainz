namespace Jellyfin.Plugin.ListenBrainz.Api.Resources;

/// <summary>
/// Accepted values for feedback score.
/// </summary>
public sealed class FeedbackScore
{
    /// <summary>
    /// Neutral score, clears any score set previously.
    /// </summary>
    public static readonly FeedbackScore Neutral = new(0);

    /// <summary>
    /// Loved score.
    /// </summary>
    public static readonly FeedbackScore Loved = new(1);

    /// <summary>
    /// Hated score.
    /// </summary>
    public static readonly FeedbackScore Hated = new(-1);

    private FeedbackScore(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets value.
    /// </summary>
    public int Value { get; }
}
