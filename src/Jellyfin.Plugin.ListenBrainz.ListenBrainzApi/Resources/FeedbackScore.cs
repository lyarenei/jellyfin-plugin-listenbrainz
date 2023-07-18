namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Resources;

/// <summary>
/// Accepted values for feedback score.
/// </summary>
public enum FeedbackScore
{
    /// <summary>
    /// Neutral score, clears any score set previously.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// Loved score.
    /// </summary>
    Loved = 1,

    /// <summary>
    /// Hated score.
    /// </summary>
    Hated = -1
}
