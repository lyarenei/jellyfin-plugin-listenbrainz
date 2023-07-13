using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;

/// <summary>
/// ListenBrainz response.
/// </summary>
public interface IListenBrainzResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether response is OK.
    /// </summary>
    [JsonIgnore]
    public bool IsOk { get; set; }

    /// <summary>
    /// Gets a value indicating whether response is not OK.
    /// </summary>
    [JsonIgnore]
    public virtual bool IsNotOk => !IsOk;
}
