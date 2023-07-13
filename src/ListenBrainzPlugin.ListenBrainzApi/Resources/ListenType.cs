namespace ListenBrainzPlugin.ListenBrainzApi.Resources;

/// <summary>
/// Listen types accepted by ListenBrainz <see cref="Endpoints.SubmitListens"/> endpoint.
/// </summary>
public sealed class ListenType
{
    /// <summary>
    /// Single listen type.
    /// Used when sending a single listen.
    /// </summary>
    public static readonly ListenType Single = new("single");

    /// <summary>
    /// Playing now listen type.
    /// Used when sending a 'playing now' update.
    /// </summary>
    public static readonly ListenType PlayingNow = new("playing_now");

    /// <summary>
    /// Import listen type.
    /// Used when sending multiple listens at once.
    /// </summary>
    public static readonly ListenType Import = new("import");

    private ListenType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets value.
    /// </summary>
    public string Value { get; }
}
