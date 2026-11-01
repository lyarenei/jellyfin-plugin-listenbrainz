using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// A Jellyfin server running in a container.
/// </summary>
internal sealed class JellyfinContainer : IAsyncDisposable
{
    private const string ContainerNamePrefix = "jellyfin-listenbrainz-itest";
    private const int ServerPort = 8096;
    private const string DataDirectory = "/jellyfin-test/data";
    private const string CacheDirectory = "/jellyfin-test/cache";

    private readonly IContainer _container;

    private JellyfinContainer(IContainer container) => _container = container;

    /// <summary>
    /// Gets the address the server is reachable on from the host.
    /// </summary>
    public Uri BaseAddress => new UriBuilder(
        Uri.UriSchemeHttp,
        _container.Hostname,
        _container.GetMappedPublicPort(ServerPort)).Uri;

    private static bool KeepContainer =>
        Environment.GetEnvironmentVariable("JELLYFIN_ITEST_KEEP_CONTAINER") is "1" or "true";

    /// <summary>
    /// Starts a Jellyfin server container.
    /// </summary>
    /// <param name="jellyfinTag">Tag of the Jellyfin image to run.</param>
    /// <param name="pluginsDirectory">Plugins to install before the server starts, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The started container.</returns>
    public static async Task<JellyfinContainer> StartAsync(
        string jellyfinTag,
        string? pluginsDirectory,
        CancellationToken cancellationToken = default)
    {
        // The image declares /config and /cache as volumes, which would outlive the container.
        // Relocating the state into a plain path keeps it in the container's writable layer.
        var builder = new ContainerBuilder($"docker.io/jellyfin/jellyfin:{jellyfinTag}")
            .WithName($"{ContainerNamePrefix}-{Guid.NewGuid():N}")
            .WithEnvironment("JELLYFIN_DATA_DIR", DataDirectory)
            .WithEnvironment("JELLYFIN_CONFIG_DIR", $"{DataDirectory}/config")
            .WithEnvironment("JELLYFIN_LOG_DIR", $"{DataDirectory}/log")
            .WithEnvironment("JELLYFIN_CACHE_DIR", CacheDirectory)
            .WithEnvironment("XDG_CACHE_HOME", CacheDirectory)
            .WithPortBinding(ServerPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(ServerPort)
                .ForPath("/System/Info/Public")))
            .WithCleanUp(!KeepContainer);

        if (pluginsDirectory is not null)
        {
            builder = builder.WithResourceMapping(new DirectoryInfo(pluginsDirectory), $"{DataDirectory}/plugins/");
        }

        var container = builder.Build();
        await container.StartAsync(cancellationToken).ConfigureAwait(false);
        return new JellyfinContainer(container);
    }

    /// <summary>
    /// Restarts the container. Its writable layer is kept, so anything installed into the server
    /// survives, but the host port can change.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await _container.StopAsync(cancellationToken).ConfigureAwait(false);
        await _container.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the server log.
    /// </summary>
    /// <returns>The combined standard output and error of the container.</returns>
    public async Task<string> GetLogsAsync()
    {
        var (stdout, stderr) = await _container.GetLogsAsync().ConfigureAwait(false);
        return stdout + stderr;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!KeepContainer)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
