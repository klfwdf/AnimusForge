# AnimusForge 决斗标签未输出与遭遇放行未执行离开缺陷修复交接文档（2026-09-07）

**日期**：2026-09-07  
**交接对象**：下一位 AnimusForge 开发者 / Agent  
**当前状态**：完成实机测试中关于“决斗同意但未输出标签”与“敌人追击放行未执行离开”两处重大交互缺陷的根因排查、代码修复、全量编译构建验证与游戏目录覆盖部署（Modules/AnimusForge）。

---

## 一、当前 Git 与环境基线

- **工作区路径**：`F:\AnimusForge-main`
- **当前 Git 分支**：`refactor/prepare-af-restructure`
- **上一个 HEAD 提交**：`8e8031b7` (`docs(handoff): record stage 8 cutover and next-agent handoff`)
- **未跟踪文件严格保护**：`AnimusForge/GUI/SpriteParts/af_courier/*.png`（共 21 个卷轴与火漆素材图片），严禁暂存、提交或清理，必须保持未跟踪状态。
- **本地修改文件清单（已验证）**：
  1. `AnimusForge/ModuleData/ActionPostprocessPrompts.json`
  2. `AnimusForge/ModuleData/RuleBehaviorPrompts.json`
  3. `DuelBehavior.cs`
  4. `DuelSettings.cs`
  5. `LordEncounterBehavior.cs`
  6. `ShoutBehavior.cs`
- **构建环境**：.NET 6 / .NET 10，双版本（1.3 / 1.4 / Bootstrap）构建 0 警告 0 错误。

---

## 二、本轮已解决的核心问题（Done）

### 1. 决斗 NPC 正文同意但未输出 `[ACTION:DUEL]` 标签

#### 根因分析
1. **前处理词汇硬匹配漏词**：原 `RuleBehaviorPrompts.json` 中 `Duel.AcceptKeywords` 仅有 10 条带标点符号的全句（如 `"我要和你决斗。"`、`"敢不敢和我单挑？"`）。玩家在输入框输入不带标点的常用短语（如 `"我要和你决斗"`、`"来决斗"`、`"单挑吧"`）时，`IndexOf` 词汇检测直接判负。当语义向量未命中时，前处理完全漏判 Duel。
2. **家族等级门禁（`player_not_qualified`）硬杀后处理规则表注入**：`ShoutBehavior.cs` 的 `CanInjectDuelPostprocessRule` 中硬编码了 `if (!ctx.IsQualified) { reason = "player_not_qualified"; return false; }`，而 `DuelSettings.MinimumClanTier` 默认值为 2。因此新开档或 0~1 级家族玩家即便触发决斗话题，后处理的 `{tag_rules}` 表中也会被彻底剥离 `[ACTION:DUEL]`，导致 LLM 遵循“禁止输出表外标签”原则，绝不可能输出决斗标签。
3. **后处理系统提示词缺少“决斗成立”词汇引导**：`ActionPostprocessPrompts.json` 的规则 B 与规则 7 中只写了交易/效忠/割让等口径，未涵盖“拔剑/接受决斗/奉陪到底/来战”等动作成立词汇，导致部分谨慎的模型将 NPC 的拔剑答应误判为非确定性动作。

#### 落实修复
1. **扩充触发词库**（`AnimusForge/ModuleData/RuleBehaviorPrompts.json`）：在 `Duel.AcceptKeywords` 中扩充了包括 `"我要和你决斗"`, `"决斗"`, `"单挑"`, `"来决斗"`, `"跟我决斗"`, `"拔剑吧"`, `"拔剑"`, `"接招"`, `"切磋"`, `"比试"`, `"比一场"`, `"一决胜负"`, `"一对一"`, `"手底下见真章"` 等不带标点的常用口语短语和词根；
2. **解除后处理标签注入锁**（`ShoutBehavior.cs`）：在 `CanInjectDuelPostprocessRule` 中移除了 `!ctx.IsQualified` 与 `!ctx.UseDuelContext` 的强杀限制，确保只要对话涉及决斗，后处理规则表中必定有 `[ACTION:DUEL]` 可供输出；
3. **调整默认家族等级门禁与 UI 反馈**：
   - 将 `DuelSettings.cs` 中的 `MinimumClanTier` 默认值调整为 `0`，初期玩家与流浪者、同伴、领主切磋不再受等级限制阻断；
   - 在 `DuelBehavior.cs` 的 `StartDuelViaAI` 中增加提示：若玩家通过 MCM 自定义提高了等级门槛导致决斗无法开始，会直接在屏幕弹出红色通知，不再无声沉默；
4. **系统提示词对齐**（`AnimusForge/ModuleData/ActionPostprocessPrompts.json`）：在 SystemPrompt 的规则 B 和规则 7 中显式增补了“接受决斗/接受挑战/来战/拔剑/奉陪到底/一决高下/成全你”，并更新了 `RuleBehaviorPrompts.json` 中 `[ACTION:DUEL]` 的标签触发描述。

---

### 2. 敌人追击放行未执行离开指令

