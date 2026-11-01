#!/usr/bin/env bash
#
# Decides whether a change is worth building, publishing and testing. A change that cannot reach the
# published package is several minutes of CI time for nothing.
#
# Usage: check-plugin-changes.sh [<base-ref>]
#
# With a base ref, the changes are compared against the merge base with it. Without one the gate is
# off and everything runs, which is what a manual dispatch wants.
#
# Writes `changed=true|false` to $GITHUB_OUTPUT when running under GitHub Actions.

set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

# Paths that never end up in the published package. Everything else counts, so a new kind of file
# errs towards running the pipeline.
is_relevant() {
    case "$1" in
        tests/* | doc/* | res/* | .github/* | .idea/* | .vscode/* | .claude/*) return 1 ;;
        *.md | LICENSE | .gitignore | .editorconfig | .prettierrc | .mcp.json) return 1 ;;
        eslint.config.mjs | *.sln | *.DotSettings | *.DotSettings.user) return 1 ;;
        *) return 0 ;;
    esac
}

decide() {
    local run=$1 reason=$2
    printf '%s\n' "$reason"

    if [ -n "${GITHUB_OUTPUT-}" ]; then
        printf 'changed=%s\n' "$run" >>"${GITHUB_OUTPUT}"
    fi

    if [ -n "${GITHUB_STEP_SUMMARY-}" ]; then
        printf '%s\n' "$reason" >>"${GITHUB_STEP_SUMMARY}"
    fi
}

base=${1-}
if [ -z "$base" ]; then
    decide true "No base ref given, so the gate is off."
    exit 0
fi

merge_base=$(git merge-base "$base" HEAD)

while IFS= read -r file; do
    if is_relevant "$file"; then
        decide true "The plugin changed since $(git rev-parse --short "$merge_base") ($file)."
        exit 0
    fi
done < <(git diff --name-only "$merge_base" HEAD)

decide false "Nothing that ends up in the published plugin changed since $(git rev-parse --short "$merge_base")."
