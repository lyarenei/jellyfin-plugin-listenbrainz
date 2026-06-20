using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.ListenBrainz.Api;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Handlers;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnderlyingClient = Jellyfin.Plugin.ListenBrainz.Http.HttpClient;

namespace Jellyfin.Plugin.ListenBrainz;

using ListenCacheData = System.Collections.Generic.Dictionary<
    System.Guid,
    System.Collections.Generic.List<Jellyfin.Plugin.ListenBrainz.Dtos.StoredListen>
>;

/// <summary>
/// Registers plugin services with the Jellyfin dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IPluginConfigService>(_ => new DefaultPluginConfigService(GetConfiguration));
        serviceCollection.AddSingleton<IListenBrainzApiClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            var httpClient = new UnderlyingClient(
                httpClientFactory,
                loggerFactory.CreateLogger(Plugin.LoggerCategory + ".HttpClient"),
                null);

            var wrapper = new HttpClientWrapper(httpClient);
            var baseClient = new BaseApiClient(
                wrapper,
                loggerFactory.CreateLogger(Plugin.LoggerCategory + ".Api"),
                null);

            return new ListenBrainzApiClient(baseClient, loggerFactory.CreateLogger(Plugin.LoggerCategory + ".Api"));
        });

        serviceCollection.AddSingleton<IMusicBrainzApiClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
            return new MusicBrainzApiClient(
                clientName,
                Plugin.Version,
                Plugin.SourceUrl,
                httpClientFactory,
                loggerFactory.CreateLogger(Plugin.LoggerCategory + ".MusicBrainzApi"));
        });

        serviceCollection.AddSingleton<IListenBrainzService>(sp => new DefaultListenBrainzService(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory),
            sp.GetRequiredService<IListenBrainzApiClient>(),
            sp.GetRequiredService<IPluginConfigService>()));

        serviceCollection.AddSingleton<IMetadataProviderService>(sp => new DefaultMetadataProviderService(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".MetadataProvider"),
            sp.GetRequiredService<IMusicBrainzApiClient>(),
            sp.GetRequiredService<IPluginConfigService>()));

        serviceCollection.AddSingleton<IValidationService>(sp => new DefaultValidationService(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".Validation"),
            sp.GetRequiredService<IPluginConfigService>(),
            sp.GetRequiredService<ILibraryManager>()));

        serviceCollection.AddSingleton<IFavoriteSyncService>(sp => new DefaultFavoriteSyncService(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".FavoriteSync"),
            sp.GetRequiredService<IListenBrainzService>(),
            sp.GetRequiredService<IMetadataProviderService>(),
            sp.GetRequiredService<IPluginConfigService>(),
            sp.GetRequiredService<ILibraryManager>(),
            sp.GetRequiredService<IUserManager>(),
            sp.GetRequiredService<IUserDataManager>()));

        serviceCollection.AddSingleton<IPlaybackTrackingService>(_ => new DefaultPlaybackTrackingService());
        serviceCollection.AddSingleton<IListensCachingService>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".ListensCache");
            var cachePath = Path.Join(Plugin.GetDataPath(), "cache.json");
            var storage = new DefaultPersistentJsonService<ListenCacheData>(cachePath);
            return new DefaultListensCachingService(logger, storage);
        });

        serviceCollection.AddSingleton<IListenBackupService>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".ListensBackup");
            var config = sp.GetRequiredService<IPluginConfigService>();
            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true,
            };

            var dummyFilePath = Path.Combine(config.BackupPath, "not-a-backup.json");
            var storage = new DefaultPersistentJsonService<List<Listen>>(dummyFilePath, serializerOptions);
            return new DefaultListenBackupService(logger, config.BackupPath, storage);
        });

        serviceCollection.AddSingleton(sp => new PlaybackStartHandler(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".PlaybackStartHandler"),
            sp.GetRequiredService<IValidationService>(),
            sp.GetRequiredService<IPluginConfigService>(),
            sp.GetRequiredService<IMetadataProviderService>(),
            sp.GetRequiredService<IListenBrainzService>(),
            sp.GetRequiredService<IPlaybackTrackingService>(),
            sp.GetRequiredService<IUserManager>()));

        serviceCollection.AddSingleton(sp => new PlaybackStopHandler(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".PlaybackStopHandler"),
            sp.GetRequiredService<IUserManager>(),
            sp.GetRequiredService<IPluginConfigService>(),
            sp.GetRequiredService<IFavoriteSyncService>(),
            sp.GetRequiredService<IValidationService>(),
            sp.GetRequiredService<IMetadataProviderService>(),
            sp.GetRequiredService<IListenBackupService>(),
            sp.GetRequiredService<IListenBrainzService>(),
            sp.GetRequiredService<IListensCachingService>()));

        serviceCollection.AddSingleton(sp => new UserDataSaveHandler(
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(Plugin.LoggerCategory + ".UserDataSaveHandler"),
            sp.GetRequiredService<IUserManager>(),
            sp.GetRequiredService<IPluginConfigService>(),
            sp.GetRequiredService<IFavoriteSyncService>(),
            sp.GetRequiredService<IValidationService>(),
            sp.GetRequiredService<IMetadataProviderService>(),
            sp.GetRequiredService<IListenBackupService>(),
            sp.GetRequiredService<IListenBrainzService>(),
            sp.GetRequiredService<IListensCachingService>(),
            sp.GetRequiredService<IPlaybackTrackingService>()));

        serviceCollection.AddHostedService<PluginEventHandlerService>();
    }

    private static PluginConfiguration GetConfiguration()
    {
        var instance = Plugin.Instance ?? throw new PluginException("Plugin instance is not available");
        return instance.Configuration;
    }
}
