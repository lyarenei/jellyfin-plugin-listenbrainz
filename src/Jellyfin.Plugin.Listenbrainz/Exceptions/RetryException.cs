using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions
{
    /// <summary>
    /// The exception that is thrown when retry limit has been reached.
    /// </summary>
    public class RetryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryException"/> class.
        /// </summary>
        /// <param name="msg">Exception message.</param>
        public RetryException(string msg) : base(msg)
        {
        }
    }
}
