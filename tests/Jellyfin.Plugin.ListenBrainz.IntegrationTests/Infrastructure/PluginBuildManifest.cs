using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// The repository's build.yaml, which is the single source of truth for plugin identity
/// and for the set of assemblies that make up a plugin release.
/// </summary>
internal sealed class PluginBuildManifest
{
    /// <summary>
    /// Gets or sets the plugin name. Also used as the plugin directory name prefix.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin GUID.
    /// </summary>
    public string Guid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyfin ABI version the plugin targets.
    /// </summary>
    public string TargetAbi { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin owner.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short plugin overview.
    /// </summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assemblies shipped in a plugin release.
    /// </summary>
    public List<string> Artifacts { get; set; } = [];

    /// <summary>
    /// Gets the directory name Jellyfin expects for an installed plugin.
    /// </summary>
    public string InstallDirectoryName => $"{Name}_{Version}";

    /// <summary>
    /// Gets the plugin GUID in the dashed form used by the plugin manifest.
    /// </summary>
    public Guid PluginId => System.Guid.Parse(Guid);

    /// <summary>
    /// Loads the manifest from the repository root.
    /// </summary>
    /// <returns>The parsed manifest.</returns>
    public static PluginBuildManifest Load()
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var manifest = deserializer.Deserialize<PluginBuildManifest>(
            File.ReadAllText(RepositoryLayout.BuildManifest));

        if (manifest.Artifacts.Count == 0)
        {
            throw new InvalidOperationException($"{RepositoryLayout.BuildManifest} lists no artifacts.");
        }

        return manifest;
    }
}
