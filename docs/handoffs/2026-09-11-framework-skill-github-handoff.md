# AF 框架 Skill 与代码定位交接（2026-09-11）

## 结论

**已发布框架包：`38c003ab`，远端 `codex/af-main-refactor-continuation-20260831` 已核实。后续仅补本交接回执，最新提交以远端 ref 为准。**

已把“AF 主体 + 同 DLL 制作组 internal 接口 + 独立子 MOD public API”固化成仓库 Skill；固化的是架构责任和兼容约定，**不是把主体功能、算法或公开能力上限永远写死**。本轮只补维护规则、定位/覆盖文档与发布隔离，不修改游戏运行代码；整个阶段 8 仍未完成。

## Skill 怎么用

仓库入口：`.agents/skills/af-core-framework/SKILL.md`。根 `AGENTS.md` 已增加读取规则；支持仓库技能发现的 Codex 可选用 `$af-core-framework`，其他工具/成员直接读取该文件即可。本轮没有安装到用户全局目录，也不声称其他成员工具会自动装载。

Skill 要求：
- 可以按批准的需求调整主体对话、Prompt、标签、记忆与调度；明确旧行为与有意新行为，保护其他渠道和公开兼容。
- 使用真实上下文与已有合适配置，不硬编码 NPC/机器路径/测试结果；不为“灵活”创造无调用接口、重复执行器或每帧反射。
- 内部 typed ports 和公开版本化 API 分开。当前 public 只读是事实，未来可经完整实现/验证扩展，不是永久封死。
- HANDOFF 必须给出代码路径、一基行号范围、符号、源码版本、实际接线及未覆盖责任；精确 hash 对照不阻止批准的功能演进。
- 发布前区分本次新接缝、新旧混合 owner、仍运行旧入口、未处理模块业务和未开放设计，不能“只改几个文件就说全完成”。

## 源码位置注释

下表核对源码提交 `8f1cd479`。行号是导航，不是冻结方法实现；以后改主体要同步更新符号与行号。完整 25 个定位点见 [代码范围图](../architecture/af-framework-code-scope.md) 和 [JSON 坐标](../architecture/af-framework-code-map.json)。

| 能力 | 路径与一基行号 | 符号 / 责任 |
|---|---|---|
| `internal.port.policy` | `Refactor/Modules/TeamModulePorts.cs:7-10` | `internal interface IPolicyModulePort` — 政策 typed 接缝，业务归原 owner |
| `internal.port.gathering` | `Refactor/Modules/TeamModulePorts.cs:17-20` | `internal interface IGatheringModulePort` — 宴会 typed 接缝，不迁移玩法 |
| `internal.port.siege` | `Refactor/Modules/TeamModulePorts.cs:27-30` | `internal interface ISiegeModulePort` — GCCZ 接缝，保留原场景门禁 |
| `internal.adapters` | `Refactor/Modules/TeamModuleAdapters.cs:7-10` | `internal sealed class PolicyModuleAdapter : IPolicyModulePort` — 同文件三组薄桥原样转接参数/返回/ref/out |
| `internal.directory` | `Refactor/Modules/InternalModuleDirectory.cs:137-140` | `internal sealed class InternalModuleDirectory` — 注册/冻结/依赖校验，非游戏执行授权 |
| `internal.runtime` | `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` | `internal static class ModuleFrameworkRuntime` — 装配与只读投影，不是第二套执行器 |
| `public.api` | `Api/V1/AfApi.cs:13-16` | `public static class AfApi` — 当前只读；其他提交/写能力未开放 |
| `native.history.capture` | `MyBehavior.HistoryPromptSnapshot.cs:35-38` | `internal static Func<string> CaptureHistoryContextWorkById` — 召回用途投影，非全局记忆事务 |
| `native.history.bridge` | `ShoutBehavior.cs:16180-16183` | `private static Func<string> CaptureNativeConversationPersistedHistoryWork` — 原身份解析在主线程捕获，非全 Shout 重写 |
| `native.history.accept` | `ShoutBehavior.cs:20201-20204` | `if (!await RunNativeConversationMainThreadFuncAsync("persisted_history_accept"` — 使用结果前再验原 admission |
| `memory.acceptance` | `MyBehavior.DialogueHistoryCommit.cs:12-15` | `internal static MemoryCommitResult CommitDialogueHistoryWithScene` — 运行期接受，不是磁盘/跨动作事务 |
| `legacy.history` | `MyBehavior.cs:28087-28090` | `public static string BuildHistoryContextForExternal(` — Scene/Courier 仍调用，共享兼容入口不能盲删 |
| `courier.prepare.reply` | `CourierDeliveryBehavior.cs:4674-4677` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(recipient,` — 回信早期准备待线程审查，未迁快照 |
| `courier.prepare.inbound` | `CourierDeliveryBehavior.cs:5102-5105` | `string historyText = (MyBehavior.BuildHistoryContextForExternal(sender,` — 来信早期准备待线程审查，未迁快照 |

## 与旧 GitHub 代码的分界

- 上次远端基线：`a58c2191`；本包运行代码：`8f1cd479`。主分支、其他制作组分支不更新。
- 原共享大文件中仍运行的逻辑保持原位，用符号/调用点区分，不为了“新旧隔开”破坏 Saveable/SyncData、改加载布局或复制第二套源码参与编译。
- Native 记忆快照已接一条真实入口；Scene/Courier 默认旧历史仍运行，persona/规则/周报、剩余 TTS、其他记忆 writer 和实机验收仍需后续。没列到的代码不自动归为已完成或可删除。

## 交付 Git 边界

- 工作区：`G:\AFMOD\AF-REFACTOR`。
- 干净本地交付分支：`codex/af-framework-skill-delivery-20260911`；获准远端目标：`origin/codex/af-main-refactor-continuation-20260831`（`https://github.com/klfwdf/AnimusForge.git`）。后续以当前 Git/HANDOFF 为准，不把这个分支名写进可复用 Skill。
- 原本地 `codex/af-main-refactor-continuation-20260831` 保留完整来源记录，**不能直接推送**：其中的 `37c43417` 含用户明确排除的简明 HANDOFF。
- 干净交付以 `8f1cd479` 为父提交，保留此前代码/测试祖先，重放所需后续文档/Skill，不引入 `37c43417` 及其本地专用文件历史。普通快进推送，不 force、不改写原历史。
- `docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md` 仅留本地原路径，交付树与新增远端提交历史均排除；不只是最终删除。两份 2026-09-06 用户草稿不纳入本次提交。
- 回滚选择本次交付提交或其中具体文件的反向提交；回滚文档/Skill 不等于反转 `8f1cd479` 运行代码。不要从来源分支 merge 全部历史回交付分支而带入排除文档。

## 验证与接续

Skill 格式、内部链接、25 处符号/行号/源码摘要要验证；定位脚本必须拒绝错行号、缺失符号和错误内容摘要。运行源码与 `8f1cd479` 完全一致，本轮不重复宣称新构建或实机测试；沿用上一轮六项 Stage/行为回归证据。交付后以远端 ref 和可达历史验证上传范围，不能把本地提交当推送成功。

总入口：根 `HANDOFF.md`。自动化 `af-7-8` 保持 PAUSED；用户只授权本次 GitHub 推送，没有授权恢复自动化、覆盖游戏或操作真实存档。后续功能工作等待新指示。
