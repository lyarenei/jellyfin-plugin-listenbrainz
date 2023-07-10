using Jellyfin.Plugin.ListenBrainz.HttpClient.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.HttpClient.Services;

/// <summary>
/// Implementation of <see cref="ISleepService"/>.
/// </summary>
public class DefaultSleepService : ISleepService
{
    /// <inheritdoc />
    public void Sleep(int interval)
    {
        Thread.Sleep(interval * 1000);
    }
}
