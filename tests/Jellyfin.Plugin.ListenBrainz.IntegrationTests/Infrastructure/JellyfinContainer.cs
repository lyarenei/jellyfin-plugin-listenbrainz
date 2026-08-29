using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

internal sealed class JellyfinContainer : IAsyncDisposable
{
    private const string PodmanExecutable = "podman";
    private const int ServerPort = 8096;
    private const int StartAttempts = 3;

    // Podman recycles ephemeral ports for containers being torn down, and a finishing teardown
    // can take the forward of a container published on one with it. These are well out of range.
    private const int MinHostPort = 20000;
    private const int MaxHostPort = 30000;

    private static readonly TimeSpan _serveProbeTimeout = TimeSpan.FromSeconds(90);

    private readonly string _containerName;
    private bool _removed;

    private JellyfinContainer(string containerName, Uri baseAddress)
    {
        _containerName = containerName;
        BaseAddress = baseAddress;
    }

    public Uri BaseAddress { get; }

    public static async Task<JellyfinContainer> StartAsync(
        string buildContext,
        string jellyfinTag,
        string imageVariant,
        CancellationToken cancellationToken = default)
    {
        await EnsurePodmanAvailableAsync(cancellationToken).ConfigureAwait(false);

        // The variant keeps images of servers with and without a baked-in plugin apart, so a run
        // never picks up the image another kind of run left behind.
        var imageTag = $"jellyfin-listenbrainz-itest:{jellyfinTag}-{imageVariant}";
        (await ProcessRunner.RunAsync(
            PodmanExecutable,
            [
                "build",
                "--tag", imageTag,
                "--file", RepositoryLayout.Containerfile,
                "--build-arg", $"JELLYFIN_TAG={jellyfinTag}",
                buildContext,
            ],
            cancellationToken).ConfigureAwait(false))
            .EnsureSuccess();

        var failures = new List<string>();
        for (var attempt = 1; attempt <= StartAttempts; attempt++)
        {
            JellyfinContainer container;
            try
            {
                container = await RunAsync(imageTag, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                // The port is reserved by binding and releasing it, so it can be taken before
                // podman publishes on it. Retrying picks a fresh one.
                failures.Add($"attempt {attempt}: the container could not be started: {ex.Message}");
                continue;
            }

            try
            {
                if (await container.WaitUntilServingAsync(_serveProbeTimeout, cancellationToken).ConfigureAwait(false))
                {
                    return container;
                }

                // Running, but the published port never became reachable: podman lost the forward.
                failures.Add($"attempt {attempt}: {container.BaseAddress} did not serve reliably, container state: " +
                             await container.GetStateAsync(cancellationToken).ConfigureAwait(false));
            }
            catch
            {
                // Nothing else holds a reference to the started container, so it leaks otherwise.
                await container.DisposeAsync(force: true).ConfigureAwait(false);
                throw;
            }

            await container.DisposeAsync(force: true).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Jellyfin container did not become reachable after {StartAttempts} attempts." +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static async Task<JellyfinContainer> RunAsync(string imageTag, CancellationToken cancellationToken)
    {
        var containerName = $"jellyfin-listenbrainz-itest-{Guid.NewGuid():N}";
        var hostPort = ReserveHostPort();
        var result = await ProcessRunner.RunAsync(
            PodmanExecutable,
            [
                "run",
                "--detach",
                "--name", containerName,
                "--publish", $"127.0.0.1:{hostPort.ToString(CultureInfo.InvariantCulture)}:{ServerPort}",
                imageTag,
            ],
            cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            // A run that fails to publish its port still leaves the created container behind.
            await ProcessRunner.RunAsync(PodmanExecutable, ["rm", "--force", "--volumes", containerName])
                .ConfigureAwait(false);

            result.EnsureSuccess();
        }

        return new JellyfinContainer(
            containerName,
            new Uri($"http://127.0.0.1:{hostPort.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static int ReserveHostPort()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var candidate = Random.Shared.Next(MinHostPort, MaxHostPort);
            try
            {
                using var probe = new TcpListener(IPAddress.Loopback, candidate);
                probe.Start();
                probe.Stop();
                return candidate;
            }
            catch (SocketException)
            {
                // Taken; try another.
            }
        }

        throw new InvalidOperationException($"Could not find a free host port in [{MinHostPort}, {MaxHostPort}).");
    }

    // Podman can serve a connection on a published port and then drop the forward, so a streak of
    // successes rather than a single one is what says the mapping is live.
    private async Task<bool> WaitUntilServingAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        const int RequiredSuccesses = 5;

        using var client = new HttpClient { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(10) };
        var deadline = DateTimeOffset.UtcNow + timeout;
        var successes = 0;

        while (true)
        {
            try
            {
                using var response = await client.GetAsync("System/Info/Public", cancellationToken).ConfigureAwait(false);
                successes = response.IsSuccessStatusCode ? successes + 1 : 0;
            }
            catch (HttpRequestException)
            {
                successes = 0;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                successes = 0;
            }

            if (successes >= RequiredSuccesses)
            {
                return true;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Restarts the container and waits until the server is reachable again. The container keeps
    /// its writable layer, so anything installed into the server survives.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        (await ProcessRunner.RunAsync(PodmanExecutable, ["restart", _containerName], cancellationToken)
            .ConfigureAwait(false))
            .EnsureSuccess();

        if (!await WaitUntilServingAsync(_serveProbeTimeout, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"{BaseAddress} did not serve again after a restart, container state: " +
                await GetStateAsync(cancellationToken).ConfigureAwait(false));
        }
    }

    private async Task<string> GetStateAsync(CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            PodmanExecutable,
            ["inspect", _containerName, "--format", "{{.State.Status}} (exit {{.State.ExitCode}})"],
            cancellationToken).ConfigureAwait(false);

        return result.StandardOutput.Trim();
    }

    public async Task<string> GetLogsAsync()
    {
        if (_removed)
        {
            return "<container already removed>";
        }

        var result = await ProcessRunner.RunAsync(PodmanExecutable, ["logs", _containerName]).ConfigureAwait(false);
        return result.StandardOutput + result.StandardError;
    }

    public ValueTask DisposeAsync() => DisposeAsync(force: false);

    private async ValueTask DisposeAsync(bool force)
    {
        if (_removed)
        {
            return;
        }

        if (!force && Environment.GetEnvironmentVariable("JELLYFIN_ITEST_KEEP_CONTAINER") is "1" or "true")
        {
            return;
        }

        // Disposal must not throw, so a failed removal is reflected in the flag instead.
        var result = await ProcessRunner.RunAsync(PodmanExecutable, ["rm", "--force", "--volumes", _containerName])
            .ConfigureAwait(false);

        _removed = result.ExitCode == 0;
    }

    private static async Task EnsurePodmanAvailableAsync(CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(PodmanExecutable, ["info", "--format", "{{.Host.Arch}}"], cancellationToken)
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "podman is not usable. Install podman and, on macOS or Windows, start the podman machine " +
                $"with 'podman machine start'.{Environment.NewLine}{result.StandardError}");
        }
    }
}
