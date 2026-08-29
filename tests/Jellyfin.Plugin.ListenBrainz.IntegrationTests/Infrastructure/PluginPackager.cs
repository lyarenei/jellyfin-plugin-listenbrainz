using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

internal static class PluginPackager
{
    private static readonly JsonSerializerOptions _metaJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static async Task<string> CreateBuildContextAsync(
        PluginBuildManifest manifest,
        CancellationToken cancellationToken = default)
    {
        var scratch = RepositoryLayout.ScratchDirectory;
        var buildOutput = Path.Combine(scratch, "plugin-build");
        var context = Path.Combine(scratch, "context");
        var installDirectory = Path.Combine(context, "plugin", manifest.InstallDirectoryName);

        // Both are wiped: a stale assembly would satisfy the artifact check below unnoticed.
        Recreate(buildOutput);
        Recreate(context);
        Directory.CreateDirectory(installDirectory);

        // Jellyfin reconciles the assembly version against meta.json, so it has to be stamped on both.
        var buildResult = await ProcessRunner.RunAsync(
            "dotnet",
            [
                "build",
                RepositoryLayout.PluginProject,
                "--configuration", "Release",
                "--output", buildOutput,
                "--nologo",
                "--verbosity", "quiet",
                "-nodeReuse:false",
                $"-p:Version={manifest.Version}",
                $"-p:AssemblyVersion={manifest.Version}",
                $"-p:FileVersion={manifest.Version}",
            ],
            cancellationToken).ConfigureAwait(false);

        buildResult.EnsureSuccess();

        foreach (var artifact in manifest.Artifacts)
        {
            var source = Path.Combine(buildOutput, artifact);
            if (!File.Exists(source))
            {
                throw new InvalidOperationException(
                    $"Build output is missing artifact '{artifact}' declared in build.yaml (looked in {buildOutput}).");
            }

            File.Copy(source, Path.Combine(installDirectory, artifact));
        }

        await File.WriteAllTextAsync(
            Path.Combine(installDirectory, "meta.json"),
            BuildMetaJson(manifest),
            cancellationToken).ConfigureAwait(false);

        return context;
    }

    private static string BuildMetaJson(PluginBuildManifest manifest)
    {
        // Mirrors the manifest that jellyfin-plugin-repository-manager writes into a release zip.
        var meta = new
        {
            category = manifest.Category,
            changelog = string.Empty,
            description = manifest.Description.Trim(),
            guid = manifest.PluginId.ToString("D"),
            name = manifest.Name,
            overview = manifest.Overview.Trim(),
            owner = manifest.Owner,
            targetAbi = manifest.TargetAbi,
            timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", null),
            version = manifest.Version,
            status = 0,
            autoUpdate = false,
            imagePath = string.Empty,
            assemblies = manifest.Artifacts,
        };

        return JsonSerializer.Serialize(meta, _metaJsonOptions);
    }

    private static void Recreate(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
    }
}
