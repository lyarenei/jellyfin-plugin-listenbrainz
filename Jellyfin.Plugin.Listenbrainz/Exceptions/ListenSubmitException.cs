using System;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception related to listen submissions.
/// </summary>
public class ListenSubmitException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    public ListenSubmitException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    /// <param name="listen">Listen instance associated with this exception.</param>
    public ListenSubmitException(Listen listen)
    {
        Listen = listen;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public ListenSubmitException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="listen">Listen instance associated with this exception.</param>
    public ListenSubmitException(string message, Listen listen) : base(message)
    {
        Listen = listen;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public ListenSubmitException(string message, Exception inner) : base(message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenSubmitException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    /// <param name="listen">Listen instance associated with this exception.</param>
    public ListenSubmitException(string message, Exception inner, Listen listen) : base(message, inner)
    {
        Listen = listen;
    }

    /// <summary>
    /// Gets listen instance associated with this exception.
    /// </summary>
    public Listen? Listen { get; }
}
