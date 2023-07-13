using ListenBrainzPlugin.Http.Interfaces;

namespace ListenBrainzPlugin.Http.Services;

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