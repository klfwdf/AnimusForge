#!/usr/bin/env bash
set -euo pipefail
project="${1:-.}"
summary="${2:-}"
if [[ ! -d "$project" ]]; then
  printf 'Project directory does not exist: %s\n' "$project" >&2
  exit 2
fi
project="$(cd "$project" && pwd)"
text="${summary,,}"
has() { [[ "$text" =~ $1 ]]; }
source_pair=false
if [[ -f "$project/AnimusForge.csproj" ]] && [[ -f "$project/AnimusForge/SubModule.xml" || -f "$project/AnimusForge/ModuleData/SubModule.xml" ]]; then
  source_pair=true
fi
printf 'AF skill routing aid\nProject: %s\n' "$project"
if [[ "$source_pair" != true ]] && ! has 'animusforge|afmod|mount-blade-bannerlord-animusforge'; then
  printf 'IDENTITY: unconfirmed\n- Read references/routing-and-identity.md before choosing a write target.\n'
  exit 0
fi
printf 'IDENTITY: likely AnimusForge; verify the selected workspace before writes.\n- Entry: SKILL.md\n'
if has '功能|玩法|制作|开发|修复|bug|feature|gameplay|develop|modding|ui|界面|资源|本地化|翻译|配置|设置|content|asset|localization|config|setting'; then
  printf -- '- Development: references/mod-development.md\n'
fi
if has '重构|抽取|拆分|职责|refactor|extract|插件|模块|bridge|桥接|manifest|profile|foundation|主底座|主体|框架|framework|internal|public api|公开.*接口|公开.*api|sdk|capability|safemode'; then
  printf -- '- Architecture: references/plugin-architecture.md\n- Work package: references/module-and-bridge-workflow.md\n'
fi
if has '仓库|整理|清理|目录|大文件|产物|许可|artifact|repository|cleanup|layout|license'; then
  printf -- '- Repository: references/repository-structure.md\n'
fi
if has '双版本|1\.3|1\.4|bootstrap|harmony|taleworlds|反射|兼容|构建|打包|部署|package|deploy|build'; then
  printf -- '- Compatibility: references/bannerlord-compatibility.md\n'
fi
if has '喊话|自由对话|原生对话|信使|prompt|llm|后处理|tag|action|afef|history|记忆链路|conversation|courier'; then
  printf -- '- Interaction: references/interaction-pipeline.md\n'
fi
if has '存档|syncdata|playerexports|用户数据|schema|chunk|save|persistence'; then
  printf -- '- Persistence: references/persistence-and-user-data.md\n'
fi
if has 'tick|线程|异步|崩溃|异常|性能|卡顿|队列|diagnostic|日志|runtime|mission|campaign|gauntlet'; then
  printf -- '- Runtime: references/runtime-safety.md\n'
fi
if has '技术债|历史审计|历史审查|debt|historical.audit'; then
  printf -- '- Debt investigation: references/known-debt.md\n'
fi
if has '原计划|重构审查|重构复核|original.plan|refactor.review|refactor.checklist' || { has '重构|refactor' && has '审查|复核|review|checklist|audit'; }; then
  printf -- '- Review method: references/refactor-review-checklist.md\n'
fi
if has '交接|台账|接续|进度|handoff|ledger|continuation'; then
  printf -- '- Handoff: references/ledger-and-handoff.md\n'
fi
if has 'skill|技能|安装.*规则|宿主|host'; then
  printf -- '- Skill maintenance: references/host-compatibility.md\n'
fi
printf -- '- Acceptance selection: references/validation.md\n'
printf 'Suggestions select references, not implementation scope or action authorization.\n'
