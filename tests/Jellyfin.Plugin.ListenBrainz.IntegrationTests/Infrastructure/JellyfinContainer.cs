using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// A Jellyfin server running in a container.
/// </summary>
internal sealed class JellyfinContainer : IAsyncDisposable
{
    private const string ImageName = "jellyfin-listenbrainz-itest";
    private const int ServerPort = 8096;

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
    /// Builds the server image from the build context and starts a container from it.
    /// </summary>
    /// <param name="buildContext">Directory with the Containerfile and the plugin to bake in.</param>
    /// <param name="jellyfinTag">Tag of the Jellyfin image to build on.</param>
    /// <param name="imageVariant">Suffix keeping images with and without a baked-in plugin apart.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The started container.</returns>
    public static async Task<JellyfinContainer> StartAsync(
        string buildContext,
        string jellyfinTag,
        string imageVariant,
        CancellationToken cancellationToken = default)
    {
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(buildContext)
            .WithDockerfile(PluginPackager.ContainerfileName)
            .WithName($"{ImageName}:{jellyfinTag}-{imageVariant}")
            .WithBuildArgument("JELLYFIN_TAG", jellyfinTag)
            .Build();

        await image.CreateAsync(cancellationToken).ConfigureAwait(false);

        var container = new ContainerBuilder(image)
            .WithName($"{ImageName}-{Guid.NewGuid():N}")
            .WithPortBinding(ServerPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(ServerPort)
                .ForPath("/System/Info/Public")))
            .WithCleanUp(!KeepContainer)
            .Build();

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
