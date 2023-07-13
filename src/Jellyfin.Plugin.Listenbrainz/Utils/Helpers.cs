using System;
using System.IO;
using System.Net.Http;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Utils
{
    /// <summary>
    /// Various helpers.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Convert datetime to UNIX timestamp.
        /// </summary>
        /// <param name="dateTime">Datetime to convert.</param>
        /// <returns>UNIX timestamp.</returns>
        public static long TimestampFromDatetime(DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

        /// <summary>
        /// Get current time in UNIX time.
        /// </summary>
        /// <returns>UNIX timestamp.</returns>
        public static long GetCurrentTimestamp() => TimestampFromDatetime(DateTime.UtcNow);

        /// <summary>
        /// Get path to listen cache file.
        /// </summary>
        /// <returns>Path to the file.</returns>
        public static string GetListenCacheFilePath() => Path.Join(Plugin.GetDataPath(), "cache.json");

        /// <summary>
        /// Get a MusicBrainz client.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>MusicBrainz client.</returns>
        public static IMusicBrainzClient GetMusicBrainzClient(
            IHttpClientFactory httpClientFactory,
            ILogger<IMusicBrainzClient> logger)
        {
            var config = Plugin.GetConfiguration();
            if (!config.GlobalConfig.MusicbrainzEnabled) return new DummyMusicbrainzClient(logger);

            var apiClient = new MusicBrainzApiClient(
                config.MusicBrainzUrl,
                "JellyfinListenBrainzPlugin",
                Plugin.Version,
                "https://github.com/lyarenei/jellyfin-plugin-listenbrainz",
                httpClientFactory,
                logger);

            return new MusicBrainzClient(logger, apiClient);
        }
    }
}
