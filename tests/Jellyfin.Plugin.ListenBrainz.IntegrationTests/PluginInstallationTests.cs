using Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests;

/// <summary>
/// Verifies that a Jellyfin server starts up with the plugin installed and running.
/// </summary>
[Collection(JellyfinServerCollection.Name)]
[Trait("Category", "Integration")]
public sealed class PluginInstallationTests
{
    private readonly JellyfinServerFixture _server;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallationTests"/> class.
    /// </summary>
    /// <param name="server">The shared Jellyfin server.</param>
    public PluginInstallationTests(JellyfinServerFixture server) => _server = server;

    [Fact]
    public async Task Server_reports_the_plugin_as_installed_and_active()
    {
        var plugins = await _server.Client.GetPluginsAsync();
        var plugin = plugins.SingleOrDefault(p => p.Id == _server.Manifest.PluginId);

        if (plugin is null)
        {
            throw new XunitException(
                "The server did not load the plugin. Reported plugins: " +
                $"{string.Join(", ", plugins.Select(p => $"{p.Name} {p.Version} ({p.Status})"))}." +
                $"{Environment.NewLine}--- server log ---{Environment.NewLine}{await _server.GetServerLogAsync()}");
        }

        Assert.Equal(_server.Manifest.Name, plugin.Name);
        Assert.Equal(_server.Manifest.Version, plugin.Version);
        Assert.Equal("Active", plugin.Status);
    }

    [Fact]
    public async Task Server_serves_the_plugin_configuration_page()
    {
        // The page is an embedded resource: serving it proves the assembly was loaded, not just found.
        using var response = await _server.Client.Http.GetAsync(
            $"web/ConfigurationPage?name={_server.Manifest.Name}");

        response.EnsureSuccessStatusCode();
        Assert.Contains("ListenBrainz", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Server_log_reports_the_plugin_as_loaded()
    {
        var log = await _server.GetServerLogAsync();

        Assert.Contains($"Loaded plugin: {_server.Manifest.Name} {_server.Manifest.Version}", log, StringComparison.Ordinal);
    }
}
