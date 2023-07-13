using ListenBrainzPlugin.ListenBrainzApi.Interfaces;

namespace ListenBrainzPlugin.ListenBrainzApi.Models.Responses;

/// <summary>
/// Submit listens response.
/// </summary>
public class SubmitListensResponse : IListenBrainzResponse
{
    /// <inheritdoc />
    public bool IsOk { get; set; }
}
