using System.Threading;

namespace Jellyfin.Plugin.Listenbrainz.Services
{
    /// <summary>
    /// Service providing sleep capabilities.
    /// </summary>
    public interface ISleepService
    {
        /// <summary>
        /// Suspends code execution for specified interval.
        /// </summary>
        /// <param name="interval">Time interval in seconds.</param>
        public void Sleep(int interval);
    }

    /// <summary>
    /// Implementation of <see cref="ISleepService"/>.
    /// </summary>
    public class SleepService : ISleepService
    {
        /// <inheritdoc />
        public void Sleep(int interval)
        {
            Thread.Sleep(interval * 1000);
        }
    }
}
