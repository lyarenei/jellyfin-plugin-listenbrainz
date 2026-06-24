using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of <see cref="IPlaylistTrackMatcher"/>.
/// </summary>
public class DefaultPlaylistTrackMatcher : IPlaylistTrackMatcher
{
    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly IPluginConfigService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlaylistTrackMatcher"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="metadataProvider">Metadata provider service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    public DefaultPlaylistTrackMatcher(
        ILogger logger,
        ILibraryManager libraryManager,
        IMetadataProviderService metadataProvider,
        IPluginConfigService configService)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _metadataProvider = metadataProvider;
        _configService = configService;
    }

    /// <inheritdoc />
    public async Task<BaseItem?> FindMatchAsync(
        IReadOnlyList<BaseItem> candidates,
        User user,
        PlaylistTrack track,
        CancellationToken cancellationToken)
    {
        // 1. Best scenario: Exact match by recording MBID
        if (!string.IsNullOrEmpty(track.RecordingMbid))
        {
            var item = candidates.FirstOrDefault(i => i.GetRecordingMbid() == track.RecordingMbid);
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by recording MBID", track.Title);
                return item;
            }
        }

        // 2. Match by Album MBID + Title
        if (!string.IsNullOrEmpty(track.ReleaseMbid))
        {
            var item = candidates.FirstOrDefault(i =>
                i.ProviderIds.TryGetValue("MusicBrainzAlbum", out var albumMbid) &&
                albumMbid == track.ReleaseMbid &&
                i.Name.Equals(track.Title, StringComparison.OrdinalIgnoreCase));

            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by album MBID + title", track.Title);
                return item;
            }
        }

        // 3. MusicBrainz related recordings (only if MBID available and MusicBrainz enabled)
        if (!string.IsNullOrEmpty(track.RecordingMbid) && _configService.IsMusicBrainzEnabled)
        {
            _logger.LogDebug("Looking up related recordings for recording MBID {Mbid}", track.RecordingMbid);

            var relatedRecordingMbids = await _metadataProvider.GetRelatedRecordingMbidsAsync(
                track.RecordingMbid,
                cancellationToken);

            foreach (var relatedMbid in relatedRecordingMbids)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = candidates.FirstOrDefault(i => i.GetRecordingMbid() == relatedMbid);
                if (item is not null)
                {
                    _logger.LogDebug(
                        "Matched track '{Title}' by related recording MBID {Mbid}",
                        track.Title,
                        relatedMbid);
                    return item;
                }
            }
        }

        // At this point, no matching with IDs can be done, so try plaintext matching.
        // Not using the candidates list as this is easier and more efficient.
        var searchCandidates = SearchJellyfinItems(user, track.Title);

        // 4. Artist + Title search
        if (!string.IsNullOrEmpty(track.Creator))
        {
            var item = searchCandidates.FirstOrDefault(i => ArtistMatches(i, track.Creator));
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by artist + title search", track.Title);
                return item;
            }
        }

        // 5. Album name + Title search
        if (!string.IsNullOrEmpty(track.Album))
        {
            var item = searchCandidates
                .OfType<Audio>()
                .FirstOrDefault(i => i.Album?.Equals(track.Album, StringComparison.OrdinalIgnoreCase) == true);
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by album name + title search", track.Title);
                return item;
            }
        }

        // 6. Title-only search would lead to too many false positives so no point in doing that...

        _logger.LogDebug("No match found for track '{Title}'", track.Title);
        return null;
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

    private IReadOnlyList<BaseItem> SearchJellyfinItems(User user, string searchTerm)
    {
        var searchItems = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            MediaTypes = [MediaType.Audio],
            SearchTerm = searchTerm,
        });

        if (searchItems.Count == 0)
        {
            _logger.LogDebug("No tracks found for search term '{Term}'", searchTerm);
        }
        else
        {
            _logger.LogDebug("Found {Count} tracks for search term '{Term}'", searchItems.Count, searchTerm);
        }

        return searchItems;
    }

    private static bool ArtistMatches(BaseItem item, string artistName)
    {
        // Handle "Artist feat. Other" format - check if any artist is contained in creator string
        if (item is not Audio audio || audio.Artists is null)
        {
            return false;
        }

        return audio.Artists
            .TakeWhile(a => !string.IsNullOrEmpty(a))
            .Any(a => artistName.Contains(a, StringComparison.OrdinalIgnoreCase));
    }
}
