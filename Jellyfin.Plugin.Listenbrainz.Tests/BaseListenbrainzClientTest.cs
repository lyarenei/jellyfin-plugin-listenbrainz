using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests;

public class TestBaseListenbrainzClient : BaseListenbrainzClient
{
    public TestBaseListenbrainzClient(IHttpClientFactory f, ILogger l, ISleepService s) : base(f, l, s) { }

    public async Task<TResponse> ExposedPost<TRequest, TResponse>(TRequest request)
        where TRequest : BaseRequest
        where TResponse : BaseResponse
    {
        return await Post<TRequest, TResponse>(request);
    }

    public async Task<TResponse> ExposedGet<TRequest, TResponse>(TRequest request)
        where TRequest : BaseRequest
        where TResponse : BaseResponse
    {
        return await Get<TRequest, TResponse>(request);
    }
}

public class BaseListenbrainzClientTest
{
     [Fact]
     public async Task BaseListenbrainzClient_SendPOST_OK()
     {
         //Arrange
         var mockFactory = new Mock<IHttpClientFactory>();
         var mockHandler = new Mock<HttpMessageHandler>();
         mockHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

         var client = new HttpClient(mockHandler.Object);
         mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

         var logger = new Mock<ILogger<BaseHttpClient>>();
         var sleepService = new Mock<ISleepService>();
         var apiClient = new TestBaseListenbrainzClient(mockFactory.Object, logger.Object, sleepService.Object);

         var request = new BaseRequest();
         var result = await apiClient.ExposedPost<BaseRequest, BaseResponse>(request);
         Assert.NotNull(result);
     }

     [Fact]
     public async Task BaseListenbrainzClient_SendGET_OK()
     {
         //Arrange
         var mockFactory = new Mock<IHttpClientFactory>();
         var mockHandler = new Mock<HttpMessageHandler>();
         mockHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

         var client = new HttpClient(mockHandler.Object);
         mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

         var logger = new Mock<ILogger<BaseHttpClient>>();
         var sleepService = new Mock<ISleepService>();
         var apiClient = new TestBaseListenbrainzClient(mockFactory.Object, logger.Object, sleepService.Object);

         var request = new BaseRequest();
         var result = await apiClient.ExposedGet<BaseRequest, BaseResponse>(request);
         Assert.NotNull(result);
     }

     [Fact]
     public async Task BaseListenbrainzClient_SendPOST_InvalidJSON()
     {
         //Arrange
         var mockFactory = new Mock<IHttpClientFactory>();
         var mockHandler = new Mock<HttpMessageHandler>();
         mockHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("foo") });

         var client = new HttpClient(mockHandler.Object);
         mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

         var logger = new Mock<ILogger<BaseHttpClient>>();
         var sleepService = new Mock<ISleepService>();
         var apiClient = new TestBaseListenbrainzClient(mockFactory.Object, logger.Object, sleepService.Object);

         var request = new BaseRequest();
         var result = await apiClient.ExposedPost<BaseRequest, BaseResponse>(request);
         Assert.Null(result);
     }

     [Fact]
     public async Task BaseListenbrainzClient_SendGet_InvalidJSON()
     {
         //Arrange
         var mockFactory = new Mock<IHttpClientFactory>();
         var mockHandler = new Mock<HttpMessageHandler>();
         mockHandler.Protected()
             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("foo") });

         var client = new HttpClient(mockHandler.Object);
         mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

         var logger = new Mock<ILogger<BaseHttpClient>>();
         var sleepService = new Mock<ISleepService>();
         var apiClient = new TestBaseListenbrainzClient(mockFactory.Object, logger.Object, sleepService.Object);

         var request = new BaseRequest();
         var result = await apiClient.ExposedGet<BaseRequest, BaseResponse>(request);
         Assert.Null(result);
     }
}
