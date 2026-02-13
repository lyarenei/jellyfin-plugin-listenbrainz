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

    /// <summary>
    /// Get a random nth minute in an hour.
    /// </summary>
    /// <param name="min">Lowest boundary. Must be in &lt;0-59&gt;.</param>
    /// <param name="max">Highest boundary. Must be in &lt;0-59&gt;.</param>
    /// <returns>Random nth minute in an hour.</returns>
    public static long GetRandomMinute(int min = 0, int max = 50)
    {
        if (min is < 0 or > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(min));
        }

        if (max is < 0 or > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(max));
        }

        var random = new Random();
        return random.Next(min, max);
    }
}
