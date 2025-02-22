namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Listen cache manager interface.
/// </summary>
public interface IListensCacheManager : ICacheManager, IListensCache, IDisposable
{
}
