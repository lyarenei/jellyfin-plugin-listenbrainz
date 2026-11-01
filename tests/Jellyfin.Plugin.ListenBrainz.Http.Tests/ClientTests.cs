using System.Net;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Microsoft.Extensions.Logging;
using Moq.Protected;

namespace Jellyfin.Plugin.ListenBrainz.Http.Tests;

public class TestClient : HttpClient
{
    public TestClient(IHttpClientFactory f, ILogger l, ISleepService s) : base(f, l, s) { }

    public Task<HttpResponseMessage> ExposedSendRequest(HttpRequestMessage request)
    {
        return SendRequest(request, CancellationToken.None);
    }
}

public class ClientTests
{
    private const string RequestUri = "http://localhost";

    [Fact]
    public async Task Client_SendRequest_OK()
    {
        var client = ClientResponding(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("OK"),
        });

        var result = await client.ExposedSendRequest(Request());

        Assert.NotNull(result);
        Assert.NotEmpty(await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Client_SendRequest_InvalidResponse()
    {
        var client = ClientThrowing(new Exception());

        await Assert.ThrowsAsync<InvalidResponseException>(() => client.ExposedSendRequest(Request()));
    }

    [Fact]
    public async Task Client_SendRequest_RetryException()
    {
        var client = ClientResponding(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable });

        await Assert.ThrowsAsync<RetryException>(() => client.ExposedSendRequest(Request()));
    }

    [Fact]
    public async Task Client_SendRequest_CancellationException_Propagates()
    {
        var client = ClientThrowing(new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.ExposedSendRequest(Request()));
    }

    private static HttpRequestMessage Request() => new(HttpMethod.Post, RequestUri);

    /// <summary>
    /// Builds a client that always returns the specified response.
    /// </summary>
    private static TestClient ClientResponding(HttpResponseMessage response) =>
        ClientWith(handler => handler.ReturnsAsync(response));

    /// <summary>
    /// Builds a client that always throws the specified exception.
    /// </summary>
    private static TestClient ClientThrowing(Exception exception) =>
        ClientWith(handler => handler.ThrowsAsync(exception));

    private static TestClient ClientWith(Action<Moq.Language.Flow.ISetup<HttpMessageHandler, Task<HttpResponseMessage>>> respond)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        respond(handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()));

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new System.Net.Http.HttpClient(handlerMock.Object));

        return new TestClient(factoryMock.Object, Mock.Of<ILogger>(), Mock.Of<ISleepService>());
    }
}
