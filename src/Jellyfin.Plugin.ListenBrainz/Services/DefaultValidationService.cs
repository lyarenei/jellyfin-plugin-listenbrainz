using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// A service for validation tasks.
/// </summary>
public class DefaultValidationService : IValidationService
{
    private readonly ILogger _logger;
    private readonly IPluginConfigService _configService;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultValidationService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="libraryManager">Library manager instance.</param>
    public DefaultValidationService(
        ILogger logger,
        IPluginConfigService configService,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _configService = configService;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public bool ValidateInAllowedLibrary(Audio item)
    {
        _logger.LogTrace("Checking if item is in any allowed libraries");

        var isInAllowed = _libraryManager
            .GetCollectionFolders(item)
            .Select(il => il.Id)
            .Intersect(GetAllowedLibraries())
            .Any();

        if (!isInAllowed)
        {
            _logger.LogTrace("Item is not in any allowed library");
            return false;
        }

        _logger.LogTrace("Item is in at least one allowed library");
        return true;
    }

    /// <inheritdoc />
    public bool ValidateBasicMetadata(Audio item)
    {
        _logger.LogTrace("Checking item metadata required for a listen");

        try
        {
            item.AssertHasMetadata();
        }
        catch (ArgumentException e)
        {
            _logger.LogDebug("Validation failed: {Message}", e.Message);
            _logger.LogTrace(e, "Exception stacktrace");
            return false;
        }

        _logger.LogTrace("Item has valid metadata for a listen");
        return true;
    }

    /// <inheritdoc />
    public bool ValidateSubmitConditions(long playedTicks, long runtime)
    {
        _logger.LogTrace("Checking listen submit conditions for playback time");

        try
        {
            Limits.AssertSubmitConditions(playedTicks, runtime);
            _logger.LogTrace("Submit listen playback condition is met");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Submit listen playback condition not met: {Message}", e.Message);
            _logger.LogTrace(e, "Exception stacktrace");
            return false;
        }
    }

    private IEnumerable<Guid> GetAllowedLibraries()
    {
        var allLibraries = _configService.LibraryConfigs;
        if (allLibraries.Count > 0)
        {
            return allLibraries.Where(lc => lc.IsAllowed).Select(lc => lc.Id);
        }

        return _libraryManager.GetMusicLibraries().Select(ml => ml.Id);
    }
}
