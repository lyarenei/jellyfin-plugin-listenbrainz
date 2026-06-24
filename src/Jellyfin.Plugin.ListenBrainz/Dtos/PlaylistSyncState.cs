using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Persistent state for ListenBrainz playlist sync.
/// </summary>
public class PlaylistSyncState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistSyncState"/> class.
    /// </summary>
    public PlaylistSyncState()
    {
        Mappings = [];
    }

    /// <summary>
    /// Gets or sets synced playlist mappings.
    /// </summary>
    [SuppressMessage("Warning", "CA2227", Justification = "Needed for deserialization")]
    public Collection<PlaylistMapping> Mappings { get; set; }
}
