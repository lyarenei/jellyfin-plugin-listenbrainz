using System;

namespace Jellyfin.Plugin.Listenbrainz.Utils
{
    public static class Helpers
    {
        public static long GetCurrentTimestamp() => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }
}
