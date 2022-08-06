using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests;

public class TestBaseHttpClient : BaseHttpClient
{
    public TestBaseHttpClient(IHttpClientFactory f, ILogger l, ISleepService s) : base(f, l, s) { }

    public Task<HttpResponseMessage> ExposedSendRequest(HttpRequestMessage request)
    {
        return base.SendRequest(request);
    }
}

public class BaseHttpClientTests
{
    [Fact]
    public async Task BaseApiClient_SendRequest_OK()
    {
        //Arrange
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("OK")
            });

        var client = new HttpClient(mockHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var logger = new Mock<ILogger<BaseHttpClient>>();
        var sleepService = new Mock<ISleepService>();
        var apiClient = new TestBaseHttpClient(mockFactory.Object, logger.Object, sleepService.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost");

        var result = await apiClient.ExposedSendRequest(request);
        Assert.NotNull(result);
        Assert.NotEmpty(await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BaseApiClient_SendRequest_RetryException()
    {
        //Arrange
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable });

        var client = new HttpClient(mockHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var logger = new Mock<ILogger<BaseHttpClient>>();
        var sleepService = new Mock<ISleepService>();
        var apiClient = new TestBaseHttpClient(mockFactory.Object, logger.Object, sleepService.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost");

        await Assert.ThrowsAsync<RetryException>(() => apiClient.ExposedSendRequest(request));
    }
}
