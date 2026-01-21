namespace Jellyfin.Plugin.ListenBrainz.Common.Extensions;

/// <summary>
/// Extensions for <see cref="Exception"/>.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Gets the full message of an exception, including inner exceptions.
    /// Source - https://stackoverflow.com/a/35084416.
    /// Posted by ThomazMoura, modified by community. See post 'Timeline' for change history.
    /// Retrieved 2025-12-23, License - CC BY-SA 3.0.
    /// </summary>
    /// <param name="ex">Exception.</param>
    /// <returns>Exception message.</returns>
    public static string GetFullMessage(this Exception ex)
    {
        return ex.InnerException == null
            ? ex.Message
            : ex.Message + " --> " + ex.InnerException.GetFullMessage();
    }
}
