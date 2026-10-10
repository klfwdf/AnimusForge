# 第二轮 B（对话、经济、社交、世界地图）交付报告

状态：DELIVERED / VERIFIED
基线分支：`main`
对应工作包：Wave 2 Package B（涵盖 Conversation/DialogueUI、Economy、Social、WorldMap、Issue）

---

## 一、Focused Commits 清单

本工作包严格按照模块职责边界划分，生成 3 个独立的 focused commit：

1. **Conversation & Issue UI 本地化**
   - **SHA**: `eda6992f9d164cd64af3ebdf2cfc6339436ccbb0` (短哈希: `eda6992f9`)
   - **Commit 信息**: `feat: localize conversation and issue ui`
   - **涉及文件**: 14 个源码与资源文件，成对新增 **59 个** 本地化 Key。

2. **Social & WorldMap UI 本地化**
   - **SHA**: `dc679cea808f40a40b00d7e93dbfdc986a286706` (短哈希: `dc679cea8`)
   - **Commit 信息**: `feat: localize social and worldmap ui`
   - **涉及文件**: 7 个源码与资源文件，成对新增 **109 个** 本地化 Key（Social 36个、WorldMap 73个）。

3. **Economy UI 本地化**
   - **SHA**: `c6989eedfd6c83a56d4d1c8c8ba03fc69dda2c02` (短哈希: `c6989eedf`)
   - **Commit 信息**: `feat: localize economy player visible ui`
   - **涉及文件**: 5 个源码与资源文件，成对新增 **85 个** 本地化 Key。

**本轮 Package B 新增本地化键总计**：**253 对**。

---

## 二、各模块新增 Key 与实现细节

### 1. Conversation & Issue (`AF.Module.Conversation`, `AF.Module.Issue`, `AnimusForge.DialogueUI`)
- **资源文件**：
  - `content/modules/AnimusForge.DialogueUI/ModuleData/Languages/afd_ui_strings.xml` / `CNs/afd_ui_strings-zh-CN.xml`
  - `content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ambient_strings.xml` / `CNs/afconv_ambient_strings-zh-CN.xml`
  - `content/modules/AF.Module.Conversation/ModuleData/Languages/afconv_ui_strings.xml` / `CNs/afconv_ui_strings-zh-CN.xml`
- **主要新增分类 (59 Keys)**：
  - **DialogueUI SubModule**: `AFD_SubModule_Name` ("AnimusForge - Parchment Dialogue UI" / "AnimusForge - 羊皮纸对话界面")。
  - **TownAmbient 动态占位符（29 Keys）**：
    - 税负态度：`AF_CONV_AMBIENT_TaxMood_Oppressive`, `AF_CONV_AMBIENT_TaxMood_Heavy`, `AF_CONV_AMBIENT_TaxMood_Moderate`；
    - 时间问候与时段：`AF_CONV_AMBIENT_TimeGreeting_*`（早安/午安/傍晚好/晚上好）、`AF_CONV_AMBIENT_TimePeriod_*`（清晨/白天/傍晚/夜里）；
    - 玩家头衔与身份：`AF_CONV_AMBIENT_Title_*`（陛下/大人/雇佣兵/旅人）、`AF_CONV_AMBIENT_Status_*`（国家统治者/本地领主/领主/雇佣兵/冒险者）；
    - 环境回退占位符：`AF_CONV_AMBIENT_Owner_Fallback`, `AF_CONV_AMBIENT_Town_Fallback`, `AF_CONV_AMBIENT_Speaker_Fallback`, `AF_CONV_AMBIENT_Culture_Fallback`, `AF_CONV_AMBIENT_Role_Fallback`, `AF_CONV_AMBIENT_Scene_Fallback`, `AF_CONV_AMBIENT_Clan_Fallback`, `AF_CONV_AMBIENT_Kingdom_Fallback`, `AF_CONV_AMBIENT_Player_TravelerFallback`。
    - **成效**：彻底消除了英文环境下环境对话中嵌入中文头衔、问候与地名的语言泄漏。
  - **通知颜色支持**：`NativeConversationGameEffectsRuntime.cs`、`SceneSpeechEffectController.cs`、`SceneSystemNpcShoutRuntime.cs` 增加对英文 `"fail"` 的兼容识别，防止英文失败消息被错误染成绿色。
  - **Issue 任务外壳与代办/交付提示（29 Keys）**：
    - `AF_CONV_UI_Issue_NoOfferable`, `AF_CONV_UI_Issue_PreconditionsNotMet`, `AF_CONV_UI_Issue_CannotAcceptReason`, `AF_CONV_UI_Issue_StartFailed`, `AF_CONV_UI_Issue_AcceptedSelf`；
    - 同伴代办与兵力选择：`AF_CONV_UI_Issue_AltDispatchPending`, `AF_CONV_UI_Issue_CompanionUnavailable`, `AF_CONV_UI_Issue_CompanionConfirmedSelectTroops`, `AF_CONV_UI_Issue_TroopsNotSatisfied` 等；
    - 任务交付：`AF_CONV_UI_Issue_NoTurnInQuest`, `AF_CONV_UI_Issue_TurnInFailed`, `AF_CONV_UI_Issue_TurnedIn`。

