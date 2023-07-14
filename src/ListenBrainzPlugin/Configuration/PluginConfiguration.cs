using MediaBrowser.Model.Plugins;

namespace ListenBrainzPlugin.Configuration;

/// <summary>
/// ListenBrainz plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private string? _musicBrainzUrlOverride;
    private string? _listenBrainzUrlOverride;
    private bool? _isMusicBrainzEnabledOverride;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Users = new List<ListenBrainzUser>();
    }

    /// <summary>
    /// Gets or sets MusicBrainz API base URL.
    /// </summary>
    public string MusicBrainzApiUrl
    {
        get => _musicBrainzUrlOverride ?? MusicBrainzApi.Resources.Api.BaseUrl;
        set => _musicBrainzUrlOverride = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether MusicBrainz integration is enabled.
    /// </summary>
    public bool IsMusicBrainzEnabled
    {
        get => _isMusicBrainzEnabledOverride ?? true;
        set => _isMusicBrainzEnabledOverride = value;
    }

    /// <summary>
    /// Gets or sets ListenBrainz API base URL.
    /// </summary>
    public string ListenBrainzApiUrl
    {
        get => _listenBrainzUrlOverride ?? ListenBrainzApi.Resources.Api.BaseUrl;
        set => _listenBrainzUrlOverride = value;
    }

    /// <summary>
    /// Gets or sets ListenBrainz user configurations.
    /// </summary>
    public IEnumerable<ListenBrainzUser> Users { get; set; }
}
