#!/usr/bin/env bash
set -euo pipefail
script_dir="${BASH_SOURCE[0]%/*}"
[[ "$script_dir" != "${BASH_SOURCE[0]}" ]] || script_dir=.
root="${1:-$(cd "$script_dir/.." && pwd)}"
if [[ ! -d "$root" ]]; then
  printf 'ERROR: AF skill directory does not exist: %s\n' "$root" >&2
  exit 2
fi
python="${AF_PYTHON:-}"
if [[ -z "$python" ]]; then
  for candidate in python3 python; do
    if command -v "$candidate" >/dev/null 2>&1 && "$candidate" -c 'import sys; assert sys.version_info.major == 3' >/dev/null 2>&1; then
      python="$candidate"
      break
    fi
  done
fi
if [[ -z "$python" ]]; then
  printf '%s\n' 'NOT-RUN: AF skill validation requires Python 3 (or AF_PYTHON).' >&2
  exit 2
fi
exec "$python" -X utf8 -B "$script_dir/verify-af-skill.py" "$root"
