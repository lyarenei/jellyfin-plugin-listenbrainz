# Integration tests

Tests that run a real Jellyfin server in a podman container with the plugin installed. The instance is controlled with
HTTP API.

The plugin can be installed into the container in two ways:

- install locally (from local build) - default
- install from remote repository

Installation from repository is done on a PR branch (dev repository) and on main branch (stable repository). Local
instance can be switched for repository installation by setting `JELLYFIN_ITEST_PLUGIN_REPOSITORY` to URL pointing to a
plugin repository manifest.

## Requirements

- .NET SDK (the one used to build the plugin)
- podman, running (`podman machine start` on macOS/Windows)
    - docker will probably work as well, but not tested
- The official Jellyfin docker image - `docker.io/jellyfin/jellyfin`

## Running

```sh
dotnet test tests/Jellyfin.Plugin.ListenBrainz.IntegrationTests
```

Against a published build instead of the working tree:

```sh
JELLYFIN_ITEST_PLUGIN_REPOSITORY=https://repo.xkrivo.net/jellyfin-dev/manifest.json \
    dotnet test tests/Jellyfin.Plugin.ListenBrainz.IntegrationTests
```

All environment variables that can be set:

| Variable                           | Default                   | Purpose                                                                                                                      |
|------------------------------------|---------------------------|------------------------------------------------------------------------------------------------------------------------------|
| `JELLYFIN_ITEST_TAG`               | `10.11.11`                | Tag of the `jellyfin/jellyfin` image to test against.                                                                        |
| `JELLYFIN_ITEST_PLUGIN_REPOSITORY` | unset                     | Manifest URL of a plugin repository. When set, the plugin is installed from it instead of being built from the working tree. |
| `JELLYFIN_ITEST_PLUGIN_VERSION`    | version from `build.yaml` | Version to install from the repository. Ignored by a local build.                                                            |
| `JELLYFIN_ITEST_KEEP_CONTAINER`    | unset                     | Set to `1` to leave the container running after the run.                                                                     |

## Not built with the solution

While the integration tests are placed in the solution, they are excluded from every build configuration. This is
achieved by the solution having `ActiveCfg` mappings and no `Build.0` mappings.

A solution-wide `dotnet build` or `dotnet test` skips the integration tests, to reduce resource usage as well as
speeding up the development time by not waiting for slow integration tests to run.

Notes:

- `dotnet sln add` writes the `Build.0` lines back and so would silently include integration tests in the default test
  command.
- Integration tests are also tagged with `Integration` category, so these can be easily filtered with
  `--filter 'Category!=Integration'`.

## Debugging a failure

To view live logs from the container:

```sh
JELLYFIN_ITEST_KEEP_CONTAINER=1 dotnet test tests/Jellyfin.Plugin.ListenBrainz.IntegrationTests
podman ps --filter name=jellyfin-listenbrainz-itest
podman logs <container>
```

Server admin credentials: `integration-admin` / `integration-password`.

Note: podman can publish a port, serve a connection on it and then drop the forward while an unrelated container is
being removed. A workaround for the integration test suite is to pick a host port outside the range podman recycles,
wait for a few successful responses before running the tests. If the workaround fails, the container is automatically
restarted.

## In CI

Dev and main workflows run these tests through `.github/workflows/integration-test.yml` after the plugin has been
published to the respective repository. Dev workflow installs the plugin from the development/unstable repository while
the main workflow installs the plugin from the stable repository.

In pull requests, these tests are gated to reduce the usage of CI time.`.github/scripts/check-plugin-changes.sh` checks
if the `build` job should run, and `integration-test` job is skipped along with it. If there are no changes to the
plugin sources (aka a version bump), then the tests are not run.

The script can be also run against any branch:

```sh
.github/scripts/check-plugin-changes.sh main
```

Releases (main workflows) are not gated: they are infrequent, and it's kind of the point to test what was published.
