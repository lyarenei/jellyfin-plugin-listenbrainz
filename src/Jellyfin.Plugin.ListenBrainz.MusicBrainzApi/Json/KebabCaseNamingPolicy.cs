using System.Text.Json;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Extensions;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Json;

/// <summary>
/// Kebab case JSON naming policy.
/// </summary>
/// Inspired by: https://stackoverflow.com/a/58576400
public class KebabCaseNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// Gets naming policy instance.
    /// </summary>
    public static KebabCaseNamingPolicy Instance { get; } = new();

    /// <inheritdoc />
    public override string ConvertName(string name) => name.ToKebabCase();
}
