namespace Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

/// <summary>
/// A temporary directory that is automatically deleted after test run.
/// </summary>
public sealed class TempDir : IDisposable
{
    private readonly DirectoryInfo _dir;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempDir"/> class.
    /// </summary>
    /// <param name="prefix">Prefix for the directory name.</param>
    public TempDir(string prefix = "lb-test") => _dir = Directory.CreateTempSubdirectory(prefix);

    /// <summary>
    /// Gets the full path of the directory.
    /// </summary>
    public string Path => _dir.FullName;

    /// <summary>
    /// Builds a path to a file inside the directory. The file itself is not created.
    /// </summary>
    /// <param name="name">File name.</param>
    /// <returns>The full path to the file.</returns>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            _dir.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Failure does not matter; just to not have false-negative test because of this.
        }
    }
}
