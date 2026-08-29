#!/usr/bin/env bash
set -euo pipefail

project="${1:-.}"
summary="${2:-}"

if [[ ! -d "$project" ]]; then
  printf 'Project directory does not exist: %s\n' "$project" >&2
  exit 2
fi

project="$(cd "$project" && pwd)"
text="$(printf '%s' "$summary" | tr '[:upper:]' '[:lower:]')"

has() {
  local pattern="$1"
  [[ "$text" =~ $pattern ]]
}

strong=0

# AF source copies have used both of these SubModule.xml layouts; accept either.
if [[ -f "$project/AnimusForge.csproj" ]]; then
  strong=$((strong + 1))
fi
if [[ -f "$project/AnimusForge/SubModule.xml" || -f "$project/AnimusForge/ModuleData/SubModule.xml" ]]; then
  strong=$((strong + 1))
fi
if [[ -d "$project/AnimusForge.Bootstrap" ]]; then
  strong=$((strong + 1))
fi
if [[ -f "$project/MyBehavior.cs" && -f "$project/ShoutBehavior.cs" ]]; then
  strong=$((strong + 1))
fi

printf 'AF skill routing aid\n'
printf 'Project: %s\n' "$project"
printf 'Strong identity signals: %s\n\n' "$strong"

if (( strong < 2 )) && ! has 'animusforge|mount-blade-bannerlord-animusforge'; then
  printf 'IDENTITY: unconfirmed\n'
  printf -- '- Read references/routing-and-identity.md.\n'
  printf -- '- Do not edit until the canonical AnimusForge worktree is established.\n'
  exit 0
fi

printf 'IDENTITY: likely AnimusForge; verify ledger/canonical worktree before writes.\n'
printf -- '- Always: SKILL.md + references/ledger-and-handoff.md\n'

if has '仓库|整理|清理|目录|git|大文件|二进制|dll|onnx|日志|zip|artifact|repository|cleanup|layout|license'; then
  printf -- '- Task route: references/repository-structure.md\n'
fi

if has '插件|模块|bridge|桥接|manifest|profile|foundation|主底座|capability|owner|依赖|卸载|safemode|safe.?mode'; then
  printf -- '- Task route: references/plugin-architecture.md\n'
  printf -- '- Task route: references/module-and-bridge-workflow.md\n'
fi

if has '1\.3|1\.4|bootstrap|harmony|taleworlds|反射|兼容|构建|打包|部署|package|deploy|build'; then
  printf -- '- Task route: references/bannerlord-compatibility.md\n'
fi

if has '喊话|自由对话|原生对话|信使|prompt|llm|后处理|tag|action|afef|history|记忆链路|conversation|courier'; then
  printf -- '- Task route: references/interaction-pipeline.md\n'
fi

if has '存档|syncdata|迁移|playerexports|用户数据|schema|chunk|save|persistence|mcm|config'; then
  printf -- '- Task route: references/persistence-and-user-data.md\n'
fi

if has 'tick|线程|异步|崩溃|异常|性能|卡顿|任务队列|diagnostic|日志|runtime|mission|campaign|gauntlet'; then
  printf -- '- Task route: references/runtime-safety.md\n'
fi

if has '重构|技术债|god object|mybehavior|shoutbehavior|rewardsystem|aiconfighandler|duelsettings|debt|refactor'; then
  printf -- '- Task route: references/known-debt.md\n'
fi

printf -- '- Before completion: references/validation.md\n'
printf '\nSuggestions are not authority. Confirm against the current ledger, code, manifests, build and user request.\n'
