# AF SETS 核心重做与影子接线 Progress Handoff

Date: 2026-08-28 (second session of the day)
Supersedes progress tracking in: `af-sets-noble-full-integration-handoff-20260828.md` (contract, architecture, and remaining-work sections there stay authoritative for the un-started items; this document records what actually landed since).
Related: `sets-urban-capture-refactor-handoff-20260825.md`, `sets-urban-capture-refactor-progress-20260826.md` (superseded core design — the 2026-08-26 `SetsUrbanCaptureContext`/`Policy`/`CompletionPlan` shapes described there were replaced in this session per the 2026-08-28 defect list).

## User Directive That Scoped This Session

"SETS 是完全不影响其他功能的，只有玩家在城内进攻的时候士兵会帮忙攻击。满足这个就行，然后你做好一定方便以后重构爆改。"

Translation of intent: do not build the full AF `AfMissionScope`/`AfSceneParticipantRegistry` cross-feature machine from the 2026-08-28 handoff's section 7 right now. SETS must stay a self-contained, zero-blast-radius feature (own the "followers help fight inside a captured town/castle" behavior only) while the internal capture core gets fixed and left easy to swap wholesale later. The noble-escort integration (handoff sections 9–12) was **not** started this session — out of scope per this directive until asked for.

## What Changed This Session (chronological)

### 1. Disk space failure recovered

Mid-session, C: dropped to 2.9 GB free (`No space left on device` on `git status`). Root cause: ~3.4 GB of stale temp data, mostly 10 leftover `codex-review-objects-*` directories (541 MB largest) and installer temp files unrelated to this repo. Deleted via `Remove-Item -Recurse -Force` on `C:\Users\28358\AppData\Local\Temp\codex-review-objects-*` plus a few named `.tmp`/random-named leftover dirs. C: now sits around 4.2 GB free — enough to build, but still tight; **flag to the user if it drops again**.

### 2. Backup tag before touching anything

`backup/pre-sets-noble-integration-20260828` created at GCCZ `d02f9cb` and NEW-10 `85cd4810` (the commits that carried the 2026-08-28 handoff itself). Earlier tag `backup/pre-sets-refactor-20260825` left untouched.

### 3. Protected pre-existing dirty work before rewriting the core

Both repos had uncommitted voice-session work (`TownOrdinarySpeakerVoiceSession.cs`, prompt catalog/composer edits, `AfGcczShoutBridge.cs`, `ShoutBehavior.cs`, `SiegeAiInterventionBehavior.cs` changes) unrelated to SETS. Committed as-is before any SETS edit, per the "no destructive shortcuts" rule:

- GCCZ `aa74301` — voice-session standalone additions
- NEW-10 `993d414d` — voice-session runtime wiring

### 4. SETS capture core rewritten to close S-01–S-06 and S-09

The 2026-08-26 core (`SetsUrbanCaptureContext`/`Policy`/`CompletionPlan`/`Session`, with an `IncidentTriggered`/owned branch baked into the same state machine) was replaced — not patched — because the 2026-08-28 handoff's defect table (section 5.1) named exactly the flaws in that shape: the transition table didn't consult context, the owned branch shared states with the hostile branch, ownership eligibility didn't require a committed victory, and the completion plan could return multiple simultaneous actions.

