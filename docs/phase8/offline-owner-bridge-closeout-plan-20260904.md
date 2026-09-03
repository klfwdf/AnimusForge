# 阶段 8 离线责任认领与 Bridge 收尾计划

日期：2026-09-04
工作区：`F:\AnimusForge-main`
分支：`refactor/prepare-af-restructure`

## 这份文件的定位

本文件记录本轮用户授权下可以先行完成的**离线准备工作**：责任角色建议、入口盘点复核顺序、
Bridge 的安全收尾边界和制作组实机接手顺序。它不是第二份领域目录，也不是 owner 身份认证。
权威字段仍在 `docs/phase8/full-domain-readiness-catalog.json`，证据门禁仍由
`tools/PhaseEightReadiness/readiness.py` 执行。

本轮采用 `PROVISIONAL_AUTHORIZED`（用户授权的临时准备）而不是正式 `ASSIGNED`。原因是当前
没有制作组成员的可验证 reviewer ID、真实 Campaign/Mission 观察记录或旧存档证据。不得把本文件
或离线测试结果写回成 `ownerAssignmentState=ASSIGNED`、`entryCoverage=COMPLETE`，也不得借此
把阶段 7/8 标为 DONE。

## 当前离线结论

- Bridge：`16 bindings / 10 wired / 6 declared-only / configEnabled=10`。
- 10 组 wired 已在生产入口存在经过审阅的 source-bound Gate；这只证明离线接线顺序和降级边界。
- 6 组 declared-only 为 `bootstrap-host`、`host-runtime`、`runtime-game-adapter`、
  `persistence-domain-owners`、`scene-duel`、`tools-content-release`。
- 入口候选清单已由 `tools/PhaseEightReadiness/entry_inventory.py` 稳定生成；它只补齐候选路径，
  当前正式目录仍是 `entryCoverage=REPRESENTATIVE`。
- 真实 Campaign/Mission、LIVE/SAVE、旧档往返、live Economy/AFEF/Notoriety、Duel 副作用和
  Release 安装仍未验证。

## 认领与晋级规则

1. 下表的“建议 owner”沿用权威目录的逻辑 owner；“维护角色”沿用目录中的 maintainer 角色，
   不是本文件新造的团队账号。
2. 制作组接手时，先把实际人员/账号写入团队的 reviewer 记录，再由对应 owner 复核全部入口，
   包括静态调用、Harmony/反射、MCM 注册、资源加载和外部工具入口。
3. 只有 owner 复核通过并具备可追溯证据时，才可以在权威目录中把角色从
   `ROLE_PLACEHOLDER` 改为 `ASSIGNED`，把入口从 `REPRESENTATIVE` 改为 `COMPLETE`。
4. `wired` 只允许在真实方法体中先经过 `FeatureBridgeRuntime` Gate，再触及凭据、网络、owner
   回调、提交或玩法副作用；不能为了提高数量添加空 caller、注释 caller 或每帧/全量扫描。
5. 任何 LIVE/SAVE 记录都必须绑定准确的 1.3/1.4 BuildInfo、测试存档 identity、Bootstrap 与
   implementation 哈希、步骤/观察日志和全部相关 ownerReview。离线 fixture、Stage 或进程存在
   不能替代这些证据。

## 20 个领域的接手矩阵

`入口复核` 的基线是权威目录中的 `entryPaths`，候选补充由 `entry_inventory.py` 产生；在 owner
确认前均不视为完整调用图。

