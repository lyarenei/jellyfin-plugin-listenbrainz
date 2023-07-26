using System.Reflection;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ListenBrainz.Controllers;

/// <summary>
/// Controller for serving internal plugin resources.
/// </summary>
[ApiController]
[Route("ListenBrainzPlugin/internal")]
public class InternalController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalController"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    public InternalController(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Load CSS from specified file and return it in response.
    /// </summary>
    /// <param name="fileName">CSS file name.</param>
    /// <returns>CSS stylesheet file response.</returns>
    [Route("styles/{fileName}")]
    public ActionResult GetStyles([FromRoute] string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName, StringComparison.InvariantCulture));
        var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream is null) return NotFound();
        return new FileStreamResult(stream, "text/css");
    }

    /// <summary>
    /// Get all music libraries in Jellyfin.
    /// </summary>
    /// <returns>Collection of all music libraries.</returns>
    [HttpGet]
    [Produces("application/json")]
    [Route("musicLibraries")]
    public Task<IEnumerable<JellyfinMusicLibrary>> GetMusicLibraries()
    {
        return Task.FromResult(_libraryManager.GetMusicLibraries().Select(ml => new JellyfinMusicLibrary(ml)));
    }
}
