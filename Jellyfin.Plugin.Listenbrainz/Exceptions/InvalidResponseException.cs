using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions
{
    /// <summary>
    /// The exception that is thrown when there was an invalid response.
    /// </summary>
    public class InvalidResponseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidResponseException"/> class.
        /// </summary>
        /// <param name="msg">Exception message.</param>
        public InvalidResponseException(string msg) : base(msg)
        {
        }
    }
}
