#!/bin/sh
# Smoke test the published (Native AOT) binaries: run every CLI mode and the
# MCP tools against a throwaway git repository, and fail on any crash.
#
# A Native AOT-only fault (for example a marshalling crash in a native
# callback) aborts the process (exit 134) without a managed stack trace, and
# the JIT-run unit tests never see it, so this runs the real published binaries.
#
# Usage: aot-smoke.sh <changelog binary> [<changelog-mcp binary>]

die() {
  if [ -t 2 ]; then
    printf '\n\033[31m✗\033[0m %s\n' "$*" >&2
  else
    printf '\n✗ %s\n' "$*" >&2
  fi
  exit 1
}

# Like die, but records the failure and carries on so every mode is reported.
fail() {
  if [ -t 2 ]; then
    printf '\n\033[31m✗\033[0m %s\n' "$*" >&2
  else
    printf '\n✗ %s\n' "$*" >&2
  fi
  FAILED=1
}

success() {
  if [ -t 1 ]; then
    printf '\n\033[32m✓\033[0m %s\n' "$*"
  else
    printf '\n✓ %s\n' "$*"
  fi
}

info() {
  if [ -t 1 ]; then
    printf '\n\033[32m→\033[0m %s\n' "$*"
  else
    printf '\n→ %s\n' "$*"
  fi
}

[ "$#" -ge 1 ] || die "usage: $0 <changelog binary> [<changelog-mcp binary>]"

CMD=$(realpath "$1") || die "changelog binary not found: $1"
[ -x "$CMD" ] || die "changelog binary not executable: $CMD"

MCP=""
if [ "$#" -ge 2 ]; then
  MCP=$(realpath "$2") || die "changelog-mcp binary not found: $2"
  [ -x "$MCP" ] || die "changelog-mcp binary not executable: $MCP"
fi

WORK=$(mktemp -d) || die "mktemp failed"
trap 'rm -rf "$WORK"' EXIT

REPO="$WORK/repo"
OUT="$WORK/out.txt"
FAILED=0

mkdir "$REPO" || die "mkdir failed"
cd "$REPO" || die "cd failed"

git init -q -b main . || die "git init failed"
git config user.email "smoke@example.com"
git config user.name "AOT Smoke Test"

# Commits are made with commit-tree so no commit hooks (global or local) run in the scratch repo.
commit() {
  git add -A || die "git add failed"
  tree=$(git write-tree) || die "git write-tree failed"
  if parent=$(git rev-parse -q --verify HEAD); then
    sha=$(echo "$1" | git commit-tree "$tree" -p "$parent") || die "git commit-tree failed"
  else
    sha=$(echo "$1" | git commit-tree "$tree") || die "git commit-tree failed"
  fi
  git update-ref "refs/heads/$(git symbolic-ref --short HEAD)" "$sha" || die "git update-ref failed"
}

# run <expected exit code> <text the output must contain> <args...>
run() {
  expected=$1
  contains=$2
  shift 2
  info "changelog $*"
  "$CMD" "$@" > "$OUT" 2>&1
  rc=$?
  check_result "$expected" "$contains" "$rc"
}

check_result() {
  if [ "$3" -gt 128 ]; then
    fail "crashed with exit code $3 (signal $(($3 - 128)))"
  elif [ "$3" -ne "$1" ]; then
    fail "exit code $3, expected $1"
  elif grep -q "Unhandled exception" "$OUT"; then
    fail "unhandled exception"
  elif [ -n "$2" ] && ! grep -qF -- "$2" "$OUT"; then
    fail "output does not contain: $2"
  else
    success "exit code $3"
    return 0
  fi

  cat "$OUT"
  return 1
}

cat > CHANGELOG.md << 'EOF'
# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Security
### Added
### Fixed
### Changed
### Deprecated
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [0.0.1] - 2020-01-01
### Added
- Initial release
EOF
commit "Initial commit"
git branch base

run 0 "" --self-test
run 0 "" --changelog CHANGELOG.md --lint
run 0 "" --changelog CHANGELOG.md --lint --fix
git checkout -q -- CHANGELOG.md
run 0 "" --changelog CHANGELOG.md --un-released
run 0 "" --changelog CHANGELOG.md --extract "$WORK/release.md" --version 0.0.1
[ -s "$WORK/release.md" ] || fail "--extract wrote no release notes"

# Changes in [Unreleased] only: the check must pass.
run 0 "" --changelog CHANGELOG.md --add Added --message "Smoke test entry"
commit "Add unreleased entry"
run 0 "Changelog is valid" --changelog CHANGELOG.md --check-insert base

run 0 "" --changelog CHANGELOG.md --remove Added --message "Smoke test entry"
run 0 "" --changelog CHANGELOG.md --add Fixed --message "Smoke test release entry"
run 0 "" --changelog CHANGELOG.md --create-release 0.0.2
git checkout -q -- CHANGELOG.md

# A change under an already released version: the check must report it, not crash.
git checkout -q -b released-edit base
awk '{ print } /^- Initial release$/ { print "- Sneaked into a release" }' CHANGELOG.md > "$WORK/changed.md"
cp "$WORK/changed.md" CHANGELOG.md
commit "Edit released section"
run 1 "Changelog modified in released section" --changelog CHANGELOG.md --check-insert base
git checkout -q main

if [ -n "$MCP" ]; then
  info "changelog-mcp initialize, tools/list and tools/call"
  {
    echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"aot-smoke","version":"1.0"}}}'
    echo '{"jsonrpc":"2.0","method":"notifications/initialized"}'
    echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
    echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"changelog_lint","arguments":{}}}'
    echo '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"changelog_unreleased","arguments":{}}}'
    echo '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"changelog_check_insert","arguments":{"branch":"base"}}}'
  } > "$WORK/mcp-in.jsonl"

  # Keep stdin open briefly after the requests so the server answers before it sees end of input.
  { cat "$WORK/mcp-in.jsonl"; sleep 10; } | "$MCP" > "$OUT" 2>&1
  rc=$?
  check_result 0 '"changelog_check_insert"' "$rc"
  for id in 1 2 3 4 5; do
    grep '"result"' "$OUT" | grep -qE "\"id\":${id}[,}]" || {
      fail "no result for MCP request $id"
      cat "$OUT"
    }
  done
  grep '"result"' "$OUT" | grep -E '"id":5[,}]' | grep -qE 'valid[^:]*:true' || {
    fail "changelog_check_insert did not report valid"
    cat "$OUT"
  }
fi

[ "$FAILED" -eq 0 ] || die "AOT smoke test failed"

success "AOT smoke test passed"
