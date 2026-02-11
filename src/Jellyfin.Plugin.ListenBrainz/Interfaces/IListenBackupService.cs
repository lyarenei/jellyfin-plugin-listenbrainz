using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Listen backup service interface.
/// </summary>
public interface IListenBackupService
{
    /// <summary>
    /// Back up listen of a current item.
    /// </summary>
    /// <param name="userName">ListenBrainz username.</param>
    /// <param name="item">Listened item.</param>
    /// <param name="metadata">Item metadata.</param>
    /// <param name="timestamp">Listen timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public Task Backup(string userName, Audio item, AudioItemMetadata? metadata, long timestamp, CancellationToken cancellationToken);
}