New shape (all in `GCCZ\src\AnimusForge.SiegeAftermathIntervention\`, mirrored byte-for-byte to `NEW-10\AnimusForge.SiegeAftermathIntervention\`):

| File | What it is now |
|---|---|
| `SetsUrbanCaptureState.cs` | 9 states, hostile-only: `Inactive → EntryPrepared → MissionActive → ConflictActive → VictoryReached → AwaitingMap → OwnershipCommitted → NativeMenuOpened → Completed`, plus terminal `Suspended`. No `IncidentTriggered`/owned-menu states — removed entirely (S-02). |
| `SetsUrbanCaptureContext.cs` | Sealed, built only via `TryCreateHostile(...)`, which returns **null** for anything not a hostile town/castle (S-01): villages, unknown scenes, empty ids, or a target the player already owns. Carries `PlayerClanId`. `ResolveRecovery(...)` implements the exact 5-row table from handoff §8.2 (S-05): missing settlement → Abandon; unchanged owner → Continue; player owns it + victory committed → ContinueOwnershipAlreadyApplied; player-owned without committed victory, third-party owner, or missing/changed player clan → Suspend. |
| `SetsUrbanCapturePolicy.cs` | `IsLegalTransition(context, from, event, ledger)` now takes context+ledger, not state alone (S-03/S-04): `CommitOwnership` requires `AwaitingMap` **and** `ledger.VictoryCommitted`; `OpenNativeMenu` requires `OwnershipCommitted` **and** `ledger.OwnershipCommitted`; `Complete` requires `ledger.MenuCommitted`; `EndMission` reaches `AwaitingMap` only with a committed victory ledger (a quiet visit with no conflict returns to `Inactive` instead). `Abort` is legal only pre-victory-commit. New `IsRestoredCombinationValid(state, ledger)` rejects every impossible pairing (S-06) — e.g. `OwnershipCommitted` state without `ledger.OwnershipCommitted`. |
| `SetsUrbanCaptureCompletionPlan.cs` | Renamed concept: no longer a multi-flag "plan" object. `SetsUrbanCaptureCompletionPlanner.ResolveNextAction(...)` returns exactly **one** `SetsUrbanCaptureNextAction` (`CommitOwnership → PrepareNativeAftermathContext → OpenNativeMenu → Complete`, or `Suspend` on an illegal restored combination) — S-04. New `SetsUrbanCaptureActionOutcome` enum (`Succeeded / AlreadyApplied / Retryable / Failed`) and `ResolveEventForOutcome(...)` map a structured runtime result to the next event; `Retryable` advances nothing, `Failed` maps to `Suspend`. `MaxRetriesPerAction = 5` with `ShouldSuspendAfterRetry(...)` bounds retries (S-09). |
| `SetsUrbanCaptureSession.cs` | Aggregate unchanged in spirit, updated to the new API: `TryApply` now delegates every check to `Policy.IsLegalTransition` (context+ledger aware); `ResolveNextAction`, `MarkNativeContextPrepared`, `RecordRetryableFailure`, `RestoreFromRecord` (forces `Suspended` on an illegal restored combination — S-06) are new. `IsSuspended` property added. |
| `SetsUrbanCaptureLedger.cs` | Unchanged from 2026-08-26 (still correct): per-agent once-only casualty gates, per-phase reserve-withdrawal gate, five commit flags (`victory/ownership/menu/villageReward/completion`), `RestoreCommittedStages` never resurrects mission-scoped agent indexes. |
| `SetsUrbanCaptureContractProfile.cs` | Unchanged (Slice A numerical anchors: 100/10 limits, wave/timing constants, save keys, phase order). |

`SetsUrbanCaptureCompletionPlan.DoNothing` / `.Resolve(...)` from 2026-08-26 no longer exist — replaced by the planner's `ResolveNextAction`. Anything referencing the old shape (there was nothing outside tests) needed updating.

Tests: the three SETS suites in `Program.cs` (`TestSetsUrbanCaptureStateMachine`, `TestSetsUrbanCaptureLedger`, `TestSetsUrbanCaptureCompletionPlan`) were rewritten around the S-01…S-09 scenarios by name (search the test file for the `S-0x:` comments). ~95 assertions, all passing.

Commits:
- GCCZ `cce9899` — core rewrite + rewritten tests
- NEW-10 `224b53ab` — byte-identical mirror (SHA256-verified per file at commit time)

### 5. Shadow-mode wiring in the fused runtime (handoff §20 actions 7–9, narrowed)

Per the user's scoping directive, this did **not** build `AfMissionScope`/`AfSceneParticipantRegistry`. Instead, `SettlementEntryTroopSelectionMissionLogic` (`NEW-10\SettlementEntryTroopSelectionBehavior.cs`) now carries one `_shadowCaptureSession` field:

- `CreateShadowCaptureSession(entry)` (called from the constructor) returns **null** unless `_defenderConflictEnabled` and the scene is Town or Castle — owned settlements, villages, and unsupported scenes are structurally untouched. On success it calls `SetsUrbanCaptureContext.TryCreateHostile(...)` (operation id = `settlementId@hoursSinceCampaignStart-TickCount`) then applies `PrepareEntry` + `StartMission`.
- `ShadowApply(event, legacyAllowed, site)` — a small helper that applies an event to the shadow session and logs `SETS shadow DIVERGENCE at {site}: legacy={x}, shadow={y}, {session.DescribeForLog()}` whenever the shadow's `TryApply` result disagrees with the legacy boolean decision at that call site. Wired at:
  - `StartConflict` (right after `_conflictActive = true`)
  - `ReachVictory` (right after `_victoryReached = true`; also commits `Ledger.TryCommitVictory()`)
  - `OnEndMission` (first line)
- `CompareShadowExitBlock(legacyBlocked)` — wired into `OnEndMissionRequest`; computes `SetsUrbanCapturePolicy.ShouldBlockExit(session.State, liveEnemies, reserveExhausted)` and logs a divergence line if it disagrees with the legacy TAB-block boolean.

**Every shadow call is wrapped in try/catch and never returns a value the legacy path consults.** If the shadow session throws, is null, or disagrees, the legacy boolean flow is what the player experiences — unchanged. This is the actual mechanism behind "SETS 完全不影响其他功能": the shadow code is additive-only, and the four wiring points are the only places it was touched.

Build-time bug found and fixed: `Campaign.Current.CampaignStartTime` does not exist in this Bannerlord API surface (`CS1061`). Replaced with `CampaignTime.Now.ToHours` for the operation-id timestamp component — cosmetic, only affects the log-friendly id string, not any decision.

Commit: NEW-10 `5cbb440f`.

### 6. Verifier kept in sync

The TAB-exit-block assertion in `verify_gccz_town_refactor.ps1` (added in the original Slice A commit) pinned the literal legacy `if (...)` condition text. Wiring the shadow compare required hoisting that condition into a local (`bool legacyBlocked = ...`) so it could be reused in both the legacy branch and the shadow comparison call. Updated the verifier's string match to the hoisted form — same boolean expression, same semantics, just no longer inline in an `if`.

Commit: GCCZ `7415c9f`.

## Commits This Session, In Order

| # | Repo | Commit | Content |
|---|---|---|---|
| 1 | GCCZ | `aa74301` | Protect pre-existing voice-session standalone work |
| 2 | GCCZ | `cce9899` | SETS core rewrite closing S-01–S-06, S-09 + rewritten tests |
| 3 | NEW-10 | `993d414d` | Protect pre-existing voice-session runtime work |
| 4 | NEW-10 | `224b53ab` | Mirror of commit 2 (SHA256-verified) |
| 5 | NEW-10 | `5cbb440f` | Shadow-mode wiring (additive only) |
| 6 | GCCZ | `7415c9f` | Verifier follows the exit-block condition's shadow-compare hoist |

Two unrelated commits from other sessions landed interleaved in NEW-10's history during this work (`9da59e03` add + `b6faeba0`/`f1acdd83`/`8b104416` revert-and-scaffold-and-revert-again of an "expedition parade" experiment) and one in GCCZ (`f355939`, native settlement population constants). None touch SETS files; verified by re-checking SHA256 of all 7 core files across repos after the fact — still byte-identical.

## Verified State As Of This Handoff

- Both repos' working trees are clean (`git status --short` empty in both).
- GCCZ standalone tests: `dotnet run --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\...csproj` → all pass.
- Verifier: `powershell -File G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1` → pass, including the SETS contract section.
- NEW-10 unified build (Bootstrap + 1.3 + 1.4, stage-only, game directory untouched): pass. Recipe below.
- 7 SETS core files: SHA256-identical between `GCCZ\src\AnimusForge.SiegeAftermathIntervention\` and `NEW-10\AnimusForge.SiegeAftermathIntervention\`.
- **Not yet done**: an actual in-game run. The shadow log has never been observed against a live mission. This is the very next required step before any legacy-boolean deletion.

## Build Recipe (unchanged from 2026-08-26, repeated for convenience)

Game is at `E:\Steam\steamapps\common\Mount & Blade II Bannerlord` (v1.4.7), workshop content at `E:\Steam\steamapps\workshop\content\261550`. Network stays restricted (github.com/nuget.org unreachable).

```powershell
cd G:\AFMOD\NEW-10
powershell -NoProfile -ExecutionPolicy Bypass -Command "
  $env:PATH = 'G:\AFMOD\.dotnet-sdk;' + $env:PATH;
  $env:DOTNET_CLI_HOME = 'G:\AFMOD\.dotnet-home';
  $env:NUGET_PACKAGES = 'C:\Users\28358\.nuget\packages';
  & './一键编译覆盖推送/build_single_module.ps1' -ProjectRoot . `
    -BannerlordRoot 'E:\Steam\steamapps\common\Mount & Blade II Bannerlord' `
    -WorkshopContentDir 'E:\Steam\steamapps\workshop\content\261550' `
    -Configuration Debug -Stage"
```

Quick 1.4-only compile check (faster than the full unified script when iterating):
```
dotnet msbuild AnimusForge.csproj -p:Configuration=Debug -p:BannerlordApi=1.4 -p:"BannerlordRoot=E:\Steam\steamapps\common\Mount & Blade II Bannerlord" -p:"WorkshopContentDir=E:\Steam\steamapps\workshop\content\261550" -p:BaseIntermediateOutputPath=obj/codex_gccz_14/
```

Standalone tests: `G:\AFMOD\.dotnet-sdk\dotnet.exe run --project G:\AFMOD\GCCZ\tests\AnimusForge.SiegeAftermathIntervention.Tests\...csproj` with `DOTNET_CLI_HOME=G:\AFMOD\.dotnet-home`.

Verifier: `powershell -NoProfile -ExecutionPolicy Bypass -File G:\AFMOD\GCCZ\tools\verify_gccz_town_refactor.ps1`.

In Git Bash: always write `-p:X`, never `/p:X` (path-mangled). Use `git -C <repo>` rather than `cd repo && git ...` compounds.

## Next Steps, In Order

1. **In-game shadow run.** Enter a hostile town or castle with a handful of followers configured, start the conflict, defeat the objective, exit to the map, enter GCCZ, and check `SETS.log` for any `SETS shadow DIVERGENCE` line. Do this at least twice (town + castle) and once with zero followers configured (should stay in `MissionActive` shadow-side, matching the legacy no-conflict quiet exit).
2. **If clean**, switch the four wired decisions to read the shadow session's result instead of just comparing against it, and delete the now-redundant legacy booleans (`_conflictActive`'s decision-making role, not necessarily the field itself if other code still reads it — check call sites first). Re-run tests, verifier, unified build after every deletion, not at the end.
3. **If divergent**, the log line names the site, both booleans, and the full session state — fix the state machine or the wiring point, not the legacy code (the legacy code is the known-correct baseline being replaced).
4. **Ownership commit and native menu bridging** (handoff §8.5–8.7, S-07/S-08) still needs the actual runtime wiring: today `TryOpenSettlementEntryVictoryMenu` and `ApplySetsSettlementEntryCaptureIfNeeded` in `SiegeAiInterventionBehavior.cs` are untouched by this session's work. The completion planner exists and is tested standalone, but nothing calls `ResolveNextAction`/`RecordRetryableFailure` from the pump chain yet (`TryPumpPendingSettlementTakenMenu`, L1794). That wiring is the next real behavior change, gated on step 1's clean shadow run.
5. **Noble escort integration (handoff §9) is still fully unstarted.** Per the user's directive this session, do not begin it unless explicitly asked — SETS's own core correctness was the requested scope.
6. Slices C (mission-logic extraction), E (reflection boundary adapter), F (resource/cleanup) from the 2026-08-25/26 handoffs remain queued behind the shadow-run gate above.

