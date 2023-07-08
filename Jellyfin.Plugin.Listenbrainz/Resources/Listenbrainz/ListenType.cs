namespace Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

/// <summary>
/// Listen types accepted by ListenBrainz API.
/// </summary>
public sealed class ListenType
{
    /// <summary>
    /// Single listen type.
    /// Used when sending a single listen.
    /// </summary>
    public static readonly ListenType Single = new ListenType("single");

    /// <summary>
    /// Playing now listen type.
    /// Used when sending a 'playing now' update.
    /// </summary>
    public static readonly ListenType PlayingNow = new ListenType("playing_now");

    /// <summary>
    /// Import listen type.
    /// Used when sending multiple listens at once.
    /// </summary>
    public static readonly ListenType Import = new ListenType("import");

    private ListenType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public string Value { get; private set; }
}
