namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// How the plugin under test gets onto the server.
/// </summary>
internal enum PluginSourceKind
{
    /// <summary>
    /// Built from the working tree and baked into the server image.
    /// </summary>
    Local,

    /// <summary>
    /// Downloaded and installed by the server from a Jellyfin plugin repository.
    /// </summary>
    Repository,
}

/// <summary>
/// Where the plugin under test comes from. Locally that is the working tree; in CI it is the
/// plugin repository the pipeline has just published to, so the tests exercise what a user
/// would actually install.
/// </summary>
internal sealed class PluginSource
{
    private const string RepositoryVariable = "JELLYFIN_ITEST_PLUGIN_REPOSITORY";
    private const string VersionVariable = "JELLYFIN_ITEST_PLUGIN_VERSION";

    private readonly Uri? _repositoryUrl;

    private PluginSource(PluginSourceKind kind, Uri? repositoryUrl, string version)
    {
        Kind = kind;
        _repositoryUrl = repositoryUrl;
        Version = version;
    }

    /// <summary>
    /// Gets the kind of this source.
    /// </summary>
    public PluginSourceKind Kind { get; }

    /// <summary>
    /// Gets the version of the plugin under test.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the URL of the repository manifest to install from.
    /// </summary>
    public Uri RepositoryUrl => _repositoryUrl
        ?? throw new InvalidOperationException("A local plugin source has no repository URL.");

    /// <summary>
    /// Reads the source configuration from the environment. Setting
    /// <c>JELLYFIN_ITEST_PLUGIN_REPOSITORY</c> to a manifest URL switches to a repository install;
    /// <c>JELLYFIN_ITEST_PLUGIN_VERSION</c> overrides the version to install, which otherwise comes
    /// from build.yaml.
    /// </summary>
    /// <param name="manifest">The plugin build manifest.</param>
    /// <returns>The configured plugin source.</returns>
    public static PluginSource FromEnvironment(PluginBuildManifest manifest)
    {
        var version = Environment.GetEnvironmentVariable(VersionVariable) is { Length: > 0 } configuredVersion
            ? configuredVersion
            : manifest.Version;

        if (Environment.GetEnvironmentVariable(RepositoryVariable) is not { Length: > 0 } repository)
        {
            return new PluginSource(PluginSourceKind.Local, repositoryUrl: null, manifest.Version);
        }

        if (!Uri.TryCreate(repository, UriKind.Absolute, out var repositoryUrl))
        {
            throw new InvalidOperationException(
                $"{RepositoryVariable} is set to '{repository}', which is not an absolute URL. " +
                "It has to point at a Jellyfin plugin repository manifest.");
        }

        return new PluginSource(PluginSourceKind.Repository, repositoryUrl, version);
    }

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        PluginSourceKind.Repository => $"{Version} from {RepositoryUrl}",
        _ => $"{Version} built from the working tree",
    };
}