## Explicitly Out of Scope This Session (per user directive)

- `AfMissionScope`, `AfSceneParticipantRegistry`, `AfSpawnLease`, `AfParticipantActionLease` (handoff §7) — not built.
- Noble captive escort work (handoff §9) — not touched.
- Harmony dispatcher unification (handoff §11), shared dialogue/command UI (handoff §12) — not touched.
- Any actual behavior switch-over — the session is shadow-only; legacy code still drives 100% of live behavior.

---

# 2026-08-28 Addendum C — 玩家反馈驱动的 SETS / 城内冲突运行时修复

本节记录 `703ab8f`（GCCZ）与 `88c9f3f1`（NEW-10）实际落地的代码。它修复的是 **SETS 随行士兵与 AF 城内冲突之间的运行时接线**；它没有把上一节的 hostile capture 状态机从 shadow 切成 authority，也没有开始俘虏贵族随行重构。三件事必须继续分开验收。

## 1. 玩家反馈与已确认根因

| 玩家现象 | 实际运行路径 | 已确认根因 |
|---|---|---|
| 随行士兵空手、不拔武器 | `SceneTauntMissionBehavior.EscalateToArmedConflict`、SETS `ReadyPlayerEntryFollowersForConflict` | SETS 只切队伍、编队、警戒，没有保证恢复被徒手冲突缓存的装备并实际持握；fallback sword 只认 `Guard/PrisonGuard/Soldier` occupation，普通 SETS 选中兵种可能永远拿不到 fallback。 |
| 挑衅后自己的 SETS 随从也变敌人 | `CollectEscortedFollowers` → `CollectOpponentSideAgents` → `StartCustomFight` | 目标护卫探测没有排除 SETS 选中随从；同一 Agent 已在 player list 后仍可能再次进入 opponent list。 |
| 同一士兵可能同时处于双方集合 | `AddAgentToFightSide` | 旧方法只向目标 `HashSet` 添加，从不从相反集合移除，也不拒绝把 player-side Agent 再加到 opponent side。 |
| 自己地盘挑衅后全城都打玩家 | `EscalateToArmedConflict`、`TryJoinArmedBystanderToConflict` | 旧逻辑把 `_guardAgentIndices` 全部无条件加到 opponent side，并把附近所有持械旁观者继续吸入 opponent side。 |
| 直接打士兵，士兵只抱头/投降，不反击 | SETS `OnAgentHit` 先于 SceneTaunt 决策，随后 `StartOwnedSettlementIncident` → `MaintainOwnedSettlementIncidentPanic` | SETS 抢先吞掉自有地盘所有物理攻击；SceneTaunt 的 passive target 又没有排除 `Guard/PrisonGuard/Soldier/Lord`，因此战斗人员也进入 hands-up/panic 路径。 |
| 国王进入封臣领地时本国人仍把玩家当外敌 | SETS `IsOwnEntrySettlement` 与 SceneTaunt `OwnerClan == PlayerClan` | 两个系统的“自己地盘”定义不一致。SETS 认“玩家直辖 + 玩家是国王时的本国封臣领地”，SceneTaunt 只认玩家直辖。 |

