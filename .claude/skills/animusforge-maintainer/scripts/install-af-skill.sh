#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: install-af-skill.sh [--host claude|codex|both] [--mode symlink|copy] [--source <skill-dir>] [--dry-run]

Install the same AnimusForge skill source for Claude Code, Codex, or both.

Defaults:
  --host both
  --mode symlink
  --source <parent of this script>

Destinations:
  Claude Code: ${CLAUDE_CONFIG_DIR:-$HOME/.claude}/skills/animusforge-maintainer
  Codex:       ${CODEX_HOME:-$HOME/.codex}/skills/animusforge-maintainer

This helper only manages the installed skill copy/link. It never modifies an
AnimusForge source worktree, its Git index, builds, assets, saves, or packages.
It refuses to replace an existing destination unless that destination is
already a symlink to the selected source. Use --dry-run to inspect first.
USAGE
}

host="both"
mode="symlink"
source="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dry_run=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --host)
      [[ $# -ge 2 ]] || { printf '%s\n' 'ERROR: --host needs a value.' >&2; exit 2; }
      host="$2"
      shift 2
      ;;
    --mode)
      [[ $# -ge 2 ]] || { printf '%s\n' 'ERROR: --mode needs a value.' >&2; exit 2; }
      mode="$2"
      shift 2
      ;;
    --source)
      [[ $# -ge 2 ]] || { printf '%s\n' 'ERROR: --source needs a value.' >&2; exit 2; }
      source="$2"
      shift 2
      ;;
    --dry-run)
      dry_run=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      printf 'ERROR: unknown option: %s\n' "$1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

case "$host" in claude|codex|both) ;; *) printf 'ERROR: invalid --host: %s\n' "$host" >&2; exit 2;; esac
case "$mode" in symlink|copy) ;; *) printf 'ERROR: invalid --mode: %s\n' "$mode" >&2; exit 2;; esac

if [[ ! -f "$source/SKILL.md" ]]; then
  printf 'ERROR: source is not a skill directory with SKILL.md: %s\n' "$source" >&2
  exit 2
fi
source="$(cd "$source" && pwd)"

validate_source() {
  local verifier="$source/scripts/verify-af-skill.sh"
  [[ -x "$verifier" ]] || {
    printf 'ERROR: source verifier is missing or not executable: %s\n' "$verifier" >&2
    exit 2
  }
  "$verifier" "$source"
}

install_one() {
  local label="$1"
  local destination="$2"
  local parent
  parent="$(dirname "$destination")"

  if [[ -e "$destination" || -L "$destination" ]]; then
    if [[ -L "$destination" ]] && [[ "$(cd "$destination" && pwd)" == "$source" ]]; then
      printf '%s already points at source: %s\n' "$label" "$destination"
      return
    fi

    printf 'ERROR: %s destination exists and will not be replaced: %s\n' "$label" "$destination" >&2
    printf '       Inspect, remove, or rename it explicitly before rerunning this helper.\n' >&2
    exit 3
  fi

  if [[ "$dry_run" == true ]]; then
    printf 'DRY-RUN: install %s via %s: %s -> %s\n' "$label" "$mode" "$source" "$destination"
    return
  fi

  mkdir -p "$parent"
  case "$mode" in
    symlink)
      ln -s "$source" "$destination"
      ;;
    copy)
      cp -R "$source" "$destination"
      ;;
  esac
  printf 'Installed %s via %s: %s\n' "$label" "$mode" "$destination"
}

validate_source

if [[ "$host" == "claude" || "$host" == "both" ]]; then
  install_one "Claude Code" "${CLAUDE_CONFIG_DIR:-$HOME/.claude}/skills/animusforge-maintainer"
fi
if [[ "$host" == "codex" || "$host" == "both" ]]; then
  install_one "Codex" "${CODEX_HOME:-$HOME/.codex}/skills/animusforge-maintainer"
fi

if [[ "$dry_run" == false ]]; then
  printf '%s\n' 'Restart or begin a new turn/session in each host before testing skill discovery.'
fi
