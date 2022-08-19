using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    /// <summary>
    /// Payload of user listens response.
    /// </summary>
    public class UserListensPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserListensPayload"/> class.
        /// </summary>
        public UserListensPayload()
        {
            Listens = new Collection<Listen>();
        }

        /// <summary>
        /// Gets or sets listen count in response.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets UNIX timestamp of last listen in response.
        /// </summary>
        public int LatestListenTs { get; set; }

        /// <summary>
        /// Gets or sets a collection of listens in response.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<Listen> Listens { get; set; }
    }
}
