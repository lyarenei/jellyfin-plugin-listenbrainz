#!/usr/bin/env bash
#
# Decides whether a change is worth building, publishing and testing, and enforces that a change to
# the plugin itself comes with a version bump.
#
# The pipeline publishes every build to the development plugin repository and the integration tests
# install it back from there, so a change that cannot reach the package is several minutes of CI
# time for a package identical to the one already published. A package published under a version
# the repository already carries is worse than useless: the server would install whichever build
# got there first, and the tests would report on that instead of this branch.
#
# Usage: check-plugin-changes.sh [<base-ref>]
#
# With a base ref, plugin sources are compared against the merge base with it, which for a pull
# request is the state the branch started from. Without one the gate is off and everything runs,
# which is what a manual dispatch or a release wants.
#
# Writes `changed=true|false` to $GITHUB_OUTPUT when running under GitHub Actions. Exits non-zero
# when the plugin changed without a version bump covering the change.

set -euo pipefail

readonly MANIFEST="build.yaml"

cd "$(git rev-parse --show-toplevel)"

# Paths that never end up in the published package, and so cannot change what a repository install
# of the plugin does. Everything else counts, so a new kind of file errs towards running the tests.
# The solution file is in here because it only lists projects: anything it can change about the
# package shows up under src/ as well.
is_relevant() {
    case "$1" in
        tests/* | doc/* | res/* | .github/* | .idea/* | .vscode/* | .claude/*) return 1 ;;
        *.md | LICENSE | .gitignore | .editorconfig | .prettierrc | .mcp.json) return 1 ;;
        eslint.config.mjs | *.sln | *.DotSettings | *.DotSettings.user) return 1 ;;
        *) return 0 ;;
    esac
}

# Relevant files changed between a commit and HEAD, one per line.
changed_since() {
    local file
    git diff --name-only "$1" HEAD | while IFS= read -r file; do
        if is_relevant "$file"; then
            printf '%s\n' "$file"
        fi
    done
}

# Prints nothing rather than failing when the commit has no manifest, which is what a commit from
# before it was added should look like here.
version_at() {
    git show "$1:${MANIFEST}" 2>/dev/null |
        awk '/^version:[[:space:]]/ { sub(/^version:[[:space:]]*/, ""); gsub(/["'"'"']/, ""); print; exit }' || true
}

# The newest commit that changed the version, which is the point from which the current version
# covers the tree. Commits touching the manifest without moving the version do not count.
find_version_commit() {
    local commit parent
    for commit in $(git rev-list HEAD -- "${MANIFEST}"); do
        parent=$(git rev-parse --quiet --verify "${commit}^") || {
            printf '%s\n' "$commit"
            return 0
        }

        if [ "$(version_at "$commit")" != "$(version_at "$parent")" ]; then
            printf '%s\n' "$commit"
            return 0
        fi
    done

    return 1
}

decide() {
    local run=$1 reason=$2
    printf '%s\n' "$reason"

    if [ -n "${GITHUB_OUTPUT-}" ]; then
        printf 'changed=%s\n' "$run" >>"${GITHUB_OUTPUT}"
    fi

    if [ -n "${GITHUB_STEP_SUMMARY-}" ]; then
        if [ "$run" = true ]; then
            printf 'Building, publishing and testing the plugin: %s\n' "$reason" >>"${GITHUB_STEP_SUMMARY}"
        else
            printf 'Nothing to build, publish or test: %s\n' "$reason" >>"${GITHUB_STEP_SUMMARY}"
        fi
    fi
}

base=${1-}
if [ -z "$base" ]; then
    decide true "No base ref given, so the gate is off."
    exit 0
fi

merge_base=$(git merge-base "$base" HEAD) || {
    printf 'Could not find a merge base with %s. The full history is needed here (fetch-depth: 0).\n' \
        "$base" >&2
    exit 1
}

changed=$(changed_since "$merge_base")
if [ -z "$changed" ]; then
    decide false "Nothing that ends up in the published plugin changed since $(git rev-parse --short "$merge_base")."
    exit 0
fi

version_commit=$(find_version_commit) || {
    printf 'No commit changing the version in %s is reachable from HEAD. A shallow clone looks like this; the gate needs the full history (fetch-depth: 0).\n' \
        "${MANIFEST}" >&2
    exit 1
}

uncovered=$(changed_since "$version_commit")
if [ -n "$uncovered" ]; then
    version=$(version_at "$version_commit")
    {
        printf 'The plugin changed after the version was last set to %s (%s):\n\n' \
            "$version" "$(git log -1 --format='%h %s' "$version_commit")"
        printf '%s\n' "$uncovered" | sed 's/^/  /'
        printf '\nBump the version in %s so that these changes are published as a new package.\n' "${MANIFEST}"
        printf 'The version has to be the last thing changed: %s is already in the plugin repository.\n' "$version"
    } >&2
    exit 1
fi

decide true "The plugin changed and the version was bumped to $(version_at "$version_commit") to cover it."
