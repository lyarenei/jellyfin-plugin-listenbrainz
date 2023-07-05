using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception thrown when ListenBrainz submission conditions are not met.
/// </summary>
public class SubmissionConditionsNotMet : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmissionConditionsNotMet"/> class.
    /// </summary>
    /// <param name="reason">Why conditions were not met.</param>
    public SubmissionConditionsNotMet(string reason) : base(reason)
    {
    }
}
