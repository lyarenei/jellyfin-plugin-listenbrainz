using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception thrown when plugin instance is not available.
/// </summary>
public class PluginInstanceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstanceException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    public PluginInstanceException(string msg = "Plugin instance is not available") : base(msg)
    {
    }
}