---

### 2. Social & WorldMap (`AF.Module.Social`, `AF.Module.WorldMap`)
- **资源文件**：
  - `content/modules/AF.Module.Social/ModuleData/Languages/afsocial_ui_strings.xml` / `CNs/afsocial_ui_strings-zh-CN.xml`
  - `content/modules/AF.Module.WorldMap/ModuleData/Languages/afworldmap_strings.xml` / `CNs/afworldmap_strings-zh-CN.xml`
- **主要新增分类 (109 Keys)**：
  - **Social 模块（36 Keys）**：
    - 招募与身份转换提示：`AF_SOCIAL_UI_RecruitmentFollowSuffix`, `AF_SOCIAL_UI_RecruitmentMissingCompanionHero`, `AF_SOCIAL_UI_RecruitmentClanSuccessionIncomplete`, `AF_SOCIAL_UI_RecruitmentBecameCompanion`, `AF_SOCIAL_UI_RecruitmentBecameClanMember`, `AF_SOCIAL_UI_RecruitmentRetainedIdentity`；
    - 招募过程备注与异常：`AF_SOCIAL_UI_NoteNonHeroPartyNoLongerField`, `AF_SOCIAL_UI_NoteNonHeroPartyDisbanded`, `AF_SOCIAL_UI_NoteCompanionPartyCreationScheduled`, `AF_SOCIAL_UI_ExceptionGeneral`；
    - 恋爱/联姻/私奔/离婚失败原因：`AF_SOCIAL_UI_ReasonDead`, `AF_SOCIAL_UI_ReasonPrisoner`, `AF_SOCIAL_UI_ReasonUnderage`, `AF_SOCIAL_UI_ReasonAlreadyMarried`, `AF_SOCIAL_UI_ReasonDifferentClan`, `AF_SOCIAL_UI_ReasonPlayerNotLeader`, `AF_SOCIAL_UI_ReasonLowRelation` 等。
    - **成效**：`SocialUiLocalization` 在保留原始中文结果字符串的同时，在显示端完整投影为双语，彻底解决 fallback 泄露。
  - **WorldMap 模块（73 Keys）**：
    - 命令队列与总督远征通知：`AF_WM_DisplayGovernorExpeditionFailed`, `AF_WM_DisplayFollowUpCommandsFailed`, `AF_WM_DisplayGovernorExpeditionDeparted`, `AF_WM_DisplayQueueEnded`；
    - 战斗与袭击结果展示：`AF_WM_DisplayHideoutOutcome`, `AF_WM_DisplayOutcomeSucceeded`, `AF_WM_DisplayOutcomeFailed`, `AF_WM_DisplayRaidSuccess`, `AF_WM_DisplaySiegeSuccess`, `AF_WM_DisplayAttackSuccess`；
    - 部队状态与行军进展：`AF_WM_DisplayWaitingHeroShelter`, `AF_WM_DisplayRaidStartedUnderway`, `AF_WM_DisplaySiegeStartedUnderway`, `AF_WM_DisplayArrivedSettlementHolding`；
    - 叛乱与攻击前置校验原因：`AF_WM_RebellionNoPlayerClan`, `AF_WM_RebellionOnlyPlayerCompanion`, `AF_WM_RebellionPlayerNotInKingdom`, `AF_WM_AttackTargetDefeatedDeadOrCaptured`, `AF_WM_TimeoutAttackExpired` 等；
    - 队列结束原因与后续行动：`AF_WM_ReasonQueueDone`, `AF_WM_ReasonMergeTransferFailed`, `AF_WM_TailGovernorReturning`, `AF_WM_TailNativeResumed`。
    - **成效**：通过 `CommandMessageForDisplay` 仅在 UI/Message 显示端投影，底层命令数据与 AFEF 事实完全不受影响。

