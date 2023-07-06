using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception thrown when the listen submission failed.
/// </summary>
public class ListenSubmitFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitFailedException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    public ListenSubmitFailedException(string msg = "") : base(msg)
    {
    }
}
