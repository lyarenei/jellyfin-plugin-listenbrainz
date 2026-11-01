using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// A package as reported by the server's /Packages endpoint, aggregated from the configured
/// plugin repositories.
/// </summary>
internal sealed class PackageInfo
{
    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GUID of the plugin this package installs.
    /// </summary>
    [JsonPropertyName("guid")]
    [JsonConverter(typeof(FlexibleGuidConverter))]
    public Guid Guid { get; set; }

    /// <summary>
    /// Gets or sets the versions offered by the repositories.
    /// </summary>
    [JsonPropertyName("versions")]
    public List<PackageVersionInfo> Versions { get; set; } = [];
}

/// <summary>
/// A single version of a <see cref="PackageInfo"/>.
/// </summary>
internal sealed class PackageVersionInfo
{
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ABI this version was built against.
    /// </summary>
    [JsonPropertyName("targetAbi")]
    public string? TargetAbi { get; set; }

    /// <summary>
    /// Gets or sets the URL the server downloads the package from.
    /// </summary>
    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the repository offering this version.
    /// </summary>
    [JsonPropertyName("repositoryUrl")]
    public string? RepositoryUrl { get; set; }
}
