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
        /// <exception cref="PluginConfigurationException">User cannot submit listen.</exception>
        public void AssertCanSubmitListen()
        {
            if (!Options.ListenSubmitEnabled)
            {
                throw new PluginConfigurationException("Listen submitting for this user is not enabled.");
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                throw new PluginConfigurationException("This user does not have API token set.");
            }
        }
    }
}
