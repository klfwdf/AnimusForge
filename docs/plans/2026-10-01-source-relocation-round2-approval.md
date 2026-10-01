# 第二轮源码归位：精确批量审批摘要

**状态：仅提案，尚未批准或移动。**

绑定分支 `codex/af-main-refactor-continuation-20260831`，HEAD `3d6641f26c6a5c4673d5cb42771009ddca735ee8`。
精确清单：`2026-10-01-source-relocation-round2-manifest.json`，SHA256 `0e23eeffb6b2214098947b8ee51880c0c15053453f3651ecf300adbe39327940`。

## 本次批准对象

- 剩余根级 tracked C# **149 = 111待归位 + 38明确保留**；若原样批准执行，根级149→38。
- P1：3项（Reward主宿主、Courier回信2 UI）；P2：MyBehavior22整家族原样进入现有Composition目录；P3：86项。
- 111中81项AF主体/混合宿主，30项逐代码证明的AF-side/Bannerlord桥单独进入 `src/bridges`。职责不拆、namespace/类型/程序集/存档key/API/算法不改。
- 测试旧路径不再阻止归位，允许**必要纯路径**修正，包括外交测试读取；不迁外交产品、不改外交fixture内容/metadata语义。实际basename假阳性不构成修改授权。
- 保留38：18外交产品、20实质制作组规则/状态机/业务存档。没有以“混合职责”或测试路径依赖保留AF宿主；没有unknown。

## 已验证与尚未执行

- 三包old/new去重、149完整互斥分区、全部111源raw SHA、全部目标不存在及38保留hash通过。JSON同时区分checkout原字节、HEAD git blob和LF标准化hash，避免CRLF误判。
- 无target/restore/build双API实际求值均1150 Compile无重、7嵌入资源；提案映射1150唯一，无漏项；**搬后实际Compile和构建/回归未执行**。
- 第一轮178不重迁；原SessionTransport dirty字节保留、无stage。旧总控线程idle；盘点时未发现旧构建/测试进程（不暴露命令行/凭据）。
- 审批后先意图checkpoint和小批次实际Compile/字节门禁，再并行移项；共享消费者/索引/正式材料唯一集成，隔离输出、串行共享构建、精确add/本地切片提交。
- 不覆盖目标；冲突具名停止。不得清理旧tools/NuGet/产物、不下载/安装、不G盘镜像、不改一键/Stage/部署/push。

## 精确 old → new

