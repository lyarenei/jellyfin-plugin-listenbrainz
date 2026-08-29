using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// Reads GUIDs in any of the formats accepted by <see cref="Guid.Parse(string)"/>.
/// Jellyfin serializes them without dashes, which the default converter rejects.
/// </summary>
internal sealed class FlexibleGuidConverter : JsonConverter<Guid>
{
    /// <inheritdoc />
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? Guid.Empty : Guid.Parse(value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString("N"));
    }
}
