using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// Handler for <see cref="IUserDataManager.UserDataSaved"/> events.
/// </summary>
public class UserDataSaveHandler : GenericHandler<UserDataSaveEventArgs>
{
    private readonly ILogger _logger;
    private readonly IPluginConfigService _configService;
    private readonly IFavoriteSyncService _favoriteSyncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataSaveHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin config service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    public UserDataSaveHandler(
        ILogger logger,
        IUserManager userManager,
        IPluginConfigService configService,
        IFavoriteSyncService favoriteSyncService) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _favoriteSyncService = favoriteSyncService;
    }

    /// <inheritdoc />
    protected override async Task DoHandleAsync(EventData data)
    {
        _logger.LogDebug(
            "Processing user data save event of {ItemName} for user {UserName} with reason {SaveReason}",
            data.Item.Name,
            data.JellyfinUser.Username,
            data.SaveReason);

        switch (data.SaveReason)
        {
            case UserDataSaveReason.UpdateUserRating:
                _logger.LogTrace("Attempting favorite sync");
                await HandleFavoriteUpdated(data, CancellationToken.None);
                return;
            default:
                throw new PluginException("Unsupported data save reason");
        }
    }

    private async Task HandleFavoriteUpdated(EventData data, CancellationToken cancellationToken)
    {
        if (!_configService.IsImmediateFavoriteSyncEnabled)
        {
            _logger.LogDebug("Immediate favorite sync is disabled, skipping sync");
            return;
        }

        await _favoriteSyncService.SyncToListenBrainzAsync(
            data.Item.Id,
            data.JellyfinUser.Id,
            cancellationToken: cancellationToken);
    }
}
