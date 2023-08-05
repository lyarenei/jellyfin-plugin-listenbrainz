namespace Jellyfin.Plugin.ListenBrainz.Utils;

/// <summary>
/// DateTime utilities.
/// </summary>
public static class DateUtils
{
    /// <summary>
    /// Gets get UNIX timestamp of <see cref="DateTime.Now"/>.
    /// </summary>
    public static long CurrentTimestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
}