| owner | old | new |
|---|---|---|
| P1 | `RewardSystemBehavior.cs` | `src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs` |
| P1 | `CourierLetterReplyPopup.cs` | `src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterReplyPopup.cs` |
| P1 | `CourierLetterReplyPopupVM.cs` | `src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterReplyPopupVM.cs` |
| P2 | `MyBehavior.CampaignLifetime.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignLifetime.cs` |
| P2 | `MyBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs` |
| P2 | `MyBehavior.DialogueHistoryCommit.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryCommit.cs` |
| P2 | `MyBehavior.DialogueHistoryDelete.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryDelete.cs` |
| P2 | `MyBehavior.HistoryPromptSnapshot.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs` |
| P2 | `MyBehavior.MemoryFailureNotice.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryFailureNotice.cs` |
| P2 | `MyBehavior.MemoryMaintenanceBudget.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryMaintenanceBudget.cs` |
| P2 | `MyBehavior.MemoryRecovery.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs` |
| P2 | `MyBehavior.MemorySealing.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySealing.cs` |
| P2 | `MyBehavior.MemorySourceFingerprint.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySourceFingerprint.cs` |
| P2 | `MyBehavior.MemorySourceWrites.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySourceWrites.cs` |
| P2 | `MyBehavior.MemorySummaryInput.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryInput.cs` |
| P2 | `MyBehavior.MemorySummaryMainThread.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryMainThread.cs` |
| P2 | `MyBehavior.MemorySummaryPlanning.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryPlanning.cs` |
| P2 | `MyBehavior.MemorySummaryRun.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryRun.cs` |
| P2 | `MyBehavior.PersonaGeneration.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaGeneration.cs` |
| P2 | `MyBehavior.PersonaReadiness.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaReadiness.cs` |
| P2 | `MyBehavior.PromotedPersonaGeneration.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PromotedPersonaGeneration.cs` |
| P2 | `MyBehavior.WeeklyActionOutcomeReceipts.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyActionOutcomeReceipts.cs` |
| P2 | `MyBehavior.WorldBulletin.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WorldBulletin.cs` |
| P2 | `MyBehavior.WorldBulletinNpc.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WorldBulletinNpc.cs` |
| P2 | `MyBehavior.WorldBulletinPanel.cs` | `src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WorldBulletinPanel.cs` |
| P3 | `SubModule.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SubModule.cs` |
| P3 | `IntegratedModuleHost.cs` | `src/AF.GameAdapter.Bannerlord/Composition/IntegratedModuleHost.cs` |
| P3 | `Patch_TriggerMassiveHook.cs` | `src/AF.GameAdapter.Bannerlord/Composition/Patch_TriggerMassiveHook.cs` |
| P3 | `SceneActionsIntegrationBoundary.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SceneActionsIntegrationBoundary.cs` |
| P3 | `DuelSettings.cs` | `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs` |
| P3 | `DuelSettings.CivilWar.cs` | `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.CivilWar.cs` |
| P3 | `DuelSettings.CourierLetter.cs` | `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.CourierLetter.cs` |
| P3 | `DuelSettings.SceneActions.cs` | `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.SceneActions.cs` |
| P3 | `DuelSettings.TerminalSave.cs` | `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.TerminalSave.cs` |
| P3 | `TownAmbientSettings.cs` | `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/TownAmbientSettings.cs` |
| P3 | `MilitaryExerciseBehavior.cs` | `src/modules/AF.Module.Exercise/Host/MilitaryExerciseBehavior.cs` |
| P3 | `DuelConfig.cs` | `src/modules/AF.Module.Prompt/Configuration/Models/DuelConfig.cs` |
| P3 | `DuelStakeConfig.cs` | `src/modules/AF.Module.Prompt/Configuration/Models/DuelStakeConfig.cs` |
| P3 | `LoanConfig.cs` | `src/modules/AF.Module.Prompt/Configuration/Models/LoanConfig.cs` |
| P3 | `RewardConfig.cs` | `src/modules/AF.Module.Prompt/Configuration/Models/RewardConfig.cs` |
| P3 | `SurroundingsConfig.cs` | `src/modules/AF.Module.Prompt/Configuration/Models/SurroundingsConfig.cs` |
| P3 | `AnimusForgeTerminalBehavior.cs` | `src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalBehavior.cs` |
| P3 | `AnimusForgeTerminalUiModels.cs` | `src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalUiModels.cs` |
| P3 | `TerminalSettingsRegistry.cs` | `src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs` |
| P3 | `AnimusForgeTagCatalog.cs` | `src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTagCatalog.cs` |
| P3 | `WorldMessageTimelineUi.cs` | `src/AF.GameAdapter.Bannerlord/UI/WorldTimeline/WorldMessageTimelineUi.cs` |
| P3 | `WorldMessageTimelineMenuBehavior.cs` | `src/AF.GameAdapter.Bannerlord/UI/WorldTimeline/WorldMessageTimelineMenuBehavior.cs` |
| P3 | `EncyclopediaHeroPersonaPatch.cs` | `src/AF.GameAdapter.Bannerlord/UI/Persona/EncyclopediaHeroPersonaPatch.cs` |
| P3 | `EncyclopediaKingdomStabilityPatch.cs` | `src/AF.GameAdapter.Bannerlord/UI/Kingdom/EncyclopediaKingdomStabilityPatch.cs` |
| P3 | `KingdomFactionTab.cs` | `src/AF.GameAdapter.Bannerlord/UI/Kingdom/KingdomFactionTab.cs` |
| P3 | `AnimusForgeMeetingMissionViews.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/AnimusForgeMeetingMissionViews.cs` |
| P3 | `AnimusForgeSettlementAccessModel.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/AnimusForgeSettlementAccessModel.cs` |
| P3 | `EncounterConversationTargetResolver.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/EncounterConversationTargetResolver.cs` |
| P3 | `LordEncounterBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs` |
| P3 | `LordEncounterRedirectGuard.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterRedirectGuard.cs` |
| P3 | `MeetingBattleLockMissionBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/MeetingBattleLockMissionBehavior.cs` |
| P3 | `MeetingBattleRuntime.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/MeetingBattleRuntime.cs` |
| P3 | `MeetingDuelBattleAgentLogicSafePatch.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/MeetingDuelBattleAgentLogicSafePatch.cs` |
| P3 | `MeetingTargetWieldBlockPatch.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/MeetingTargetWieldBlockPatch.cs` |
| P3 | `Patch_Conversation_Start_Intercept.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_Conversation_Start_Intercept.cs` |
| P3 | `Patch_ConversationManager_OpenMapConversation.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_ConversationManager_OpenMapConversation.cs` |
| P3 | `Patch_ConversationManager_SetupAndStartMapConversation.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_ConversationManager_SetupAndStartMapConversation.cs` |
| P3 | `Patch_GameMenu_ActivateGameMenu.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_GameMenu_ActivateGameMenu.cs` |
| P3 | `Patch_Meeting_SuppressChangeRelationAction.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_Meeting_SuppressChangeRelationAction.cs` |
| P3 | `Patch_Meeting_SuppressDeclareWarAction.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_Meeting_SuppressDeclareWarAction.cs` |
| P3 | `Patch_Meeting_SuppressEncounterHostileAction.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_Meeting_SuppressEncounterHostileAction.cs` |
| P3 | `Patch_MenuHelper_EncounterAttackConsequence_RaidVillageRestore.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_MenuHelper_EncounterAttackConsequence_RaidVillageRestore.cs` |
| P3 | `Patch_NpcSurrender_SkipHeroCaptureConversations.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_NpcSurrender_SkipHeroCaptureConversations.cs` |
| P3 | `Patch_PlayerEncounter_Start.cs` | `src/AF.GameAdapter.Bannerlord/Encounter/Patch_PlayerEncounter_Start.cs` |
| P3 | `AnimusForgeSettlementLoyaltyModel.cs` | `src/AF.GameAdapter.Bannerlord/Kingdom/AnimusForgeSettlementLoyaltyModel.cs` |
| P3 | `PlayerKingdomRebellionImmunity.cs` | `src/AF.GameAdapter.Bannerlord/Kingdom/PlayerKingdomRebellionImmunity.cs` |
| P3 | `Patch_PlayerKingdomNameChange_RecordMaterials.cs` | `src/AF.GameAdapter.Bannerlord/Patches/Memory/Patch_PlayerKingdomNameChange_RecordMaterials.cs` |
| P3 | `Patch_PrisonBreakRescue_RecordSuccess.cs` | `src/AF.GameAdapter.Bannerlord/Patches/Memory/Patch_PrisonBreakRescue_RecordSuccess.cs` |
| P3 | `AnimusForgeDialogueHistoryEntry.cs` | `src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs` |
| P3 | `TownAmbientDialogueBehavior.cs` | `src/modules/AF.Module.Conversation/Ambient/Host/TownAmbientDialogueBehavior.cs` |
| P3 | `TownAmbientTime.cs` | `src/modules/AF.Module.Conversation/Ambient/Host/TownAmbientTime.cs` |
| P3 | `SceneTauntBehavior.cs` | `src/modules/AF.Module.Taunt/Host/SceneTauntBehavior.cs` |
| P3 | `TroopInspectionBehavior.cs` | `src/AF.GameAdapter.Bannerlord/TroopInspection/TroopInspectionBehavior.cs` |
| P3 | `TroopInspectionPrisonerSlaughterRuntime.cs` | `src/AF.GameAdapter.Bannerlord/TroopInspection/TroopInspectionPrisonerSlaughterRuntime.cs` |
| P3 | `SettlementEntryTroopSelectionBehavior.cs` | `src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs` |
| P3 | `SettlementEntryTroopSelectionLog.cs` | `src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionLog.cs` |
| P3 | `AfGcczShoutBridge.cs` | `src/bridges/Siege/Host/AfGcczShoutBridge.cs` |
| P3 | `CastleAftermathActionRuntimeBridge.cs` | `src/bridges/Siege/Host/CastleAftermathActionRuntimeBridge.cs` |
| P3 | `CastleAftermathArmyRosterRuntimeBridge.cs` | `src/bridges/Siege/Host/CastleAftermathArmyRosterRuntimeBridge.cs` |
| P3 | `CastleAftermathDefensiveDeviceRuntimeBridge.cs` | `src/bridges/Siege/Host/CastleAftermathDefensiveDeviceRuntimeBridge.cs` |
| P3 | `CastleAftermathLordSaleRuntimeBridge.cs` | `src/bridges/Siege/Host/CastleAftermathLordSaleRuntimeBridge.cs` |
| P3 | `CastleAftermathMissionEntryBridge.cs` | `src/bridges/Siege/Host/CastleAftermathMissionEntryBridge.cs` |
| P3 | `CastleAftermathPrisonerAllocationRuntimeBridge.cs` | `src/bridges/Siege/Host/CastleAftermathPrisonerAllocationRuntimeBridge.cs` |
| P3 | `CastleAftermathRuntimeBridge.cs` | `src/bridges/Siege/Host/CastleAftermathRuntimeBridge.cs` |
| P3 | `CastleAftermathSiegeSceneBridge.cs` | `src/bridges/Siege/Host/CastleAftermathSiegeSceneBridge.cs` |
| P3 | `CastleAftermathBattleMusicUpdatePatch.cs` | `src/bridges/Siege/Host/CastleAftermathBattleMusicUpdatePatch.cs` |
| P3 | `CastleAftermathConstructionSpeedPatch.cs` | `src/bridges/Siege/Host/CastleAftermathConstructionSpeedPatch.cs` |
| P3 | `GcczVolunteerRecruitmentRatePatch.cs` | `src/bridges/Siege/Host/GcczVolunteerRecruitmentRatePatch.cs` |
| P3 | `GcczLocalizedResourceLoader.cs` | `src/bridges/Siege/Resources/GcczLocalizedResourceLoader.cs` |
| P3 | `GcczTownActionPresentationResourceProvider.cs` | `src/bridges/Siege/Resources/GcczTownActionPresentationResourceProvider.cs` |
| P3 | `GcczTownEntryPresentationResourceProvider.cs` | `src/bridges/Siege/Resources/GcczTownEntryPresentationResourceProvider.cs` |
| P3 | `GcczTownHiddenResidentResourceProvider.cs` | `src/bridges/Siege/Resources/GcczTownHiddenResidentResourceProvider.cs` |
| P3 | `GcczTownManualResourceProvider.cs` | `src/bridges/Siege/Resources/GcczTownManualResourceProvider.cs` |
| P3 | `GcczTownPromptResourceProvider.cs` | `src/bridges/Siege/Resources/GcczTownPromptResourceProvider.cs` |
| P3 | `GcczDiagnosticLog.cs` | `src/bridges/Siege/Resources/GcczDiagnosticLog.cs` |
| P3 | `GcczTownManualInquiryPresenter.cs` | `src/bridges/Siege/UI/GcczTownManualInquiryPresenter.cs` |
| P3 | `GcczTownManualMcmBridge.cs` | `src/bridges/Siege/UI/GcczTownManualMcmBridge.cs` |
| P3 | `EncyclopediaTownRuleMemoryPatch.cs` | `src/bridges/Siege/UI/EncyclopediaTownRuleMemoryPatch.cs` |
| P3 | `GcczTownRuleMemoryDeveloperBridge.cs` | `src/bridges/Siege/RuleMemory/GcczTownRuleMemoryDeveloperBridge.cs` |
| P3 | `GcczTownRuleMemoryGenerationBridge.cs` | `src/bridges/Siege/RuleMemory/GcczTownRuleMemoryGenerationBridge.cs` |
| P3 | `GcczTownRuleMemoryRulerAdapter.cs` | `src/bridges/Siege/RuleMemory/GcczTownRuleMemoryRulerAdapter.cs` |
| P3 | `GcczTownRuleMemorySpeakerResolver.cs` | `src/bridges/Siege/RuleMemory/GcczTownRuleMemorySpeakerResolver.cs` |
| P3 | `InterventionHiddenResidentSpawnMissionBehavior.cs` | `src/bridges/Siege/Scene/InterventionHiddenResidentSpawnMissionBehavior.cs` |
| P3 | `VengeanceRuntimeBridge.cs` | `src/bridges/Vengeance/Host/VengeanceRuntimeBridge.cs` |
| P3 | `ExecutionAddressLlm.cs` | `src/bridges/Vengeance/Host/ExecutionAddressLlm.cs` |
| P3 | `Patch_SiegeAftermath_AFIntervention.cs` | `src/bridges/Siege/Host/Patch_SiegeAftermath_AFIntervention.cs` |

