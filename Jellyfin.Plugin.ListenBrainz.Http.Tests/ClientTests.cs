using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Http;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Http.Tests;

public class TestClient : Client
{
    public TestClient(IHttpClientFactory f, ILogger l, ISleepService s) : base(f, l, s) { }

    public Task<HttpResponseMessage> ExposedSendRequest(HttpRequestMessage request)
    {
        return base.SendRequest(request, CancellationToken.None);
    }
}

public class ClientTests
{
    private const string RequestUri = "http://localhost";

    [Fact]
    public async Task Client_SendRequest_OK()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("OK")
            });

        var httpClient = new System.Net.Http.HttpClient(mockHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var logger = new Mock<ILogger>();
        var sleepService = new Mock<ISleepService>();
        var client = new TestClient(mockFactory.Object, logger.Object, sleepService.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, RequestUri);

        var result = await client.ExposedSendRequest(request);
        Assert.NotNull(result);
        Assert.NotEmpty(await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Client_SendRequest_InvalidResponse()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new Exception());

        var httpClient = new System.Net.Http.HttpClient(mockHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var logger = new Mock<ILogger>();
        var sleepService = new Mock<ISleepService>();
        var client = new TestClient(mockFactory.Object, logger.Object, sleepService.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, RequestUri);

        await Assert.ThrowsAsync<InvalidResponseException>(() => client.ExposedSendRequest(request));
    }

    [Fact]
    public async Task Client_SendRequest_RetryException()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
                )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable });

        var httpClient = new System.Net.Http.HttpClient(mockHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var logger = new Mock<ILogger>();
        var sleepService = new Mock<ISleepService>();
        var client = new TestClient(mockFactory.Object, logger.Object, sleepService.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, RequestUri);

        await Assert.ThrowsAsync<RetryException>(() => client.ExposedSendRequest(request));
    }

    [Fact]
    public async Task Client_SendRequest_CancellationException_Timeout()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
                )
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new System.Net.Http.HttpClient(mockHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var logger = new Mock<ILogger>();
        var sleepService = new Mock<ISleepService>();
        var client = new TestClient(mockFactory.Object, logger.Object, sleepService.Object);
        var request = new HttpRequestMessage(HttpMethod.Post, RequestUri);

        await Assert.ThrowsAsync<RetryException>(() => client.ExposedSendRequest(request));
    }
}
