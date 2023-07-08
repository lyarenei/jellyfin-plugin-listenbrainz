using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception related to listen submissions.
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
