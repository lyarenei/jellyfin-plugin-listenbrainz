using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Http.Services;

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
