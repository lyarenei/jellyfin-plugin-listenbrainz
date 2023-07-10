using System;

namespace Jellyfin.Plugin.Listenbrainz.Exceptions;

/// <summary>
/// Exception for things related to plugin configuration.
/// </summary>
public class PluginConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigurationException"/> class.
    /// </summary>
    public PluginConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigurationException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public PluginConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigurationException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public PluginConfigurationException(string message, Exception inner) : base(message, inner)
    {
    }
}
