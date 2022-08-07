namespace Jellyfin.Plugin.Listenbrainz.Models
{
    /// <summary>
    /// Listenbrainz user options.
    /// </summary>
    public class UserOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether submitting listens is enabled.
        /// </summary>
        public bool ListenSubmitEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether favorites syncing is enabled.
        /// </summary>
        public bool SyncFavoritesEnabled { get; set; }
    }
}
