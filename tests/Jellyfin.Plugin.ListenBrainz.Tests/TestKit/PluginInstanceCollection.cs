namespace Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

/// <summary>
/// Groups every test which constructs a <see cref="Plugin"/>.
/// </summary>
/// <remarks>
/// This forces to run the tests sequentially/in isolation because the Plugin instance is a singleton.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PluginInstanceCollection
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public const string Name = "Plugin instance";
}
