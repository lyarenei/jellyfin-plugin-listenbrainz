using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ListenBrainz.Controllers;

/// <summary>
/// Controller for serving internal plugin resources.
/// </summary>
[ApiController]
[Route("ListenBrainzPlugin/internal")]
public class InternalController : ControllerBase
{
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
}
