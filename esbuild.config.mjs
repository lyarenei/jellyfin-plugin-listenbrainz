import * as esbuild from "esbuild";

const entryPoints = [
    {
        in: "src/Jellyfin.Plugin.ListenBrainz/Configuration/configurationPage.ts",
        out: "src/Jellyfin.Plugin.ListenBrainz/Configuration/index",
    },
];

await esbuild.build({
    entryPoints: entryPoints,
    bundle: true,
    format: "esm",
    target: "es2017",
    outdir: ".",
    logLevel: "info",
});