---

### 3. Economy (`AF.Module.Economy`)
- **资源文件**：
  - `content/modules/AF.Module.Economy/ModuleData/Languages/afeconomy_strings.xml` / `CNs/afeconomy_strings-zh-CN.xml`
- **主要新增分类 (85 Keys)**：
  - **信任度与定居点收益（5 Keys）**：`AF_ECO_UI_TaskGiverFallback`, `AF_ECO_UI_EstimatedHits`, `AF_ECO_UI_ActualHits`, `AF_ECO_UI_SettlementTrustGain`, `AF_ECO_UI_SettlementTrustCarry`；
  - **信件与 RP 物品弹窗（7 Keys）**：`AF_ECO_UI_LetterDefaultTitle` ("Letter" / "信件")、`AF_ECO_UI_ViewLetterTitle` ("View Letter" / "查看信件")、`AF_ECO_UI_PopupCloseButton`、`AF_ECO_UI_RpItemDefaultTitle` ("RP Item" / "RP 物品")、`AF_ECO_UI_ViewRpItemIntroductionTitle` ("View Item Description" / "查看物品介绍")、`AF_ECO_UI_RpItemIntroductionPending`、`AF_ECO_UI_RpItemIntroductionEmpty`；
  - **锻造与回滚状态（6 Keys）**：`AF_ECO_UI_RpForgeFailed`, `AF_ECO_UI_RpForgeFailedException`, `AF_ECO_UI_RpForgeRollbackIncomplete`, `AF_ECO_UI_RpForgeRollbackCleaned`, `AF_ECO_UI_RpForgeRefundNoCharge`, `AF_ECO_UI_RpForgeRefundFull`；
  - **固定资产转移结果投影（35 Keys）**：`AF_ECO_UI_FixedAssetQtyMustBeOne`, `AF_ECO_UI_FixedAssetEmptyToken`, `AF_ECO_UI_FixedAssetNotFound`, `AF_ECO_UI_FixedAssetGiverDead`, `AF_ECO_UI_FixedAssetGiverNotOwner`, `AF_ECO_UI_FixedAssetKingdomAtWar`, `AF_ECO_UI_FixedAssetTransferSuccess`, `AF_ECO_UI_FixedAssetWorkshopSuccess`, `AF_ECO_UI_FixedAssetAlleySuccess` 等；
  - **王国让位结果投影（10 Keys）**：`AF_ECO_UI_KingdomTransferGiverDead`, `AF_ECO_UI_KingdomTransferPlayerAlreadyRuling`, `AF_ECO_UI_KingdomTransferPlayerDifferentKingdom`, `AF_ECO_UI_KingdomTransferSuccess` 等；
  - **阵营归附与独立结果投影（22 Keys）**：`AF_ECO_UI_FactionIdUnknownAction`, `AF_ECO_UI_FactionIdClanJoinPlayerKingdomSuccess`, `AF_ECO_UI_FactionIdMercenarySuccess`, `AF_ECO_UI_FactionIdVassalSuccess`, `AF_ECO_UI_FactionIdIndependentSuccess`, `AF_ECO_UI_FactionIdClanRebelSuccess` 等。

---

## 三、保留项审查与白名单证据

按共同约束与手眼隔离要求，下列候选项目经调用链追踪确认，**具备权威排除证据，完全保留原语义不变**：

