using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

/// <summary>
/// Helpers for test data.
/// </summary>
public static class TestData
{
    /// <summary>
    /// Creates an audio item.
    /// </summary>
    /// <param name="name">Track name.</param>
    /// <param name="artist">Artist name.</param>
    /// <param name="runtime">Track runtime.</param>
    /// <param name="recordingMbid">MusicBrainz recording ID.</param>
    /// <param name="artistMbids">Jellyfin raw MusicBrainz artist ID value.</param>
    /// <returns>The audio item.</returns>
    public static Audio Audio(
        string name = "track",
        string artist = "artist",
        TimeSpan? runtime = null,
        string? recordingMbid = null,
        string? artistMbids = null)
    {
        var audio = new Audio
        {
            Id = Guid.NewGuid(),
            Name = name,
            Artists = [artist],
            RunTimeTicks = (runtime ?? TimeSpan.FromMinutes(2)).Ticks,
        };

        if (recordingMbid is not null)
        {
            audio.ProviderIds["MusicBrainzRecording"] = recordingMbid;
        }

        if (artistMbids is not null)
        {
            audio.ProviderIds["MusicBrainzArtist"] = artistMbids;
        }

        return audio;
    }

    /// <summary>
    /// Creates a Jellyfin user.
    /// </summary>
    /// <param name="name">User name.</param>
    /// <returns>The user.</returns>
    public static User User(string name = "foobar") => new(name, "auth-provider-id", "pw-reset-provider-id");

    /// <summary>
    /// Creates a plugin user configuration.
    /// </summary>
    /// <param name="jellyfinUserId">Jellyfin user ID the config belongs to.</param>
    /// <param name="isStrictModeEnabled">Whether strict mode is enabled.</param>
    /// <param name="userName">ListenBrainz username.</param>
    /// <returns>The user config.</returns>
    public static UserConfig UserConfig(
        Guid? jellyfinUserId = null,
        bool isStrictModeEnabled = false,
        string userName = "foobar") => new()
    {
        JellyfinUserId = jellyfinUserId ?? Guid.NewGuid(),
        UserName = userName,
        IsListenSubmitEnabled = true,
        IsStrictModeEnabled = isStrictModeEnabled,
        PlaintextApiToken = "some-token",
    };
}
