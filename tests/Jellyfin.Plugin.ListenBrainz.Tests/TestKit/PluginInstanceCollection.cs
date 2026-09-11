namespace Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

/// <summary>
/// Groups every test which constructs a <see cref="Plugin"/> so such tests are run sequentially to prevent issues.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PluginInstanceCollection
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public const string Name = "Plugin instance";
}
