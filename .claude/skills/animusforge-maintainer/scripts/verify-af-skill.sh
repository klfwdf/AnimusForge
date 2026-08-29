#!/usr/bin/env bash
set -euo pipefail

root="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"

if [[ ! -d "$root" ]]; then
  printf 'ERROR: AF skill directory does not exist: %s\n' "$root" >&2
  exit 2
fi

errors=0
warnings=0

error() {
  printf 'ERROR: %s\n' "$1" >&2
  errors=$((errors + 1))
}

warn() {
  printf 'WARN: %s\n' "$1" >&2
  warnings=$((warnings + 1))
}

require_file() {
  local rel="$1"
  [[ -f "$root/$rel" ]] || error "Missing $rel"
}

require_file "SKILL.md"
require_file "agents/openai.yaml"
require_file "references/routing-and-identity.md"
require_file "references/host-compatibility.md"
require_file "references/ledger-and-handoff.md"
require_file "references/repository-structure.md"
require_file "references/plugin-architecture.md"
require_file "references/module-and-bridge-workflow.md"
require_file "references/bannerlord-compatibility.md"
require_file "references/interaction-pipeline.md"
require_file "references/persistence-and-user-data.md"
require_file "references/runtime-safety.md"
require_file "references/validation.md"
require_file "references/known-debt.md"
require_file "assets/module/module.yaml"
require_file "assets/module/README.template.md"
require_file "assets/bridge/module.yaml"
require_file "assets/bridge/README.template.md"
require_file "scripts/suggest-reference-route.sh"
require_file "scripts/install-af-skill.sh"
require_file "scripts/verify-af-skill.sh"

if [[ -f "$root/SKILL.md" ]]; then
  [[ "$(sed -n '1p' "$root/SKILL.md")" == "---" ]] || error "SKILL.md must start with YAML frontmatter"
  sed -n '2,20p' "$root/SKILL.md" | grep -Eq '^name: animusforge-maintainer$' \
    || error "SKILL.md frontmatter name must be animusforge-maintainer"
  sed -n '2,20p' "$root/SKILL.md" | grep -Eq '^description: .+' \
    || error "SKILL.md frontmatter needs a one-line description"

  if command -v python3 >/dev/null 2>&1; then
    SKILL_FILE="$root/SKILL.md" python3 - <<'PY' || error "SKILL.md YAML frontmatter is not portable YAML"
import os
from pathlib import Path

try:
    import yaml
except ImportError:
    raise SystemExit(0)

content = Path(os.environ["SKILL_FILE"]).read_text(encoding="utf-8")
frontmatter = content.split("---", 2)[1]
data = yaml.safe_load(frontmatter)
if not isinstance(data, dict):
    raise SystemExit(1)
if set(data) - {"name", "description", "license", "allowed-tools", "metadata"}:
    raise SystemExit(1)
if not isinstance(data.get("name"), str) or not isinstance(data.get("description"), str):
    raise SystemExit(1)
PY
  else
    warn "python3 unavailable; portable YAML frontmatter parse was NOT-RUN"
  fi

  for phrase in \
    "specific Mount & Blade II: Bannerlord mod AnimusForge" \
    "Apply only to positively identified AnimusForge work" \
    "Manual invocation" \
    "Prove this is AnimusForge" \
    "execution ledger" \
    "AF.Foundation.Runtime" \
    "AF.Bridge" \
    "portable between Claude Code and Codex"; do
    grep -Fq "$phrase" "$root/SKILL.md" || error "SKILL.md is missing routing/architecture phrase: $phrase"
  done
fi

link_file="${TMPDIR:-/tmp}/af_skill_links.$$"
if grep -RhoE '\]\((references|assets|scripts)/[^)# ]+' "$root"/*.md "$root"/references/*.md 2>/dev/null \
  | sed -E 's/^\]\(//' | sort -u >"$link_file"; then
  while IFS= read -r rel; do
    [[ -e "$root/$rel" ]] || error "Broken relative link target: $rel"
  done <"$link_file"
fi
rm -f "$link_file"

if grep -RIl --exclude='*.template.md' --exclude='module.yaml' '__[A-Z][A-Z0-9_]*__' "$root/SKILL.md" "$root/references" "$root/agents" >/dev/null 2>&1; then
  warn "Non-template AF skill files contain placeholder-looking tokens"
fi

if [[ -f "$root/agents/openai.yaml" ]]; then
  grep -Fq 'AnimusForge Maintainer' "$root/agents/openai.yaml" || error "agents/openai.yaml needs AF display name"
  grep -Fq '$animusforge-maintainer' "$root/agents/openai.yaml" || error "agents/openai.yaml default prompt needs skill invocation"
  grep -Eq '^policy:$' "$root/agents/openai.yaml" || error "agents/openai.yaml needs explicit invocation policy"
  grep -Eq '^  allow_implicit_invocation: true$' "$root/agents/openai.yaml" \
    || error "agents/openai.yaml must keep normal implicit invocation enabled"

  if command -v python3 >/dev/null 2>&1; then
    AGENT_FILE="$root/agents/openai.yaml" python3 - <<'PY' || error "agents/openai.yaml is not portable YAML"
import os
from pathlib import Path

try:
    import yaml
except ImportError:
    raise SystemExit(0)

data = yaml.safe_load(Path(os.environ["AGENT_FILE"]).read_text(encoding="utf-8"))
interface = data.get("interface") if isinstance(data, dict) else None
policy = data.get("policy") if isinstance(data, dict) else None
if not isinstance(interface, dict) or not isinstance(policy, dict):
    raise SystemExit(1)
if not all(isinstance(interface.get(key), str) for key in ("display_name", "short_description", "default_prompt")):
    raise SystemExit(1)
if policy.get("allow_implicit_invocation") is not True:
    raise SystemExit(1)
PY
  else
    warn "python3 unavailable; agents/openai.yaml portable YAML parse was NOT-RUN"
  fi
fi

if [[ -f "$root/scripts/install-af-skill.sh" ]]; then
  grep -Fq 'CLAUDE_CONFIG_DIR' "$root/scripts/install-af-skill.sh" \
    || error "install helper lacks Claude Code destination support"
  grep -Fq 'CODEX_HOME' "$root/scripts/install-af-skill.sh" \
    || error "install helper lacks Codex destination support"
fi

if [[ "$errors" -gt 0 ]]; then
  printf 'AF skill validation failed with %s error(s) and %s warning(s).\n' "$errors" "$warnings" >&2
  exit 1
fi

printf 'AF skill structure is valid at %s (%s warning(s)).\n' "$root" "$warnings"
