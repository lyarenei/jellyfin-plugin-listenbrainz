using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception thrown when ListenBrainz submission conditions were not met.
/// </summary>
public class SubmissionConditionsNotMetException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionConditionsNotMetException"/> class.
    /// </summary>
    /// <param name="reason">Why conditions were not met.</param>
    public SubmissionConditionsNotMetException(string reason) : base(reason)
    {
    }
}
