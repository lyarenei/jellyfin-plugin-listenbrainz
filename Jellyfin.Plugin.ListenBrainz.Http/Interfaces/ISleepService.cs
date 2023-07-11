namespace Jellyfin.Plugin.ListenBrainz.Http.Interfaces;

/// <summary>
/// Sleep service.
/// </summary>
public interface ISleepService
{
    /// <summary>
    /// Suspends code execution for specified interval.
    /// </summary>
    /// <param name="interval">Time interval in seconds.</param>
    public void Sleep(int interval);
}
