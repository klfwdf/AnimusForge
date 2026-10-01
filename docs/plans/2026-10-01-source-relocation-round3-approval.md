# 第三轮：剩余根级源码精确归位审批

**状态：只读提案，未批准或移动。未来新外交覆盖不在本轮执行。**

绑定HEAD `24424b67cd89337a0cde8639fb0bbcdb8e2b2db2` / `codex/af-main-refactor-continuation-20260831`。
精确总表SHA256：`33f852bd63b6d47a110787a809a26323c2566d2983a048a35af387fdd1852c74`。

## 审批范围与目标

- 当前38根级tracked C#：18外交产品→现有 `src/modules/AF.Module.Diplomacy/` 对应职责子目录；19 GCCZ实际游戏业务宿主→`src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/`；1 Gathering业务宿主→`Composition/Gathering/`。
- 原样执行后 **38→0** 根级tracked C#，两轮已迁289、本轮38、累计327。此数只统计根级tracked .cs，不声称删除/清空其他根目录、资源或旧产物。
- 实质制作组业务依然是业务owner，不冒称薄桥；不放入standalone `AnimusForge.SiegeAftermathIntervention/` purecore SDK递归树，不改coreproject、不拆算法/DLL、不G盘镜像。
- StrategicProfile2、VoteDeal4、Siege6整partial家族一起批准/迁移/验证；整文件保持所有嵌套/同文件DTO/SaveableTypeDefiner。namespace/类型/程序集/存档key及ID/API/默认入口不变。

## 新外交将来如何接入

- canonical源码根为 `src/modules/AF.Module.Diplomacy/`；本表逐项new路径就是这38份现实现的归属，其中18份为当前外交实现。已有World/Direct/Rules以及新分组Agenda/Vassalage/Annexation/Profiles/Guards同一DLL身份保持。
- 将来新外交需另行明确批准具体覆盖/合并/删除/业务变更清单，本轮不覆盖新实现，不留root兼容副本或新旧双编译实现，也不改保存身份来迎合目录。
- **写者状态限制：** 当前可见任务只有本任务active、两旧J17任务idle，未识别独立新外交任务ID/分支/目标包；另Git worktree `E:/AnimusForge-klfwdf` main96a1c60f仍原状。未联系其他线程，不能把可见快照当成全球“无人写”。未知本身不阻止此提案、不扩大历史审计。审批实施前重核可见写者快照/HEAD/38源hash/targets；发现新外交正写这些old/new或冲突，停对应包并获用户授权协调，不覆盖。

## 已验与未做

- 完整38分区、P1/P2与P3实际old/new一致、全部raw SHA/新目标不存在/大小写无冲突、历史Git LF基线相等通过。
- 双API无target实际MSBuild各1150唯一Compile、7嵌入资源；提案38映射仍1150无漏重，资源不搬/LogicalName保留。standalone purecore实际184唯一Compile且全在core树内，38目标都不进入该树。
- P3一次消费者审计：6测试文件需direct current-path修正、2只加共享mapping；当前bridge manifest2/catalog16/codeMap1物理路径由P4接通。runtime module-root GUI loader、CLR fulltype反射/Saveable身份不随源目录改。
- 历史git show/review keys/hash/save fixtures/metadata语义不刷新，shared helper仅测试289+38=327映射；外交测试只必要purepath，无业务/资源/玩家数据变动。
- 尚无移动、消费者/产品编辑、测试或完整构建、提交。无push/Stage/部署/清理/安装下载/外仓写/一键改变；旧tools/NuGet/产物保留。
- 原SessionTransport rawSHA不变；仍M状态但LF内容与HEAD相同、index空，不为整理它而改文件或index flags。

## 精确 old → new

