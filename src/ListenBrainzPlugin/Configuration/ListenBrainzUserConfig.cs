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
        IsEnabled = false;
    }

    /// <summary>
    /// Gets or sets Jellyfin user id.
    /// </summary>
    public Guid JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ListenBrainz submission is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets a value indicating whether ListenBrainz submission is not enabled.
    /// </summary>
    public bool IsNotEnabled => !IsEnabled;
}
