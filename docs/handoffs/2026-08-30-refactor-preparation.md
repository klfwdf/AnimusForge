# Handoff：AF 重构准备阶段

- 日期：2026-08-30
- 当前分支：`refactor/prepare-af-restructure`
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`（包含已单独推送的模块版本提交）
- 前一功能基线：`96a1c60f1877813a9fb3440ddad068d6e92afa1e`
- 当前状态：IN PROGRESS
- 用户已确认：允许将准备文件和 skill 提交并推送到新的远端分支；暂不拆分多个玩法 DLL；旧存档兼容是硬目标；游戏基线采用最小可复现场景记录，不要求现在立即完成全量手测。

## 本次已完成

- 已确认当前工作区为 `E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`。
- 已确认 `origin` 指向 `https://github.com/klfwdf/AnimusForge.git`。
- 已创建本地准备分支；没有提交或推送。
- 已将 `D:\APP\QQ\document\af-skill.zip` 安装到项目级 `.claude/skills/animusforge-maintainer/`。
- 已创建公共重构台账：`docs/animusforge-refactoring-and-repository-reorganization-plan.md`。
- 已创建起点基线：`docs/animusforge-baseline-2026-08-30.md`。
- 已完成有限的只读结构盘点。
- 已确认 `SubModule.cs` 是当前组合根瓶颈：同时负责生命周期组合、Harmony 注册、CampaignBehavior/模型注册、每帧调度和关闭协调。
- 已确认 Bootstrap 已是清晰的独立边界；`SceneActionsIntegrationBoundary` 是现有的薄适配器边界范例。
- 已确认后续 owner map 至少应区分 Host/Composition、Conversation/AI、World Simulation、Settlement/Siege、Mission/Combat、Policy、Progression/Social、UI/Diagnostics 与 Compatibility/Safety；这些目前只是逻辑所有权，不代表立即拆 DLL。

## 重要注意

- 开始准备时 `AnimusForge/SubModule.xml` 已有用户修改：版本 `v1.3.7` → `v1.3.7.2`，且末尾换行变化。本次没有回滚或覆盖它。
- 尚未运行任何构建、打包、部署或游戏测试；相关状态必须保持 `NOT-RUN`。
- 已尝试使用统一构建脚本进行 stage-only 基线构建，但由于当前机器不存在默认的 `F:` 盘 Bannerlord 路径，在脚本路径解析处失败；这不是 C# 编译失败。
- 不要把临时 ZIP 解压目录 `D:\APP\QQ\document\.af-skill-inspect\` 当作源码目标；skill 已从原 ZIP 安装到项目内。
- 不要修改现有一键编译/覆盖/推送流程。

## 下一步建议

1. 审阅 skill、基线和主重构台账。
2. 继续只读盘点源码、模块数据、脚本、工具、原版参考树、构建产物和用户数据边界。
3. 确认代表性存档与游戏内测试计划。
4. 按现有统一脚本运行并记录 1.3、1.4、Bootstrap 构建（如果环境和权限允许）。
5. 形成第一版“现有功能 → 当前文件/入口 → 目标模块 → 依赖 → 风险”矩阵。
6. 只有准备/清理 gate 完成后，才开始第一条可回退的代码迁移切片。

## 接手规则

新对话或新开发者应先读取：

- `CLAUDE.md`
- `.claude/skills/animusforge-maintainer/SKILL.md`
- `docs/animusforge-refactoring-and-repository-reorganization-plan.md`
- `docs/animusforge-baseline-2026-08-30.md`
- 本 handoff

随后核对 `git status`、当前分支、HEAD 和实际文件状态，再进行任何写入。
