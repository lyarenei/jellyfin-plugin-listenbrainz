namespace Jellyfin.Plugin.ListenBrainz.Common;

/// <summary>
/// Various functions which can be used across the project.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Get a new ID. The ID is 7 characters long.
    /// </summary>
    /// <returns>New ID.</returns>
    public static string GetNewId()
    {
        return Guid.NewGuid().ToString("N")[..7];
    }
}
