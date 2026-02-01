using Jellyfin.Plugin.ListenBrainz.Exceptions;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Validation service interface.
/// One-stop shop for all validation tasks.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Check if an audio item is in any of the allowed libraries.
    /// </summary>
    /// <param name="item">Audio item to check.</param>
    /// <returns>Validation passed.</returns>
    public bool ValidateInAllowedLibrary(Audio item);

    /// <summary>
    /// Check if an audio item has basic metadata available for a listen.
    /// </summary>
    /// <param name="item">Audio item to check.</param>
    /// <returns>Validation passed.</returns>
    public bool ValidateBasicMetadata(Audio item);

    /// <summary>
    /// Check if delta ticks meet ListenBrainz submission conditions, respective to the runtime.
    /// </summary>
    /// <param name="playedTicks">Playback time in ticks.</param>
    /// <param name="runtime">Played item runtime in ticks.</param>
    /// <returns>Validation passed.</returns>
    public bool ValidateSubmitConditions(long playedTicks, long runtime);

    /// <summary>
    /// Check if an audio item meets strict mode conditions.
    /// </summary>
    /// <param name="item">Audio item to validate.</param>
    /// <throws cref="ValidationException">Thrown if strict mode conditions are not met.</throws>
    public void ValidateStrictModeConditions(Audio item);
}
