using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// A plugin entry as reported by the server's /Plugins endpoint.
/// </summary>
internal sealed class PluginInfo
{
    /// <summary>
    /// Gets or sets the plugin name.
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    [JsonPropertyName("Version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin GUID.
    /// </summary>
    [JsonPropertyName("Id")]
    [JsonConverter(typeof(FlexibleGuidConverter))]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the plugin status, for example Active or Malfunctioned.
    /// </summary>
    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the plugin configuration file.
    /// </summary>
    [JsonPropertyName("ConfigurationFileName")]
    public string? ConfigurationFileName { get; set; }
}