| owner | old | new |
|---|---|---|
| P1 | `DiplomacyPeaceTermsService.cs` | `src/modules/AF.Module.Diplomacy/Direct/DiplomacyPeaceTermsService.cs` |
| P1 | `DiplomacyRecentPeaceGuard.cs` | `src/modules/AF.Module.Diplomacy/Guards/DiplomacyRecentPeaceGuard.cs` |
| P1 | `KingdomAnnexationBehavior.cs` | `src/modules/AF.Module.Diplomacy/Annexation/KingdomAnnexationBehavior.cs` |
| P1 | `KingdomAnnexationDiagnosticLog.cs` | `src/modules/AF.Module.Diplomacy/Annexation/KingdomAnnexationDiagnosticLog.cs` |
| P1 | `KingdomStrategicProfileBehavior.cs` | `src/modules/AF.Module.Diplomacy/Profiles/KingdomStrategicProfileBehavior.cs` |
| P1 | `KingdomStrategicProfileBehavior.DevUi.cs` | `src/modules/AF.Module.Diplomacy/Profiles/KingdomStrategicProfileBehavior.DevUi.cs` |
| P1 | `NpcTributeVassalageBehavior.cs` | `src/modules/AF.Module.Diplomacy/Vassalage/NpcTributeVassalageBehavior.cs` |
| P1 | `Patch_Diplomacy_GuardFactionManagerDeclareWar.cs` | `src/modules/AF.Module.Diplomacy/Guards/Patch_Diplomacy_GuardFactionManagerDeclareWar.cs` |
| P1 | `Patch_Diplomacy_RegisterMakePeaceAction.cs` | `src/modules/AF.Module.Diplomacy/Guards/Patch_Diplomacy_RegisterMakePeaceAction.cs` |
| P1 | `PermanentAllianceGuard.cs` | `src/modules/AF.Module.Diplomacy/Guards/PermanentAllianceGuard.cs` |
| P1 | `TerminalVassalageTributeHistoryPopupVM.cs` | `src/modules/AF.Module.Diplomacy/Vassalage/UI/TerminalVassalageTributeHistoryPopupVM.cs` |
| P1 | `VassalageBehavior.cs` | `src/modules/AF.Module.Diplomacy/Vassalage/VassalageBehavior.cs` |
| P1 | `VassalageDiagnosticLog.cs` | `src/modules/AF.Module.Diplomacy/Vassalage/VassalageDiagnosticLog.cs` |
| P1 | `VoteDealBehavior.cs` | `src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.cs` |
| P1 | `VoteDealBehavior.Agenda.cs` | `src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.Agenda.cs` |
| P1 | `VoteDealBehavior.MapNotification.cs` | `src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.MapNotification.cs` |
| P1 | `VoteDealBehavior.Propose.cs` | `src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.Propose.cs` |
| P1 | `WorldDiplomacyLlmClient.cs` | `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyLlmClient.cs` |
| P2 | `CastleAftermathArmamentRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathArmamentRuntimeBridge.cs` |
| P2 | `CastleAftermathDispositionSessionBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathDispositionSessionBridge.cs` |
| P2 | `CastleAftermathLordDuelRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathLordDuelRuntimeBridge.cs` |
| P2 | `CastleAftermathLordExecutionRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathLordExecutionRuntimeBridge.cs` |
| P2 | `CastleAftermathLordRecruitmentRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathLordRecruitmentRuntimeBridge.cs` |
| P2 | `CastleAftermathPrisonerTrustRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathPrisonerTrustRuntimeBridge.cs` |
| P2 | `CastleAftermathSettlementRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathSettlementRuntimeBridge.cs` |
| P2 | `GcczSettlementCulturePersistenceBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/GcczSettlementCulturePersistenceBehavior.cs` |
| P2 | `GcczTownRuleMemoryRuntimeBridge.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/GcczTownRuleMemoryRuntimeBridge.cs` |
| P2 | `InterventionNativeTownCivilianPopulationMissionBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/InterventionNativeTownCivilianPopulationMissionBehavior.cs` |
| P2 | `NobleGatheringBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/Gathering/NobleGatheringBehavior.cs` |
| P2 | `NoblePrisonerExecutionOrderBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/NoblePrisonerExecutionOrderBehavior.cs` |
| P2 | `NoblePrisonerExecutionRuntime.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/NoblePrisonerExecutionRuntime.cs` |
| P2 | `SiegeAiInterventionBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.cs` |
| P2 | `SiegeAiInterventionBehavior.DirectAftermathAdapter.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.DirectAftermathAdapter.cs` |
| P2 | `SiegeAiInterventionBehavior.TownColonizationLoadRecovery.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.TownColonizationLoadRecovery.cs` |
| P2 | `SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs` |
| P2 | `SiegeAiInterventionBehavior.TownEconomyEffectAdapter.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.TownEconomyEffectAdapter.cs` |
| P2 | `SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs` |
| P2 | `VillageAftermathBehavior.cs` | `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/VillageAftermathBehavior.cs` |

职责/一基源码证据、所有保存与运行身份线索、唯一消费者审计、精确计划hash和新外交写者门禁均在总表及其绑定package；没有新增38之外源码移动。审批后先本地意图checkpoint与小批次实际字节/Compile/资源门禁，再两包并行、共享文件单写者、隔离输出和必要验证/本地切片提交。
