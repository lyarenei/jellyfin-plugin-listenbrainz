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

    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(1);

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
