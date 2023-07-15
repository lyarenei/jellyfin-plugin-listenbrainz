using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ListenBrainzPlugin.Configuration;

/// <summary>
/// ListenBrainz user configuration.
/// </summary>
public class ListenBrainzUserConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzUserConfig"/> class.
    /// </summary>
    public ListenBrainzUserConfig()
    {
        IsListenSubmitEnabled = false;
        ApiToken = string.Empty;
    }

    /// <summary>
    /// Gets or sets Jellyfin user id.
    /// </summary>
    public Guid JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets ListenBrainz API token.
    /// </summary>
    public string ApiToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ListenBrainz submission is enabled.
    /// </summary>
    public bool IsListenSubmitEnabled { get; set; }

    /// <summary>
    /// Gets a value indicating whether ListenBrainz submission is not enabled.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public bool IsNotListenSubmitEnabled => !IsListenSubmitEnabled;

    /// <summary>
    /// Gets or sets a value indicating whether ListenBrainz favorites sync is enabled.
    /// </summary>
    public bool IsFavoritesSyncEnabled { get; set; }
}
