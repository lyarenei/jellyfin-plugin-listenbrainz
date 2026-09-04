using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Configuration;

public class UserDefaultsParityTests
{
    /// <summary>
    /// The page always overwrites this with the selected user's ID, so only its presence matters.
    /// </summary>
    private const string UserIdProperty = "JellyfinUserId";

    [Fact]
    public void UserDefaults_ExposeTheSamePropertiesAsUserConfig()
    {
        var pageDefaults = ReadPageUserDefaults();
        var configKeys = GetSerializedUserConfigProperties().Keys;

        Assert.Equal(
            configKeys.OrderBy(k => k, StringComparer.Ordinal),
            pageDefaults.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void UserDefaults_MatchTheUserConfigDefaults()
    {
        var pageDefaults = ReadPageUserDefaults();

        foreach (var (name, expected) in GetSerializedUserConfigProperties())
        {
            if (name == UserIdProperty)
            {
                continue;
            }

            Assert.True(
                pageDefaults.TryGetValue(name, out var actual),
                $"constants.ts is missing a default for {name}");

            Assert.True(
                expected == actual,
                $"Default for {name} is '{expected}' in UserConfig but '{actual}' in constants.ts");
        }
    }

    /// <summary>
    /// Gets the default value of every <see cref="UserConfig"/> property the plugin configuration
    /// API sends to the page, keyed by the name the page sees.
    /// </summary>
    private static Dictionary<string, string> GetSerializedUserConfigProperties()
    {
        var defaults = new UserConfig();
        return typeof(UserConfig)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .ToDictionary(p => p.Name, p => Render(p.GetValue(defaults)), StringComparer.Ordinal);
    }

    private static string Render(object? value) => value switch
    {
        null => string.Empty,
        bool b => b ? "true" : "false",
        Guid g => g.ToString(),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    /// <summary>
    /// Parses the userDefaults object literal out of the config page constants.
    /// </summary>
    private static Dictionary<string, string> ReadPageUserDefaults()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ConfigPage", "constants.ts");
        Assert.True(File.Exists(path), $"Could not find the config page constants at {path}");

        var source = File.ReadAllText(path);
        var literal = Regex.Match(source, @"export const userDefaults[^{]*\{(?<body>[^}]*)\}");
        Assert.True(literal.Success, "Could not find the userDefaults literal in constants.ts");

        var entries = Regex.Matches(
            literal.Groups["body"].Value,
            @"^\s*(?<key>\w+)\s*:\s*(?<value>.+?),?\s*$",
            RegexOptions.Multiline);

        Assert.NotEmpty(entries);

        return entries.ToDictionary(
            m => m.Groups["key"].Value,
            m => m.Groups["value"].Value.Trim().Trim('"'),
            StringComparer.Ordinal);
    }
}
