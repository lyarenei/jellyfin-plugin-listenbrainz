using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception thrown when the listen submission failed.
/// </summary>
public class ListenSubmitException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    public ListenSubmitException(string msg = "") : base(msg)
    {
    }
}
