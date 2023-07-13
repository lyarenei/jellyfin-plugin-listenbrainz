using System.Text.Json;
using ListenBrainzPlugin.ListenBrainzApi.Extensions;

namespace ListenBrainzPlugin.ListenBrainzApi.Json;

/// <summary>
/// Snake case JSON naming policy.
/// </summary>
/// From: https://stackoverflow.com/a/58576400
public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// Gets naming policy instance.
    /// </summary>
    public static SnakeCaseNamingPolicy Instance { get; } = new();

    /// <inheritdoc />
    public override string ConvertName(string name) => name.ToSnakeCase();
}
