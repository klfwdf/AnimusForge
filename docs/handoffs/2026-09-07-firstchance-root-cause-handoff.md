# AnimusForge 阶段 8 / FirstChance 根因调查交接

日期：2026-09-07  
交接对象：下一位 AnimusForge 开发者  
状态：阶段 8 仍为 `VERIFY`，本交接不代表最终发布或游戏验收完成。

## 当前 Git 与工作区

- Git 根目录：`E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`
- 当前分支：`main`
- 当前 HEAD：`b16cb47c`（`fix: recover diplomacy relay and captive ruler context`）
- `origin`：`https://github.com/klfwdf/AnimusForge.git`
- `main` 相对 `origin/main` 在本交接提交前领先 2 个提交。
- `origin/refactor/prepare-af-restructure` 为 `b51c8f4a`；它是当前 `main` 历史的祖先，可用普通 fast-forward 同步。
- 工作区有多项既有未提交修改和未跟踪文件。它们属于前序工作，必须保留；本轮只提交本 handoff 与阶段计划追加记录，禁止 `reset`、`stash`、`clean` 或 `git add -A`。

## 阶段 8 状态

阶段 8 的完整 20 领域准备态门禁（`LOCAL-8-A`）和相关离线契约/组合检查已通过；这表示责任域、Bridge、入口清单和清理候选已有可审计材料，不表示 20 个物理 DLL 已完成，也不表示三渠道默认流程已经切换。

仍未完成的主要事项：

1. 三渠道（Native、Scene Shout、Courier）新入口的完整等价接管，以及 Scene 后处理、流式/主动开场、多人 relay 的等价验证。
2. 真实 Bannerlord Campaign/Mission host、live Economy、AFEF 写回和旧存档 load/save/reload 验收。
3. 阶段 8 的清理候选必须逐 symbol 对账后再决定；`KEEP/HOLD/REVIEW` 不能直接删除。
4. FirstChance 异常根因尚未修复，不能以“抑制日志”代替修复。

## 日志证据与 48k FirstChance 问题

用户提供的日志归档：`E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge\Logs.zip`。本轮解压审阅目录：`.tmp/logs-review-20260907-latest/Logs`。

`Mod_Logic.txt` 在约 `01:56:46` 记录到 `FirstChance count=48220`；异常主体是 `System.NullReferenceException`，重复出现在 `TaleWorlds.CampaignSystem.CampaignEventDispatcher.Tick` 期间。详细堆栈还出现：

- `TaleWorlds.CampaignSystem.CampaignTime.get_Now()`
- `TaleWorlds.CampaignSystem.Clan.get_PlayerClan()`
- `AnimusForgeRelationshipPatch.RelationshipSeaStopPreparation.CurrentHour()` 第 1192 行
- 一次 `AnimusForge.NpcRulerPolicyBehavior.CaptureUnifiedNpcPolicyHistorySnapshot()`（`CustomPolicyBehavior.Generation.cs` 第 2755 行）失败并抛出玩家政策历史快照异常。

关键阻塞：日志中的 `AnimusForgeRelationshipPatch.RelationshipSeaStopPreparation` 来自外部工作树路径：

`C:\Users\Jamin\Documents\Codex\2026-08-07\codex-threads-019fcdcd-d437-7223-8072\work\love_hate_v0631a\AnimusForgeRelationshipPatch\RelationshipSeaStopPreparation.cs`

当前仓库没有该类。当前源码的 `WorldDiplomacyBehavior.CurrentHour()` / `CurrentDay()` 也直接访问 `CampaignTime.Now`；即使 `try/catch` 捕获，Bannerlord 的 FirstChance 事件仍会计数。1.4 的 `CampaignTime.Now` 最终读取 `Campaign.Current.MapTimeTracker.Now`，在 Campaign 尚未初始化或销毁阶段可能为空。`BannerlordExceptionSentinel` 会记录带 `TaleWorlds` 或 `AnimusForge` 堆栈的 FirstChance 异常，因此仅调低日志频率不会消除根因。

## 下一项准确任务

1. 在游戏目录核对当前 `AnimusForge.Bootstrap.dll`、`versions/1.3/AnimusForge.dll`、`versions/1.4/AnimusForge.dll` 的来源、修改时间和 SHA-256；与 project-local stage 及本工作树提交逐一对账，确认是否混入了外部 `love_hate_v0631a` 实现。
2. 定位 `RelationshipSeaStopPreparation` 的实际程序集/模块归属和加载链；若来自旧部署或另一工作树，先隔离来源并记录证据，再决定替换或移除，禁止直接覆盖未知 DLL。
3. 对所有 CampaignTime/PlayerClan 读取建立统一的初始化状态门禁（优先使用现有兼容 helper），在 Campaign 生命周期外 fail-closed；避免在 Tick 热路径反复抛出再捕获异常。
4. 将 `CaptureUnifiedNpcPolicyHistorySnapshot()` 的玩家历史缺失/不可用状态改为无异常的显式结果或受控诊断，保持主线程提交、stale/version 和存档兼容语义。
5. 补充最小纯测试与双版本/Bootstrap 构建；只有在日志计数下降且无新根因后，才进入授权的游戏内回放和旧存档验收。

## 本轮边界

- 本轮未修改生产代码、构建/覆盖/推送脚本或游戏目录。
- 未部署、未启动游戏、未操作真实存档；自动化/真实 provider 保持关闭。
- 未改变程序集身份、模块 ID、SyncData key/type 或存档类型。
- 交接后仍应把真实 Campaign/Mission、三渠道 live commit、AFEF readback、旧存档和安装目录加载标为 `NOT-RUN`，直到取得对应证据。
