using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ListenBrainz.Controllers;

/// <summary>
/// Controller for serving internal plugin resources.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
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
    /// Get all libraries in Jellyfin.
    /// </summary>
    /// <returns>Collection of all music libraries.</returns>
    [HttpGet]
    [Produces("application/json")]
    [Route("libraries")]
    public Task<IEnumerable<JellyfinMediaLibrary>> GetLibraries()
    {
        return Task.FromResult(
            _libraryManager
                .GetLibraries()
                .Cast<CollectionFolder>()
                .Select(ml => new JellyfinMediaLibrary(ml)));
    }
}
