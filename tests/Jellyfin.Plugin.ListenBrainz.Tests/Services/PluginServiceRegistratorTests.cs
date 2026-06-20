using System;
using System.Net.Http;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Handlers;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

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
    [InlineData(typeof(PlaybackStopHandler))]
    [InlineData(typeof(UserDataSaveHandler))]
    public void RegisterServices_ResolvesRegisteredService(Type serviceType)
    {
        using var provider = BuildProvider();

        var service = provider.GetService(serviceType);

        Assert.NotNull(service);
    }

    [Fact]
    public void RegisterServices_RegistersEventHandlerService()
    {
        using var provider = BuildProvider();

        var hostedServices = provider.GetServices<IHostedService>();

        Assert.Contains(hostedServices, h => h is PluginEventHandlerService);
    }

    private static ServiceProvider BuildProvider()
    {
        MockPlugin.Init(new Mock<IApplicationPaths>(), new Mock<IXmlSerializer>(), new PluginConfiguration());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<IHttpClientFactory>().Object);
        services.AddSingleton(new Mock<ILibraryManager>().Object);
        services.AddSingleton(new Mock<IUserManager>().Object);
        services.AddSingleton(new Mock<IUserDataManager>().Object);
        services.AddSingleton(new Mock<ISessionManager>().Object);

        new PluginServiceRegistrator().RegisterServices(services, new Mock<IServerApplicationHost>().Object);

        return services.BuildServiceProvider();
    }
}
