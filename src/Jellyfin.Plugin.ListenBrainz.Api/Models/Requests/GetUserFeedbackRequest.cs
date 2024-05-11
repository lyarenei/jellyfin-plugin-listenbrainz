using System.Globalization;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// User feedback request.
/// </summary>
public class GetUserFeedbackRequest : IListenBrainzRequest
{
    private readonly string _userName;
    private readonly CompositeFormat _endpointFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserFeedbackRequest"/> class.
    /// </summary>
    /// <param name="userName">ListenBrainz username.</param>
    /// <param name="score">Feedback type (score) to get. If unset, both loved and hated feedbacks are returned.</param>
    /// <param name="count">Number of feedbacks to get.</param>
    /// <param name="offset">Feedback list offset.</param>
    /// <param name="metadata">Include metadata.</param>
    public GetUserFeedbackRequest(
        string userName,
        FeedbackScore? score = null,
        int count = Limits.DefaultItemsPerGet,
        int offset = 0,
        bool? metadata = null)
    {
        _endpointFormat = CompositeFormat.Parse(Endpoints.ListensEndpoint);
        _userName = userName;
        BaseUrl = General.BaseUrl;
        QueryDict = new Dictionary<string, string>
        {
            { "count", count.ToString(NumberFormatInfo.InvariantInfo) },
            { "offset", offset.ToString(NumberFormatInfo.InvariantInfo) }
        };

        if (score is not null)
        {
            QueryDict.Add("score", score.Value.ToString(NumberFormatInfo.InvariantInfo));
        }

        if (metadata is not null)
        {
            QueryDict.Add("metadata", metadata.ToString()!.ToLowerInvariant());
        }
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => string.Format(CultureInfo.InvariantCulture, _endpointFormat, _userName);

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
