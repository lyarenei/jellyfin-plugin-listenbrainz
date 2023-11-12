using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Api.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Api.Tests;

public class BaseClientTests
{
    [Fact]
    public async Task BaseClient_SendRequest_RetryException()
    {
        var mockClient = new Mock<IHttpClient>();
        mockClient
            .Setup(c => c.SendRequest(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Headers = { { Headers.RateLimitResetIn, "1" } }
            });

        var logger = new Mock<ILogger>();
        var sleepService = new Mock<ISleepService>();
        var client = new BaseApiClient(mockClient.Object, logger.Object, sleepService.Object);
        var request = new ValidateTokenRequest("foobar");

        var action = async () => await client.SendPostRequest<ValidateTokenRequest, ValidateTokenResponse>(request, CancellationToken.None);
        var ex = await Record.ExceptionAsync(action);

        Assert.IsType<ListenBrainzException>(ex);
        Assert.Contains("rate limit window", ex.Message);
    }
}
