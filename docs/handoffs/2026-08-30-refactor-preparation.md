# Handoff：AF 重构准备阶段

- 日期：2026-08-30
- 当前分支：`refactor/prepare-af-restructure`
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`（包含已单独推送的模块版本提交）
- 当前准备分支已推送提交：`23449caf0c6d1e38235d752f8fa1b0975dce17a5`（重构地图与 owner matrix）
- 当前状态：VERIFY
- 用户已确认：允许将准备文件和 skill 提交并推送到新的远端分支；暂不拆分多个玩法 DLL；旧存档兼容是硬目标；游戏基线采用最小可复现场景记录，不要求现在立即完成全量手测。

## 本次已完成

- 已确认当前工作区为 `E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`。
- 已确认 `origin` 指向 `https://github.com/klfwdf/AnimusForge.git`。
- 已创建本地准备分支，并已将准备文档与重构地图推送到 `origin/refactor/prepare-af-restructure`。
- 已将 `D:\APP\QQ\document\af-skill.zip` 安装到项目级 `.claude/skills/animusforge-maintainer/`。
- 已创建公共重构台账：`docs/animusforge-refactoring-and-repository-reorganization-plan.md`。
- 已创建起点基线：`docs/animusforge-baseline-2026-08-30.md`。
- 已完成有限的只读结构盘点。
- 已完成第一版重构地图：`docs/animusforge-refactor-map.md`，涵盖运行链、组合根、目标 owner、持久化、交互管线、风险和推荐顺序。
- 已完成仓库边界、功能域和持久化审计；审计结论已合并到 `docs/animusforge-refactor-map.md`。
- 已完成第一版逐文件 owner matrix：`docs/animusforge-owner-matrix.md`，涵盖运行、交互、持久化、Policy、World、Siege、Mission、Social、Knowledge、UI、工具和参考树。
- 已确认 Bootstrap 已是清晰的独立边界；`SceneActionsIntegrationBoundary` 是现有的薄适配器边界范例。
- 已确认后续 owner map 至少应区分 Host/Composition、Conversation/AI、World Simulation、Settlement/Siege、Mission/Combat、Policy、Progression/Social、UI/Diagnostics 与 Compatibility/Safety；这些目前只是逻辑所有权，不代表立即拆 DLL。

## 重要注意

- 开始准备时 `AnimusForge/SubModule.xml` 已有用户修改：版本 `v1.3.7` → `v1.3.7.2`，且末尾换行变化。本次没有回滚或覆盖它。
- 尚未运行任何构建、打包、部署或游戏测试；相关状态必须保持 `NOT-RUN`。
- 已使用用户提供的 Bannerlord 根目录成功识别实际游戏版本 `v1.4.7.117484`；一次显式指定 `.tmp\build_check\1.4\0Harmony.dll` 的重试随后因缺少默认路径的 `MCMv5.dll` 停止，尚未完成构建。
- 用户已确认主要游戏内基线版本为 Bannerlord 1.4，采用 `1.4.x` 兼容目标，并确认存在可备份的代表性存档。
- 已从实际游戏 `TaleWorlds.Library.dll` 读取到代表性版本 `v1.4.7.117484`；仓库 `.tmp\build_check\1.4` 是 `v1.4.6.115628`。不同开发者可使用不同 1.4.x 补丁，但每次构建需记录精确版本，共享验收再指定固定 overlay。
- 后续构建需准备 1.4.x 目标线的完整依赖闭包：当前构建曾因默认路径缺少 `0Harmony.dll`/`MCMv5.dll` 停止。
- 不要把临时 ZIP 解压目录 `D:\APP\QQ\document\.af-skill-inspect\` 当作源码目标；skill 已从原 ZIP 安装到项目内。
- 不要修改现有一键编译/覆盖/推送流程。

## 下一步建议

1. 处理构建依赖闭包：确认与 `1.4.x` 目标线匹配的 `0Harmony.dll`、`MCMv5.dll`、UIExtender 和 MBOptionScreen 引用；必要时以本机 1.4.7 为代表性 overlay，但不修改构建脚本。
2. 备份一个代表性旧存档到仓库外，并记录最小 1.4.x 游戏基线场景；不要求立刻全量手测。
3. 继续完善仓库 gate 的分类和依赖记录，随后为 `SubModule.cs` 组合边界建立第一条可回退代码切片。



## 接手规则

新对话或新开发者应先读取：

- `CLAUDE.md`
- `.claude/skills/animusforge-maintainer/SKILL.md`
- `docs/animusforge-refactoring-and-repository-reorganization-plan.md`
- `docs/animusforge-baseline-2026-08-30.md`
- 本 handoff

随后核对 `git status`、当前分支、HEAD 和实际文件状态，再进行任何写入。
