# AF 20 领域全量推进报告

日期：2026-09-03  
工作区：`F:\AnimusForge-main`  
分支：`refactor/prepare-af-restructure`  
源码提交：`490232ea31dc3823b979e644fe734330594a7f59`

## 1. 本轮结论

本轮已完成 20 个领域的自动化离线推进：入口候选盘点、Bridge/组合契约、双版本相关回放、
持久化/经济/记忆/Gateway/World/Policy/Social/工具 smoke 均已按现有 runner 执行并保存本地
transcript。

这份报告**不把离线结果写成完整领域验收**。权威目录中的 20 个领域仍保持原有的
`ROLE_PLACEHOLDER` / `REPRESENTATIVE`，原因是：

- 真实 owner/reviewer 身份尚未登记；
- LIVE 1.3/1.4 和 SAVE 1.3/1.4 证据尚未按领域绑定；
- Release 安装、rollback drill 和完整 20 域签收尚未完成；
- 1.4 场景带路仍只有“启动成功”证据，尚无抵达/返回闭环。

## 2. 工作树边界

本轮只提交了 Persistence fixture 行号修复：

```text
490232ea fix: refresh persistence binding catalog line numbers
```

以下终端 UI 本地修改属于用户已有内容，本轮没有暂存、没有提交、没有覆盖：

- `AiErrorAnalysisInquiry.cs`
- `AnimusForgeTerminalBehavior.cs`
- `AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml`
- `AnimusForgeTerminalUiModels.cs`

## 3. 全量离线验证索引

本轮 transcript 位于被 `.gitignore` 的目录：

```text
F:\AnimusForge-main\artifacts\phase8-full-domain-20260903
```

已通过的主要切片：

| 范围 | 结果 |
|---|---|
| AF.Contracts | 9 contracts / 3 events / 6 capabilities / 18 invalid cases PASS |
| Foundation metadata | 6 contracts / 8 health states / 16 invalid cases PASS |
| GameAdapter metadata | 14 cases / 2 API lines / 7 helpers PASS |
| Bridge fixture/binding | 10 fixture cases；16 bindings / 10 wired / 6 declared-only PASS |
| Composition/Module catalog | 18 composition cases / 24 invariants；8 modules / 3 profiles / 16 invalid cases PASS |
| Phase 8 readiness tests | 68/68 PASS；入口 inventory `--check` PASS |
| Persistence/Profile/Config | 95 literal / 121 typed / 42 symbolic / 44 flattened / 3 profiles PASS |
| Persistence chunk/recovery | chunk replay PASS；memory recovery PASS |
| Interaction pipeline | 40 pipeline / 69 detached-host / 39 receipt cases PASS |
| Gateway/transport | Configured、Validation、Primary LLM、SSE、TTS、Xihai 全部 PASS |
| Production host | Configured、Courier、Detached、Opt-in、Validation Provider 全部 PASS |
| Economy | executor、port、production owner、economy-aware commit 全部 PASS |
| World/Policy | Policy effects 9031 assertions；World smoke 269/1168/453 assertions；Gateway PASS |
| Duel/Courier/Social | Duel dispatch 16/16、Duel outcome 18/18、Courier inbound、Notoriety 14/14、Weekly PASS |
| Prompt/Content tooling | ActionPostprocess smoke PASS；Preprocess smoke PASS |

PlayerExportsEditor smoke 的工具代码路径已执行，但本地内容包暴露数据问题：
`权游国度编年史-大帝版` 有 176 条知识字段错误，`战锤旧世界编年史` 有 1247 条警告。
这些属于内容数据清理任务，不能改写成运行时重构失败或成功。

## 4. 20 个领域当前状态