#### 根因分析
1. **谈判资格公式死锁**：`LordEncounterBehavior.cs:6853` 原判定为 `negotiable = averageRelation > kingRelation;`。在大地图截停交战中，玩家与敌方领主、敌国王的关系通常均 `<= 0`（如 -10 或 0），导致 `negotiable` 恒为 `false`。这使得后处理的 `BuildMeetingPlayerReleasePostprocessRulesForExternal` 返回空列表，`[ACTION:LET_PLAYER_GO]` 被完全剔除；主提示词甚至告诉 AI “你绝不可以放玩家走，必须在末尾输出[ACTION:MEETING_TAUNT_BATTLE]”；即使模型强行输出了放行标签，C# 端的 `TryConsumeMeetingPlayerReleaseTag` 与 `ScheduleNativeConversationMeetingPlayerRelease` 也因为 `!negotiable` 而直接拒绝处理。
2. **未授权逃逸判定误杀手动退出**：调度放行后未第一时间标记授权。如果玩家在 10 秒缓冲期内主动点击了“离开/我得走了”，原生任务销毁时 `flag19`（放行授权）为 `false`，命中未授权逃跑分支（`flag20`），直接将 `PlayerEncounter.LeaveEncounter = false;` 强行覆盖，使玩家掉入原生战斗/俘获菜单。
3. **10 秒倒计时结束后对话死锁不退出**：`LordEncounterBehavior.cs:7316` 的 `TryForcePendingNativeConversationMeetingReleaseIfReady` 检测到 `ActiveState is MissionState` 时直接每帧 `return;`，从未主动调用 `EndConversation()`，导致 10 秒后对话永远不会自动关闭。

#### 落实修复
1. **开放谈判放行机制**（`LordEncounterBehavior.cs`）：将 `negotiable` 设置为 `true`，并更新提示词，告知 AI：若接受了玩家的巨额赎金、赔偿或承诺放行，必须在回复末尾输出标签 `[ACTION:LET_PLAYER_GO]`；若拒绝放行执意攻击，则输出 `[ACTION:MEETING_TAUNT_BATTLE]`；
2. **提前授予离开通行证**（`LordEncounterBehavior.cs`）：在 `ScheduleNativeConversationMeetingPlayerRelease` 入口处立即调用 `AuthorizeMeetingPlayerRelease`，无论玩家是等待 10 秒自动退出，还是提前主动点击离开，都会被系统识别为合法放行；
3. **10 秒主动关闭对话与早退即时响应**（`LordEncounterBehavior.cs`）：
   - 10 秒倒计时结束时，若对话仍活跃，主动调用 `Campaign.Current?.ConversationManager?.EndConversation()` 与 `EndMission()`；
   - 若玩家提前关闭对话回到大地图，不再等待 10 秒，即时下发安全通行（5天内敌军不主动攻击）并执行 `PlayerEncounter.LeaveEncounter = true` 安全卸载遭遇。
4. **决斗野外遭遇解绑强化**（`DuelBehavior.cs`）：在 `TryFinishPlayerEncounterForWildernessDuelOpening` 中，在 `Finish` 前补齐 `PlayerEncounter.CampaignBattleResult = null; PlayerEncounter.LeaveEncounter = true; PlayerEncounter.Update();`，并在 `GlobalSourceMissionLeaveTick` 中补齐了 10 秒主动关闭对话与提前退出唤醒逻辑。

---

## 三、验证与部署结果

1. **单模块编译验证**：
   - 1.3.x 实现编译：**0 警告，0 错误**
   - 1.4.x 实现编译：**0 警告，0 错误**
   - Bootstrap 编译：**0 警告，0 错误**
2. **契约测试全量通过**：
   - `python tools/BridgeBindingContractTests/test_validate_bridge_bindings.py`：20 项测试全部通过（Ran 20 tests in 3.807s -> OK）。
3. **部署到游戏（Deploy）**：
   - 已成功覆盖部署至 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`，游戏内文件已处于最新状态。

---

## 四、下一步待办事项清单（Backlog for Next Agent）

### 优先级 P1：FirstChance 异常治理（治理后台日志刷屏）
- **现象**：后台日志中存在高频的 `CampaignTime.Now` / `PlayerClan` 空指针异常（NPE），以及 `CaptureUnifiedNpcPolicyHistorySnapshot` 异常，刷屏达 4.8 万行。
- **目标**：在对话初始化与 Tick 热路径中补齐空对象防护，降低后台异常抛出与垃圾回收开销。

### 优先级 P2：实机游玩双功能复测（User Acceptance）
- **决斗**：测试在各种家族等级、不同场景下，向 NPC 发起决斗是否均能正确输出 `[ACTION:DUEL]` 并在 10 秒倒计时结束（或手动退出时）顺利进入决斗；
- **遭遇放行**：测试在大地图被敌方领主截停交战时，交付巨额赎金后领主答应放行，是否能正确触发 `[ACTION:LET_PLAYER_GO]` 并安全脱离遭遇回到大地图。

### 优先级 P3：提交与推送（Push）
- 当用户明确授权允许 `git push` 时，将本地提交推送到远端 `origin/refactor/prepare-af-restructure`。
- **注意**：继续保持未跟踪的 21 个 `af_courier` PNG 素材隔离，绝不可带入提交。

### 优先级 P4：阶段 8 死代码清理与阶段 9 发布打包
- 根据 `docs/phase8/cleanup-candidates.json` 清理废弃的临时桥接适配器；
- 运行 `一键打包AnimusForge.bat` 打包发布单一模块安装包。
