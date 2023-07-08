using System;
using Jellyfin.Plugin.Listenbrainz.Exceptions;

namespace Jellyfin.Plugin.Listenbrainz.Models
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
        /// <returns>User can submit listens.</returns>
        /// <exception cref="ListenSubmitException">User cannot submit listen.</exception>
        public bool CanSubmitListen()
        {
            if (!Options.ListenSubmitEnabled) throw new ListenSubmitException("Listen submitting is not enabled.");
            if (string.IsNullOrWhiteSpace(Token)) throw new ListenSubmitException("User does not have API token set.");
            return true;
        }
    }
}
