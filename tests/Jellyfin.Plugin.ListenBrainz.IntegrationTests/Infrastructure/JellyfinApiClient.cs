using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

internal sealed class JellyfinApiClient : IDisposable
{
    private const string ClientName = "ListenBrainzIntegrationTests";
    private const string DeviceName = "podman";
    private const string ClientVersion = "1.0.0";

    private const string RepositoryName = "ListenBrainz integration tests";

    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _repositoryPollDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _installPollDelay = TimeSpan.FromSeconds(1);

    private readonly HttpClient _client;
    private readonly string _deviceId = Guid.NewGuid().ToString("N");

    public JellyfinApiClient(Uri baseAddress)
    {
        _client = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(30) };
        SetAuthorization(token: null);
    }

    public HttpClient Http => _client;

    public async Task SetUpServerAsync(
        string userName,
        string password,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        // /health reports healthy while the server still initializes; the wizard endpoint answers
        // 503 until setup can actually proceed, which makes it the readiness signal.
        (await GetAsync("Startup/User", deadline, cancellationToken).ConfigureAwait(false)).Dispose();

        await PostAsync(
            "Startup/Configuration",
            new { UICulture = "en-US", MetadataCountryCode = "US", PreferredMetadataLanguage = "en" },
            deadline,
            cancellationToken).ConfigureAwait(false);

        await PostAsync(
            "Startup/User",
            new { Name = userName, Password = password },
            deadline,
            cancellationToken).ConfigureAwait(false);

        await PostAsync(
            "Startup/RemoteAccess",
            new { EnableRemoteAccess = true, EnableAutomaticPortMapping = false },
            deadline,
            cancellationToken).ConfigureAwait(false);

        await PostAsync("Startup/Complete", content: null, deadline, cancellationToken).ConfigureAwait(false);

        SetAuthorization(await AuthenticateAsync(userName, password, deadline, cancellationToken).ConfigureAwait(false));

        await WaitUntilSettledAsync(deadline, cancellationToken).ConfigureAwait(false);
    }

    // Completing the wizard makes Jellyfin reload its network settings and re-bind the HTTP
    // listener, so a single successful response does not mean the server is up for good.
    private async Task WaitUntilSettledAsync(DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        const int RequiredSuccesses = 5;

        var successes = 0;
        while (true)
        {
            // Probed directly: SendWithRetryAsync would retry a mid-streak failure away.
            try
            {
                using var response = await _client.GetAsync("System/Info", cancellationToken).ConfigureAwait(false);
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
                return;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"The server did not serve {RequiredSuccesses} consecutive authorized requests before the deadline.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<PluginInfo>> GetPluginsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("Plugins", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<PluginInfo>>(cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The server returned an empty plugin list payload.");
    }

    /// <summary>
    /// Points the server at a single plugin repository and installs the requested plugin version
    /// from it, the way a user would from the dashboard. Returns once the server has downloaded
    /// and registered the plugin; loading it still takes a restart.
    /// </summary>
    /// <param name="manifest">The plugin identity to install.</param>
    /// <param name="source">The repository and version to install from.</param>
    /// <param name="timeout">Budget for the whole installation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InstallFromRepositoryAsync(
        PluginBuildManifest manifest,
        PluginSource source,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var repositoryUrl = source.RepositoryUrl.ToString();

        // The list is replaced rather than appended to: the default Jellyfin repository has
        // nothing to contribute here and would be fetched on every package listing.
        await PostAsync(
            "Repositories",
            new[] { new { Name = RepositoryName, Url = repositoryUrl, Enabled = true } },
            deadline,
            cancellationToken).ConfigureAwait(false);

        await WaitForPublishedVersionAsync(manifest, source, deadline, cancellationToken).ConfigureAwait(false);

        var query = $"?assemblyGuid={manifest.PluginId:D}" +
                    $"&version={Uri.EscapeDataString(source.Version)}" +
                    $"&repositoryUrl={Uri.EscapeDataString(repositoryUrl)}";

        await PostAsync(
            $"Packages/Installed/{Uri.EscapeDataString(manifest.Name)}{query}",
            content: null,
            deadline,
            cancellationToken).ConfigureAwait(false);

        await WaitForRegisteredPluginAsync(manifest, source, deadline, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until the server serves authorized requests reliably, for use after a restart.
    /// </summary>
    /// <param name="timeout">How long to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
        WaitUntilSettledAsync(DateTimeOffset.UtcNow + timeout, cancellationToken);

    /// <summary>
    /// Lists the packages the configured repositories offer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The available packages.</returns>
    public async Task<IReadOnlyList<PackageInfo>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync("Packages", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<PackageInfo>>(cancellationToken).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The server returned an empty package list payload.");
    }

    // The pipeline publishes to the repository right before the tests run, so a manifest that does
    // not carry the version yet is worth waiting on rather than failing outright.
    private async Task WaitForPublishedVersionAsync(
        PluginBuildManifest manifest,
        PluginSource source,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        var offered = "the server did not answer with a package list";

        while (true)
        {
            try
            {
                var package = (await GetPackagesAsync(cancellationToken).ConfigureAwait(false))
                    .FirstOrDefault(p => p.Guid == manifest.PluginId);

                if (package is null)
                {
                    offered = "the repository does not list the plugin at all";
                }
                else if (package.Versions.Exists(v => string.Equals(v.Version, source.Version, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
                else
                {
                    offered = $"offered versions: {string.Join(", ", package.Versions.Select(v => v.Version))}";
                }
            }
            catch (HttpRequestException ex)
            {
                offered = ex.Message;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"{source.RepositoryUrl} does not offer {manifest.Name} {source.Version} ({offered}).");
            }

            await Task.Delay(_repositoryPollDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    // Installation is asynchronous: the request only queues it. The server registers the plugin
    // once the package is downloaded and unpacked, which is what makes the install observable.
    private async Task WaitForRegisteredPluginAsync(
        PluginBuildManifest manifest,
        PluginSource source,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var plugins = await GetPluginsAsync(cancellationToken).ConfigureAwait(false);
            if (plugins.Any(p => p.Id == manifest.PluginId && p.Version == source.Version))
            {
                return;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"The server did not install {manifest.Name} {source.Version} from {source.RepositoryUrl} " +
                    $"before the deadline. Reported plugins: " +
                    $"{string.Join(", ", plugins.Select(p => $"{p.Name} {p.Version} ({p.Status})"))}.");
            }

            await Task.Delay(_installPollDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => _client.Dispose();

    private async Task<string> AuthenticateAsync(
        string userName,
        string password,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(
            () => _client.PostAsJsonAsync("Users/AuthenticateByName", new { Username = userName, Pw = password }, cancellationToken),
            "POST Users/AuthenticateByName",
            deadline,
            cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        return document.RootElement.GetProperty("AccessToken").GetString()
               ?? throw new InvalidOperationException("Authentication succeeded but returned no access token.");
    }

    private Task<HttpResponseMessage> GetAsync(string path, DateTimeOffset deadline, CancellationToken cancellationToken) =>
        SendWithRetryAsync(() => _client.GetAsync(path, cancellationToken), $"GET {path}", deadline, cancellationToken);

    private async Task PostAsync(string path, object? content, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(
            () => content is null
                ? _client.PostAsync(path, content: null, cancellationToken)
                : _client.PostAsJsonAsync(path, content, cancellationToken),
            $"POST {path}",
            deadline,
            cancellationToken).ConfigureAwait(false);
    }

    // A booting server initializes its network settings after it starts answering, so calls made
    // during the wizard can hit a refused connection or a 404 for a route that is not populated yet.
    private static async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> send,
        string description,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        string? lastFailure = null;

        while (true)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await send().ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    // The caller owns it from here.
                    var successful = response;
                    response = null;
                    return successful;
                }

                lastFailure = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                var isTransient = IsTransient(response.StatusCode);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!isTransient)
                {
                    throw new InvalidOperationException($"{description} failed with {lastFailure}: {Summarize(body)}");
                }
            }
            catch (HttpRequestException ex)
            {
                lastFailure = ex.Message;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastFailure = ex.Message;
            }
            finally
            {
                // Also covers the body read faulting on a connection dropped mid-response.
                response?.Dispose();
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException($"{description} did not succeed before the deadline. Last failure: {lastFailure}");
            }

            await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.NotFound or
        HttpStatusCode.RequestTimeout or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout;

    private static string Summarize(string body) =>
        body.Length <= 500 ? body : string.Concat(body.AsSpan(0, 500), "…");

    private void SetAuthorization(string? token)
    {
        var parameters = $"Client=\"{ClientName}\", Device=\"{DeviceName}\", DeviceId=\"{_deviceId}\", Version=\"{ClientVersion}\"";
        if (token is not null)
        {
            parameters += $", Token=\"{token}\"";
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MediaBrowser", parameters);
    }
}
