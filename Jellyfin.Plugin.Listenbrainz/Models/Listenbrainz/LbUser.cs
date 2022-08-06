using System;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz
{
    /// <summary>
    /// Listenbrainz user model.
    /// </summary>
    public class LbUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LbUser"/> class.
        /// </summary>
        public LbUser()
        {
            Name = string.Empty;
            Token = string.Empty;
            Options = new UserOptions();
        }

        /// <summary>
        /// Gets or sets user name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets API token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets jellyfin user GUID associated with this user.
        /// </summary>
        public Guid MediaBrowserUserId { get; set; }

        /// <summary>
        /// Gets or sets user options.
        /// </summary>
        public UserOptions Options { get; set; }

        /// <summary>
        /// Checks if this user can submit listens.
        /// </summary>
        /// <returns>User can submit listens. If cannot, returns false and reason.</returns>
        public (bool CanSubmit, string Reason) CanSubmitListen()
        {
            if (!Options.ListenSubmitEnabled)
            {
                return (false, "listen submitting disabled");
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                return (false, "no API token set");
            }

            return (true, string.Empty);
        }
    }

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