这些都来自当前 live caller，而不是注释或旧文档推测。

## 2. 新增纯策略（双仓库镜像）

新增：

- `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention\SetsCityConflictPolicy.cs`
- `G:\AFMOD\NEW-10\AnimusForge.SiegeAftermathIntervention\SetsCityConflictPolicy.cs`
- SHA256：`BF381FDA66F68719C8BC3B5214F4541C19F50FB022A8D204C9A2A37E641C9369`

策略只接受布尔事实，不引用 Bannerlord 类型：

1. `ResolveSide(...)` 每次只返回一个 `SetsCityConflictSide`。
2. SETS selected follower 的优先级最高，任何 escort detector 都不能把它偷到 opponent side。
3. 当前冲突对象与其真实护卫进入 opponent side。
4. 武装冲突中，玩家统治权定居点的其他守卫/士兵进入 player side；外部定居点守卫仍进入 opponent side。
5. `ResolveOwnedAttackRouting(...)` 将自有/统治权地盘的直接攻击分为：
   - 普通居民：`PassiveSurrender`；
   - `Guard/PrisonGuard/Soldier/Lord`：`ArmedConflict`；
   - gangster/bandit/alley：`ExistingFlow`，继续原生犯罪冲突链。
6. 自己地盘存在 SETS 选中随从时，语言冲突直接升级为 armed support，避免士兵被当作徒手斗殴者剥掉武器。

