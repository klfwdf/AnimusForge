# 当前接续：J07b 正文阶段已抽取，raw 线程问题已修复（2026-09-20）

- **仍为 J07b_IN_PROGRESS，J07/J10 未完成。** `00574541` 抽出真实正文接收阶段；`d9e9aae1` 将场景动作观察和提前 TTS 放回已有主线程校验回调。主编排由 484 行到 454 行，不把局部拆分报成“拆干净”。
- 新正文对照 19 场景/179 检查 + 10 项具名负例；raw 边界 37 项 + 4 项负例，修复前已复现。Native 五组 44/589/111/91/184、TTS fallback 14，以及六项双版本/Bootstrap 构建通过。真实游戏/旧档/真实语音/provider 未验。
- [详细台账、源码位置、实际验证与回滚](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j07b-mainreply-thread-boundary-20260920)；[当前计划与 25 条约束](docs/plans/j07-conversation-native-plan.md)；[298 锚点源码图](docs/architecture/af-framework-code-map.json)。
- 下一步继续 raw/展示或后处理捕获阶段的真实拆分，守住五步 Prompt、历史 fork/join、后处理→动作以及 unknown 不重试。不得把同步 LLM 搬进主线程；仍有后续游戏属性读取需要逐段审查。
- 自动化 `af-7-8` 保持 ACTIVE，UTC `2026-09-20T17:48:36Z` 截止；届时如实交接未完成项并暂停。当前未推送、部署、Stage、打包、切默认或操作游戏/存档。
- 唯一施工树 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`；本地分支 `codex/af-modularize-j04-20260918`；未来交付目标 `origin/codex/af-main-refactor-continuation-20260831`。保留 `.dotnet-cli-home/`；本地日志/转发版不上传。