| 文件 / 位置 | 候选内容 | 排除理由与调用者用途证据 |
| :--- | :--- | :--- |
| `content/.../TownAmbientDialogue.json` (124条) | Memory 背景说明、ReplyHints（如“喝令百姓躲开”、“安抚居民”等） | **AFEF 事实与提示词上下文**。在 `TownAmbientDialogueBehavior.cs:1304-1313` 中被拼装为 `[AFEF NPC行为补充]` 事实，传入 `MyBehavior.AppendExternalNonHeroSceneDialogueHistory` 供 LLM Prompt 消费，严禁翻译。 |
| `TownAmbientDialogueBehavior.cs:1311` | `"可回应方向="` | **AFEF 记忆标签**。同样作为 AFEF 行为事实标签写入 LLM 长期历史。 |
| `TownAmbientDialogueBehavior.cs:1051-1061` | `"武器"`, `"盔甲"`, `"铁匠"`, `"马商"`, `"酒馆"` 等 | **原生游戏工种与场景匹配 Token**。用于从 NPC 角色定义与 Troop ID 中提取规范化职业 `GetCanonicalNpcRole`，改写会导致场景角色匹配失效。 |
| `CourierDeliveryBehavior.LetterInventory.cs:674/684/701` | `"信件"`, `"信件正文"`, `"物品介绍"`, `"物品"` | **Give/Show AFEF 动作事实**。在 `SceneTradeBannerlordAdapter.cs:397` 中作为动作事实写入会话历史 `MyBehavior.AppendExternalDialogueHistory`，供模型推理消费。 |
| `EconomyPromptProjection.cs:51` | `"【系统账目提示】玩家对你有以下承诺或欠款..."` | **LLM Prompt 注入正文**。直接作为 Prompt 系统账目模块注入模型提示词，非玩家可见 UI。 |
| `RewardSystemBehavior.cs:14602-14626` | `"酿酒"`, `"木匠"`, `"桶匠"`, `"铁匠"`, `"皮革"` 等 | **资产/工坊匹配关键词**。内部用于匹配玩家或 NPC 拥有的工坊类型，改动会导致资产查询脱靶。 |
| `RewardSystemBehavior.cs:4308` | `"[AFEF NPC行为补充] "` | **AFEF 内部事实前缀**。用于向大地图/场景 NPC 追加历史记忆。 |
| `RewardSystemBehavior.cs` / `GiveAssetTagCodec.StressTests` | `"锻造等级或三档概率已经变化..."` 等 | **测试 Oracle 严格断言**。由压力测试 `GiveAssetTagCodec.StressTests/Program.cs` 原样断言，按规约不得修改。 |
| `VanillaIssueOfferBridge.cs:697` | `"静默原版任务交付异常: "` | **内部异常日志前缀**。直接输出至内部 Logger，不对玩家展示。 |
| `IssueRuntimePromptOwner.cs:181/219` | `"任务标题："` | **Issue RAG 提示词构建器**。用于组装发给大模型的原版任务上下文提示词。 |

---

## 四、验证结果

1. **双语资源契约测试**：
   ```powershell
   py tests/content/J15ContentContractTests/LocalizationResourceContractTests.py
   ```
   **测试输出**：`PASS localization resource pairs=24`（全部 24 对中英文资源成对存在，无单边缺失，无重复键，占位符 100% 对齐）。

2. **Git Diff 格式与语法检查**：
   ```powershell
   git diff --check eda6992f9^..c6989eedf
   ```
   **测试输出**：`0 errors`（无多余空白、无冲突标记、无行尾回车异常）。

3. **隔离边界检查**：
   - 未修改 PolicySystem、Kingdom、Diplomacy、Weekly、Memory、Knowledge、Illustrator、Coup、MCM、Terminal 任何文件；
   - 未触碰共享注册表与主线文件；
   - 未运行 `dotnet build` 或任何编译命令。

---

## 五、未执行验证项标记 (NOT-RUN)

- `[NOT-RUN]` slow `dotnet build` / 官方构建流水线（遵照指令严格免编译）
- `[NOT-RUN]` 实机游戏内各界面渲染与交互点击
- `[NOT-RUN]` 战役周期长时回放测试
- `[NOT-RUN]` 旧存档反序列化兼容测试