## 3. NEW-10 运行时接线

### 3.1 阵营唯一性

`SceneTauntBehavior.cs` 现在有三层防线：

- `IsPlayerAlignedConflictAgent` 统一识别 MainAgent、SETS 选中随从、玩家保护目标、主队英雄和原版 accompanying follower。
- `CollectEscortedFollowers` 在运行 escort detector 前排除上述玩家侧成员，并使用 `SetsCityConflictPolicy.ResolveSide` 再确认。
- `NormalizeInitialConflictSides` 在 `StartCustomFight` 前删除双方交集；如果 active target 不再存在于 opponent side，则拒绝开启含糊冲突。
- `AddAgentToFightSide` 不再盲加：
  - SETS follower 的 opponent 请求会被重定向到 player side；
  - player-side Agent 不允许再转入 opponent side；
  - 若从 opponent side 转入 player side，必须先从 `MissionFightHandler` opponent 列表真实移除；失败则拒绝继续，避免 HashSet 与引擎内部列表分裂。

### 3.2 自己地盘守卫归属

“玩家统治权定居点”现在统一复用 SETS 的 `IsOwnEntrySettlement` 定义，通过 `IsPlayerAuthoritySettlementForExternal` 暴露给 SceneTaunt：

- 玩家 Clan 直辖城：是。
- 玩家为 Kingdom ruler 时，本国其他 Clan 封地：是。
- 普通封臣进入同国其他 Clan 封地：否，保持既有犯罪/敌对规则。

