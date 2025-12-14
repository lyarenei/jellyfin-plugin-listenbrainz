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
    /// Check if an audio item has sufficient metadata for a 'playing now' listen.
    /// </summary>
    /// <param name="item">Audio item to check.</param>
    /// <returns>Validation passed.</returns>
    public bool ValidateForPlayingNow(Audio item);
}
