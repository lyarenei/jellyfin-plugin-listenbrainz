using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Handlers;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Tests.TestKit;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

[Collection(PluginInstanceCollection.Name)]
public class PluginServiceRegistratorTests
{
    [Theory]
    [InlineData(typeof(IPluginConfigService))]
    [InlineData(typeof(IListenBrainzApiClient))]
    [InlineData(typeof(IMusicBrainzApiClient))]
    [InlineData(typeof(IListenBrainzService))]
    [InlineData(typeof(IMetadataProviderService))]
    [InlineData(typeof(IValidationService))]
    [InlineData(typeof(IFavoriteSyncService))]
    [InlineData(typeof(IPlaybackTrackingService))]
    [InlineData(typeof(IListensCachingService))]
    [InlineData(typeof(IListenBackupService))]
    [InlineData(typeof(PlaybackStartHandler))]
    [InlineData(typeof(PlaybackProgressHandler))]
    [InlineData(typeof(PlaybackStopHandler))]
    [InlineData(typeof(UserDataSaveHandler))]
    public void RegisterServices_ResolvesRegisteredService(Type serviceType)
    {
        using var plugin = new MockPlugin();
        using var provider = BuildProvider();

        var service = provider.GetService(serviceType);

        Assert.NotNull(service);
    }

    [Fact]
    public void RegisterServices_RegistersEventHandlerService()
    {
        using var plugin = new MockPlugin();
        using var provider = BuildProvider();

        var hostedServices = provider.GetServices<IHostedService>();

        Assert.Contains(hostedServices, h => h is PluginEventHandlerService);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHttpClientFactory>());
        services.AddSingleton(Mock.Of<ILibraryManager>());
        services.AddSingleton(Mock.Of<IUserManager>());
        services.AddSingleton(Mock.Of<IUserDataManager>());
        services.AddSingleton(Mock.Of<ISessionManager>());

        new PluginServiceRegistrator().RegisterServices(services, Mock.Of<IServerApplicationHost>());

        return services.BuildServiceProvider();
    }
}