武装升级时：

- active target 始终是 opponent，即使 target 本身是本地士兵；
- target 的真实私人护卫是 opponent；
- 其他本地 `Guard/PrisonGuard/Soldier` 是 player side；
- 普通居民保持 fight 外部，不再因为“附近且持械”被全局吸入敌方；
- hostile settlement 的守卫归属完全保持原逻辑，仍是 opponent。

跨 location 的 armed carryover 也按同一规则处理：玩家统治权地盘不会在换场景后把全体本地权威重新拉成敌军。

### 3.3 直接攻击分流

SETS `ShouldHandlePhysicalAttack` 不再吞掉全部自有地盘攻击：

| 目标 | 现在由谁处理 | 预期行为 |
|---|---|---|
| 普通居民/普通 notable | SETS owned incident / SceneTaunt passive | hands-up、逃跑、后续 SETS/GCCZ 自有地盘处置保持不变。 |
| Guard / PrisonGuard / Soldier / Lord | SceneTaunt custom fight | 被打目标真实反击；SETS 随从与其他本地守卫保护玩家；使用真实伤害/武装冲突链。 |
| Gangster / Bandit / Alley member | 原生 criminal/alley flow | 不被 SETS passive 或新阵营策略劫持。 |
| SETS follower / 玩家保护成员 | 不可作为 opponent target | 保留友伤保护与玩家侧身份。 |

### 3.4 随从武器保证

武装准备现在按以下顺序执行：

1. 从 `_cachedUnarmedConflictEquipment` 恢复武器。
2. 清除敌我缓存、目标缓存和 AI weapon selection。
3. `WieldInitialWeapons`。
4. 如果仍未持握，逐槽寻找第一件真实武器并强制持握。
5. 如果压根没有真实武器，SETS selected follower 也允许领取 fallback sword（不再受 occupation 限制）。
6. armed conflict 期间每 0.5 秒对 player-side SETS follower 重试一次；SETS hostile defender conflict 的 1 秒维护 tick 与 owned massacre tick 也会重新保证武装状态。

该维护只对 `selected follower + player side + armed conflict` 生效，不会在和平逛城时强制拔刀，也不会持续干预玩家本人手动收刀。

## 4. 明确保留的旧行为

- 普通自有地盘居民被直接攻击时仍可投降/逃跑；本次只把真正战斗人员从 passive path 分出去。
- 外部/敌对定居点的守卫仍会在持械冲突中攻击玩家。
- Alley/gangster/bandit 继续走原生犯罪冲突。
- hostile SETS capture 的新 9-state core 仍是 shadow-only；`_conflictActive/_victoryReached/PendingSettlementVictoryMenuEntry` 仍是 live authority。
- Noble captive escort 尚未开始；本次没有伪装成“SETS + noble 已完全融入 AF”。

## 5. 测试、Verifier 与构建

新增 `TestSetsCityConflictPolicy`，覆盖：

- selected follower 即使被 escort detector 命中也必须是 player side；
- 被直接攻击的本地士兵必须是 opponent；
- 其他本地守卫必须保护玩家；
- 外部守卫必须敌对；
- 普通居民不进入全局 armed fight；
- authority direct hit → armed；普通居民 → passive；criminal → existing native；
- selected follower 只在 player-side armed conflict 获得武装维护；
- 自己地盘带 SETS follower 的语言冲突升级为 armed support，外部地盘不改写既有升级规则。

Verifier 新增纯策略存在性、镜像、运行时桥、双边互斥、随从武装、统治权判定和旁观者不全局敌对检查。

