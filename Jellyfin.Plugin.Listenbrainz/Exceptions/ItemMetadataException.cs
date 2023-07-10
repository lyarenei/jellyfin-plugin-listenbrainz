using System;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception related to <see cref="BaseItem"/> metadata.
/// </summary>
public class ItemMetadataException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemMetadataException"/> class.
    /// </summary>
    public ItemMetadataException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemMetadataException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public ItemMetadataException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemMetadataException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public ItemMetadataException(string message, Exception inner) : base(message, inner)
    {
    }
}
