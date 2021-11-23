using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class MbClient : BaseMbClient
    {
        public MbClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

        public async Task<RecordingIdResponse> GetRecordingId(string trackId)
        {
            return await Get<RecordingIdRequest, RecordingIdResponse>(new RecordingIdRequest(trackId));
        }
    }
}