本节落地后已执行：

- GCCZ standalone tests：全部通过。
- `verify_gccz_town_refactor.ps1`：通过；core source files = `182`。
- NEW-10 Bannerlord API 1.3：0 warning / 0 error。
- NEW-10 Bannerlord API 1.4：0 warning / 0 error。
- Bootstrap：0 warning / 0 error。
- unified stage：成功，输出 `G:\AFMOD\NEW-10\bin\Debug\single_module_stage\AnimusForge`。
- Stage mode 明确未修改游戏目录。
- `git diff --check`：通过；无 conflict marker。
- 新纯策略双仓库 SHA256：一致。

**仍未验证：真实游戏任务。** 编译、纯测试和字符串 verifier 不能证明 Bannerlord AI 最终会实际拔刀、寻敌、挥砍，也不能证明具体第三方 scene template 没有额外 Team/AI override。

## 6. 必须执行的实机矩阵

每项都从新进场景开始，不要复用上一场冲突残留：

1. **玩家直辖城，直接空手打本地士兵**
   - 被打士兵应拔刀/反击；
   - SETS 随从应拔武器并站 player side；
   - 其他本地守卫应保护玩家；
   - 普通居民不应全部变红名敌军。
2. **玩家直辖城，语言挑衅普通 NPC，带 3–5 名 SETS 随从**
   - conflict 应升级为 armed support；
   - selected followers 不得空手；
   - active target 仍是 opponent，不得被本地归属规则吞掉。
3. **玩家为国王，进入本国封臣领地重复 1/2**
   - 结果应与玩家直辖城一致；这是本次“本族人也打我”口径修复的关键用例。
4. **普通封臣进入同国另一封臣领地**
   - 不应获得国王统治权保护；维持既有守卫敌对/犯罪后果。
5. **外部/敌对城镇回归测试**
   - 守卫仍敌对；SETS hostile capture 波次、TAB 阻断、胜利条件不得改变。
6. **Alley gangster/bandit**
   - 必须继续进入 native criminal/alley fight，不能进入 owned passive 或本地守卫保护分支。
7. **普通居民直接受击**
   - 仍可 hands-up/逃跑并产生 owned incident；不得因本次修复全部改成战士。
8. **武器恢复压力测试**
   - 先进入徒手冲突，再触发武装升级；确认 SETS follower 原装备恢复；
   - 用一个原始装备无可用武器的普通兵测试 fallback sword；
   - 查看 `SceneTaunt`/SETS 日志是否出现 side reassignment rejection、weapon readiness exception 或 shadow divergence。

## 7. 提交与回滚

- GCCZ code commit：`703ab8f` — `fix: define SETS city conflict allegiance policy`
- NEW-10 runtime commit：`88c9f3f1` — `fix: arm SETS followers and preserve player-side guards`
- 双仓库回滚标签：`backup/pre-sets-city-conflict-runtime-fix-20260828`
  - GCCZ tag target：`354afe3c010c4ed1b1b376801790e6e09d409fee`
  - NEW-10 tag target：`c4c3328d910faf3c6faad4b79a260966035368cc`

安全回滚优先 `git revert 703ab8f` / `git revert 88c9f3f1`，不要 hard reset。NEW-10 当前 `ahead 10, behind 17`；未为本次修复 pull/rebase，也不要在未审计远端 17 个提交前直接合并。

## 8. 下一阶段边界

1. 先完成上面的实机矩阵并保存日志；失败时按“武器恢复 → Agent 双边 → Team 敌对关系 → MissionFightHandler 内部列表 → AI target”顺序回查，不要先扩大重构面。
2. 实机通过后，才开始 hostile capture Slice B：让 9-state session 从 shadow comparison 逐点接管 conflict/victory/exit/ownership/menu decision，并在每删一个 legacy decision 后单独构建和实测。
3. Noble captive escort 应在 Slice B 稳定后进入共享 participant registry/lease 方案；不要把贵族俘虏塞入 SETS 的 selected follower HashSet，也不要复用本地守卫 allegiance 规则冒充 noble escort 归属。
4. 当前 C: 仅约 `2.82 GB` 空闲。继续全量构建或并行工具前需要关注磁盘，避免再次出现 `No space left on device`。
