using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception thrown when ListenBrainz submission conditions were not met.
/// </summary>
public class ListenBrainzConditionsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzConditionsException"/> class.
    /// </summary>
    /// <param name="reason">Why conditions were not met.</param>
    public ListenBrainzConditionsException(string reason) : base(reason)
    {
    }
}