## 根级明确保留

| old | 真实归属/保留原因 |
|---|---|
| `CastleAftermathArmamentRuntimeBridge.cs` | Substantive castle spoils rule locally computes dropRatio=min(.30,.10+tier*.025), equipment payout and tier*count*3 gold; not merely API forwarding (45-75). Team/GCCZ business retained. |
| `CastleAftermathDispositionSessionBridge.cs` | Owns terminal/disposition action selection and deferred prisoner outcome ledger/roster revisions, a castle gameplay state responsibility; not pure host forwarding. |
| `CastleAftermathLordDuelRuntimeBridge.cs` | Owns castle captive-lord duel mission state/virtual-health transitions/outcome selection. Rules partly profiles but this is substantive GCCZ-specific execution state machine; retained. |
| `CastleAftermathLordRecruitmentRuntimeBridge.cs` | Owns castle lord-recruitment branch application and courier-free queued letter lifecycle with joins/status/1-2-day task; substantive team workflow retained. |
| `CastleAftermathPrisonerTrustRuntimeBridge.cs` | Owns GCCZ-specific defeated-garrison trust dictionary/save key and update workflow (_gcczCastleRegularPrisonerTrustByTroopId_v1); not solely thin conversion. |
| `CastleAftermathSettlementRuntimeBridge.cs` | Owns annual castle economic/recruitment/construction effect persistence and daily settlement processing, six _gcczCastle save dictionaries (41-48); team business retained. |
| `DiplomacyPeaceTermsService.cs` | Tribute calculation/clamping domain service of diplomatic peace terms; diplomatic business excluded. |
| `DiplomacyRecentPeaceGuard.cs` | Recently signed peace pair tracking and hostile-encounter prevention policy; diplomatic business excluded. |
| `GcczSettlementCulturePersistenceBehavior.cs` | Owns GCCZ culture-overrides save and daily/reload mutation of actual Settlement.Culture (_gcczSettlementCultureOverrides_v1); substantive persistence workflow retained. |
| `KingdomAnnexationBehavior.cs` | Actual kingdom annexation/AI merge campaign business and save owner; diplomatic product excluded. |
| `KingdomAnnexationDiagnosticLog.cs` | Dedicated kingdom-annexation domain diagnostic event schema; diplomatic companion excluded. |
| `KingdomStrategicProfileBehavior.cs` | Actual kingdom strategic profiles for diplomacy/policy world context and persistence; diplomatic product owner excluded. |
| `KingdomStrategicProfileBehavior.DevUi.cs` | Developer editor partial for same strategic-profile diplomacy owner; whole family excluded. |
| `NobleGatheringBehavior.cs` | Actual gathering invitation/ceremony/effect domain records, campaign registration and save state. Team Gathering business excluded, not generic AF conversation adapter. |
| `NpcTributeVassalageBehavior.cs` | NPC tribute/vassalage relationship and power-context campaign business owner; diplomatic product excluded. |
| `Patch_Diplomacy_GuardFactionManagerDeclareWar.cs` | Direct Prefix of faction war invokes diplomatic guard owner; not AF meeting suppressor; diplomatic product excluded. |
| `Patch_Diplomacy_RegisterMakePeaceAction.cs` | MakePeace Postfix forwards to diplomacy peace guard/history registration owner; diplomatic product excluded. |
| `PermanentAllianceGuard.cs` | Permanent-alliance break authorization policy/state; diplomatic product excluded. |
| `SiegeAiInterventionBehavior.cs` | Substantive GCCZ siege/town aftermath mission/menu/action/economic/culture/save state machine; entire six-file team host family retained, no name-only ownership inference. |
| `SiegeAiInterventionBehavior.DirectAftermathAdapter.cs` | Substantive GCCZ siege/town aftermath mission/menu/action/economic/culture/save state machine; entire six-file team host family retained, no name-only ownership inference. |
| `SiegeAiInterventionBehavior.TownColonizationLoadRecovery.cs` | Substantive GCCZ siege/town aftermath mission/menu/action/economic/culture/save state machine; entire six-file team host family retained, no name-only ownership inference. |
| `SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs` | Substantive GCCZ siege/town aftermath mission/menu/action/economic/culture/save state machine; entire six-file team host family retained, no name-only ownership inference. |
| `SiegeAiInterventionBehavior.TownEconomyEffectAdapter.cs` | Substantive GCCZ siege/town aftermath mission/menu/action/economic/culture/save state machine; entire six-file team host family retained, no name-only ownership inference. |
| `SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs` | Substantive GCCZ siege/town aftermath mission/menu/action/economic/culture/save state machine; entire six-file team host family retained, no name-only ownership inference. |
| `TerminalVassalageTributeHistoryPopupVM.cs` | Dedicated vassalage/tribute history VM binds Vassalage domain record, diplomatic product UI excluded (not generic mixed terminal). |
| `VassalageBehavior.cs` | Treaty agreements, subject obedience/tribute/payment/war lifecycle and persisted diplomacy owner; excluded. |
| `VassalageDiagnosticLog.cs` | Dedicated vassalage domain log event/schema/detail companion; diplomatic product excluded. |
| `VillageAftermathBehavior.cs` | GCCZ village administration/actions plus gradual culture targets _gcczVillageGradualCultureTargets_v1 and DailyTick mutations; substantive team business, retained. |
| `VoteDealBehavior.Agenda.cs` | Actual kingdom vote deals/propose/agenda notifications and campaign save workflow; diplomatic product family excluded. |
| `VoteDealBehavior.cs` | Actual kingdom vote deals/propose/agenda notifications and campaign save workflow; diplomatic product family excluded. |
| `VoteDealBehavior.MapNotification.cs` | Actual kingdom vote deals/propose/agenda notifications and campaign save workflow; diplomatic product family excluded. |
| `VoteDealBehavior.Propose.cs` | Actual kingdom vote deals/propose/agenda notifications and campaign save workflow; diplomatic product family excluded. |
| `WorldDiplomacyLlmClient.cs` | World diplomacy-specific API job generation/result/compression/model contract, not general AF LLM transport; diplomatic product owner excluded. |
| `InterventionNativeTownCivilianPopulationMissionBehavior.cs` | Local GCCZ/regular-town civilian population formulas and business bounds remain here: RegularTownPopulationMultiplierMin=2.1,Max=2.8,HardCap=220 (69-72), village20..80 and prosperity-weighted target calculation (264-278). Not pure live spawn forwarding; team/mixed substantive population policy retained. |
| `CastleAftermathLordExecutionRuntimeBridge.cs` | Owns PendingExecution/RuntimeStage/Tick and captive-lord terminal execution progression (mission notification, fatal completion). Substantive GCCZ-specific outcome state machine, same exclusion criterion as captive-lord duel; not pure native notification wrapper. |
| `GcczTownRuleMemoryRuntimeBridge.cs` | Actual authoritative Store and _gcczTownRuleMemoryRecordsBySettlement_v1 plus second version key save projection/SyncData, dictionary, restore and governance-observation lifecycle. External codec alone does not make owner pure transport; GCCZ memory persistence host retained. |
| `NoblePrisonerExecutionOrderBehavior.cs` | Owns consented execution task dictionary, _afNoblePrisonerExecutionOrders_v1 SyncData/hourly task progression. Attribution/delay/codec are delegated, but actual task-save/state host is substantive team-assisted execution lifecycle, retained. |
| `NoblePrisonerExecutionRuntime.cs` | Owns PendingExecution Stage (QueuedConfirmation/WaitingForNotification/WaitingForConversationExit/PerformingAttack/ApproachingPlayer), mission isolation/completion state, not stateless core forwarding; keep family with execution-order business host. |

精确声明行/代码证据与逐行消费者分别冻结在p1/p2/p3计划，SHA绑定于总清单。消费者清单103文件是待判读证据，不授权机械全局替换；历史git show/review/hash原样保留。
