# AF 当前源码目录简明说明（第二轮，2026-10-01）

物理源码归位，不新增DLL，不拆算法；namespace、CLR类型、程序集、存档key、公开API与默认运行入口保持。目录中的Host/Composition表达现有混合游戏宿主，不冒称职责已经解耦。

## 当前目录职责

| 目录 | 第二轮原样归位数 | 现有责任 |
| --- | ---: | --- |
| `src/AF.GameAdapter.Bannerlord` | 70 | 游戏引擎入口/Composition混合宿主、配置、UI、patch、Encounter适配 |
| `src/bridges` | 30 | 经逐项源代码证明的AF-side薄桥；制作组规则/状态机/业务存档仍原位 |
| `src/modules/AF.Module.Conversation` | 2 | 现有同DLL领域源码，具体职责见精确审批清单 |
| `src/modules/AF.Module.Economy` | 1 | 现有同DLL领域源码，具体职责见精确审批清单 |
| `src/modules/AF.Module.Exercise` | 1 | 现有同DLL领域源码，具体职责见精确审批清单 |
| `src/modules/AF.Module.Memory` | 1 | 现有同DLL领域源码，具体职责见精确审批清单 |
| `src/modules/AF.Module.Prompt` | 5 | 现有同DLL领域源码，具体职责见精确审批清单 |
| `src/modules/AF.Module.Taunt` | 1 | 现有同DLL领域源码，具体职责见精确审批清单 |

- `src/AF.GameAdapter.Bannerlord/Composition`：SubModule、IntegratedModuleHost、MyBehavior全部22根级partial等整体归位，仍保持同一类型及原Campaign/save/thread责任。
- `Configuration/Mcm`：DuelSettings原根级五个具名partial；同类型的Exercise/Ambient/GCCZ UI文件按文件真实职责放入对应宿主/桥目录，不拆partial类或改SettingProperty。
- `src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs`与已有13个Reward partial保持同一类型。
- `src/AF.GameAdapter.Bannerlord/UI/Courier`：回信Popup/VM；外交测试只修必要当前源码定位，无外交产品/fixture/metadata语义变更。
- `src/bridges/Siege`、`src/bridges/Vengeance`：本轮30个纯AF-side/Bannerlord适配文件；没有移动制作组core规则或镜像G盘。
- 第一轮178归位保持；既有Contracts/Foundation/Persistence/Bootstrap/其他模块及资源职责不变。

## 根级仍保留什么

根级tracked C#累计 **327 → 149 → 38**：第一轮178，第二轮111（81 AF主体/混合宿主 + 30纯桥），累计289原样归位。38为18外交产品+20制作组实质规则/状态机/业务存档；根级并非空壳，明确不把排除业务擅自搬入AF。混合AF宿主与旧测试路径已不作为保留理由。

| 分类 | 根级文件 |
| --- | --- |
| 外交产品（18） | `DiplomacyPeaceTermsService.cs`, `DiplomacyRecentPeaceGuard.cs`, `KingdomAnnexationBehavior.cs`, `KingdomAnnexationDiagnosticLog.cs`, `KingdomStrategicProfileBehavior.cs`, `KingdomStrategicProfileBehavior.DevUi.cs`, `NpcTributeVassalageBehavior.cs`, `Patch_Diplomacy_GuardFactionManagerDeclareWar.cs`, `Patch_Diplomacy_RegisterMakePeaceAction.cs`, `PermanentAllianceGuard.cs`, `TerminalVassalageTributeHistoryPopupVM.cs`, `VassalageBehavior.cs`, `VassalageDiagnosticLog.cs`, `VoteDealBehavior.Agenda.cs`, `VoteDealBehavior.cs`, `VoteDealBehavior.MapNotification.cs`, `VoteDealBehavior.Propose.cs`, `WorldDiplomacyLlmClient.cs` |
| 制作组实质业务（20） | `CastleAftermathArmamentRuntimeBridge.cs`, `CastleAftermathDispositionSessionBridge.cs`, `CastleAftermathLordDuelRuntimeBridge.cs`, `CastleAftermathLordRecruitmentRuntimeBridge.cs`, `CastleAftermathPrisonerTrustRuntimeBridge.cs`, `CastleAftermathSettlementRuntimeBridge.cs`, `GcczSettlementCulturePersistenceBehavior.cs`, `NobleGatheringBehavior.cs`, `SiegeAiInterventionBehavior.cs`, `SiegeAiInterventionBehavior.DirectAftermathAdapter.cs`, `SiegeAiInterventionBehavior.TownColonizationLoadRecovery.cs`, `SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs`, `SiegeAiInterventionBehavior.TownEconomyEffectAdapter.cs`, `SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs`, `VillageAftermathBehavior.cs`, `InterventionNativeTownCivilianPopulationMissionBehavior.cs`, `CastleAftermathLordExecutionRuntimeBridge.cs`, `GcczTownRuleMemoryRuntimeBridge.cs`, `NoblePrisonerExecutionOrderBehavior.cs`, `NoblePrisonerExecutionRuntime.cs` |

逐文件真实代码行/保留理由见[第二轮总表](../plans/2026-10-01-source-relocation-round2-manifest.json)。未动第三方、原版参考、资源、依赖、历史档及旧产物；初始SessionTransport dirty仍不纳入提交。

## 现态与历史定位

1. [唯一主台账第二轮入口](../animusforge-refactoring-and-repository-reorganization-plan.md#source-relocation-round2-20261001)、[代码范围说明](af-framework-code-scope.md#source-relocation-round2-20261001)与[813代码坐标图](af-framework-code-map.json)为现态导航。第一轮旧保留/hold记录只作历史，已被第二轮精确批准明确替代。
2. `tests/output_isolation.py::current_source_path`现有289项只处理测试当前路径；review键/历史git show路径与原hash保持。严格具名inverse不提供源码fallback、不刷新生产digest。
3. 主项目SDK默认Compile包含全部src；主csproj/sln无改动，双API各1150 Compile/7资源LogicalName保持；不新增重复Include，未新增或改变SourceLink配置/namespace/程序集身份；PDB远端源码下载未验。
4. 一键编译/覆盖/打包行为不变；本轮离线门禁只用已审隔离runner与新UUID输出，本地提交不表示push/Stage/部署授权。

限于结构/构建/契约证明；实机、旧档、真实网络、音频与性能仍未验。
