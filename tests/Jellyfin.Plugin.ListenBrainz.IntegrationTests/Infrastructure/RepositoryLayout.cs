namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// Well-known paths in the plugin repository, resolved relative to the test assembly.
/// </summary>
internal static class RepositoryLayout
{
    private const string SolutionFileName = "Jellyfin.Plugin.ListenBrainz.sln";

    /// <summary>
    /// Gets the repository root directory.
    /// </summary>
    public static string Root { get; } = FindRoot();

    /// <summary>
    /// Gets the path to the plugin build manifest.
    /// </summary>
    public static string BuildManifest => Path.Combine(Root, "build.yaml");

    /// <summary>
    /// Gets the path to the main plugin project.
    /// </summary>
    public static string PluginProject => Path.Combine(
        Root,
        "src",
        "Jellyfin.Plugin.ListenBrainz",
        "Jellyfin.Plugin.ListenBrainz.csproj");

    /// <summary>
    /// Gets the path to the Containerfile used to build the test server image.
    /// </summary>
    public static string Containerfile => Path.Combine(
        Root,
        "tests",
        "Jellyfin.Plugin.ListenBrainz.IntegrationTests",
        "Containerfile");

    /// <summary>
    /// Gets a scratch directory for harness artifacts. It lives under obj/ so it is git-ignored
    /// and gets cleaned up along with the rest of the build output.
    /// </summary>
    public static string ScratchDirectory => Path.Combine(
        Root,
        "tests",
        "Jellyfin.Plugin.ListenBrainz.IntegrationTests",
        "obj",
        "integration");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate {SolutionFileName} in any parent of {AppContext.BaseDirectory}.");
    }
}