| # | Domain ID | 建议 owner | 维护角色 | 关联 Bridge | 当前离线状态 | 接手动作 |
|---:|---|---|---|---|---|---|
| 1 | `bootstrap-build` | `Build.Release` | `build-owner` | `bootstrap-host`, `tools-content-release` | LOCAL_PASS | 双 API 启动与单 Bootstrap 选择；核对包清单和回滚 |
| 2 | `host-composition` | `Foundation.Composition` | `foundation-owner` | `bootstrap-host`, `host-runtime` | VERIFY | 复核注册顺序、部分启动清理和设计入口类型 |
| 3 | `runtime-diagnostics` | `Foundation.Runtime` | `foundation-owner` | `host-runtime`, `runtime-game-adapter`, `ui-runtime-integration` | VERIFY | 复核生命周期诊断；保持非 Tick Bridge |
| 4 | `game-adapter-compatibility` | `Compatibility.GameAdapter` | `adapter-owner` | `runtime-game-adapter` | LOCAL_PASS | 1.3/1.4 反射、Harmony 和 Native fallback 实机复核 |
| 5 | `persistence-config` | `Foundation.Persistence` | `persistence-owner` | `persistence-domain-owners` | LOCAL_PASS | identity、坏数据、旧档 round-trip；不增 key/type |
| 6 | `conversation-encounter` | `Conversation.Encounter` | `conversation-owner`, `encounter-owner` | `conversation-gateway`, `conversation-action`, `conversation-siege`, `conversation-courier` | LOCAL_PASS | Native/自由对话/场景喊话入口与目标解析逐项确认 |
| 7 | `gateway-prompt-protocol` | `Conversation.Gateway` | `gateway-owner`, `prompt-owner` | `conversation-gateway`, `gateway-knowledge-profile` | LOCAL_PASS | provider、超时、取消和三渠道 prompt/history 对齐 |
| 8 | `action-commit` | `Interaction.ActionCommit` | `action-owner` | `conversation-action`, `action-memory`, `action-economy` | LOCAL_PASS | 非经济副作用、拒绝/重复/过期请求和默认入口保持不变 |
| 9 | `memory-afef` | `Memory.Persistence` | `memory-owner` | `action-memory`, `persistence-domain-owners`, `memory-social-reports` | LOCAL_PASS | AFEF、Daily/Recent、保存重载和未知结果不重放 |
| 10 | `economy-reward-debt` | `Economy.RewardDebt` | `economy-owner` | `action-economy`, `persistence-domain-owners` | LOCAL_PASS | Hero/Party/Merchant/Debt 各自单动作与部分/未知结果 |
| 11 | `policy-political` | `Policy.Political` | `policy-owner` | `policy-world-diplomacy`, `persistence-domain-owners` | VERIFY | policy effect、目标和保存状态与外交共同复核 |
| 12 | `world-simulation-worldmap` | `World.Simulation` | `world-diplomacy-owner`, `world-map-owner` | `policy-world-diplomacy`, `persistence-domain-owners` | VERIFY | 世界地图事件、外交通知、跨日和旧档复核 |
| 13 | `settlement-siege-gccz-sets` | `Settlement.Siege` | `siege-owner`, `settlement-owner` | `conversation-siege`, `persistence-domain-owners` | VERIFY | 攻城/普通场景隔离、GCCZ/SETS 结果和保存状态 |
| 14 | `scene-mission-combat` | `Scene.Mission` | `scene-owner`, `mission-owner` | `scene-duel`, `ui-runtime-integration` | VERIFY | Mission 生命周期、Agent 目标、原版战斗隔离 |
| 15 | `duel` | `Duel.Combat` | `duel-owner` | `scene-duel`, `persistence-domain-owners` | LOCAL_PASS | accept/reject/queue/start/cancel/death/exit、stake/AFEF/Fourberie |
| 16 | `courier-proactive-issue` | `Courier.ProactiveIssue` | `courier-owner`, `proactive-owner`, `issue-owner` | `conversation-courier`, `persistence-domain-owners` | VERIFY | delivery restart、重复 completion、旧档和 vanilla issue 权限 |
| 17 | `social-progression-reports` | `Social.ProgressionReports` | `social-owner`, `reports-owner` | `memory-social-reports`, `persistence-domain-owners` | VERIFY | Notoriety/weekly 精确顺序、真实随机数与保存中断 |
| 18 | `knowledge-persona-profile` | `Knowledge.PersonaProfile` | `knowledge-owner`, `profile-owner` | `gateway-knowledge-profile`, `persistence-domain-owners` | VERIFY | model/index/authority 缺失、profile precedence 和重载 |
| 19 | `ui-tts-external-integration` | `UI.ExternalIntegration` | `ui-owner`, `tts-owner` | `ui-runtime-integration`, `tools-content-release` | VERIFY | Gauntlet focus、TTS/扩展缺失和关闭清理 |
| 20 | `tools-content-package` | `Tools.ContentRelease` | `tools-owner`, `release-owner` | `tools-content-release` | VERIFY | package allowlist、资源/ONNX 配对、备份与回滚 |

