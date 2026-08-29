using Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests;

/// <summary>
/// Brings up a Jellyfin server in podman with the locally built plugin installed, completes the
/// setup wizard and authenticates. Shared by every test in <see cref="JellyfinServerCollection"/>,
/// so the server is started once per test run.
/// </summary>
public sealed class JellyfinServerFixture : IAsyncLifetime
{
    private const string DefaultJellyfinTag = "10.11.11";
    private const string AdminUserName = "integration-admin";
    private const string AdminPassword = "integration-password";

    private JellyfinContainer? _container;
    private JellyfinApiClient? _client;

    /// <summary>
    /// Gets the plugin identity declared in build.yaml.
    /// </summary>
    internal PluginBuildManifest Manifest { get; } = PluginBuildManifest.Load();

    /// <summary>
    /// Gets an authenticated API client for the running server.
    /// </summary>
    internal JellyfinApiClient Client =>
        _client ?? throw new InvalidOperationException("The fixture has not been initialized.");

    /// <summary>
    /// Gets the tag of the Jellyfin image under test.
    /// </summary>
    /// <remarks>
    /// Override with the JELLYFIN_ITEST_TAG environment variable to test against another release.
    /// </remarks>
    public static string JellyfinTag =>
        Environment.GetEnvironmentVariable("JELLYFIN_ITEST_TAG") is { Length: > 0 } tag ? tag : DefaultJellyfinTag;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var buildContext = await PluginPackager.CreateBuildContextAsync(Manifest).ConfigureAwait(false);
        _container = await JellyfinContainer.StartAsync(buildContext, JellyfinTag).ConfigureAwait(false);

        var client = new JellyfinApiClient(_container.BaseAddress);
        try
        {
            await client.SetUpServerAsync(AdminUserName, AdminPassword, TimeSpan.FromMinutes(3)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            client.Dispose();
            var logs = await _container.GetLogsAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Could not set up the Jellyfin server.{Environment.NewLine}--- server log ---{Environment.NewLine}{logs}",
                ex);
        }

        _client = client;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads the server log, for diagnostics in failing tests.
    /// </summary>
    /// <returns>The server log.</returns>
    internal Task<string> GetServerLogAsync() =>
        _container?.GetLogsAsync() ?? Task.FromResult("<no container>");
}

/// <summary>
/// Test collection sharing a single Jellyfin server instance.
/// </summary>
[CollectionDefinition(Name)]
public sealed class JellyfinServerCollection : ICollectionFixture<JellyfinServerFixture>
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public const string Name = "Jellyfin server";
}