| # | Domain ID | 逻辑 owner / maintainer | 入口盘点 | 本轮离线结果 | LIVE/SAVE/Release 阻塞 |
|---:|---|---|---|---|---|
| 1 | `bootstrap-build` | `Build.Release` / `build-owner` | 候选存在；人工复核待做 | Build marker、Bootstrap-only XML、双实现结构 PASS | 真实 1.3/1.4 启动、包与回滚 |
| 2 | `host-composition` | `Foundation.Composition` / `foundation-owner` | 候选存在；人工复核待做 | Composition/Host 相关契约 PASS | 生命周期顺序、partial-start cleanup |
| 3 | `runtime-diagnostics` | `Foundation.Runtime` / `foundation-owner` | 候选存在；人工复核待做 | Foundation/health 纯契约 PASS | 真实生命周期、队列/缓存预算 |
| 4 | `game-adapter-compatibility` | `Compatibility.GameAdapter` / `adapter-owner` | 候选存在；人工复核待做 | 1.3/1.4 metadata 与回放 PASS | 真实双 API 反射/Harmony/fallback |
| 5 | `persistence-config` | `Foundation.Persistence` / `persistence-owner` | 候选存在；人工复核待做 | Persistence/Profile/Chunk/Identity PASS | 旧档、坏数据、SAVE round-trip |
| 6 | `conversation-encounter` | `Conversation.Encounter` / `conversation-owner`, `encounter-owner` | 候选存在；人工复核待做 | 三渠道 host、history、cancel/fallback PASS | 真实 Native/Scene/Courier 组合 |
| 7 | `gateway-prompt-protocol` | `Conversation.Gateway` / `gateway-owner`, `prompt-owner` | 候选存在；人工复核待做 | Configured/Primary/SSE/Validation PASS | 真实 provider、三渠道 prompt/history |
| 8 | `action-commit` | `Interaction.ActionCommit` / `action-owner` | 候选存在；人工复核待做 | Pipeline/receipt/detached commit PASS | 真实主线程副作用、重复/部分失败 |
| 9 | `memory-afef` | `Memory.Persistence` / `memory-owner` | 候选存在；人工复核待做 | Memory recovery、AFEF/receipt PASS | 真实 AFEF、旧档重载、未知结果 |
| 10 | `economy-reward-debt` | `Economy.RewardDebt` / `economy-owner` | 候选存在；人工复核待做 | Economy executor/port/owner PASS | Hero/Party/Merchant/Debt 实机与 SAVE |
| 11 | `policy-political` | `Policy.Political` / `policy-owner` | 候选存在；人工复核待做 | Policy effect/gateway PASS | 真实 policy apply、跨日和存档 |
| 12 | `world-simulation-worldmap` | `World.Simulation` / `world-diplomacy-owner`, `world-map-owner` | 候选存在；人工复核待做 | World compression/intent/result/gateway PASS | 世界地图实机、跨日、旧档 |
| 13 | `settlement-siege-gccz-sets` | `Settlement.Siege` / `siege-owner`, `settlement-owner` | 候选存在；人工复核待做 | 现有 policy/world/bridge 契约 PASS | GCCZ/SETS 与攻城/普通场景实机 |
| 14 | `scene-mission-combat` | `Scene.Mission` / `scene-owner`, `mission-owner` | 候选存在；人工复核待做 | Scene 结构可回放；1.4 带路闭环未通过 | 当前寻路/抵达/返回问题 |
| 15 | `duel` | `Duel.Combat` / `duel-owner` | 候选存在；人工复核待做 | Dispatch/outcome/production 35/35 PASS | 真实 Mission 死亡/退出/stake/SAVE |
| 16 | `courier-proactive-issue` | `Courier.ProactiveIssue` / `courier-owner`, `proactive-owner`, `issue-owner` | 候选存在；人工复核待做 | Courier host/inbound/production PASS | 真实 delivery lifecycle 与旧档 |
| 17 | `social-progression-reports` | `Social.ProgressionReports` / `social-owner`, `reports-owner` | 候选存在；人工复核待做 | Notoriety/Weekly contract/production PASS | 真实随机数、ConversationEnded、SAVE |
| 18 | `knowledge-persona-profile` | `Knowledge.PersonaProfile` / `knowledge-owner`, `profile-owner` | 候选存在；人工复核待做 | Knowledge/Model catalog PASS | 真实 index/profile precedence 与重载 |
| 19 | `ui-tts-external-integration` | `UI.ExternalIntegration` / `ui-owner`, `tts-owner` | 候选存在；人工复核待做 | TTS、Prompt tooling、UI boundary 结构 PASS | Gauntlet focus、扩展缺失、关闭清理 |
| 20 | `tools-content-package` | `Tools.ContentRelease` / `tools-owner`, `release-owner` | 候选存在；人工复核待做 | Content/tool smoke 部分 PASS；Editor 内容包有数据错误 | package allowlist、安装、rollback |

## 5. Owner 与入口晋级规则

当前 `entry_inventory.py --check` 已通过，但它只证明候选路径存在、稳定且没有明显越界；
它不会自动把 `REPRESENTATIVE` 改成 `COMPLETE`。

每个领域的晋级顺序必须是：

1. 记录真实人员/账号与逻辑 maintainer 的映射；同一人可以兼任多个角色，但必须显式写出。
2. owner 复核普通 C#、Harmony、反射、MCM、资源、工具和外部入口。
3. 把完整入口清单绑定到该 owner 的 review 记录。
4. 采集 OFFLINE、LIVE 1.3/1.4、SAVE 1.3/1.4、RELEASE evidence。
5. 对相关 Bridge 逐个覆盖 required cases，并在记录的 `bridgeIds` 中声明。
6. 所有模块 maintainer、领域 maintainer、Bridge co-owner 签收后，才更新：

```text
ROLE_PLACEHOLDER -> ASSIGNED
REPRESENTATIVE   -> COMPLETE
```

不能通过新增空 caller、注释 caller、测试 runner caller 或 Tick 全量扫描来提高 wired 数量。

## 6. 接下来可以并行做的事情

### 我负责

- 分析 1.4 `SceneGuide` 的导航未抵达问题并做小范围修复；
- 继续生成/校验 evidence JSON 和 hash-bound transcript；
- 把你提供的真实存档与日志整理成 SAVE 记录；
- 根据 owner roster 更新入口 review 和 readiness 报告；
- 继续维护 20 域状态，不触碰终端 UI 本地改动。

### 你只需要确认

- 哪个真实账号承担哪些逻辑 owner/maintainer 角色；
- 哪些旧档允许复制到验收工件目录；
- 实机是否观察到动作、状态变化和保存重载结果；
- 最终是否允许 Release 安装、rollback drill、推送和默认切换。

## 7. 当前不能宣称的事项

- 20 个领域已完成正式 owner 认领；
- 20 个领域已达到 `entryCoverage=COMPLETE`；
- 阶段 7/8 已 DONE；
- 1.3/1.4 LIVE/SAVE 全部完成；
- Release 已安装并发布；
- 默认入口可以切换或旧 facade 可以删除。

本报告是推进台账，不是发布许可，也不改变权威 readiness 门禁。
