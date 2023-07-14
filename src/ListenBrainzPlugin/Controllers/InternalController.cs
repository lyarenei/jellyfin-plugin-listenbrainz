using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ListenBrainzPlugin.Controllers;

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
    /// <returns>CSS stylesheet file response.</returns>
    [Route("styles/{fileName}")]
    public ActionResult GetStyles([FromRoute] string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
        var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream is null) return NotFound();
        return new FileStreamResult(stream, "text/css");
    }
}
