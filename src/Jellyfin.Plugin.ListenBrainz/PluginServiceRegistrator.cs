using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.ListenBrainz.Api;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Handlers;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller;
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
        serviceCollection.AddSingleton<IPluginConfigService>(_ =>
            new DefaultPluginConfigService(() => Plugin.RequireInstance().Configuration));

        serviceCollection.AddSingleton<IListenBrainzApiClient>(sp =>
        {
            var clientLogger = GetLogger(sp, "HttpClient");
            var apiLogger = GetLogger(sp, "Api");

            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = new UnderlyingClient(httpClientFactory, clientLogger, null);
            var baseClient = new BaseApiClient(new HttpClientWrapper(httpClient), apiLogger, null);
            return new ListenBrainzApiClient(baseClient, apiLogger);
        });

        serviceCollection.AddSingleton<IMusicBrainzApiClient>(sp =>
        {
            var logger = GetLogger(sp, "MusicBrainzApi");
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
            return new MusicBrainzApiClient(
                clientName,
                Plugin.Version,
                Plugin.SourceUrl,
                httpClientFactory,
                logger);
        });

        AddPluginService<IListenBrainzService, DefaultListenBrainzService>(serviceCollection);
        AddPluginService<IMetadataProviderService, DefaultMetadataProviderService>(
            serviceCollection,
            "MetadataProvider");

        AddPluginService<IValidationService, DefaultValidationService>(serviceCollection, "Validation");
        AddPluginService<IFavoriteSyncService, DefaultFavoriteSyncService>(serviceCollection, "FavoriteSync");
        AddPluginService<IPlaylistTrackMatcher, DefaultPlaylistTrackMatcher>(serviceCollection, "PlaylistTrackMatcher");
        AddPluginService<IPlaylistManager, DefaultPlaylistManager>(serviceCollection, "PlaylistManager");

        serviceCollection.AddSingleton<IPlaybackTrackingService, DefaultPlaybackTrackingService>();

        serviceCollection.AddSingleton<IPlaylistSyncStateService>(sp =>
        {
            var statePath = Path.Join(Plugin.GetDataPath(), "playlist-sync-state.json");
            var storage = new DefaultPersistentJsonService<PlaylistSyncState>(statePath);
            return new DefaultPlaylistSyncStateService(GetLogger(sp, "PlaylistSyncState"), storage);
        });

        serviceCollection.AddSingleton<IListensCachingService>(sp =>
        {
            var cachePath = Path.Join(Plugin.GetDataPath(), "cache.json");
            var storage = new DefaultPersistentJsonService<ListenCacheData>(cachePath);
            return new DefaultListensCachingService(GetLogger(sp, "ListensCache"), storage);
        });

        serviceCollection.AddSingleton<IListenBackupService>(sp =>
        {
            var config = sp.GetRequiredService<IPluginConfigService>();
            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true,
            };

            var storage = new DefaultPersistentJsonService<List<Listen>>(serializerOptions: serializerOptions);
            return new DefaultListenBackupService(GetLogger(sp, "ListensBackup"), config.BackupPath, storage, config);
        });

        AddPluginService<PlaybackStartHandler>(serviceCollection, "PlaybackStartHandler");
        AddPluginService<PlaybackProgressHandler>(serviceCollection, "PlaybackProgressHandler");
        AddPluginService<PlaybackStopHandler>(serviceCollection, "PlaybackStopHandler");
        AddPluginService<UserDataSaveHandler>(serviceCollection, "UserDataSaveHandler");

        serviceCollection.AddHostedService<PluginEventHandlerService>();
    }

    private static void AddPluginService<TInterface, TService>(IServiceCollection services, string logCategory = "")
        where TService : class, TInterface
        where TInterface : class
        => services.AddSingleton<TInterface>(sp =>
            ActivatorUtilities.CreateInstance<TService>(sp, GetLogger(sp, logCategory)));

    private static void AddPluginService<T>(IServiceCollection services, string logCategory)
        where T : class
        => services.AddSingleton(sp => ActivatorUtilities.CreateInstance<T>(sp, GetLogger(sp, logCategory)));

    private static ILogger GetLogger(IServiceProvider sp, string categorySuffix = "")
    {
        var loggerCategory = string.IsNullOrWhiteSpace(categorySuffix)
            ? Plugin.LoggerCategory
            : $"{Plugin.LoggerCategory}.{categorySuffix}";

        return sp.GetRequiredService<ILoggerFactory>().CreateLogger(loggerCategory);
    }
}