## 16 组 Bridge 的处置

### 已 wired（10 组）

`conversation-gateway`、`conversation-action`、`action-memory`、`action-economy`、
`policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、
`memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration`。

这些入口继续使用 manifest 中的频率和 fallback；不另造第二套 coordinator。制作组需要补的是
真实 LIVE/SAVE 证据和 ownerReview，而不是再次复制 caller。

### 保持 declared-only（6 组）

| Bridge | 为什么暂不接线 | 晋级所需条件 |
|---|---|---|
| `bootstrap-host` | Bootstrap 已是唯一加载边界；再加 loader 会破坏单模块/单实现契约 | 1.3/1.4 真实启动、选择日志、失败降级和包证据 |
| `host-runtime` | `SubModule` 生命周期/诊断没有安全的独立事件 caller；不能在 Tick 加 gate | 真实生命周期隔离、部分启动清理和 stale completion 证据 |
| `runtime-game-adapter` | 兼容 helper 是 API 适配边界，不是一个可安全复用的单事件入口 | 两 API 线真实反射/Harmony 与 Native fallback 证据 |
| `persistence-domain-owners` | 多 owner 存档交叉面没有获授权的总 coordinator；不能新增 save identity | 各 owner 独立、组合、坏数据和旧档 round-trip 证据 |
| `scene-duel` | 需要真实 Mission/死亡/退出副作用，当前明确先不做实机验证 | Mission 组合、原版战斗隔离、Duel 结果和保存证据 |
| `tools-content-release` | 工具/发布不是游戏运行时 caller；接线会混淆发布和玩法边界 | 完整 package/install/resource/rollback 证据，另行评审发布授权 |

禁止以“新增一个返回布尔值的方法”或“把测试 runner 当生产 caller”的方式改变上述状态。

## 制作组接手顺序

1. 先依据本矩阵确认实际 owner/reviewer 身份，保留逻辑角色与真实人员的对应记录。
2. 在隔离测试档重新部署与源码匹配的 Stage，分别启动 1.3 和 1.4 Campaign；记录 Bootstrap
   选择和三份 DLL 哈希。
3. 按领域逐项执行 `docs/phase8/full-domain-acceptance-package.md` 的 LIVE/SAVE case；每条
   记录显式绑定 `domainIds`、`bridgeIds`、`apiLine`、`source.commit` 和附件哈希。
4. 入口 owner 完成静态/动态/Harmony/MCM/资源入口复核后，才申请把 `REPRESENTATIVE` 晋级为
   `COMPLETE`；缺一项就保持原状态。
5. 全部证据和 rollback drill 通过后，再单独评审 facade 清理、默认入口切换、Release 安装和
   发布；本计划不授予这些操作权限。

## 可重复的离线检查

```powershell
python -B .\tools\PhaseEightReadiness\entry_inventory.py --check
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py
python -B -m unittest discover -s .\tools\BridgeBindingContractTests -p 'test_*.py' -v
python -B -m unittest discover -s .\tools\PhaseEightReadiness -p 'test_*.py' -v
python -B .\tools\PhaseEightReadiness\readiness.py `
  --project-root . `
  --manifest .\docs\phase8\all-missing.evidence.json
# 预期：BLOCKED / exit 2；缺证据是正确结果。
```

本文件不改变运行时代码、默认入口、存档身份、部署目录或远端分支状态。任何正式认领、实机
证据和发布动作都必须在制作组 review 后另行提交。
