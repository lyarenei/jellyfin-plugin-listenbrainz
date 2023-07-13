using System;
using Jellyfin.Plugin.Listenbrainz.Exceptions;

namespace Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

/// <summary>
/// ListenBrainz limits, thresholds, etc...
/// </summary>
public static class Limits
{
    /// <summary>
    /// Maximum listens to send in a request.
    /// API docs states this limit is set to 1000, we will be a bit conservative.
    /// </summary>
    public const int MaxListensPerRequest = 100;

    // ListenBrainz rules for submitting listens:
    // Listens should be submitted for tracks when the user has listened to half the track or 4 minutes of the track, whichever is lower.
    // If the user hasn't listened to 4 minutes or half the track, it doesn't fully count as a listen and should not be submitted.
    // https://listenbrainz.readthedocs.io/en/latest/users/api/core.html#post--1-submit-listens

    /// <summary>
    /// ListenBrainz condition A for listen submission - at least 4 minutes of playback.
    /// <seealso cref="MinPlayPercentage"/>
    /// </summary>
    private const long MinPlayTimeTicks = 4 * TimeSpan.TicksPerMinute;

    /// <summary>
    /// ListenBrainz condition B for listen submission - at least 50% of track has been played.
    /// <seealso cref="MinPlayTimeTicks"/>
    /// </summary>
    private const double MinPlayPercentage = 50.00;

    /// <summary>
    /// Convenience method to check if ListenBrainz submission conditions have been met.
    /// </summary>
    /// <param name="playbackPosition">Playback position in track (in ticks).</param>
    /// <param name="runtime">Track runtime (in ticks).</param>
    /// <returns>Conditions have been met.</returns>
    /// <exception cref="ListenBrainzConditionsException">Conditions have not been met.</exception>
    public static bool EvaluateSubmitConditions(long playbackPosition, long runtime)
    {
        var playPercent = ((double)playbackPosition / runtime) * 100;
        var percentageRulePassed = playPercent >= MinPlayPercentage;
        var playtimeRulePassed = playbackPosition >= MinPlayTimeTicks;

        if (percentageRulePassed || playtimeRulePassed) return true;

        var msg = $"Played {playPercent}% (== {playbackPosition} ticks), but required {MinPlayPercentage}% or {MinPlayTimeTicks} ticks";
        throw new ListenBrainzConditionsException(msg);
    }
}
