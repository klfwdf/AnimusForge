## 主体 J13e2 Taunt 开工意图（2026-09-25）

状态：`J13e2_ACTIVE / J13_ACTIVE`，基线 `d8059bd1`。按[原计划](plans/j13-domain-owners-plan.md)只推进和平场景 Taunt，随后 e3 Encounter、e4 Settlement/Inspection、e5 Exercise；不提前 J14。已重读[场景伤害上下文](scene_damage_context_guard_case.md)与 1.3/1.4 差异。`SceneTauntBehavior.cs:115–324,1863–2049,2127–3188,4090–4228,5455–5692,8236–8310,10301–10620` 当前同时持有 Campaign 保存/事件、Mission 回调、和平资格、冲突升级/犯罪/队伍恢复和五个原 patch；`CampaignComposition.cs:38` 与 `StartupPatchComposition.cs:333–369` 原装配保留。`IsOwnedSettlementPassiveAttackPeaceLocationScene` 已有严格 allowlist/战斗排除，但 `CanStartConflict` 的普通初始化尚仅凭 settlement/原 FightHandler，不能假定 siege/野战/部署等反例由旧门禁自动排除；此风险先用受控正反用例定位，再抽最小真实资格 owner 并接生产入口。MCM 的 `EnablePeaceSceneConflict` **只关闭直接攻击转换**，`TerminalSettingsRegistry.cs:218` 明文保留对话挑衅；不可误把 verbal path 也禁用。队伍变更/犯罪/恢复需逐段追真实状态，不批量搬 10k 行。先做入口资格/反例，再做升级/惩罚/恢复闭包、生产 DLL 与聚合接线；所有真实 Campaign/Mission/Harmony/旧档/帧性能验收仍待实机，不以源码串代替。`.dotnet-cli-home/` 不动，无 push、Stage、部署、打包、外仓写入或自动化。

## 主体 J13e1 Duel 有限离线收口（2026-09-25）

状态：**`J13e1_OFFLINE_VERIFIED / J13_ACTIVE`**，仅 e1 离线门禁，下一包 e2 Taunt，随后 e3–e5；不提前 J14。意图 `8cc8ef63`，只读构建产物回放入口 `054781b1`，生产 owner/回放 `88643619`、`ca49023f`，聚合契约 `d0d15a57`，真实 DLL 受理行为回放 `d89a13b5`。此前 d4 的完成状态不扩大为场景实机验收。

- **代码与责任**：`src/modules/AF.Module.Duel/DuelBehavior.DispatchOwner.cs:13–448` 持有原 exact DuelId 的有界 tombstone、`IDetachedDuelDispatchOwner.TryQueue/Reject/Cancel/MarkUnknownAfterStart`、代际/目标校验、delayed `Queued && HostAccepted`、副作用前取消/之后 Unknown 与替换逻辑；同一 `DuelBehavior` ABI 保留。`src/modules/AF.Module.Duel/DuelSettlementEffectOwner.cs:7–41` 把 meeting/arena/wilderness 已观察到的 Hero/non-Hero、胜负、renown/stake 效果投影成 typed terminal receipt，不执行经济、Mission 或记忆副作用。`DuelBehavior.Outcomes.cs:12–130,134–562,735–833` 继续复用现有 `DuelOutcomeOwner`、精确 fingerprint/主体、stake/debt/after-lines 绑定与按 DuelId 清理、结果先锁定再 final/Unknown、process-local readback 和原 load 清理；`DuelBehavior.cs:1767–1881,4339–4462,7454–7600` 的三类 Mission/游戏对象终局 writer 保持在原宿主并真实调用领域投影。此处不是移动文件或字符串断言作为完成凭据：三个 writer 的当前双版本 DLL 调用路径、Duel typed/dispatch 行为、当前 DLL exact owner 回放均另验。原注册 `CampaignComposition.cs:24`、应用 tick `ApplicationTickComposition.cs:71,112`、Harmony `StartupPatchComposition.cs:469,477` 不变。
- **Patch/消费核对（仅离线，非实机事件顺序）**：`DuelBehavior.cs:2239–2260` 原 `RegisterHarmonyPatches` 注册 13 个显式 class；`MapEvent.CalculateAndCommitMapEventResults` → `WildernessDuelMapEventResultsPatch.Prefix(MapEvent)` 仅 `IsWildernessDuelMapEvent` 时拦原版并由野外清理链收尾，非本机制/异常返回原版；`PlayerEncounter.DoApplyMapEventResults/DoPlayerVictory/DoPlayerDefeat/DoEnd` → 四个 `WildernessDuelPlayerEncounter*Patch.Prefix(PlayerEncounter)` 同守卫并调用 `CleanupWildernessDuelRuntime` 或原 map-event/encounter 清理。`PlayerEncounter.GetBattleRewards` → `WildernessDuelBattleRewardsZeroPatch.Prefix`，1.4 `ExplainedNumber`/loot rate/`Figurehead`，1.3 float/gold/loot percentage + `ref ExplainedNumber` 双签名；仅野外决斗零奖励，非决斗和异常返回原版。`MenuHelper`/`GameMenuOption` 的其余七个原 patch 仍由同一注册点处理野外菜单/袭击资格；`FourberieDuelCompatibility.EnsurePatched` 仅在 Fourberie 存在时动态 patch `OnMissionBehaviorInitialize(Mission)` 与列明的任务回调，未重复 `PatchAll`。Native/Scene exact 请求消费同一 dispatch owner，Courier 仍在 `CourierDeliveryBehavior.DomainCommit.cs:186` 明确拒绝 `unsupported_channel`；不把三渠道例外同化。
- **真实行为与构建证据**：`DuelOutcomeContractTests` **20/20**（包括三种结算投影、非英雄、无效 session/effect、终局一次），`DuelDispatchContractTests` **16/16**；`ProductionDuelOutcomeReplayTests --use-build-artifacts` 对 Debug/Release 的 1.3/1.4 各 **35/35**，校验双版本 marker/hash/freshness、typed ABI、IL 调用、stake/debt、delayed timeout 与 Courier 排除。当前 Debug 1.4 候选 SHA256 `64CBF30A1F436F1D05B8A0A8817DBA9A4A186C568F9484080E67B4427BC6A700` 的 Phase8 全通过；其中 `DuelDispatchOwnerReplay` 反射调用**当前生产 DLL** 的 exact owner，覆盖新受理/重复不重派、错 fingerprint、旧代际、桥关闭、接受前不得启动、越界后 Unknown、取消/重入及拒绝。`J13E1DomainOwnerContractReplay` 另核对组成/patch/原保存及消费者，但只是源码接线。原脚本在四个仓内重建目录绝对路径/内容/无链接复核后，不带 `-Stage/-Deploy` 完成 Debug/Release × Bannerlord `v1.3.15.110062`/`v1.4.7.117484` + Bootstrap 六构建；四实现 SHA256 依次为 Debug 1.3 `32D51FCDBC482E11C7C5D3D6F38AABD204AC89422C3806FA728B87346C4952A2`、Debug 1.4 如上、Release 1.3 `C512451E355F1AE4297081A8559E1DE81364D0C92463B1527AA1D4B180966C1F`、Release 1.4 `6C5999F5987C1263CD19635EA564A42B0D5C6418309529FFFD14D0D801152E52`。V1 **119**/四 DLL metadata **1064**、PersistenceIdentity **142/36**、source inventory **7**、代码地图 **617** 锚点 recorded/working-tree 通过。
- **频率与未验**：exact queue/ready/tombstone 是原请求/延迟 tick 的 O(1) 判断，有界 4096；结算投影只在终局调用一次，不加集合全扫、额外反射或新锁；当前 DLL 回放的反射仅在测试进程内，并还原桥配置内存字段。桥关闭 fixture **不是**游戏 MCM 菜单实际关闭验收。真实 Mission/MapEvent/Harmony 触发顺序、1.3/1.4 游戏内加载、Fourberie 交互、旧档、stake/debt 经济副作用、MCM/UI 与帧性能均 **NOT-RUN**。无 push、Stage、部署、打包、写游戏/外仓或自动化；`.dotnet-cli-home/` 保留。

## 主体 J13e1 Duel 开工意图（2026-09-25）

状态：`J13e1_ACTIVE / J13_ACTIVE`，基线 `6567d11c`。按[原计划](plans/j13-domain-owners-plan.md)只推进 Duel，再依序 e2–e5，不提前 J14。已读场景伤害/军团会面、三渠道/标签及 1.3/1.4 兼容案例；不改提示词、渠道例外、原 Mission/Harmony 注册或玩法。

- **现有真实链路**：`Refactor/Runtime/DuelOutcomeReceipt.cs` 已有 typed request/start/result/effects 与有界 `DuelOutcomeOwner`，必须复用；`DuelBehavior.Outcomes.cs:12–1059` 承担 process-local owner/索引、detached dispatch 受理与转移，`DuelBehavior.cs:2004–2148,2611–3015,5976–6200,7465–7615` 仍持有待启动/会面/竞技场/野外状态与三类终局协调。`CampaignComposition.cs:24` 原行为、`StartupPatchComposition.cs:469` 原 Harmony、`ApplicationTickComposition.cs:71,112` tick 消费者保留。先核对 `DuelOutcomeContractTests`、`DuelDispatchContractTests`、`ProductionDuelOutcomeReplayTests` 的实际可运行入口；后者历史入口依赖 Stage，不得为了运行而擅自 Stage。
- **切片顺序/门禁**：先从 exact dispatch 的受理/延迟 ready/拒绝状态决策提取可测真实 owner，再迁运行态与结算协调；每片应覆盖 exact DuelId/subject/fingerprint、重复与错对象、延迟启动/超时、stake/debt、终局一次性及非决斗/MCM 关闭/退出重入，宿主仍做 TW 主线程副作用。逐片本地提交；每包补聚合接线、双版本构建/三个 Duel 套件的可运行部分、代码地图/简短 HANDOFF。数据契约/fixture 不能冒充 Mission、旧档或实机性能；不新增重复 PatchAll/订阅，不改 Harmony target/signature，`.dotnet-cli-home/` 与他人改动不动。

## 主体 J13d4 WarStats 有限离线收口（2026-09-25）

状态：**`J13d4_OFFLINE_VERIFIED / J13_ACTIVE`**；基线 `a30035d9`，开工意图 `7c2bc643`，逐片产品/回放 `6d5657e9`（唯一状态/归档/终端删除）、`c8cc0efd`（战斗计数）、`ab05a736`（死亡/近期战斗）、`db1830bc`（v1–v5 投影/恢复/旧账迁移）、`f722dd41`（宣战/清空转换），聚合接线 `f0cc3ede`、日 Tick 接线补充 `6b0c07f7`。以下是**有限离线**收口，不是实际 Campaign、旧档、Gauntlet 或性能验收；下一包按[原计划](plans/j13-domain-owners-plan.md)进入 e1 Duel，依序 e2–e5，不提前 J14。

- **代码坐标与职责**：`src/modules/AF.Module.WarStats/WarStatsLedgerOwner.cs:9–482` 的 `WarStatsLedgerOwner` 独占活动/历史/旧账集合及近期战斗序号；`:34–163` 决定宣战、清空、近期战斗记录与死亡去重/更新，`:191–268` 决定同对象终战快照/移除和顺序/负数约束的战斗计数，`:270–425` 处理 v1–v5 平行列表投影、坏行/缺列恢复和旧账迁移去向，`:427–482` 按 pair/start/end 身份删除历史并克隆归档死亡。`WarStats/AfWarStatsBehavior.cs:15,238–253` 保留原类型并从唯一 owner 读取状态；`:358–437` 保留事件和所有 `IDataStore` 键，`:441–597` 保留终端投影/清空 facade，`:606–695,1099–1260` 仍在主线程解析 Kingdom/MapEvent/Hero、时间、战争事实及终战元数据。`src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs:54` 原行为注册不变；`WarStats/AfWarStatsPopupVM.cs:995–1103,1228–1292,1676–1680` 继续消费原行为、确认删除/清空与同一历史身份。产品修订 `f722dd411e0647d7fa857bb38876d2e89835a671`、契约修订 `6b0c07f7dad77d756adb79f82bbe388ca00989e2`，完整锚点见[代码地图](architecture/af-framework-code-map.json)；源码坐标只定位，不替代行为证据。
- **行为/接线证据**：Phase8 先对旧候选验证红例（缺 owner/计数/死亡/保存/清空方法），再对当前 Debug 1.4 DLL SHA256 `656D4E9BF9E5894FEA899B6E3CA05BC264B1F725A16D32853E4874B6F051EC07` 全套通过。WarStats 回放覆盖重复和平幂等、旧对象不得归档重开 pair、归档独立快照、终端按身份删除且不删活动战事、正反序/负数战斗计数、死亡重入/已知杀手与战场名保留、近期战斗同英雄替换/空身份拒绝/序号饱和、死亡历史克隆、v5 空 key/负数/缺 start 恢复、v1 完整旧行与活动/结束迁移、宣战与清空。`tools/PhaseEightParityReplayTests/J13D4DomainOwnerContractReplay.cs:4–56` 单独核对行为注册、五事件、原保存键、owner 调用和终端消费者；它只是源码接线契约。合成空王国与私有状态回放未运行原版事件派发或游戏存档。
- **兼容与边界**：原脚本在逐一核对 `bin/{Debug,Release}/single_module_artifacts`、`obj/single_module/{Debug,Release}` 的绝对路径和重解析点后，仅在仓内重建；Debug/Release × Bannerlord 1.3.15/1.4.7 + Bootstrap 六构建均 0 警告/错误。四实现 DLL 的 V1 119 与元数据 1060 断言、PersistenceIdentity `sync=142 behavior=36`、迁移 fixture 10、source inventory 7、代码地图 600 锚点 recorded/working-tree 通过。近期战斗/死亡仍复用原 MapEvent 多边解析和按活动/历史扫描，不新增定时扫描、反射或锁；真实游戏频率/帧耗时 **NOT-RUN**。旧档、实机战争/战斗事件顺序、终端点击、游戏内 1.3/1.4 装载均 **NOT-RUN**；没有 push、Stage、部署、打包、游戏/外仓写入或改自动化，未跟踪 `.dotnet-cli-home/` 保留。

## 主体 J13d4 WarStats 开工意图（2026-09-25）

状态：`J13d4_ACTIVE / J13_ACTIVE`；基线 `a30035d95a5ea281dea3dc7210b3a1321e50f226`，只推进[原计划](plans/j13-domain-owners-plan.md)的 WarStats，之后依序 e1–e5，不提前 J14。

- **真实边界**：`WarStats/AfWarStatsBehavior.cs:15,238–424` 保留原 `AFWarStatsTerminal.Behaviors.AfWarStatsBehavior`、Campaign 事件注册、v1–v5 `SyncData` 字段/键；`:619–715` 收日 Tick、宣战/停战、战斗与英雄死亡；`:1164–1408` 对账、归档与战斗统计；`:433–618` 终端投影和删除；`:1865–2070` 平行保存列表。`CampaignComposition.cs:54` 原注册、`WarStats/AfWarStatsPopupVM.cs` 的终端消费者保持，游戏对象读取留主线程 host。
- **有限切片**：先让单一 ledger owner 拥有活动/历史/旧账实例及终战归档/删除转换，复用 Phase8 现有真实 host 归档/幂等/列表往返；再按计数、近期战斗/死亡、迁移/保存投影分别迁算法与状态决策。不能仅把旧方法放进 partial 或机械转发。测试逐片覆盖重复和平、重开独立、错配对象、死亡去重/最近战斗、v5 旧/坏数据与终端筛选；合成空王国 fixture 不代表实际 Campaign 事件顺序。
- **退出门**：每片生产 owner+消费者+行为反例单独提交；d4 聚合接线契约、原 Debug/Release × 1.3/1.4+Bootstrap、当前候选 Phase8、保存/API/Compile 和代码地图/HANDOFF 后才报有限离线收口。无 Stage、部署、打包、游戏/外仓写入、推送或自动化更改，`.dotnet-cli-home/` 保留。

## 主体 J13d3 WorldEvents 有限离线收口（2026-09-25）

状态：**`J13d3_OFFLINE_VERIFIED / J13_ACTIVE`**，只表示本包真实 owner、生产消费者、相关行为与兼容离线门禁闭合；下一包是 J13d4 WarStats，随后 e1–e5，不提前 J14。意图 `82f406ca`，产品/行为 `62ec9065`，聚合接线及政策 UI 契约 `b07898cb`；[代码范围图](architecture/af-framework-code-scope.md)和[代码地图](architecture/af-framework-code-map.json)绑定产品/契约修订 `b07898cb`，586 锚点 recorded/working-tree 通过，定位不冒充行为验收。

- **唯一 owner/保留边界**：`src/modules/AF.Module.WorldEvents/WorldEventInboxOwner.cs:9–181` 的 `WorldEventInboxOwner` 独占 `_records`、未读、stable-key 索引、version、导入/导出、去重/容量/已读转换；`WorldEvents/WorldEventInbox.cs:44–85` 保留 `AnimusForgeWorldEventBehavior`、DTO/public facade、两条 v1 保存键与 chunk/`IDataStore` 适配。`CampaignComposition.cs:31` 原注册不变；政策发布 `NpcRulerPolicyBehavior.Generation.cs:1279–1285,5473–5475` 仍用 version 增长确认，Policy UI `PolicySystemUi.cs:66`、外交档案 `WorldDiplomacyBehavior.cs:10885` 和弹窗已读 `WorldEventInbox.cs:437` 共用原入口。现有代码里没有 Weekly 直接 upsert 该收件箱的生产调用，不能虚称已覆盖不存在的发布源。
- **行为/成本**：同 stable key 的重复发布不再产生第二记录，更新投影时保留首个 EventId/已读状态，并为已接受的重复 upsert 增 version 维持政策确认；加载旧 EventId JSON/坏记录及旧重复项时保持有效记录与未读联合。最多 240 项的 trim/save、最多 200 项的快照只在发布/保存/UI 读取调用；没有新 Tick 扫描、后台游戏对象访问或第二持久化键。单次最多 240 项排序/JSON 成本仍存在，实机帧耗时未测。
- **验证层级**：在旧 d2 DLL 上具名红灯、当前 Debug 1.4 SHA256 `C70D15E9B5251356F98FBDBA633E61205A45EB1B48D18FB0CE39C68E5BDE17B0` 的完整 Phase8（含 d3 行为和聚合接线）绿灯；PolicyEffect UI 路径在其源码链接测试产物上 387 断言通过。原脚本无 Stage/Deploy、引用 1.3.15.110062/1.4.7.117484 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；四实现 SHA256 见下方聚合回执。V1 119/四 DLL metadata 1060、PersistenceIdentity 基线 `053ad485` 142 key/type 对/36 behaviors、source inventory 7、代码地图 586 锚点两模式均通过。真实 Campaign 发布顺序、`IDataStore` 实例、旧存档、Gauntlet 点击、provider、音频、帧性能和 Stage/部署均 `NOT-RUN`；不把合成 JSON/源码接线当实机。

## 主体 J13d3 WorldEvents 聚合接线与兼容门禁（2026-09-25）

产品/行为切片 `62ec9065ecbadd1ff147efc1e188438b59eaa3ab` 后，`J13D3DomainOwnerContractReplay` 已核对原 Campaign 注册、两条保存键与 chunk 导入/导出、公开发布/快照/已读门面、政策 version 确认及 Policy UI/外交档案/终端的真实消费者；这是源码接线证据，与当前生产 DLL 的 `WorldEventInboxOwnerReplay` 行为证据分层。原 `PolicyEffectModule.ContractTests` 的王国公告断言仍期待无参调用，但实际终端在基线 `938ce116` 已传 `OpenCustomPolicyManagementView` 回调；将断言更新为当前完整回调路由，不删除 re-review/prefab/政策身份检查。

- **聚合结果**：当前 Debug 1.4 DLL SHA256 `C70D15E9B5251356F98FBDBA633E61205A45EB1B48D18FB0CE39C68E5BDE17B0` 的完整 Phase8（含 d3 状态与聚合）通过；`PolicyEffectModule.ContractTests --player-policy-ui-contracts-only` 在源码链接产物上 387 断言通过，`source inventory` 7 通过。原脚本不带 Stage/Deploy 且明确 1.4.7 参考目录的 Debug/Release × 1.3/1.4+Bootstrap 六构建均 0 warning/0 error；四实现 SHA256：Debug 1.3 `6AFC6CC40700C0B8101E00B98E301C8F9826CF8F2D302BD858ECBB27B3C0B74E`、Debug 1.4 如上、Release 1.3 `9A3CB45C4AEC5AF31ECD7990D9B241E7666BDB25675C54453FB2A978E81D2E26`、Release 1.4 `99FDFB4B96FEFB3B0EAF7492F412A4790C6D57C4E2314ABDEB9CD3F7076ECC3D`。
- **保存/API**：`PersistenceIdentityAudit.py --baseline 053ad485` 比较 142 个 key/type 对与 36 个 CampaignBehavior 无变化；使用仓库固定 SDK 8.0.425 的 V1 119 / 四 DLL metadata 1060 通过。首次误用系统 SDK 10 导致离线 targeting pack `NU1100`，改为计划已指定的固定 SDK 后通过；首次省略存档审计基线得到历史新增 WarStats 误报，指定 `053ad485` 后通过。`PolicyEffect` 显式加载另一候选 DLL 时发生双程序集同名类型冲突，改由该 runner 加载它自身刚源码编译的产物后通过，故此 387 断言不是脚本候选 DLL 的行为回放。
- **余项**：代码地图 recorded/working-tree、简短 HANDOFF 尚待更新后才能标 d3 有限整包收口。真实 Campaign、旧档、UI 点击、Stage/部署和帧性能 `NOT-RUN`；不以当前离线证据替代。

## 主体 J13d3 WorldEvents 收件箱 owner 行为切片（2026-09-25）

状态：`J13d3_INBOX_SLICE_OFFLINE_VERIFIED / J13d3_ACTIVE / J13_ACTIVE`。开工意图 `82f406ca`；本片把原行为内的权威记录、未读、stable-key 索引、容量、version、导入/导出和已读转换移至 `src/modules/AF.Module.WorldEvents/WorldEventInboxOwner.cs:9–181`。`WorldEvents/WorldEventInbox.cs:44–85` 仍保留原 CampaignBehavior/DTO、`_afWorldEventInboxRecords_v1` 与 `_afWorldEventInboxUnread_v1`、chunk 适配和公开静态入口；政策发布/Policy UI/外交档案/弹窗已读仍沿原入口消费同一 owner，没有第二份收件箱。

- **行为变化与兼容**：同一 `StableKey`、不同 `EventId` 的重复发布刷新投影但保留最初 UI `EventId` 和已读状态；新事件 `markUnread:false` 的 JSON 已读位与未读集合保持一致。成功重复 upsert 仍增长 version，维持政策发布的确认契约。旧 JSON 和按 EventId 存储的保存键不改；导入按 Day/ticks 稳定选择较新记录，并合并旧重复项的未读状态。发布/存档/快照最多处理 240 项，仍为低频主线程操作，无新增 tick 扫描；实际帧耗时未测。
- **红绿证据**：针对 d2 Debug 1.4 DLL SHA256 `0071A62D00D40E4113222F7A3E1FCDA641CABFA26EA4FC9371301025EF745544`，新 `WorldEventInboxOwnerReplay` 在“stable key owns one inbox record”具名断言失败；此后原脚本不带 Stage/Deploy、显式 1.4.7 参考集的 Debug 1.3/1.4+Bootstrap 三构建均 0 warning/0 error。当前 Debug 1.4 SHA256 `C70D15E9B5251356F98FBDBA633E61205A45EB1B48D18FB0CE39C68E5BDE17B0` 的完整 Phase8 回放通过，包括重复/已读/version、坏 JSON 与旧重复加载、容量/全读；source inventory 7 通过。首次用游戏 bin 直接作 replay 引用失败于非托管 `TaleWorlds.Native.dll`，改用仓库现有 `local/bannerlord-refs/1.4.7.117484` 后通过；不把失败隐藏为产品回归。
- **仍待本包收口**：补独立 d3 聚合接线契约、保存/API 与 PolicyEffect 对应回归、Release 双实现/Bootstrap、代码地图及 HANDOFF。回放使用合成条目/JSON，不证明真实 Campaign 发布、实际 IDataStore、旧档或 Gauntlet 点击；这些与帧性能均 `NOT-RUN`，d3 整包尚未宣称完成。

## 主体 J13d3 WorldEvents 开工意图（2026-09-25）

状态：`J13d3_ACTIVE / J13_ACTIVE`。基线 `938ce11645e0a93fb6b07d359e7fab68b8712740`；只推进[原计划](plans/j13-domain-owners-plan.md)的 WorldEvents 收件箱，随后按 d4、e1–e5 顺序施工，不提前 J14。当前仅 `.dotnet-cli-home/` 未跟踪，原样保留。

- **目标与消费者**：把 `WorldEvents/WorldEventInbox.cs:44–182` 的 records/unread、归一化、stable-key 去重、容量与 version 决策迁入唯一领域 owner；保留 `AnimusForgeWorldEventBehavior`、`AnimusForgeWorldEventInboxEntry`、两条 v1 保存键与原静态入口身份。生产消费者包括 `PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs:1279–1285,5449–5475` 的发布确认、`PolicySystem/UI/PolicySystemUi.cs:66` 与 `src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs:10885` 的快照，以及弹窗已读操作。
- **保持与风险**：Policy 现用 inbox version 增长确认 upsert，不能让幂等去重变成假失败；既有存档按 EventId 存放，不能借抽取改 JSON/键/公开 DTO。重复 stable key 不得出现双记录或无意重置未读；加载坏记录不丢其他记录。发布/载入/快照为低频主线程操作，不新增 tick 扫描或第二份权威账本。
- **有限退出门**：先写针对当前生产路径的重复、已读、容量、载入、version 行为反例；迁真实状态与算法并接通上述消费者；补 d3 聚合源码接线契约，运行当前候选回放、保存/API/源码成员及双版本构建，再更新代码地图/HANDOFF。候选离线结论不覆盖真实 Campaign、旧档、UI 点击或帧性能。

## 主体 J13d2 Proactive/Issue 有限离线收口（2026-09-25）

按[原 d2 计划](plans/j13-domain-owners-plan.md)核对后，状态为 **`J13d2_OFFLINE_VERIFIED / J13_ACTIVE`**；仅表示本次职责归属与所列离线门禁闭合，不是实机 Quest/旧档验收。Proactive 资格产品 `a59a0fc0` 及此前 opening/cooldown/scan/session 产品见下文；Issue 依次为完成回执 `fe74d4fa`、运行状态 `3f330c58`、三渠道提示词/后处理 `f739dbaf`、受理/交付动作 `4e4ad8bf`、交付选项判定 `96d058ca`，聚合接线契约 `36037592bc6038138b1ef8d27309a565697d09a7`、事实顺序断言 `7fc6f680917a75a71df332fcc3ce288e4de96f18` 及重复领取行为回放 `d34fd858023cb9d8638bad51f0a7fbe769660d0e`。

- **真实 owner 与消费者**：Social 的 `src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs:24–3734` 处理基础资格、全部需要候选/快照、玩家可受理过滤及触发判定；`ProactiveRequestSessionOwner`、`ProactiveOpeningOwner`、`ProactiveRequestCooldownOwner`、`ProactiveCandidateScanOwner` 分别拥有保存会话运行状态、pending 消费、冷却和增量扫描。`ProactiveNpcRequestBehavior.cs:403–540,542–1005,1349–1360` 仍接 Campaign 小时/TW 主线程、追逐/会面、旧保存键、AFEF；`src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs:216–233` 送达只记信件历史及需要疲劳，不当作接受任务。Issue 的 `src/modules/AF.Module.Issue/Runtime/IssueRuntimeStateOwner.cs:12–114` 负责 offer/ready/in-progress 资格和显式完成信号，`src/modules/AF.Module.Issue/Runtime/IssueRuntimePromptOwner.cs:27–299` 负责各状态 prompt 与状态门控标签，`src/modules/AF.Module.Issue/Actions/IssueActionOwner.cs:27–314` 负责自行/同伴受理、精确任务身份重验和交付事实，`src/modules/AF.Module.Issue/Actions/IssueTurnInDecisionOwner.cs:27–125` 负责原版 discuss 选项评分，`src/modules/AF.Module.Issue/Completion/IssueCompletionReceiptOwner.cs:9–91` 负责完成事件回执。原 `VanillaIssueOfferBridge.cs:182–305,340–1055` 保留共享公开入口、标签清理、原版 Quest/派兵窗口/静默 ConversationManager 适配、反射与快照恢复；`VanillaIssuePromptBehavior.cs:12–55` 保留 Campaign 事件/AFEF 提交。`TryGetRecentCompletionRecord` 仍是原未调用返回 false 兼容桩，不擅自启用消费一次的另一条回执。
- **门禁与风险**：聚合 `tools/PhaseEightParityReplayTests/J13D2DomainOwnerContractReplay.cs:4–65` 核对 Social 小时→扫描→资格→会话→opening 消费、Issue 状态→后处理→受理/交付→回执、Native/Scene/Courier 同一入口，且送达不等于受理；它只是源码接线证据。当前生产 DLL 回放另覆盖 Social 缺 Party/候选、资格过滤、零紧急度、重复/过期 session、opening 单次消费/取消、扫描旧批次、冷却导入；Issue 缺任务/同伴、同伴窗口重复/取消/迟到/载入、交付选项正负评分、完成回执正/无奖励/取消和空 Quest。`IssueRuntimeStateOwnerReplay` 还用当前生产方法与合成的原版 `IssueBase`/`Hero` 验证未受理 offer、已受理拒绝重复 offer、错 owner 拒绝；不代表真实 Campaign 正向执行。受理只在 `StartIssueQuest` 与原版接受收尾成功后写事实，交付只在原版执行成功后写事实；真实 Quest 正向执行与 `PartyScreen` 事件顺序仍 `NOT-RUN`，不能由源码断言替代。原每帧最多 16 队伍/1.5ms 截断和按小时扫描未改；Issue 只在 prompt/标签/窗口/Quest 事件执行原有工作，不加 tick 全扫、轮询、反射注册或第二份保存状态；实机帧耗时未测。
- **实际验证**：经已批准四目录复核，原 `一键编译覆盖推送/build_single_module.ps1` 不传 `-Stage/-Deploy`，Debug/Release × Bannerlord 1.3.15.110062/1.4.7.117484 加 Bootstrap 六项均 0 warning/0 error；Debug 1.4 SHA256 `0071A62D00D40E4113222F7A3E1FCDA641CABFA26EA4FC9371301025EF745544` 的完整 Phase8（含 d2 聚合）通过。四实现 DLL 余下 SHA256：Debug 1.3 `F7B738663D208B13B74599235063F665A2C67AFDEF2FF18D936B2FED42554E62`、Release 1.3 `128D6D07C6C948F7D16C35C6F862AFF0CF93B9E1250393C4951EB9F0259EEE5C`、Release 1.4 `E06112A67B3C0B7E9C1F2EF52E3DAAC13AA317FDD1B801E1570BD05D8FFB942D`。V1 119/四 DLL metadata 1060、PersistenceIdentity 142 key/type 对/36 behaviors、source inventory 7、[代码地图](architecture/af-framework-code-map.json) 575 锚点 recorded/working-tree 均通过。真实 Campaign/Quest/party-screen、旧档、provider、UI/音频、外部 sub-MOD、帧耗时及 Stage/部署均 `NOT-RUN`。
- **后续与边界**：按原计划进入 J13d3 WorldEvents，再 d4 WarStats；J13e/f/g 仍未由本包代替。未 push、Stage、部署、打包、游戏/外仓写入、自动化变更或 J14；`.dotnet-cli-home/` 未触碰。需要撤销时对上述具名本地产品提交作定向 inverse/revert，不 reset/改历史。

## 以下为 d2 Proactive 资格切片记录（历史）

## 主体 J13d2 主动资格完整决策归 Social（2026-09-25）

原目标仍为[计划 d2](plans/j13-domain-owners-plan.md)的 Proactive 与 Issue 整包；本片产品/回放 `a59a0fc07d8879ebf072b1d8b590ea3adbbde5aa`，开工意图 `cd776c19`。Proactive 资格子包有限 `OFFLINE_VERIFIED`，**J13d2/J13 仍 `ACTIVE`**，下一包必须实际迁移并核验 Issue offer/in-progress/turn-in/完成回执。

- **归属与接线**：`src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs:24–3734` 现独占 `FindBestRequestCandidate`、基础 Party/Hero 资格、各类需要候选/快照、玩家可受理过滤、紧急度/名声触发和信件需要投影。`ProactiveNpcRequestBehavior.cs:403–540` 保留小时门禁、每帧增量扫描、MCM/TW 主线程读取与完成适配，`:542+` 保留追逐/会面/AFEF 和保存适配；Courier 原信件入口仍调用同一 `BuildLetterNeedSnapshots`，送达只计疲劳，不算 Issue 接受。原 `ProactiveCandidateScanOwner` 的 16 队伍/帧与 1.5ms 截断保留，没有新增热路径扫描、重复反射、锁或缓存副本；实际帧耗时未测。
- **等价与门禁**：把前驱 `cd776c19` 的原连续资格单元从当前 host+新 owner 字节级重组，归一化换行后与原源码完全一致；仅位置变化。`tools/PhaseEightParityReplayTests/ProactiveQualificationReplay.cs:6–50` 对当前生产 DLL 覆盖缺队伍、无效 Hero、普通粮食需要、重复/未知需要过滤、零紧急度在 RNG/名声读取前拒绝，并断言完整资格源码归属；原 opening/cooldown/scan/session 回放也通过。原脚本无 `-Stage/-Deploy` 的 Debug/Release × 1.3/1.4+Bootstrap 六构建均 0 warning/0 error（引用 1.3.15 / 1.4.7）。Debug 1.4 SHA256 `DD81C07FD3F4146DC696A498A88467F703B886A8CBA69548BACABF1DCC1A85B5` 的 Phase8 全通过；其他实现：Debug 1.3 `4F70724EE4EFC608EA900EE3406BBF6BF458E768010337B5E1721289CDED5A3F`、Release 1.3 `72927F40AD14C43D847304C674FA4DA3EDCB05AEFF58FC2A393F5837716304`、Release 1.4 `4E1CC38D752E0AEB4BB6A281DDA9EF34E1F9AEB5FE45294BF6835ACEB7716304`。V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、[代码地图](architecture/af-framework-code-map.json) 560 锚点 recorded/working-tree 均通过。真实 Campaign 小时触发/资格 RNG、Quest/party-screen、旧档、provider、UI/音频、帧性能仍 `NOT-RUN`。
- **保留/下一步**：Social 的 active session、opening、cooldown、scan 沿各已验证前片；原 host 仍有合法 TW/存档/会面职责。Issue 当前仅派遣 pending 在 `IssueAlternativeDispatchOwner`，`VanillaIssueOfferBridge.cs` 的 offer/in-progress/turn-in 和 `VanillaIssuePromptBehavior.cs` 完成回执尚未归 Issue owner。先读其真实三渠道调用、原版 Quest 身份与反射/回调顺序，再作完整领域闭包和 d2 聚合负例。未推送、Stage、部署、打包、游戏/外仓写入或 J14；`.dotnet-cli-home/` 未触碰。逆转本片须定向 inverse/revert `a59a0fc0`，不 reset/改历史。

## 以下为 d2 主会话切片记录（历史）

## 主体 J13d2 主动主会话 owner 切片（2026-09-25）

目标仍是按[原 d2 计划](plans/j13-domain-owners-plan.md)完成 Proactive 资格/状态/opening 与 Issue offer/in-progress/turn-in/完成回执；本片不缩减最终退出门。起点 `171e2a8f`，意图 `614e2745`，产品/回放 `f1ecb315e1b444c05032e07945af5d9be5ee9fe3`。本主会话片有限 `OFFLINE_VERIFIED`，**J13d2/J13 仍 `ACTIVE`**。

- **状态 owner/真实消费者**：`src/modules/AF.Module.Social/Proactive/ProactiveRequestSessionOwner.cs:9–112` 独占原 `ProactiveNpcRequestSession` DTO 实例、读档旧 ID/单需要规范化、重复启动保护、追逐探测节流、过期与 Hero/party 身份、Menu/Native/Scene 阶段和一次疲劳标记。`ProactiveNpcRequestBehavior.cs:130–151` 保留原保存键 `_af_proactive_npc_request_state_v1`、DTO/SyncData；`:373–384` Campaign tick、`:4252–4414` 候选启动、`:4415–4624` 会面与 opening、`:4680–4713` 消费/完成、`:5059–5110` 取消与主线程 AI 释放是真实调用者。候选游戏对象、MCM、党队/会面与 AFEF 仍在 host；无新 DLL/保存键。重复启动在任何名声观察或游戏 AI 副作用前拒绝，旧 pending opening 与 session 取消同路清理。静态繁忙原因只按原 siege/raid/native/map_event 分类取消；信件送达仍仅记 need fatigue，不当作受理。
- **运行成本/验证**：会话身份/阶段判定 O(1)，原追逐探测 0.35s 间隔，不新增每 tick 集合扫描/分配。`tools/PhaseEightParityReplayTests/ProactiveSessionOwnerReplay.cs:5+` 的当前 DLL 回放覆盖旧档补 ID/单需要、Hero/party 大小写、严格过期边界、重复启动、探测节流、繁忙原因、阶段转换、一次疲劳与取消；旧 opening 回放改为直接导入同一 owner。获准四目录复核后原脚本无 `-Stage/-Deploy` 完成 Debug/Release × 1.3/1.4+Bootstrap 六构建均 `0 warning / 0 error`；Debug 1.4 候选 SHA256 `D42E34E7105D35D9289D4FAD8DCEBC0D879E47E4159EE9B87D063EB17A835DEB` 完整 Phase8 通过。四实现 DLL 其他 SHA：Debug 1.3 `6690B8D26FFEFBD19628A178132EED027D86D87DABC2B9BBE98B1991807D1D13`、Release 1.3 `B7BD426011AD8DAC7C4E1828932449773B2C15D96B508D080709DA4CFAB1B773`、Release 1.4 `71BFFB484C53353195C1D30F9ADD580FA8C6F0009ECF69ABDEA78B3498D75FE6`；V1 119/四 DLL metadata 1060、PersistenceIdentity 142 key/type 对/36 behaviors、source inventory 7、[代码地图](architecture/af-framework-code-map.json) 554 锚点两模式通过。
- **剩余关闭项**：主动候选资格/需要判断仍主要由 `ProactiveNpcRequestBehavior.cs` 原 host 执行，需要实际归 Social 子包并做资格源／MCM／取消的聚合接线反例；Issue 除派遣 pending 外，offer/in-progress/turn-in/完成回执尚未迁入业务 owner。下一步优先核对并迁移这些真实决策及三渠道消费者，完成 d2 聚合门禁后才能标整包 `OFFLINE_VERIFIED`。真实 Campaign/Quest/party-screen、旧档、provider、UI/音频、帧耗时均 `NOT-RUN`。未 push、Stage、部署、打包、外仓/游戏写入、改自动化或进入 J14；`.dotnet-cli-home/` 原样保留。

## 以下为 d2 增量扫描切片记录（历史）

## 主体 J13d2 主动请求增量扫描 owner 切片（2026-09-25）

目标仍是按[原 J13d2 退出范围](plans/j13-domain-owners-plan.md)完整关闭 Proactive/Issue，而非以本片缩小目标。起点 `fe8d8d4a`，意图 `31641e3f`，产品/回放 `6eb3d170f68d1aecd40b158246cd07d800842223`。**本扫描片有限 `OFFLINE_VERIFIED`，J13d2/J13 仍 `ACTIVE`**。

- **真实归属和消费者**：`src/modules/AF.Module.Social/Proactive/ProactiveCandidateScanOwner.cs:10–87` 独占运行时扫描实例、起始/批大小、批次统计与候选排名、按身份一次性完成/清理；旧批次不能污染或退休新扫描。`ProactiveNpcRequestBehavior.cs:410–549` 原小时触发/主线程 `MobileParty` 快照与 Campaign tick 每帧处理调用 owner；`:736` 批内评优和`:1543` 候选排序也共用同一权重/紧急度/名声/距离决策。原主线程游戏对象读取及设置/资格仍在 host；无新 DLL/保存键，保存中的扫描继续以 `LastScanHour=-99999f` 在载入后重试。载入/MCM 关闭清理 owner 当前扫描。
- **性能/验证**：建扫描时原有全 lord party 快照 O(N) 未谎称消除；批大小仍以 45 帧为目标并封顶每帧 16 队伍，host 仍按原 1.5ms `Stopwatch` 截断。`tools/PhaseEightParityReplayTests/ProactiveCandidateScanOwnerReplay.cs:5–76` 先在旧 DLL 红灯，再在当前 DLL 覆盖空/900 项批大小、重复发起、旧完成、统计隔离、排名全部 tie-break、一次完成、清理；不等于实机帧耗时。获准四目录复核后原脚本不带 `-Stage/-Deploy` 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 `0 warning / 0 error`。Debug 1.4 SHA256 `507BE34AEDA844518AC537DC43E05C773D96DDA1FEACB36EBF43DB919FF0580C` 的完整 Phase8 通过；四实现 DLL 其他 SHA：Debug 1.3 `437F5CD97499452F6F4FD369E163A59289B429BED212ECAB3EBC68B838168E9B`、Release 1.3 `2B5BE9217CCE9EE542D67192AE60329ACF030B9DD5D12820F9CD091C352B59F4`、Release 1.4 `BC4BAC6920AE7EA55B60CDF54F0423C96020ACCB46783B8F7DF06F6FB1AE016A`。V1 119、四 DLL metadata 1060、PersistenceIdentity 142 key/type 对/36 behaviors、source inventory 7 和[代码地图](architecture/af-framework-code-map.json) 546 锚点两模式通过。
- **后续仍须关闭**：主动主 session 的资格/失效/取消/完成事实状态转换，及 Issue offer/in-progress/turn-in/完成回执与重复受理；此前 pending opening、冷却、派遣 pending 证据不能替代它们。下一步从 `ProactiveNpcRequestBehavior.cs:4259` 启动、`:4562` 清理、`:5087` 取消链及实际消费者读入，再转 Issue。真实 Campaign 触发、Quest/party-screen、旧档、provider/UI/音频/帧性能均 `NOT-RUN`，不得称实机验收。未 push、Stage、部署、打包、游戏/外仓写入或 J14，`.dotnet-cli-home/` 不动。

## 以下为 d2 冷却切片记录（历史）

## 主体 J13d2 主动请求冷却与扫描节流 owner 切片（2026-09-25）

承接 d2 opening/Issue 派遣生命周期首片 `580a1466`；本片意图 `a64442d6`，产品/回放 `43566f0f218775364fd3980e00b329f19ea61e7d`。**本片有限 `OFFLINE_VERIFIED`，J13d2/J13 仍 `ACTIVE`**；J13a–c/d1 既有有限结论不扩大为实机验收。未推送、Stage、部署、打包、改自动化、写游戏/外仓或进入 J14。

- **状态/算法唯一归属与真实消费者**：`src/modules/AF.Module.Social/Proactive/ProactiveRequestCooldownOwner.cs:10–132` 的 `ProactiveRequestCooldownOwner` 独占三张 ID→绝对天数表和 global/lastScan 小时状态，负责旧字典键规范化、扫描节流、Hero/需要类型/外交话题冷却判定与记录、过期/256 容量裁剪。`ProactiveNpcRequestBehavior.cs:107–153` 的原 SyncData/DTO 是兼容适配；`:410–453` 小时扫描、`:528–543` 增量扫描完成重验、`:1270–1279` Hero 候选、`:2668–2675` 外交候选、`:4443–4447` 请求启动、`:4730–4773` 完成/会面/信件送达疲劳是真实调用者。MCM 读取、游戏时间/Hero/party、候选资格和主 session 仍在原 host。Courier `RecordLetterNeedDeliveredForExternal` 仍只记录需要类型疲劳，不声称 Issue 已受理。
- **兼容与成本**：保留 `_af_proactive_npc_request_state_v1`、原 `ProactiveNpcRequestStorage` 字段及旧 `NeedCooldownUntilDays` 回退，保存中增量扫描的 `LastScanHour=-99999f` 重试语义和载入失败只清三表的原规则。原顺序仍是启用/时间间隔门→更新 lastScan→两次过期裁剪→active/global/busy 门；global 冷却在扫描完成前再核对。热候选冷却查询为字典 O(1)，过期裁剪 O(n) 只在通过小时扫描间隔门时执行，话题超 256 后排序裁剪沿旧规则；不新增每帧全表扫描、反射、线程或双份状态。
- **反例与验证**：`tools/PhaseEightParityReplayTests/ProactiveCooldownOwnerReplay.cs:5–63` 对当前生产 DLL 验证脱离式旧键导入、大小写、边界相等、扫描重入、global/Hero/类型/话题记录、到期裁剪/256 上限/载入失败清表；先在旧候选红灯 `production host does not own a single cooldown state`，接线后 Debug 1.4 候选 SHA256 `D8C3AC41C447D13CE6DB3FA2D6F270C4CF811D3BB3EE6E61E736A8394E9A51D2` 的完整 Phase8 通过。经先前明确授权逐路径核实，原脚本无 `-Stage/-Deploy` 跑 Debug/Release × 1.3/1.4+Bootstrap 六构建，均 0 warning/0 error；四实现 DLL SHA256：Debug 1.3 `B1A231AE72080C86ACC25F96E68AAAE30DECF01E4DBAA7C55DB66ACB40C2E86E`，Release 1.3 `D985AF103ABEC0D062F3BB30A9ECD8BF38135683B3F41EC0668B46B4BC894113`，Release 1.4 `3CE30F3D0DBA433A7D8DB33419ACFD635E76A686766C23BA8C86FD3A2E18F8E8`。V1 119/四 DLL metadata 1060、PersistenceIdentity 142 key/type 对/36 behaviors、source inventory 7 tests、[代码地图](architecture/af-framework-code-map.json) 539 锚点 recorded/working-tree 均通过。
- **未满足的门禁/下一动作**：本片不是完整主动请求资格/主 session owner，更不是 Issue offer/in-progress/turn-in/完成回执闭包；继续从 `ProactiveNpcRequestBehavior.cs:410` 小时资格→`:528` 扫描完成→`:4288` session 启动/失效/取消的实际调用链读入并迁移完整状态转换，再独立完成 Issue 四类职责。真实 Campaign 小时触发与随机资格、Courier 信件、Quest/party-screen、旧档、provider、UI/音频、帧性能仍 `NOT-RUN`；旧测试基建独立问题不因本片通过而豁免。`.dotnet-cli-home/` 未跟踪且未触碰；需撤销以定向 inverse/revert，不 reset/改历史。

## 以下为 d2 前一切片记录（历史）

## 主体 J13d2 主动会话／原版任务派遣生命周期首片（2026-09-25）

最新请求恢复按[既定 J13 计划](plans/j13-domain-owners-plan.md)推进；当前分支 `codex/af-main-refactor-continuation-20260831`，产品与回放提交 `580a1466aa22581ce7ea5c0d9b7748355698751d`，起点 `0c13ee56`，此前 d2 意图 `3cf846bb`。状态：`J13a/J13b/J13c/J13d1_OFFLINE_VERIFIED`，**本 d2 生命周期切片有限 `OFFLINE_VERIFIED`，J13d2/J13 仍 `ACTIVE`**；d3/d4、e、f、g 未施工。不把本片 fixture 当真实任务受理/旧档或实机验收；本轮未推送、Stage、部署、打包、改自动化、写游戏/外仓或进入 J14。

- **Social 实际 owner/消费者**：`src/modules/AF.Module.Social/Proactive/ProactiveOpeningOwner.cs:8–94` 的 `ProactiveOpeningOwner` 独占运行时 Native/Scene pending，按现有 session ID＋Hero ID 匹配、同会话单次消费、换渠道淘汰旧 opening、取消/载入清理。`ProactiveNpcRequestBehavior.cs:4652–4728,4795–4808,5138–5153` 的真实会面/对话路由仍组装原 AFEF/prompt，调用 owner 并在消费后沿旧路径结算 Hero cooldown 与追逐队伍；旧存档 active session 缺 ID 时只补运行身份。`NpcInitiatedOpeningRouter.cs:10–27`、`ShoutBehavior.cs:2662–2674,26308` 和 `LordEncounterBehavior.cs:5959,5992` 的现有消费者入口身份未改。保存键 `_af_proactive_npc_request_state_v1`、既有 session DTO、Courier 单独的送达疲劳语义保持。
- **Issue 实际 owner/消费者**：`src/modules/AF.Module.Issue/Dispatch/IssueAlternativeDispatchOwner.cs:8–46` 独占同伴派兵窗口的唯一 pending 身份。`VanillaIssueOfferBridge.cs:608–744` 的真实 `PartyScreenHelper.OpenScreenAsQuest` 三回调捕获精确 pending；迟到的旧窗口不能清除/操作新窗口，取消与兵种不合格沿旧 roster 恢复；确认前再次核对 giver 当前仍持有同一 offerable issue，失效不会写成功事实，立即派遣返回真实成败。`VanillaIssuePromptBehavior.cs:11–29` 构造及读档清理静态运行时 pending；无新增存档键或反射注册。原 `TryGetRuntimeState` `VanillaIssueOfferBridge.cs:267–287`、`TryAcceptIssueSelf` `:555–606`、`TryTurnInIssue` `:1281–1301`、原版 quest/party-screen/完成事实 host 仍在原处，不将本片误称完整 Issue owner。
- **离线证据**：经明确授权、逐目录核实在本工作区且无 reparse point，原 `一键编译覆盖推送/build_single_module.ps1` 不带 `-Stage/-Deploy` 对 Debug/Release 各完成 1.3、1.4、Bootstrap，共六项 `0 warning / 0 error`；引用为 `v1.3.15.110062` 与 `v1.4.7.117484`。Debug 1.4 候选 SHA256 `DDFBE3B5BE81CCED67D110524EE01F9AADDAC273D5974184E1367A3D49401300` 的 `PhaseEightParityReplayTests` 全通过，其中新增生产 DLL 回放覆盖会话/Hero/渠道单次消费、取消、旧 session ID、同伴派遣重复/迟到/载入及缺 Issue 无假成功。四实现 DLL SHA256：Debug 1.3 `AB8CFBF835C2D60FB4B16A570C0F9BCE71AA5D2C163F27D5479DED27AD5BCA40`、Release 1.3 `AD03C76C57A3B7C910D596FB61DE29E33B6D79F5CFE65B3D394627CD08D04065`、Release 1.4 `9AA843A337ECAF7A83F60E806A294C594C4CADF50C70BA5ADC2B2CB1DC24320B`；V1 119、四 DLL metadata 1060、PersistenceIdentity 对 `053ad485` 的 142 key/type 对与 36 behaviors、source inventory 7 tests 均通过。[代码地图](architecture/af-framework-code-map.json) 531 锚点 recorded/working-tree 均通过，仅证明坐标。
- **失败诊断/证据层级**：首次 Phase8 因本地 1.4.7 测试引用缺 `StbSharp.dll` 被严格拒绝；核对同版游戏文件后按本次授权只补工作区忽略的三项依赖、替换 runner 的旧 1.4.6 复制 DLL，保留其日志/CustomPrompts，随后通过。首次 API runner 用系统 SDK 10 因缺 net8 `8.0.30` targeting pack 报 `NU1100`；改用仓库已核实 `local/dotnet/8.0.425/dotnet.exe` 后通过。没有通过改脚本、放宽断言或借旧候选计成功。
- **成本与保留风险**：新 pending/派遣状态每次会面、对话或任务窗口事件 O(1)；不新增小时扫描、tick 轮询、全量 Hero/任务遍历、后台访问或缓存复制。原 `TryStartNewRequest` `ProactiveNpcRequestBehavior.cs:417`、候选资格 `:783`、主 session/冷却 `:4296,4750+` 与 Issue offer/in-progress/turn-in/完成回执算法仍是 d2 后续责任；`VanillaIssuePromptBehavior.cs:31–38` 的 recent-completion 查询原本返回 false，本片未擅自启用，`:40+` 仍沿原 AFEF 完成事实入口。真实 Campaign 小时触发、Courier 送达/受理区分、原版窗口、兵员转移/奖励、真实 Quest 事件顺序、旧档、provider/UI/帧性能均 `NOT-RUN`。下一切片先核对并迁移主动资格/冷却/会话主状态，再独立关闭 Issue offer/in-progress/turn-in/完成回执；每片继续定向负例与双版构建，不提前标 d2 完成。

## 以下为先前暂停交付记录（历史；其停工/推送授权已被最新请求取代）

## 主体 J13 暂停交付：d1 已有限离线收口，d2 尚未实施（2026-09-25）

用户最新要求暂停开发、推送本次主体重构并更新主体 HANDOFF，明确排除另一外交任务及其 HANDOFF。本次授权覆盖下列历史时点的“不推送、不更新 HANDOFF”；其余无 Stage/部署/打包/游戏或存档写入/J14 的限制不变。状态为 `J13a/J13b/J13c/J13d1_OFFLINE_VERIFIED / J13d2_PAUSED / J13_PAUSED`，不再继续施工。

- 产品仍为 `eb641aaa`，证据提交 `6281ec6f`，停点 `3cf846bb` 仅空意图与随后只读调查，没有 d2 产品/测试改动。下一项仍是下文所列 Proactive/Issue；d2–d4、e、f、g 未完成。最新产品验证、真实消费者/保留 host、源码坐标及回滚依据沿用各切片记录，本次仅更新交接文档，不重跑产品构建。
- 本地工作树 `.wt/diplomacy-latest-20260925` / 分支 `codex/diplomacy-refactor-20260925` 的 24 个待推送提交，已逐提交核对名称与路径；从 `053ad48502f49c96753ece20be4cc25006fcb3ae` 到 `3cf846bb` 无合并提交、无另一外交任务或其 HANDOFF 增量。目标仅 `origin/codex/af-main-refactor-continuation-20260831`，采用当前 HEAD 的明确 refspec 普通快进推送；不能误推另一个工作树里同名的本地分支。
- 外交工作树 `.wt/af-continuation` 的 HEAD `6d5e0331687679fe3e9c01aa127cfc7e32bb0480` 不在本次 HEAD 的祖先中，其已提交与未提交改动及 HANDOFF 均原样保留。主 checkout 及其他工作树不参与交付。
- [主体 HANDOFF](../HANDOFF.md) 为当前简明入口；代码地图 sourceRevision 仍为 `eb641aaa`，522 锚点定位证据不变。最新 Release/四 DLL 综合门禁仍待 J13g，真实游戏/旧档/provider/UI/音频/帧性能 NOT-RUN，旧聚合器失败没有被本次交付豁免。实际推送完成以远端 ref 校验结果为准。

## 以下为历史切片记录，其状态及授权只代表各自时点

## J13d1 Recruitment 与升格人设离线收口，进入 d2（2026-09-25）

状态 `J13d1_OFFLINE_VERIFIED / J13d2_ACTIVE / J13_ACTIVE`；意图 `855dde0e`，产品 `eb641aaa`。d1 的 Notoriety/Romance 证据见紧邻旧段，仍保留所列 host 与未验证范围；本段补齐此前 c/d1 留下的升格生命周期。无推送、HANDOFF 更新、Stage/部署/打包/游戏存档写入/J14。

- `RecruitmentOwner` 实际接走 Hero 入队、non-Hero 普通入队/酒馆池/升格分流、升格事务的原算法和执行顺序；原 Reward 公开/标签入口保留窄包装。该 owner 是同步主线程应用协调，不保存 live 对象，也不是游戏 API 全隔离完成：现有家族/配偶身份、继承/领地、俘虏、原队伍清理、Agent 外观装备/Native token、事实记录仍经原 host helpers 与 TW API 执行。`ApplyRewardTags` 仍有 mixed-domain 消费者，不删除、不重复 Economy 转移。
- `MyBehavior.PromotedPersonaGeneration.cs` 将原独立升格 profile→fallback→skills 接回 Persona 唯一预约 owner 与现有有预算主线程 dispatcher。游戏对象、配置、prompt 捕获、profile/fallback、技能写入和失败 UI 全在主线程；后台只等待已启动传输。每阶段重验 owner/generation/lease/同一 Hero，profile 对比初始两字段，skills 对比请求前原 18 项技能摘要及人设；期间编辑、读档、换 owner/target、清理、重复请求均拒绝旧写入。lease 覆盖两个请求，普通生成人设不能同时占用同一 Hero；旧完成不能释放新预约。原 prompt、route、失败文案、本地后备、voice 和技能 parser 保持。
- 验证：既有 Hero harness 加入真实升格实现/dispatcher，269 checks 零失败，三渠道 consumer 169 零失败；网络/parser/技能底层为 fixture，未调用 provider。绕过升格主线程 commit 的 mutant 产生 20 个行为失败。`verify_j13_recruitment.py --revision eb641aaa` 对前驱 `a49642bf` 的完整 Reward host 与三个搬迁算法作精确逆变换、MyBehavior 仅移出三方法、所有原 prompt/route/fallback 字符串保持；不拿 null fixture 证明实机正向招募。当前 DLL public entry 回放缺 Hero/non-Hero、重复空调用、缺 Native 请求、无假事实通过。
- 当前 Debug1.4 SHA256 `87D9D6AD6472F457CC349BF300E1F33D6AC17D5A547BC6E1C4574D5FF90CD49B` 的 Phase8 全通过；Debug 1.3/1.4+Bootstrap 零警告错误，V1 119/两 DLL metadata 530、入口 11、PersistenceIdentity 142 key/type 对/36 behaviors 无差异。原历史 Persona 全仓 inverse 的先前失配仍保留；c 的精确证据需使用其记录 revision，不能拿现行新增安全逻辑声称与 c 前驱全文相同。Release/四 DLL 留 J13g 最终重建。
- 成本：入队是事件低频，沿用原 roster/家族遍历；升格每角色两个原请求，新增 O(1) lease/dispatcher，技能源比较固定 18 项，无新增 tick 全量扫描。同步捕获/本地 parse/commit 的实际帧耗时未测。真实家族/继承/俘虏/队伍转移与原版事件顺序、旧档/provider/UI、帧性能仍 NOT-RUN。
- 下一步 d2 Proactive/Issue：主动资格/冷却/会话/pending consumption，原版 offer/in-progress/turn-in/完成回执及同伴窗口迟到回调；保持 J10 Courier 语义。

## J13d1 Notoriety/Romance 切片验证，Recruitment 接续（2026-09-25）

状态 `J13d1_ACTIVE / J13_ACTIVE`；已验证产品 `e7a16ba7`（意图 `3252444d`）及 `a49642bf`（意图 `f7392390`）。J13a–c 有限离线完成。未推送/改 HANDOFF/部署/写游戏或存档，继续 Recruitment 与其后各包。

- Notoriety：`NotorietyObservationOwner` 接走唯一原 state graph/active 字典、观察者创建、会话判定缓存、近期信息渠道规则、有效名声计算、文化/世界累加、legacy 结算与低调状态变更。作为私有嵌套 owner 仅为保持原私有 JSON DTO 类型身份；不是只搬 partial 方法。根 Behavior 属性是同一状态的兼容视图，原 SyncData JSON/witness、Hero/Agent 资格、RNG/时钟、历史摘要、归一化/事件/UI 和精确 receipt journal 保留。原 NormalizeState 多次 O(N) 复制/排序仍存在，未声称本包解决全仓热路径；新增观察/缓存操作平均 O(1)，无新增 tick 扫描。
- Romance：`RomanceRelationshipOwner` 接走唯一 love 字典、一次性 marriage topic context、数值/年龄及指令/约束状态决策。宿主保留 Hero/Clan 资格读取、年龄权限例外、已授权标签执行规则、真实婚姻/家族/信任变更、原记录 wire 与存档键。love 保存用同类型局部 ref 接口回填唯一字典，不复制第二张表。
- 修复有据：原 context TryGetValue→TryRemove 允许并发重复消费，且静态表跨 Behavior 替换/读档残留。现在单次 TryRemove 消费，绑定 Behavior 实例，加载清理；缺 owner 不授予 topic context。原正常话题注入/一次消费顺序不变，未新增婚姻规则或默认入口。请求/对话时 O(1)，保存规范化 O(love 项数)，未引入轮询。
- 验证：Notoriety 当前 DLL host 重复结束一次、空会话不加分、冻结会话判定、文化增量限幅/世界 1/3、信使阈值/低调清理/加载状态；原 AFNR1 14/14 与原 DLL metadata/IL 接线回放通过。Romance 当前 DLL 32 并发只有一次成功消费、禁用/读档清理/换 owner、love 饱和/归一化、年龄/权限例外与资格状态矩阵通过。合并当前 Debug 1.4 SHA256 `084E5361B935D554F1DBE7ECB4499B6962E9A6F76BB4C7C10A4AACD93226F771` 全 Phase8、Debug 双版本+Bootstrap 零警告错误、V1 119/两 DLL metadata 530 通过；入口 11/source 7 通过于 Notoriety 切片。
- PersistenceIdentity 原字段扫描不识别局部 SyncData alias，补齐局部类型声明（与既有 Profile validator 一致），保留 baseline 双向比较；新增 local/field 等价、变更类型失败、return 不作声明反例，工具 6 tests 通过。相同基线 `053ad485` 比较 142 key/type 对与 36 behaviors 无差异；原 146 是包含 4 个重复 key 的 UNRESOLVED 类型记录，不是删除保存键。原 JSON DTO 和 love 的 Dictionary<string,int> 保持。
- 未验证：真实 Campaign Hero/Agent 资格与 MBRandom、真正婚姻/家族/信任 mutation、旧档、UI/provider/帧性能 NOT-RUN。Notoriety 历史摘要未迁移且不以本次 owner 回放代替其异步验收。Recruitment（含升格 Persona/skills 主线程与编辑保护）尚未完成，故 d1/d/J13 不标完成。

## J13c Persona 有限离线收口，转入 J13d（2026-09-25）

状态 `J13a/J13b/J13c_OFFLINE_VERIFIED / J13d_ACTIVE / J13_ACTIVE`；意图 `97882c5a`，产品 `877ba4a9`。本段是最新状态；继续所有后续包，不推送、不更新 HANDOFF、不 Stage/部署/打包/写存档/J14。

- 原唯一 `NpcPersonaGenerationOwner` 原样归位 `AF.Module.Persona/Generation`，namespace/预约/冷却/lease 不变；`NpcPersonaProfilePolicy` 实际拥有 readiness、生成文本标准化、缺字段补齐和 reroll 编辑冲突合并。`MyBehavior.PersonaGeneration.cs`、`MyBehavior.PersonaReadiness.cs` 和原标准化入口真实消费。主线程 capture/dispatcher/profile/voice、三渠道 preparation、百科/外部入口、存档键保持。
- 证据：Hero 128、三渠道 169 检查零失败；ignore_edit 负向变异触发 4 个具名行为失败。`j13_source_parity.py --revision 877ba4a9` 对立即前驱 `89b38557` 的完整 host/helper/readiness 作精确逆变换，证明原预约 owner 和升格/skills/未命名/editor/Reward 调用者未被额外改写；未删除旧逆变换。旧全仓 inverse 分别在此前已有 `MemorySummaryRunOwnerTests/fixture_support.py` 和 `GameLifetimeTests/run_bindings.py` hash 漂移处失败，本次未改这两文件；不冒称旧聚合器 PASS。
- 原 Debug 1.3/1.4/Bootstrap 三构建零警告错误；当前 Debug1.4 SHA256 `07EEEF4EB22A09B7D1D1E5DF2511E76D9AAC1CF547CA3788BB34B8E0CFCF600C` Phase8 通过，V1 119/两 DLL metadata 530，PersistenceIdentity 146 keys/36 behaviors，inventory 11/source 7 通过。Release/四 DLL 留 J13g 统一最终候选。规则只在准备/请求/提交时运行，O(文本长度)，预约仍单一有界 owner，无 tick 扫描。
- 保留边界：`GeneratePromotedNonHeroCompanionProfileAsync`/`GeneratePromotedNonHeroCompanionSkillsAsync` 是独立升格 profile→fallback→skills 流程，只有原 generation 检查；await 后 live Hero/profile/UI 访问及中途编辑安全未由普通 Hero fixture 证明。它的现有算法未迁移，明确在接下来的 J13d1 Recruitment 核对并处理，不标该路径线程安全。未命名角色/技能随机/游戏对象写入继续原 host。
- 真正 provider、Campaign/旧档、百科点击/音频/帧性能 NOT-RUN。下一步 d1 按 Notoriety→Romance→Recruitment 独立闭包，再 d2–d4。

<a id="j13-plan-20260924"></a>
## J13b Kingdom 有限离线收口，转入 J13c（2026-09-25）

状态 `J13a/J13b_OFFLINE_VERIFIED / J13c_ACTIVE / J13_ACTIVE`；意图 `64284105`，产品 `89b38557`。从 J13a 继续，遵守不推送、不更新 HANDOFF、不 Stage/部署/打包/写存档/J14。

- `KingdomStabilityPolicy` 实际拥有数值上下界、分档、关系目标、皇家直辖地忠诚度、周度平衡与叛乱概率；`KingdomStabilityOwner` 拥有原三份状态字典及关系对账/替换撤销、周报 delta 去重。`MyBehavior` 原同名私有成员改为指向唯一字典的保存兼容属性；保存字符串字典/SyncData 键、原模型/百科/公开入口不变。关系饱和只记真实 applied 值，撤销不抹除同期其他变化。
- `KingdomMaintenanceOwner<Kingdom>` 持有逐王国关系游标和一次捕获的周度列表/游标；`AutomaticKingdomRebellionOwner<原私有context>` 拥有 FIFO、命名在途/就绪和 Weekly 阻塞状态。真实 `ProcessWeeklyKingdomRebellionsSlice`、自动命名/重试/消费/取消、`TryStartDeferredAutoWeeklyReports` 已接入；游戏对象仍只在原主线程读写。原叛乱候选资格/随机选择、家族/领地动作、命名 Prompt/gateway、弹窗、MCM/玩家免疫判断仍由现有 host 执行，未重写 J12 或制作组业务。
- 具体修复：原自动命名主线程回调只验存档代际，取消后若同代回包会无条件重新置 Ready。现在 BeginNaming 返回请求版本，Cancel 使它失效，旧结果不能恢复已取消流程或占用新请求；当前 owner/generation 仍由原 main-thread action pump 校验。回放覆盖 canceled/result、替换请求/旧结果、duplicate completion；不宣称真实 provider 回调时序已实测。
- 验证：同一 owner 源码用例及当前 Debug 1.4 DLL（SHA256 `5B9DE7C3E61BD4C95FE8AC0652AB948DC854C2486F74D02CCAF058F104E643DC`）Phase8 通过；包括忠诚度/稳定度边界、关系饱和/重复/撤销/缺对象、周 delta、列表缩减/异常不跳游标/加载重置、已保存周不重跑、队列阻塞/恢复/取消。原 Debug 1.3+1.4+Bootstrap 三构建零警告错误，V1 119 / 两 DLL metadata 530、PersistenceIdentity 146 keys/36 behaviors、入口 11/source 7 通过。Release 四实现最终统一在 J13g 重建；本包必要双版本已通过。
- 性能：规则 O(1)，FIFO 出队由列表前删改为 Queue O(1)；关系 reconcile 仍一次遍历已记录 offsets，原每个关系 slice 的 Kingdom 列表捕获和每王国成员枚举保留，未声称每 tick 常数成本。周列表每周一次，单步一个王国；主线程资格/Clan mutation、实机模型效果、真实叛乱和旧档读取/帧性能 NOT-RUN。
- 下一步 J13c Persona：归位原唯一预约 owner，转移生成合并/编辑冲突/准备状态决策，保持编辑器/三渠道原契约；单独核对升格同伴与非 Hero 路径，不用 Hero fixture 代替它们。


## J13a Weekly 有限离线收口，转入 J13b（2026-09-25）

状态 `J13a_OFFLINE_VERIFIED / J13b_ACTIVE / J13_ACTIVE`，意图 `be50b6c6`、恢复回放 `c637bc82`、a3 产品 `8f960d65`。下列旧段落记录各自当时的 ACTIVE，当前由本段和恢复组合段取代；a1–a3 的必要离线退出门已满足，直接进入 Kingdom，不再围绕 Weekly helper 增加独立切片。不更新 HANDOFF、不推送，仍无 Stage/部署/打包/游戏或存档写入。

- 真实 owner：`src/modules/AF.Module.Weekly/Receipts/WeeklyMemoryMaterialOutcomeReceipt.cs` 保持原 namespace、枚举、fingerprint v1、wire/checksum、64 pending/512 terminal 容量和原全部状态迁移；`Publication/WeeklyActionOutcomePublicationOwner.cs` 接走唯一 ledger、确认导入、保存代际、work flag、5 秒 retry deadline 和 due selection。`MyBehavior.WeeklyActionOutcomeReceipts.cs` 原静态 prepare/complete/publish 入口、SaveData 字典键 `_af_weeklyActionOutcomeReceipts_v1`、游戏估值/草稿 exact-trigger/readback 写入仍留主线程适配。没有第二账本或由模型文本推断成功。
- 性能：原每次刷新/tick `GetEntries()` 对最多 576 条排序并分配列表，改为 O(N) 无分配 FirstConfirmed，保持 CreatedUtcTicks/Ordinal ReceiptId 先后；无工作/未到期为 O(1)，仍每 tick 至多尝试一份材料。加载/导出保持原冷路径排序和容量/原子导入，坏 journal 原文仍由 host 保留，后续保存不覆盖它。
- 验证：原一键脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `16DD726D8C7B494A5DBE8156963011FC2BBDE029D4D134D762B8CB8FA10AA980` 的 Phase8（新增原 WeeklyActionOutcomeProductionReplay 直接复用）通过。原材料回执契约 + publication 激活/重试/代际/坏导入/加载/仅一次/稳定排序断言通过；Economy executor、三渠道 InteractionPipeline、DuelDispatch 16、schedule net6、PersistenceChunk、PersistenceIdentity（基线 053ad485，146 keys/36 behaviors）、Migration 10 通过。V1 119 / 四 DLL metadata 1060、入口清单 11/source inventory 7 通过；清单补入此前遗漏的 wave owner 及本次两个文件，不升级历史覆盖状态。
- 限定：旧 `MemorySummaryMainThreadBoundaryTests/run_terminal.py` 在进入本次业务前因此前移除的 `TagSceneSessionHistoryLine` 提取失败；已随 owner 更新源码路径/harness 字段，但没有删旧断言或冒称该历史聚合器通过。与本次直接相关的 ledger、publication lifecycle、生产接线/trigger sanitizer 与跨域三渠道契约均已通过。本轮未改草稿写入算法。真实 Campaign/Mission、旧 SAVE、provider、UI 渲染、60 秒墙钟与帧性能仍 NOT-RUN。
- 下一包 J13b：核对稳定度/关系偏移/皇家领地忠诚度与周度叛乱的字段、模型消费者和主线程变更，将规则与批次协调归 Kingdom；不改 J12 外交或制作组规则。


## J13a a2 恢复组合与实际工作量（2026-09-25）

本轮新增 `tools/PhaseEightParityReplayTests/WeeklyReportRecoveryReplay.cs`，由既有 Phase8 入口消费当前 `02812536` 产品候选（Debug 1.4 SHA256 `B8FB17208EF267D6A1A252CBF525B522B0A03C25783D23847F3E5362AAD195F4`）。真实部分 commit 保留已发布王国胜出者、为世界分组形成失败上下文；真实 `ShowWeeklyReportFailurePopup` 的 InquiryData 回调在显式点击后重采且仅选失败目标，重复旧按钮不替换新上下文。请求停在真实 prompt 队列并取消，随后以 detached 固定响应进入真实 capture/commit 边界；这不是完整网络端到端，也不是 Gauntlet 点击验收。成功 world product revision 只增加一次，重复 completion 不重写/重发。

- 当前候选 Phase8 全部通过（退出码 0）；产品未改，沿用同候选已通过的六构建与 metadata。fixture 修正了初次错误的空 capture 和关闭预算参数，未改产品或删断言。
- 工作量回放：5 个积压 context，每 context 1 条报告、97 项材料、4096 report 字符、196630 SnapshotText 字符；真实 pump 无丢失清空。已耗尽时间预算时游标不推进；材料逐项克隆。一次 tick 可以处理多个 context，没有固定记录/字符上限。排序初始化 O(M log M)、单项克隆/字符串标准化 O(字符数)、目标查询 O(已有记录数)、最终分组结算 O(目标数) 仍保留，不能由回调数或本机 fixture 耗时推导游戏帧上限。
- a2 有限离线门：既有自动/按需、强制异步、owner/generation、源修订、正常/部分/异常/清理证据，加本次真实 UI 回调/发布/积压，已满足当前离线组合范围。动态投影在材料/Prompt 主线程捕获；已注册 Kingdom/Clan 结构事件使快照失效。显示名/距离等现场投影与第三方绕过事件的直接写入不保证实时同步，保留捕获时语义；真实 Campaign/provider/60 秒墙钟/UI 渲染/旧档/帧性能 NOT-RUN。J13a a2 `OFFLINE_VERIFIED`（上述限定），a3 开始，J13a/J13 仍 ACTIVE。继续 a3→J13b–g；不更新 HANDOFF、不推送、不部署、不执行 J14。

## J13a a2 多波协调与入队竞态切片（2026-09-25）

状态 `J13a_A2_WAVE_ADMISSION_SLICES_OFFLINE_VERIFIED / J13a_A2_ACTIVE`。从 `053ad485` 接续；实际工作树为 `E:/Mount-Blade-Bannerlord-AnimusForge-mod-main/.wt/diplomacy-latest-20260925`，本地分支 `codex/diplomacy-refactor-20260925` 跟踪原目标远端分支。意图 `5f54b889` / `375a55ae`；生产与测试 `ce8413dd` / `02812536`。用户明确要求不推送、不更新 HANDOFF，二者均保持；没有 Stage、部署、打包、游戏/存档/外仓写入或 J14。

### 责任与实际修复

- `ce8413dd`：`src/modules/AF.Module.Weekly/Generation/WeeklyReportWaveCoordinator.cs:9–62` 接走分波游标、60 秒等待、跨波在途汇总和未发批次失败补齐；真实 `MyBehavior.cs:45694,45793–45810` 调用 `CoordinateWeeklyReportWavesAsync`，原启动队列、主线程 `ProcessPendingWeeklyWaveLaunches` 和最终 commit/source 门禁保留。保留波内/跨波并发、批次顺序和拒绝后已发结果；每个异步边界同时复核当前 owner，旧 host 不提交。
- `02812536`：旧候选真实 `EnqueueWeeklyWaveLaunchAsync` 在 owner 退役后仍入队，且无人 tick 时任务永不结算；`admission-old-red.log` 保留已编译运行后的具名失败，不是构建失败。`WeeklyReportCommitQueueOwner.cs:24–46` 新 `EnqueueIfCurrent` 与 `CancelAll` 共用原锁，锁内仅检查 owner 引用与 generation，拒绝时锁外立即结算。`MyBehavior.cs:43086,45815,45914,45997` 的 retry/preparation/wave/commit 四个真实入口接通，读档 advance→clear 与 worker 迟到入队的两种顺序均不遗留旧等待者；旧 completion 不能移除新代队首。
- 没有新增保存键、公开 ABI、游戏 API、第二队列或网络客户端；未改原重试次数、源规则、UI 成败含义和一键脚本。协调总工作/存储 O(批次数)，每波仅分配其批次引用列表；入队沿用一次锁并增加 O(1) 校验/一个低频委托，无 tick 新扫描、轮询或跨锁 continuation。单波请求启动、材料构建和单次 commit 的真实成本仍不是常数或帧保证。

### 当前候选证据

- 原脚本 `一键编译覆盖推送/build_single_module.ps1` 无 Stage/Deploy，Debug/Release × 1.3/1.4 + Bootstrap 六项 **0 warning/error**。本轮用户已明确批准当前工作树下 `bin/{Debug,Release}/single_module_artifacts`、`obj/single_module/{Debug,Release}` 四目录的创建/清理/重建；每次清理前核实绝对边界及无 reparse point。1.3 引用 `_deps_auto` 为 `v1.3.15.110062`，1.4 固定引用 `.tmp/build_check/1.4` 为 `v1.4.6.115628`；系统 SDK 8.0.424。游戏安装根仅作读取依赖来源，不作部署目标；1.3 overlay 未覆盖的库仍按原构建 fallback，不能据编译宣称全部 1.3 运行 API 已验。
- 当前 Debug 1.4 SHA256 `B8FB17208EF267D6A1A252CBF525B522B0A03C25783D23847F3E5362AAD195F4` 的 Phase8 显式候选/marker/新鲜度检查与全回放通过。新共享回放既运行 source-linked owner，也运行当前 DLL：三波重叠、稳定索引、60000ms 延迟端口、迟到结果/换 owner、后续波拒绝及全部未发结算。真实 host 协调→入队→pump 的两波链、同周源变化、队列清理与退役 prompt/commit 拒绝通过。
- `WeeklyReportQueueAdmissionReplay` 强制 generation advance 与锁内准入并发，验证先入队再清理、清理后迟到、新代可继续和旧 completion 隔离。新增 `tests/modules/AF.Module.Weekly/WaveCoordination/WaveCoordination.csproj` 仅 source-link 相同生产 owner 与相同回放，不复制算法。
- V1 **119** 与四实现 DLL metadata **1060** 通过；既有 WeeklyMemoryMaterialOutcomeContract、WeeklyReportSchedulePolicy 原 net6 runner、repository source inventory **7** 通过。schedule runner 只有原 NETSDK1138 警告。代码地图绑定 `02812536`，498 个锚点的 recorded/working-tree 两种验证通过；仅两个已改生产文件的既有 hash/位移更新，另增加六个责任/回放锚点。定位验证不冒充玩法验收。
- 复现入口：`dotnet run --project tests/modules/AF.Module.Weekly/WaveCoordination/WaveCoordination.csproj -c Release`；既有两个 Weekly 工具同名 csproj；`python -B tools/test_repository_source_inventory.py`；`python -X utf8 -B tools/ModuleFrameworkApiTests/run.py --dotnet "C:/Program Files/dotnet/dotnet.exe" --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts`；代码地图 verifier 默认与 `--working-tree`。完整构建/Phase8 参数及日志保留在本地 `artifacts/j13-wave-20260925/{run-build.ps1,run-phase8.ps1,admission-build-Debug.log,admission-build-Release.log,phase8-final.log,admission-api-final.log}`，不上传产物或日志。首轮 Phase8 有一次参数传递错误及一次测试局部变量重名编译错误，修正后通过，不计行为红例。

### 未覆盖与下一项

受控延迟端口校验 60000ms，实际协调和 pump 已运行；**没有实际等待 60 秒，也没有游戏 Campaign 事件/provider 成功请求或真实 UI 操作**。原部分提交/RPM/异常恢复测试与新多波测试分别通过，尚未组合成“部分提交＋UI 显式重新采集＋一次发布”完整闭环。下一项先补此组合和每次 commit 的 records/chars/积压工作量，再核对剩余动态源投影与自动/按需模式退出门；a2/J13a/J13 仍 ACTIVE，a3/J13b–g 不提前报完成。实机、旧 SAVE、provider、音频、游戏帧性能均 NOT-RUN。需要撤销时定向 inverse/revert `02812536` 后 `ce8413dd`，保留意图记录，不 reset/rebase。

以下为接续前的历史交接记录。

## J13 暂停与远端交接停点（2026-09-25）

用户要求暂停施工、写妥 `HANDOFF.md` 并将本工作分支交付到其已配置远端。中断发生在 a2 多波协调抽取的阅读/设计阶段，**没有**半途未提交的生产/测试改动；截至本段编写前 HEAD `66eda319`，工作树仅有原未跟踪 `.dotnet-cli-home/`，不纳入提交。当前状态保持 J13a a1 有限 `OFFLINE_VERIFIED`，a2/J13a/J13 `ACTIVE`，a3/J13b–g 未施工；不得因交接或推送标整体离线验收。最近产品 `78b87434` 的六构建及当前候选 Phase8/API/代码地图证据、回放 `c0221d02` 的边界见下方两节。实机、旧档、provider、音频、帧性能仍 `NOT-RUN`。

本次远端交付仅针对 `origin/codex/af-main-refactor-continuation-20260831` 的普通快进：推送前核对 `origin`、远端 tip、outgoing 提交/路径及排除物。历史本地专用提交 `ae8e6b89` 与聚焦逆提交 `3a57007d` 已在共同祖先 `0624d502` 之前且已存在于该远端分支；本次新增区间不包含这些提交，也不包含 `.dotnet-cli-home/`、PlayerExports、构建 DLL、ZIP 或日志。推送结果以实际远端 ref 回读为准。恢复工作时先完成 a2 的真实 minute orchestration/Campaign tick、部分提交＋UI 显式重采＋一次发布组合及积压/工作量门禁，再按原计划进入 a3 与 J13b–g；未授权 Stage、部署、打包、外仓写入或 J14。

## J13a a2 双排队波次与源失效回放切片（2026-09-25）

状态 `J13a_A2_WAVE_PUMP_REPLAY_VERIFIED / J13a_A2_ACTIVE`，回放 `c0221d02`，无产品变更。`tools/PhaseEightParityReplayTests/WeeklyReportCommitQueueReplay.cs:64–125` 直接调用真实 `MyBehavior.cs:45839–45903` 的入队与 Campaign 主线程波次 pump：两个已排队波次每次 pump 只处理队首，第一波保留原 batch index，未准备 Prompt 的 fixture 在网络前被拒；同周源修订后，第二波在网络前结算为空并清空队列。仅是受控双排队反例，**没有**穿过 `Task.Delay(60000)`、真实多 wave orchestration、Campaign 游戏事件或 provider，不能据此关闭 a2 的多波门禁。

- 验证：当前 Debug 1.4 候选 DLL SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 build marker 来源/新鲜度与 Phase8 全回放通过；产品未改，沿用前一切片已完成的原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。代码地图 492 锚点 recorded/working-tree 通过。此回放没有 Stage/Deploy。
- 未完：真实分钟延迟、Campaign tick/部分提交/UI 显式重采/一次发布组合、积压和提交工作量仍无闭合证据；a2/J13a/J13 `ACTIVE`，a3/J13b–g 未施工。实机、旧档、provider、音频、帧性能 `NOT-RUN`；`.dotnet-cli-home/` 未动，未推送、Stage、部署或打包。

## J13a a2 部分提交组合与异常恢复切片（2026-09-25）

状态 `J13a_A2_PARTIAL_EXCEPTION_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，回放 `0a1d6c8d`，产品与异常反例 `78b87434`。真实 `ProcessPendingWeeklyReportCommitContext` 消费者在已有完整周报胜出、另一王国批次 RPM 失败的组合下，只给失败目标建立原有 RPM 恢复上下文，不改已有胜出记录，也不重复发布通知。`MyBehavior.cs:46209–46303` 的提交异常路径现在按稳定 report ID 重查权威完整/短报记录，只对未完成目标生成 `RequiresFreshMaterials` 的显式重采上下文并结算等待者；`:1503,46341` 记录本轮尝试写入的目标，以便写入后、通知前异常时幂等补通知。异常恢复只在失败时 O(分组数 × 权威记录查找)，不新增每 tick 扫描；重采仍须用户显式触发，不静默重发请求。保留同 DLL、保存键、公开入口、原 UI/网关和双版本身份。

- 验证：四获准目录绝对路径、内容、无链接复核后，原脚本不带 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 DLL SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8 显式候选来源/新鲜度与真实部分提交、RPM 目标隔离、异常结算/仅失败目标新素材恢复回放通过；V1 119/四 DLL metadata 1060、入口 11/source 7、492 锚点代码地图 recorded/working-tree 通过。首次新增断言因 fixture 把泛型 `HashSet<string>` 强转非泛型 `ICollection` 失败，改读 `Count` 后用同一当前候选重跑通过；不是产品构建失败。代码证据：`MyBehavior.cs:1503,46065–46303,46341`、`tools/PhaseEightParityReplayTests/WeeklyReportCommitQueueReplay.cs:63–186`；覆盖结果/异常恢复，不覆盖 live Campaign tick、弹窗或 provider。
- 未完：多 wave/Campaign tick、真实 UI 显式重采与一次发布组合、队列积压/每次提交 record 与字符工作量、其他动态投影源重验未闭合。a2/J13a/J13 `ACTIVE`，a3/J13b–g 未施工；实机、旧档、provider、音频、帧性能 `NOT-RUN`。未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。

## J13a a2 批量失败恢复元数据切片（2026-09-25）

状态 `J13a_A2_FAILURE_METADATA_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `683987dd`。活动批量链的 `WeeklyReportBatchRequestResult` 已携带 RPM/配额/Retry-After/尝试次数，但原 `MyBehavior.cs` 的 pending commit finalizer 丢弃这些字段，固定生成 `AttemptsUsed=3` 的通用失败上下文，导致原 `ShowWeeklyReportFailurePopup` 的“修改RPM并重试”分支不能按真实错误触发。现 `MyBehavior.cs:43134,43183–43195` 每次 attempt 重新设置错误分类，HTTP 成功但后处理失败时不会沿用前一次 429；`MyBehavior.cs:46329,46350–46383` 在最终失败目标对应的批次执行结果中选取错误元数据，传入原 `CreateWeeklyReportRetryContext` 与 UI，不把另一失败王国的配额错误误配给当前 RPM 目标。未改请求次数上限、退避、route、保存身份或手动恢复入口。最终一次 O(批次数 × 每批分组数) 查找，不在每 tick 扫描历史。

- 验证：四获准目录绝对路径、内容及无链接复核后，原脚本不带 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 SHA256 `C22AC98C51B314330927FB848E80D9B2C9D80F75E01AE85C81BB9EE50187C7C4` 的 Phase8 显式候选通过真实失败目标批次匹配、RPM/Retry-After 到重试上下文、其他目标 quota 隔离和后续 HTTP 成功清除旧 429 标记回放；V1 119/四 DLL metadata 1060、入口 11/source 7、490 锚点地图 recorded/working-tree 通过。回放只覆盖结果映射与分类，不是 live 弹窗或 provider 请求。
- 未完：多 wave/Campaign tick、部分成功/失败 UI 和一次发布的组合回放未做；其他动态投影的源重验仍需核对，a2/J13a/J13 `ACTIVE`，a3/J13b–g 未施工。实机、旧档、provider、音频、帧性能 `NOT-RUN`；未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。

## J13a a2 王国拓扑事件源失效切片（2026-09-25）

状态 `J13a_A2_KINGDOM_EVENT_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `dc2dd917`。复核 `MyBehavior.cs:37253–37279,44698–44791` 可见 Weekly 分组/Prompt 会读取当前王国资格、英雄归属和统治者上下文；这些 live 投影发生变化时不一定产出目标周界内的素材，前一切片的逐日版本可能漏判。因此已注册的 `OnClanChangedKingdom`、`OnClanDefected`、`OnRulingClanChanged`、`OnClanLeaderChanged`、`OnKingdomDestroyed`、`OnClanDestroyed` 在 `MyBehavior.cs:3993,4084,4166,4398,7134,7210` 先使同一修订 owner 的全局版本失效，再沿原事件处理；无被跟踪 Lord/无新增同周素材时也会拒绝请求期间捕获的旧投影。事件触发 O(1)，不增加 tick 扫描；可能保守取消与目标王国无关的在途周报，这是刻意的安全侧失效，用户仍可通过已有显式重采恢复。未改变原事件订阅、玩法、存档/公开身份或 API route。

- 验证：四获准目录绝对路径、内容与无链接复核后，原脚本不带 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 SHA256 `2D1E29B437B67FF955BF2511A3749B3FB8CFC54B46E0723F65FBAA746294C942` 的 Phase8 显式候选、空王国消亡事件不产生日素材仍使快照失效的真实回调回放、V1 119/四 DLL metadata 1060、入口 11/source 7、487 锚点地图 recorded/working-tree 通过。
- 未完：这只覆盖已注册并接线的王国/家族变化事件，不能证明所有 live 名称、位置/邻近、未注册的第三方直接状态修改；多 wave/Campaign tick、部分失败 UI 与一次发布组合仍未验收。a2/J13a/J13 `ACTIVE`，a3/J13b–g 未施工；实机、旧档、provider、音频、帧性能 `NOT-RUN`，未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。下方 `d4443bcb` 的“live Kingdom 尚未覆盖”是该旧切片时点状态，由此处部分补充，不据此宣称整体源状态已闭合。

## J13a a2 周报源素材修订门禁切片（2026-09-25）

状态 `J13a_A2_SOURCE_REVISION_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `d4443bcb`。`src/modules/AF.Module.Weekly/Generation/WeeklyReportMaterialRevisionOwner.cs:6–63` 持有非存档的逐日、全局淘汰/合并、开局概要修订；真实写入点包括 `MyBehavior.cs:13687,13707,15504–15529,25587,25846,29626–29646,35570,35666,36131,37033,47186–47187,53084,53138`。自动分阶段、同步自动及手动预览在材料构造前捕获周界，`MyBehavior.cs:6127,6394,45591,45630–45632` 将快照贯穿准备、minute wave、重试与 pending commit；`MyBehavior.cs:45846–45863,46060,46105,46219–46241` 在主线程每波发起前、解析 block 和写入前重验，源变更时旧结果不覆盖，失败组走已有显式重新采集入口。已发波次保留其结果结算；未发波次补失败结果供最终目标结算，避免遗失等待者。原目标胜出者、存档键、公开接口及单一 DLL 身份保留。捕获/重验 O(周天数)，写入标记 O(1)；淘汰/合并采用保守全局失效，无每 tick 全 Hero 扫描。

- 验证：四个获准构建目录的绝对路径、内容及无链接复核后，原脚本不带 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 SHA256 `1560FCA28974CEE714465BCF8B042BC8777B5A012B30625D2F4DAC5F683799E5` 的 Phase8 显式候选来源/新鲜度与跨周不误杀、同周变更、开局概要、全局淘汰及真实 block 写入前拒绝回放通过；V1 119/四 DLL metadata 1060、入口清单 11、source inventory 7、484 锚点地图 recorded/working-tree 通过。回放未驱动真实 Campaign tick、provider 或弹窗。
- 未完：修订 owner 只覆盖已定位的事件源、NPC 行动、开局概要写入；live Kingdom/统治者资格等动态投影尚无同等级修订证明。多 wave/Campaign tick、部分成功/失败 UI、一次发布与积压成本的组合证据不足，故 a2/J13a/J13 仍 `ACTIVE`，a3/J13b–g 未施工。实机、旧档、provider、音频、帧性能 `NOT-RUN`；未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。

## J13a a2 跨批次部分结果目标结算切片（2026-09-24）

状态 `J13a_A2_PARTIAL_TARGET_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `d6fea0e6`。源码复核发现原 `FinalizePendingWeeklyReportCommitBatch` 会对同一 `MissingReportIds` 重复项及跨批次暂缺立即累加失败，而稍后其他批次解析成功时不会撤销早先失败，导致计数/重试目标失真。`src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitTargetOwner.cs:6–40` 现在拥有本次 commit 的大小写不敏感已结算 ID 与待恢复缺失目标；真实 `MyBehavior.cs:1487,46018–46055,46170–46217` 在每批只按稳定 ID 登记暂缺、后续成功/冲突结算时移除它，最终只对仍未恢复的目标各计一次失败与对应原因。已解析 block 的原目标记录/胜出者门禁、主线程写入、通知和存档/公开身份不变。owner 平均 O(1) 去重/结算，最终 O(尚未恢复目标数)，不增加每 tick 全量来源扫描。

- 验证：四获准构建目录路径、内容与递归链接复核后，原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 SHA256 `3587CB727BBE95A2B5F3B3CD4289165B41C0204288A6280937B2CC9AB3BAFCF4` 的 Phase8 显式候选通过：重复缺失、跨批次晚成功、成功后迟到缺失、未恢复目标及真实 batch finalizer 去重；V1 119/四 DLL metadata 1060、Phase8 入口 11、source inventory 7、477 锚点地图 recorded/working-tree 通过。入口清单首次因新 owner 未登记而失败，补入原 `social-progression-reports` 分类后重跑通过；未改其 ownerAssignmentState/entryCoverage。
- 未完：此切片不证明 live 材料源提交重验，也未完整驱动多 wave/Campaign tick/部分结果 UI 与一次发布；a2/J13a/J13 仍 ACTIVE，a3 与 J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。

## J13a a2 批量自动重试的配置读取线程边界（2026-09-24）

状态 `J13a_A2_RETRY_ATTEMPT_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `4721460f`。`MyBehavior.cs:43022–43047,43071` 的真实批量重试消费者保留首轮同波并发直发；第二、三次 attempt 无论 continuation 落在哪个线程都先进入 `_weeklyBatchApiAttemptQueue`，由 Campaign tick `:17509–17514,45813–45838` 在当前 owner、generation 和主线程验证后调用原 `CallWeeklyReportApiDetailed`，因此 `DuelSettings.GetSettings`、route、max tokens、temperature、thinking 等可变配置仍在每次 attempt 开始时读取，但不在 `Task.Delay` 后的后台线程读取。没有把密钥放入队列 DTO/日志或更改 J08 gateway。读档 `:2552` 清空未启动 attempt 并结算等待者；退役 owner/旧代在排队前拒绝。队列无工作时是 volatile 快路径，每 tick 最多启动一项；密集重试可能积压，尚无帧耗时或真实 provider 数据。

- 验证：四获准目录绝对路径、内容与递归链接检查通过；原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 SHA256 `47452DC940B1E6D4199E6449928088A8303D06BDDDF83D8B934F49F2F0145369` 的 Phase8 显式候选及强制后台排队、清理结算、旧代/退役 owner 网络前拒绝回放通过；入口 11、source inventory 7、V1 119/四 DLL metadata 1060、472 锚点地图 recorded/working-tree 通过。首次回放失败是测试宿主的默认 `TWParallel` driver 将所有线程均报告为主线程；改为显式首轮/重试区分后重新构建与回放通过，未删失败断言。
- 未完：真实多 wave/Campaign tick/provider 组合、独立 live 材料源状态、部分成功/失败/popup/一次发布和积压成本尚未闭合；a2/J13a/J13 仍 ACTIVE，a3 与 J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。

## J13a a2 minute wave 主线程发布切片（2026-09-24）

状态 `J13a_A2_WAVE_LAUNCH_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `5ca5e5c3`。真实批量消费者 `MyBehavior.cs:45501–45593` 仍按原 `burstSize` 和 `Task.Delay(60000)` 分波，但每波经 `EnqueueWeeklyWaveLaunchAsync` 排入复用 `WeeklyReportCommitQueueOwner` 的 `_weeklyWaveLaunchQueue`；`OnCampaignTick` `:17487–17492` 的 `ProcessPendingWeeklyWaveLaunches` `:45718–45759` 在主线程发 UI 通知、记录波次并启动该波请求。读档清理 `:2537` 结算未发波次等待者，旧代/换 owner 在发布前拒绝；完成源使用异步 continuation，避免同步回调在 tick 内继续下一波。原完成任务汇总、批次索引、三次重试、API route 和保存/公开身份不变。每 tick 至多启动一波，单波请求启动仍为 O(该波批次数)，没有帧耗时上界。

- 验证：四获准构建目录绝对路径、内容、递归链接检查通过；原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 DLL SHA256 `114EFBCB2B1053BEA585E06AE99A8BE99F79B0CFE04DFAB7D4466D0747960288` 作为显式候选通过 Phase8 来源/新鲜度/哈希及原队列 FIFO、旧 context/新队首和清理回放；入口 11、source inventory 7、V1 119/四 DLL metadata 1060、469 锚点代码地图 recorded/working-tree 通过。此处未用假时钟完整驱动多 wave/Campaign tick，真实分钟节奏仍待验证。
- 未完：自动重试在延迟后重新读取可变 API 配置的主线程边界、独立 live 材料源提交重验、部分成功/失败/popup/一次发布及积压成本尚未闭合；a2/J13a/J13 仍 ACTIVE，a3 与 J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，`.dotnet-cli-home/` 未动。

## J13a a2 批量自动重试旧代请求拦截切片（2026-09-24）

状态 `J13a_A2_STALE_RETRY_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `39606755`。`MyBehavior.cs:42966–43059,45553` 将请求开始时的 `SaveRuntimeGuard` generation 传入每个批次执行与最多三次自动重试；每次尝试发 API 前、每次回包后均复核原代。读档/换代期间已在途请求的完成仍由原结果链清理，**不会**在延迟结束后向新存档再发送旧批次自动重试。未改原每分钟 wave、1200ms/限流/Retry-After 间隔、API route 或部分结果解析；`#if false` 历史路径不作为活动消费者。

- 验证：四个获准构建目录绝对路径/内容/链接复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 SHA256 `9A6376610A6CE7223AEEDC8FE509B69606A1FFF69ED54FDDF334BF7627C2D303` 的 Phase8 当前候选与“已准备 Prompt 但 generation 过期时网络前拒绝”反例通过；入口 11、source inventory 7、V1 119/四 DLL metadata 1060、466 锚点地图 recorded/working-tree 通过。fixture 用无效代令牌，不触碰真实存档或 provider；尚未完整回放实际三次延迟和换档事件。
- 未完：后续 minute wave 的 UI 通知线程与配置快照、独立 live 材料源、部分失败/一次发布以及总体请求 owner 仍需验证和归位，a2/J13a/J13 ACTIVE；a3 与 J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，未动 `.dotnet-cli-home/`。

## J13a a2 批次 Prompt 主线程分阶段准备切片（2026-09-24）

状态 `J13a_A2_PROMPT_PUMP_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `f812ec1b`。审阅 `MyBehavior.cs:41439,42966,45484` 发现自动周报已有分阶段 Prompt 准备，但手动/显式重试的后续 minute wave 可能在 `Task.Delay` 后于非主线程进入 `GenerateWeeklyReportBatchWithRetriesAsync`，原先 `PrepareWeeklyReportBatchPrompt` 会读取 live Kingdom/统治者及前期周报。现 `MyBehavior.cs:1391–1405,1937–1938,2510,17453–17462,45502,45649–45713` 把所有待发批次放进独立的主线程准备队列，Campaign tick 按原每日预算与 `WeeklyMaterialStageCursor` 逐批准备，旧代/读档清理会结算等待者；全部准备完且同代后才发第一波。worker 对任何未准备的批次在调用 API 前直接拒绝，不再从后台补建 Prompt。已有自动阶段预备的批次保持幂等，只补缺失的预览/显示标签，不改 API route 或 J08 transport。

- 验证：四个授权构建目录复核绝对路径/内容/无链接后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 SHA256 `E68FF3F12D19C426E3D6611CC1BD24D2BB977838946A94E3689DBA3C13C30962` 的 Phase8 当前候选及 worker 未准备请求在网络前被拒反例通过；入口 11、source inventory 7、V1 119/四 DLL metadata 1060、465 锚点地图 recorded/working-tree 通过。队列复用已测 FIFO/清理 owner 和单步 cursor，但尚无 live 多 wave/Campaign tick 组合回放，不能冒称整体请求门禁通过。
- 性能/剩余：每次循环只推进一个 batch，tick 内在 batch 间检查帧预算；单 batch Prompt 构建仍原子 O(该批素材/字符)，首次网络启动可能在队列完成回调所在线程继续执行，暂无实机帧耗时保证。后续 minute wave 的 UI 通知线程、独立 live 源状态、部分失败和发布全链路尚未闭合；a2/J13a/J13 仍 ACTIVE，a3 与 J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，未动 `.dotnet-cli-home/`。

## J13a a2 目标变更后的显式重采恢复切片（2026-09-24）

状态 `J13a_A2_FRESH_RETRY_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `240c10aa`。`MyBehavior.cs:43081–43111,43220–43257,43397,45719–45753,45922` 在目标不完整但已变更时把失败上下文标为需要新素材，不再显示误导性的 API 修复/旧素材重试动作。专用暂停弹窗明确告知“重新采集并生成”可能替换这些失败分组尚未完成的手工编辑，须用户点击才从当前 Campaign 重新构建周界内素材；按稳定 report ID 只选原失败分组，任一分组已不存在则不发送新请求，已完成分组不重跑。另可保存并退出。新请求重新捕获目标状态，不沿用旧请求的准入权；原 API/RPM 失败仍走原弹窗。

- 验证：授权四构建目录的路径/内容/无链接复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 SHA256 `E7738A4EE42B65C625D3C65D9748A7ADB582BF1CFD0077664B91C0A71834112C` 的 Phase8 当前候选、只重建失败目标/缺目标拒绝反例通过；入口 11、source inventory 7、V1 119/四 DLL metadata 1060、462 锚点地图 recorded/working-tree 通过。UI 文案/回调接线经源码检查，**未**运行 live 弹窗或真实 API 请求；不能把纯选择回放当作完整恢复验收。
- 成本/剩余：显式按钮触发的 `BuildWeeklyEventMaterialPreviewGroups` 仍是同步 O(王国/素材) 主线程工作，未量化帧耗时；不在引擎 tick 增加轮询。独立 live 源在请求期间变更、minute burst 与部分失败/发布全链路尚待验证，a2/J13a/J13 ACTIVE；a3 及 J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，未动 `.dotnet-cli-home/`。

## J13a a2 批量完整/短报胜出者切片（2026-09-24）

状态 `J13a_A2_WINNER_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `32a8379c`。`MyBehavior.cs:42469–42488,45598–45703` 的真实 pending commit 在目标状态冲突时，分别以完整报的非空 `Summary` 和短报的非空 `ShortSummary` 判定同周、同 kind/scope 的已有胜出者；把它计为已满足目标，但不写入、不再次发地图通知或发布变更。只有目标变更且尚无相应完成内容才进入失败/重试上下文。完整/短报判定与原按需全文“已发布者胜出”语义对齐，不把任意编辑误当成功；跨 batch 的同 ID 仍只结算一次。

- 验证：四个获准构建目录路径/内容/无链接复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 DLL SHA256 `9650264CA89CEFC48FD5D7FA9C52A66FEAF3AFB8395FCCF402DF76EAC064D6BE` 的 Phase8 显式候选与全文/短报胜出者、错误周界和未完成编辑反例通过；首次回放因 fixture 在测试胜出者前留空 `Summary` 失败，补正 fixture 后通过，非产品编译回归。入口 11、source inventory 7、V1 119/四 DLL metadata 1060、459 锚点地图 recorded/working-tree 通过。纯判定回放与源码接线不等于 live 多请求竞争/通知验收。
- 剩余：不完整编辑导致的暂停/失败弹窗仍需正确恢复入口；独立 live 素材源变化、minute burst/部分失败/请求协调以及 a3 回执发布未闭合。a2/J13a/J13 仍 ACTIVE；J13b–g 未施工。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送，未动 `.dotnet-cli-home/`。

## J13a a2 失败后手动重试沿用目标状态切片（2026-09-24）

状态 `J13a_A2_RETRY_ADMISSION_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `8b1f703d`。`MyBehavior.cs:42469,43006,43302–43324,45338–45389,45829,46334–46340` 使 pending commit 的失败上下文携带原请求前目标状态到手动重试，而不是在重试开始时对旧素材重新授予覆盖权。重试前在当前主线程 owner/同代检查后比较失败组的原状态与当前状态；任一失败组目标变更就不发送本次重试请求，回执标记记录变更并提示重新收集素材；所有失败组均未变时才沿原手动重试路径。仅失败组参与比较，先前成功组的记录更新不会错误挡住剩余组。

- 验证：四个获准构建目录绝对路径/内容/链接复核后，原脚本 Debug/Release × Bannerlord 1.3/1.4 + Bootstrap 六构建均 0 warning/error；当前 Debug 1.4 SHA256 `231869AC9A7D7854B5066C08D0D350E14EE8B5DD0F6A10ACE02E8DB0CD448F05` 的 Phase8 当前候选及重试同态准入、目标/保存素材变更/缺失状态反例通过；入口 11、source inventory 7、V1 119/四实现 DLL metadata 1060、458 锚点地图 recorded/working-tree 通过。反例是生产纯判定与源码接线，不是 live popup 或 API 重试验收。
- 剩余：记录变更后旧失败弹窗仍沿原 API 修复/手动重试 UI 流程显示，虽不会再次发旧请求，却尚无合适的“重新采集本周素材”恢复动作；独立 live 源变更、minute burst/部分失败/发布的整体生命周期仍未闭合。a2/J13a/J13 保持 ACTIVE，a3 与 J13b–g 未施工。无实机、旧档、provider、音频或帧性能数据，均 NOT-RUN；未 Stage/部署/打包/推送，未动 `.dotnet-cli-home/`。

## J13a a2 批量目标记录迟到回包防覆盖切片（2026-09-24）

状态 `J13a_A2_TARGET_GUARD_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `d59848d1`。`MyBehavior.cs:42429–42464,45326–45397,45558–45742` 在请求前由当前主线程 owner 为各目标捕获原事件记录状态（含保存的素材、全文与展示字段），在主线程 pending commit 的 block 接纳和材料游标结束后写入前重验；不同代/非当前 owner 直接结算旧任务。期间编辑或其他请求已写入的胜出者不再被旧结果覆盖、不计成功、不发地图通知；按原失败路径计入部分失败并保留显式重试弹窗。跨 batch 重复 report ID 只结算一次。原 `UpsertWeeklyReportEventRecord` 写入/发布逻辑、存档 DTO 与外部接口身份未改。

- 验证：授权的四个构建目录均复核绝对路径、内容、无 reparse point 后，原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 DLL SHA256 `FCAB4573DD0A2DE6FC9524BF8A786C93D7AC1C9A35738C46ABFA47FC30FAA066` 的 Phase8 显式候选 marker/新鲜度与记录 absent/编辑/胜出者/保存素材改变回放通过；入口 11、source inventory 7、四实现 DLL metadata 1060/公开 V1 119、456 锚点地图 recorded/working-tree 通过。回放只证明状态序列化反例与源码接线，未在 live Campaign 并发驱动整个 pending commit。
- 成本/未完：请求前一次 O(R+G) 记录索引捕获并序列化命中目标；每个 block 至少一次当前记录线性查找 O(R) 与按记录字符/素材大小的序列化，延迟材料克隆恢复时再重验。没有帧耗时上界或大积压证据。独立于目标记录的 live 材料源变化尚未被本门禁覆盖；当时显式重试会重新捕获目标状态，该风险由上方 `8b1f703d` 切片取代，恢复 UI 仍待处理。minute burst/请求协调、失败 popup/恢复和 a3 回执发布未闭合，**a2/J13a/J13 仍 ACTIVE**。实机、旧档、provider、音频、性能 NOT-RUN；未 Stage/部署/打包/推送，未动 `.dotnet-cli-home/`。

## J13a a2 分块材料提交游标切片（2026-09-24）

状态 `J13a_A2_BLOCK_MATERIAL_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE`，产品 `bd972386`。`src/modules/AF.Module.Weekly/Generation/WeeklyReportBlockMaterialCursor.cs:6,20` 持有单个已解析 block 的有序源材料下标与克隆结果，`Advance` 每次只处理一项并跳过 null 克隆。`MyBehavior.cs:1458,45620–45662` 的真实 pending commit 消费者在预算检查之间调用该 owner，完成后仍由主线程 `UpsertWeeklyReportEventRecord` 写记录；原 `OrderedMaterials/ClonedMaterials/MaterialIndex` 三份瞬态字段从宿主 DTO 删除，不改保存身份或 API。每项处理 O(1) 外加原克隆成本；首次排序仍 O(M log M)、最终记录写入仍原子且没有帧耗时证明。

- 验证：复核获准四目录绝对路径/内容/reparse 后，原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 SHA256 `01AB3C5A0069C223B3A06104BF45F8947E7B6CE09CA6DFAEC076239EB6867F99`，Phase8 显式当前候选校验与新增 one-step/null/恢复/不重放回放通过；入口 11、source inventory 7、453 锚点地图 recorded/working-tree 通过。没把队列/材料游标用例冒充整体批量请求或 live 发布验收。
- 发现的剩余门禁：活动批量 `MyBehavior.cs:45517–45667,44217–44261` 虽在提交前检查 `SaveRuntimeGuard` generation，但 `UpsertWeeklyReportEventRecord` 会直接改现有目标记录；当前未见按需全文的“已发布胜出者/源状态变化”同等级重验。a2 **不能**标完成。下一步先设计/复现该反例并核对自动与手动重试的目标身份、捕获时点与分块预算，补主线程 owner/目标/源门禁而不把 worker 读取 live 对象，也不悄悄改变成功/失败或一次发布语义。a3 与 J13b–g 未施工；实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送。

## J13a a2 批量 commit 队列生命周期切片（2026-09-24）

状态 `J13a_A2_COMMIT_QUEUE_SLICE_OFFLINE_VERIFIED / J13a_A2_ACTIVE / J13a_ACTIVE`，产品 `e4a78829`。`src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitQueueOwner.cs:7,22,32,46,60` 持有 worker 完成到主线程提交之间的 FIFO、无工作 volatile 快路径、仅队首可移除、读档清理及每个遗弃请求等待者结算。`MyBehavior.cs:1908–1909,2481,17423–17427,45453–45512` 保留原 Campaign tick、主线程预算/分块提交、generation 检查、结果投影及私有嵌套 DTO 身份；队列锁/标志/容器已从宿主删除，无第二套队列。泛型 owner 用原私有 DTO 和结算回调，不扩大存档/公开类型可见性。实际请求 worker 仍在 `:45302–45373` 经 `EnqueueWeeklyReportCommitAsync` 等待提交；`WeeklyFullReportCompletionOwner` 的按需全文队列保持独立既有语义。

- 回归：Phase8 新 `WeeklyReportCommitQueueReplay` 覆盖 FIFO、非队首不可出队、CancelAll 结算等待者、旧 context 不移除读档后新队首及二次清理。队列入/出/查 O(1)，读档清理 O(Q) 且只在重置触发；未把 worker 结果当已发布。首次 Debug 1.3 编译因将嵌套 DTO 改 `internal` 触发 `CS0052` 三项，诊断后改为泛型 owner 并恢复 private 身份；最终 Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error。
- 当前 Debug 1.4 候选 SHA256 `CF2279401002FABF2F140289AF834C7806836631F6E198783EFAF5129BDEAD86`，Phase8 显式当前候选 marker/新鲜度及原有/新增回放通过；入口清单 11、source inventory 7、451 锚点地图 recorded/working-tree 通过。没有 Stage/Deploy/Push；实机、旧档、provider、音频、帧性能 NOT-RUN。此切片仅解决队列生命周期，**不**宣称 a2 完成：minute burst 发送/重试、pending commit 每块工作量、记录源重验/一次发布、失败 popup/恢复仍留宿主。下一动作核对 `MyBehavior.cs:42876–42972,45302–45373,45513–45650,45668–45752` 的请求和提交游标，优先将分块提交的状态转换归 owner 并验证大积压/旧代/部分失败；a3 与 J13b–g 未施工。

## J13a a1 调度与材料有限退出门（2026-09-24）

状态 `J13a_A1_OFFLINE_VERIFIED / J13a_ACTIVE`；此结论以 `577f7cac` 的同输入材料组合回放补齐并**取代**下方三阶段游标切片的 `J13a_A1_VERIFY` 临时状态，不代表 a2/a3 或 J13 全包完成。已接通的真实责任为自动补周/叛乱延期 `WeeklyAutoScheduleOwner`，材料分组/全文短报/批次 `WeeklyMaterialBatchPlanner`，聚合 `WeeklyMaterialAggregationOwner`，recent/major action 游标 `WeeklyActionMaterialCursor`，Full/Short PromptMaterials 与劫掠归并 `WeeklyPromptMaterialOwner`，三阶段单步推进 `WeeklyMaterialStageCursor`。真实宿主 `MyBehavior.cs:6033–6065,6081–6121,6130–6299,37140–37169,42356` 保留 Campaign/主线程 Kingdom/Hero 捕获与预算/生成入口；`MyBehavior` 保存游标、DTO 类型身份、周界和公开接口未改。各具体锚点见[代码地图](architecture/af-framework-code-map.json)与[范围图](architecture/af-framework-code-scope.md)。

- 同输入离线门禁：`WeeklyMaterialPipelineParityReplay` 在同一组已捕获 detached 世界/近远王国材料上分别执行同步与单步分阶段 owner 链，比较 PromptMaterials 类型/稳定键/日期、模式、顺序、批次身份与周界；通过。分项回放覆盖空邻近回退、大小写去重、劫掠开始/完成归并、1000 个失效 action owner、空/null 组与完成后不重放。宿主源码审阅确认自动入口 `GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport)` 和同步入口 `foreach GetDevEditableKingdoms` 后同一资格判断；fixture 不构造 live Kingdom，故不冒称实机资格验收。首次安装/补周/禁用/叛乱延期由 schedule smoke 覆盖，日期 `42–48` 批次由生产 DLL replay 覆盖。
- 验证边界：Debug/Release × Bannerlord 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `9FE600156D304C3E97B5682AFF1F720A8BFFA8A7DD7713E0A5B039354168349A` 的 Phase8 当前候选回放通过。Weekly outcome contract、Phase8 入口 11/source inventory 7、source-linked V1 119/snapshot 36/5 mutation、四 DLL metadata 1060、449 锚点地图 recorded/working-tree 通过。没有 Stage/Deploy/Push。`SanitizeEventSourceMaterials` 和 action owner 字典在每次周报初始化/首预览分别 O(N) 快照；每组材料聚合/Prompt 和每批 Prompt 仍原子工作，只有组/owner 之间检查预算，没有实机帧耗时保证。跨 tick live 源变更仍按原捕获时点语义，旧档/实机/provider/音频/性能 NOT-RUN。
- 下一包 a2：读取并建立自动/手动、多模式、minute burst、批量重试、按需全文、pending commit 与发布/失败路径的单一请求/完成责任清单；不只复用已完成的按需全文队列切片。先定向核对 `MyBehavior.cs:42400–42800,43000–43700,45500–46900` 的实际符号/调用与 SaveRuntimeGuard、源状态重验，再逐有限切片迁 owner 和验证。a3 回执发布、J13b–g 均未完成。

## J13a 自动材料三阶段游标切片（2026-09-24）

状态 `J13a_STAGE_CURSOR_SLICE_OFFLINE_VERIFIED / J13a_A1_VERIFY / J13a_ACTIVE`，产品 `cd37d2f2`。`src/modules/AF.Module.Weekly/Materials/WeeklyMaterialStageCursor.cs:5,17` 持有单阶段下标与完成状态，每次预算回调最多取一个组/批次。`MyBehavior.cs:1512–1520,6238–6298` 的聚合、PromptMaterials、Batch Prompt 三个真实消费者共用此 owner，删除原三套下标/完成布尔；宿主保留主线程游戏调用、每日预算检查和例外清理。空组立即完成；null 组只消费一次预算机会，不会一帧空转扫完整表；结束后不能重放。该 cursor 为瞬态，不影响 SyncData、公开类型或保存键。

- 验证：获准四目录复核绝对路径、内容和递归 reparse point 后，原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 DLL SHA256 `9FE600156D304C3E97B5682AFF1F720A8BFFA8A7DD7713E0A5B039354168349A` 的 Phase8 marker/来源/新鲜度校验及三阶段游标回放通过；Phase8 入口 11、source inventory 7、代码地图 449 锚点 recorded/working-tree 通过。schedule smoke 先因 `dotnet run` 仍选 net6.0 缺 exe 失败；诊断后对同一源码用本地 SDK 显式 `-p:TargetFramework=net8.0` build，再直接执行 net8 DLL 通过。Weekly outcome contract 通过；source-linked V1 119、snapshot 36、5 变异拒绝及 Debug/Release 四实现 DLL metadata 1060 项通过。
- 成本与剩余：Weekly 触发时 `MyBehavior.cs:6033–6065` 的王国列表、`SanitizeEventSourceMaterials` 和 owner 字典快照仍一次性 O(N)，每组聚合/Prompt 及每批 Prompt 仍原子执行；计划要求登记而非假称常数帧成本。本切片证明游标单步和三个宿主接线，不等于真实 Campaign 的相同材料/日期整体 parity。a1 仍 `VERIFY`，下一步补同步/延迟同输入的材料顺序/全文短报/周界集成回放并审阅初始化快照一致性，门禁过后才进入 a2 请求/完成。a3 和 J13b–g 未完成；实机、旧档、provider、音频、帧性能 NOT-RUN，未 Stage/部署/打包/推送。

## J13a PromptMaterials 组装切片（2026-09-24）

状态 `J13a_PROMPT_MATERIAL_SLICE_OFFLINE_VERIFIED / J13a_ACTIVE`，产品 `c2d8565b`。`src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.cs:10,12,35,71` 真正接管全文/短报 PromptMaterials 选择与组装、普通材料克隆、短报大会/开局摘要排除、定居点统计/劫掠归并一次，以及全文劫掠开始/结果按稳定 key 合并。真实消费者 `MyBehavior.cs:6286` 延迟自动周报、`:37168` 同步预览和 `:42356` 独立劫掠材料构造均调用同一 owner；原 host 只保留 live Settlement/Hero 解析和既有专用文字/素材转换 helper，未迁 Campaign/存档/Harmony 身份，也未复制第二个组装循环。

- 频率/成本：每次材料组准备最多一次，源材料排序/归并 O(M log M)，仍由延迟准备按组预算调用；单组内部和初始 O(N) snapshot 是原子工作，尚无严格帧耗时上界。没有新增逐帧轮询、跨线程 live 游戏读取或全量历史重复扫描。
- 验证：获准四目录复核路径/内容/reparse 后，原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 DLL SHA256 `C319221CE7B0D9C523EA305DBFA315B71FE6EABC46D612A86540812B1C348DE1` 的 Phase8 marker/来源/新鲜度及新增 Full/Short/克隆/排除/劫掠合并回放通过。WeeklyMemoryMaterialOutcomeContractTests 通过，Phase8 入口 11、source inventory 7、447 锚点地图 recorded/working-tree 通过。上述回放不证明实机事件顺序或旧档。
- 未完：a1 的自动阶段游标与一次性快照预算/边界仍待核查；a2 请求/完成、a3 回执发布及 J13b–g 未施工。下一动作将 `MyBehavior.cs:6244–6315` 的聚合/Prompt/Batch 三阶段推进状态移交同一 Weekly owner 并验证重入/空组/预算反例，然后有限关闭 a1，转入 a2。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送。

## J13a 周报全文/短报材料选择切片（2026-09-24）

状态 `J13a_PROMPT_MODE_SLICE_OFFLINE_VERIFIED / J13a_ACTIVE`，产品 `12f9e3d2`。`src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs:9–29` 统一邻近王国 ID 规范化去重、最多三国全文、无邻近结果时按原 group 顺序回退，以及其余王国短报判定。同步预览 `MyBehavior.cs:37146–37169` 与延迟自动准备 `:6266–6293` 两个真实消费者改用同一 owner；后者在 context 中缓存 `HashSet<string>`，不再每次预算回调重复分配。原 live 玩家距离计算、PromptMaterials 具体内容、周报 UI 和存档身份不变。

- 频率/成本：每次周报预览/准备选择一次 O(G) ID 过滤与最多 3 个集合元素；延迟路径每组短报判定为 O(1)，不新增每 tick 全量扫描。原 `SanitizeEventSourceMaterials` 初始化 O(N)、group 内聚合 O(M log M)、具体 PromptMaterials 准备仍按现有每组预算执行；没有实机帧耗时证据，不能报总体预算完成。
- 验证：获准四目录路径/内容/reparse 复核后，原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 DLL SHA256 `AD1E082F0DC73ADE5E17AD6D0B87BC1A85916309CAA620FF2A8954B196397E92`，Phase8 校验当前候选来源与新鲜度并通过；`WeeklyMaterialBatchPlannerReplay` 新增空邻近回退、大小写/空白去重、世界与已选王国全文/未选短报断言。入口 11、source inventory 7、444 锚点地图 recorded/working-tree 通过。
- 未完：a1 的 PromptMaterials 具体构造和自动准备阶段游标仍在 host；a2 请求/完成、a3 回执发布以及 J13b–g 尚未完成。下一动作检查 `MyBehavior.cs:37171–37240,6266–6321` 的 Full/Short PromptMaterials 构造、分批阶段和预算游标，确定可迁移的完整材料责任及反例，再进入 a2。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送。

## J13a action 材料游标切片（2026-09-24）

状态 `J13a_ACTION_CURSOR_SLICE_OFFLINE_VERIFIED / J13a_ACTIVE`，产品 `8d934447`。`src/modules/AF.Module.Weekly/Materials/WeeklyActionMaterialCursor.cs:6,21` 持有 recent/major action 的 owner/action 下标及当前 Hero 缓存，每次 `Advance` 至多消费一个 owner 边界或一条 action。真实自动周报预览入口 `MyBehavior.cs:6132–6172` 逐次检查日维护预算；`MyBehavior.cs:6195–6228` 保留主线程 Hero 解析及世界/王国材料回调。原 `PendingAutoWeeklyReportBuild` 两套下标/缓存已移除，没有并行游标；瞬态类型不是存档身份，原 Campaign/SyncData 不动。

- 具名反例：旧 `ProcessPendingAutoWeeklyReportActionSlice` 在一次调用中会连续跳过任意多个空或失效 owner，直到有效 action/尾部才返回，外层预算无法在中途介入。新 `WeeklyActionMaterialCursorReplay` 以连续 1000 个无效 owner、空 owner、双 action 和完成后重入验证每步至多一次解析/消费、同一 Hero 两条 action 不重放。没有更改 action 的世界/王国资格或素材文本规则。
- 性能/边界：每步 owner/action 游标 O(1) 加一次原有 live 解析/材料分发；分发仍可能遍历合格王国。初始化 `.ToList()` owner 快照、`SanitizeEventSourceMaterials` O(N) 及每组聚合 O(M log M) 仍是原子工作，**未**声称整体帧预算完成。主线程使用缓存 Hero 与旧实现一致，旧档/实机事件顺序未验证。
- 验证：复核获准四目录绝对路径、内容及递归 reparse point；原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error。当前 Debug 1.4 候选 SHA256 `E169B058AF9653A908B4814126CE09CB83CBFE6C35B8EE17903A67E845593537` 的 Phase8 marker/来源/新鲜度及含新游标负例的回放通过；Phase8 入口 11、source inventory 7、442 锚点地图 recorded/working-tree 通过。J13a2/a3 和 J13b–g 未完；实机、旧档、provider、音频、帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- 下一动作：继续核对 `MyBehavior.cs:6039–6151,6244–6321` 的一次性 source/owner snapshot、每组聚合与提示准备预算，决定不改变源状态语义的增量化边界；再做 a2 请求/完成与 a3 回执发布。

## J13a 材料聚合分桶切片（2026-09-24）

状态 `J13a_AGGREGATION_SLICE_OFFLINE_VERIFIED / J13a_ACTIVE`，产品 `f54fbcf6`。`src/modules/AF.Module.Weekly/Materials/WeeklyMaterialAggregationOwner.cs:6,8–61` 接管预览材料克隆、普通材料保留、按 action 分类与 stable event key 分桶、分类顺序及最终排序；`MyBehavior.cs:38550–38553` 的自动预算入口 `:6301–6321` 和同步预览入口 `:37220–37227` 共用该 owner。原 host 仍按现有 Hero/Kingdom 规则渲染聚合文本，避免把 live 游戏查询搬到 worker；DTO 全名、存档键、公开 API 不变，没有第二套分桶算法。

- 频率/成本：每个材料组在每次周报准备时聚合一次；克隆/分桶 O(M)、排序 O(M log M)，自动路径仍每次预算回调只处理一个组。单组聚合仍不可中断，初始化全量 action snapshot 仍 O(N)，**未**证明帧耗时或预算严格上界；后续 a1 游标切片必须处理/量化。新增 owner 不在逐帧空转时扫描历史。
- 门禁：获准四目录复核绝对路径、内容和递归 reparse point 均正常；原脚本无 `-Stage/-Deploy` 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error。当前 Debug 1.4 DLL SHA256 `5FF0628720BA2D06A27F12C99096AC8D82A087A3134526CACFAF80F5727FF1C9`，Phase8 从显式当前候选读取并校验 marker/新鲜度；`WeeklyMaterialAggregationReplay` 覆盖普通材料克隆、分类和同事件键归桶，原 Phase8 断言通过。入口清单 11、source inventory 7、440 锚点地图 recorded/working-tree 通过。以上为离线证据，不是实机或性能验收。
- 未完：预览游标与预算、a2 自动/多模式请求完成、a3 动作回执发布，及 J13b–g。下一动作核对 `MyBehavior.cs:6051–6389` 的一次性 snapshot 与每组聚合/提示准备的预算单元，设计可复现的单次工作量反例并把状态转换移交 Weekly owner。实机、旧档、provider、音频、帧性能 `NOT-RUN`；未 Stage/部署/打包/推送。

## J13a 材料分组/批次切片（2026-09-24）

状态 `J13a_MATERIAL_BATCH_OFFLINE_VERIFIED / J13a_ACTIVE`，不代表整个材料构建或 Weekly 包完成。产品 `008a3f84`：`src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs:7,9,35,63` 接走邻近王国/世界/其他分组优先级和世界单列、全文/短报按上限分批的真实算法；`MyBehavior.cs:41620–41624,43621–43625` 的实际自动/手动/重试消费者继续在主线程筛掉失效王国并捕获邻近顺序，然后调用 owner。`MyBehavior` 私有嵌套 DTO/枚举仅改为 `internal` 可见性，类型全名、字段、程序集、存档键和外部 public API 不改；未复制第二套批次算法。

- 频率/成本：分组按每次周报材料准备或手动重试执行，O(G log G) 排序、O(G) 模式划分；不在每 tick 对全量历史重扫。分批预览里每次 `ApplyWeeklyPromptMaterialAggregation`、主线程王国和 action snapshot 的 O(N) 成本仍待后续 a1 切片量化/归属；不能据此声称帧预算完成。
- 验证：Debug/Release × 1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628` + Bootstrap 原脚本六构建均 0 warning/error，无 Stage/Deploy。当前 Debug 1.4 DLL SHA256 `746E51D79EC3C7F4CBF5DF2114320BA9CCF1BD2281A22007D36078BFCA126655` 的 Phase8 生产 DLL 回放通过；新增 `WeeklyMaterialBatchPlannerReplay` 检验邻近/世界/兜底次序、批次上限、全文/短报分区、原 group 身份和周界。Debug 1.3 `39D997A35F61B43DE29030BB9385D899E06E48982A3DC6B7935453E4703B9E88`；Release 1.3 `CED7ECE20A876D7C3B8ECFEE152573D7E0AE3781E186547F4EBDA5C2F135559B`、1.4 `1FFE55A850053A9B99DEA883AC09A3FD7B572CDC307E349BA2CCA7C23DFECA0C`。Phase8 inventory 11、source inventory 7、438 代码地图 recorded/working-tree 通过，均不替代实机。
- 四 DLL V1/legacy/内部端口 metadata 1060 项通过；source-linked API 119 项、snapshot 边界 36 项与 5 个负向变异拒绝通过。首次误用系统 SDK 10.0.400，因本机缺 net8.0.30 targeting pack 而在 NuGet 禁用网络时 `NU1100`；改用仓库现有 `local/dotnet/8.0.425/dotnet.exe` 与其 net8.0.31 pack 后原 runner 全部通过，未修改断言/产品目标框架或联网安装。metadata/fixture 不证明 CLR 实际游戏加载。
- 下一动作：完成预览素材聚合/准备游标与真实预算证据，再进入 a2 生成请求/批量完成与 a3 回执发布；之后才可判定 J13a 退出门。J13b–g 尚未施工。实机、旧档、provider、音频和帧性能 `NOT-RUN`；未 Stage/部署/打包/推送。

## J13a 调度责任切片（2026-09-24）

状态 `J13a_SCHEDULING_OFFLINE_VERIFIED / J13a_ACTIVE`，仍不代表材料、请求/批量完成或回执发布结束。产品 `d1697338`：`src/modules/AF.Module.Weekly/Scheduling/WeeklyAutoScheduleOwner.cs:6,12,28,44,49` 真正持有瞬态待处理周次及同步/延迟选择、叛乱延期、恢复时最早缺周/已补齐清理决策；`WeeklyReportSchedulePolicy.cs:5` 与 `WeeklyReportTextHelper.cs:19` 原样归位 Scheduling/Materials。真实消费者 `MyBehavior.cs:5790–5851,6051–6085,6392–6437` 保留日维护事件、MCM/Campaign 读取、材料主线程捕获与实际生成入口；保存游标 `_lastAutoGeneratedWeeklyReportWeek_v1` 与相关状态身份不变。Phase8 入口清单与测试 Compile 路径已指向新文件；不是仅移动 helper 的结构交付。

- 行为与性能：首次安装选最新完整周、旧游标按最早缺周补齐，禁用不新开请求，叛乱期间保留最新待处理周次且恢复后按最早缺周生成；已补齐或载入清理时清除瞬态 pending。选择/恢复为 O(1)，每日/载入触发；没有新增逐帧扫描。`TryInitializePendingAutoWeeklyReportBuild` 的王国列表和素材 snapshot 初始化仍为 O(N) 且未迁，后续材料包另验。
- 验证：原 schedule Program 增加同步/延迟/叛乱/禁用/读档清理负例，显式 net8 运行通过（本机无 net6 targeting pack）；PhaseEightReadiness entry inventory 11 项、repository source inventory 7 项、432→435 代码地图 recorded/working-tree 通过。获准的原脚本 Debug/Release × 1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628` + Bootstrap 六构建均 0 warning/error；当前 Debug 1.4 DLL SHA256 `D45D3320F3A7520FDF72F92220808D43A1BB641D3EC6897F40F1AE05E0F7F218` 的 Phase8/Weekly owner replay 通过。Debug 1.3 `76391D88C24A386A50C53B116709AB1BDD14B9836525FDFB57FB959F4E13B649`；Release 1.3 `3A5A4F6CEF64E304A78896BB0B90FD2FED36EC138D221F6752F39A04BE988794`、1.4 `3F1D897C21F883C4700BA552D8049591BBF506B61FCFF2E1972274C6CAE57A20`。
- 下一动作：J13a 材料构建的分组/聚合/游标，再请求/批量完成和回执发布；首两切片成功不能豁免 a2/a3。未 Stage/部署/打包/推送，实机/旧档/provider/音频/帧性能仍 `NOT-RUN`。

## J13a1 首切片离线验证闭合（2026-09-24）

状态 `J13a1_OFFLINE_VERIFIED / J13a_ACTIVE`，仅指按需全文完成队列切片，**不表示整个 Weekly/J13 完成**。用户已明确授权原脚本清理工作区内 `bin/{Debug,Release}/single_module_artifacts` 与 `obj/single_module/{Debug,Release}` 四目录；运行前核实绝对路径、无链接/越界及现存内容。`dd303847` 代码候选由原 `build_single_module.ps1`（无 Stage/Deploy）构建 Debug/Release × Bannerlord 1.3/1.4 + Bootstrap，六构建均 0 warning/error。1.3 来源 `_deps_auto` 为 `v1.3.15.110062`，1.4 来源 `.tmp/build_check/1.4` 为 `v1.4.6.115628`，Harmony/私有运行依赖只读来自已确认游戏安装目录；未改来源校验。

- 当前 Debug DLL SHA256：1.3 `0AA9856AA37634549B8D4CB546CD4EBECF5B644F85BF35E7C3FEAFE900E45797`、1.4 `FEC1F04BB7F5B6BB4E4D025886A735FA44BE4A87E06331891FA377392F6E02F6`、Bootstrap `0E984A78B28B8710394E990C87B58DFC179F9E0850F480EF1059861B7EEDD278`。Release：1.3 `D52BD54F9FF6C9BF030B4837FCDD18A6280655EBDDD8C45323693C865F6E5FDF`、1.4 `0D74FEBB416D57DCFCFF1C3F64744C5CCD9BF5613E3A96298A4D481A518AE8D1`、Bootstrap `8F20B3AEA861D417B4461E48D6FCA2AECDA341DB2CA7B4E359F5C84340AF09C0`。
- `b240778f` 仅给 Phase8 测试宿主增加显式当前候选 DLL 参数和 build marker/SHA256/源码时间校验，不改变其他 replay 默认 Stage 路径或产品代码。回放使用上述 Debug 1.4 当前 DLL，通过 `WeeklyReportOwnerReplay` 的 worker→主线程、两次限额、旧代/旧 owner、清理等待者、异常及 Phase8 全部既有断言；错误 SHA256 负例按预期拒绝。该回放是离线生产 DLL/fixture，不是实际 Campaign、Gauntlet 或旧档证明。
- 原 net6 schedule runner 本机无 targeting pack；相同 Program/生产源码显式 net8 编译运行通过，`WeeklyMemoryMaterialOutcomeContractTests`、source inventory、432 坐标地图通过。按需全文 `MyBehavior` 中材料捕获、同代源变化/已发布胜出者重验未迁，仍由原 host 承担；自动/批量生成及实际 frame work 量未覆盖。实机/旧档/provider/音频/帧性能 `NOT-RUN`；未 Stage/部署/打包/推送。
- 下一动作：J13a1 后进入 J13a 调度与材料切片，先归位现有 schedule/text helper 及显式读取路径，再把补周/叛乱延期决策与瞬态 pending week 状态交 Weekly owner；之后按计划 a2/a3，不为目录迁移报完成。

## J13a1 按需全文完成队列切片（2026-09-24）

状态 `J13_ACTIVE / J13a_VERIFY`，并非 J13a 或 J13 离线完成。开工检查点 `f3b79d4c`，产品/测试切片 `83cc314b`，测试编译修正 `d12e8d65`。`src/modules/AF.Module.Weekly/Generation/WeeklyFullReportCompletionOwner.cs:8,34,48,70` 拥有按需全文完成的队列、锁、generation/owner 受理、每 tick 最多两次主线程提交、异常传递和清理等待者；真实消费者 `MyBehavior.cs:2120,2222–2225,42738–42751` 保留 Campaign/UI 引擎入口与原反射 replay 方法名，自动/批量生成队列仍在原 host。`MyBehavior.cs:42642–42728` 继续主线程捕获源材料、worker 请求和提交时同代源状态/已发布胜出者重验。未改保存键、默认开关、API route、制作组玩法或原共享 Actions 回执。

- 行为/性能：仅迁走完成队列状态与转换，未新增长期轮询/扫描；原每 tick 最多两次 `Apply` 保持，但单次 Apply 的 record/字符工作量尚未量化，不能视为帧预算通过。独立锁不再与批量提交队列共用；两队列原本无统一顺序语义，互不读写对方状态。
- 验证：`dotnet restore ...WeeklyReportSchedulePolicy.SmokeTests.csproj -p:TargetFramework=net8.0 --ignore-failed-sources` 与相同属性 `dotnet run --no-restore` 通过（原 net6 runner 无本机 targeting pack，此为相同 Program/生产源码的 net8 运行，不等同原 net6）；覆盖 worker 预约、两次限额、清理等待者、旧代/旧 owner、异常。`WeeklyMemoryMaterialOutcomeContractTests` 通过；1.3/1.4 `Compile` 集合均包含新 owner；`test_repository_source_inventory.py` 7 项通过；代码地图 432 锚点 recorded/working-tree 通过（只证明定位）。首次将 worker 测试误写成 `Task.Run` 已解包 bool，`83cc314b` 提交前编译失败 `CS0029`，后续 `d12e8d65` 改为保留内层 Task 并复测通过。
- **未过门禁**：双版本 Debug/Release + Bootstrap 构建和当前候选 Phase8 replay 均未运行。原构建脚本会递归重建四个已存在产物目录；已核实它们位于工作区、无符号链接/重解析跳转，内容分别为 `bootstrap/versions`、`bootstrap/implementation_1.3/implementation_1.4`。已请求这四个精确目录的清理确认，未获确认前不调用脚本、不以其他构建命令绕过。Stage DLL replay 仍需当前候选的显式来源校验；不能用旧 DLL 凑 PASS。实机/旧档/provider/音频/帧性能仍 `NOT-RUN`。
- 下一动作：取得精确构建清理授权后运行原脚本的 Debug/Release（无 Stage/Deploy）并运行当前候选 replay；随后继续 a1 自动调度/材料真实职责，不把此队列切片冒称 J13a 全包完成。代码坐标及保留范围见[代码范围图](architecture/af-framework-code-scope.md)。

## J13a0 开工检查点（2026-09-24）

状态 `J13_ACTIVE / J13a0`，不改变 J07–J12 的离线结论。实际 Git 根 `E:/AnimusForge-refactor-continuation-20260831`、分支 `codex/af-main-refactor-continuation-20260831`、开工 HEAD `8f7445b86794534af83f0ea58b9e176a38dc8e78`；仅 `.dotnet-cli-home/` 未跟踪，保持不动，暂存及已跟踪差异为空。

- 首切片范围：Weekly 自动调度与全文完成队列；保留 `MyBehavior` Campaign/SyncData 身份、`_lastAutoGeneratedWeeklyReportWeek_v1`、日维护事件、API route 与 UI 提交入口。目标是把具体调度决策或完成队列状态/转换交给 Weekly owner，原 host 只捕获游戏状态并触发；不改玩家规则、其他领域或生成模式。
- 当前源码责任：`MyBehavior.cs:5805–5867,6066–6157,6395–6482` 管理同步/延迟自动周报、叛乱延期及材料构建；`:42647–42791` 管理按需全文捕获、异步请求、重验及完成队列；`:45830–45900` 的批量提交仍用独立队列/预算；`:18100,18452` 保持保存游标。`WeeklyReportSchedulePolicy.cs` 现只有补周/日期算法，真实消费者是日维护、延迟恢复与载入；`MyBehavior.WeeklyActionOutcomeReceipts.cs` 的共享 Actions 回执另属 a3，不能把 prepare 当成功。
- 首切片退出门：真实自动/按需消费者接线，首次安装/补周/禁用/叛乱暂停与 owner/generation/同代源变更/清理等待者语义不退化；现有 schedule、material contract 与对应 replay 通过；双版本编译在获准运行构建脚本后核实。热路径不新增全量扫描；现存 `GetDevEditableKingdoms` 和材料 snapshot 初始化仍有 O(N) 一次性成本，不能冒称已预算化。
- G0 环境：本机 SDK 10.0.400、net8/net10 reference pack、net6 runtime 但无 net6 reference pack。原 net6 schedule runner 缺 assets 时失败 `NETSDK1004`；以不改断言的 `TargetFramework=net8.0` 显式还原/运行通过。net8 material contract 通过；代码地图 working-tree 429 锚点通过，均非游戏验证。构建脚本会递归清理 `bin/<Configuration>/single_module_artifacts`、`obj/single_module/<Configuration>`，未获精确清理确认前不运行；Stage DLL replay 不能拿旧产物冒充当前候选。
- 下一动作：提交本检查点；核对全文完成队列与批量提交共享锁的实际关系后，做一个状态/决策真实迁移切片，定向补测并记录受阻门禁。实机、旧档、provider、音频、性能、Stage/部署/打包/推送均 `NOT-RUN`。

## 当前接续：J13 领域职责计划（2026-09-24）

**当前状态：J07–J12_OFFLINE_VERIFIED；J13_PLANNED，尚未施工。** 本节替代下方历史段落的“当前下一步”指令，不撤销 J12 最终离线结论；历史工作树和本地路径不选择本次施工目录。

- **计划入口**：[J13 可执行计划](plans/j13-domain-owners-plan.md)。顺序：G0 → J13a Weekly → J13b Kingdom → J13c Persona → J13d Social/Issue/WorldEvents/WarStats → J13e Duel/Taunt/Encounter/Settlement/Exercise → J13f UI/Onboarding → J13g 离线收口。近期 Weekly 包细化到调度材料、请求完成、回执发布及有限退出门；后续包开工按真实源码细化。
- **基线与工作区**：`0624d5025fac98877332ccb1d5221dd4f85f836e`；`E:/AnimusForge-refactor-continuation-20260831`；分支 `codex/af-main-refactor-continuation-20260831`。规划开始时跟踪分支同步，仅 `.dotnet-cli-home/` 未跟踪，保持不动；新对话仍以实际 Git 为准。
- **证据位置**：计划第 4 节集中记录本次核对的真实入口、行号、符号、消费者和边界；第 5–7 节列实施/验证入口。实施后详细回执仍留本台账，代码定位沿用[代码范围图](architecture/af-framework-code-scope.md)。旧方法数量不作完成标准，不把 R100/partial/转发壳当职责完成。
- **已发现环境/安全边界**：Weekly schedule runner 为 net6，部分 Persona/Encounter runner 写死旧 SDK 路径；PhaseEight replay 读取 Stage DLL，不能直接复用旧产物。现有一键构建即使无 Stage/Deploy 也会清理产物目录，执行前必须核实并取得精确清理范围确认，不改脚本或绕过引用来源校验。
- **本轮范围与验证**：只有计划、台账、HANDOFF 文档；15 个本地链接/锚点与代码围栏检查通过，`git diff --check` 通过；代码地图 recorded/working-tree 各 429 锚点通过（只证明定位，不证明玩法），核对无产品源码改动。没有产品测试/构建 PASS。实机、旧档、provider、音频和性能仍 NOT-RUN；未 Stage/Deploy/Package/push，未修改自动化或其他工作树。
- **下一动作**：新对话使用计划第 1 节启动指令，先完成 G0 和 J13a0 的实际责任清单/最小基线，再做首个 Weekly 生产切片。J13 必要退出门通过前不标 DONE，不提前实施 J14。

以下 J12 及更早记录保留为历史证据；当前接续以上节为准。

<a id="j12-final-closeout-20260922"></a>
## J12 Economy / Diplomacy / WorldMap 最终离线收口（2026-09-22）

**当前状态：J12_OFFLINE_VERIFIED；下一阶段 J13。** 产品提交 `5c3e7b0e2ba73b3d024182f559ae054215bb529e` 完成复审后剩余的 J12b2–b4 与 J12c1–c4，取代下方 `J12_REOPENED_PARTIAL` 状态；历史纠错过程保留作审计。

- **Diplomacy Direct**：`DiplomacyCrossDomainActionOwner.cs:10` 接走附庸/吞并 tag 的解析和执行，`RewardSystemBehavior` 只调用外交 owner；七类 Direct action 与 `DirectDiplomacyWarGuard` 保持原资格、通知和真实状态回执。
- **WorldDiplomacy runtime**：`WorldDiplomacyJobRuntimeCoordinator.cs:32` 负责 detached 队列选择、route 与 completion generation 判定；`WorldDiplomacyBehavior.JobRuntime.cs:46,220` 持有真实 start/request/completion host。后台任务捕获不可变 request，不闭包读取可变 job；旧 generation 不能污染或释放新请求。
- **WorldMap**：`WorldMapOrderCoordination.cs:65` 负责 token/STOP/admission route；`WorldMapPartyCommandBehavior.{Protocol,Admission,QueueRuntime,EventLifecycle,DelayedRequests}.cs` 分别接真实消费者；`WorldMapPendingRequestCoordinator.cs:11` 为总督延迟请求提供 owner+ticket one-shot。四个 SyncData key、嵌套保存类型、Campaign 注册和 TaleWorlds mutation 留原 host。
- **清理/兼容**：旧方法体从主文件删除，没有第二 parser、第二 queue 或双执行；不新增 SyncData key、公有 V1 ABI、默认开关或持久幂等状态。活动兼容入口及 Campaign/Saveable/Harmony/UI/game mutation 因真实消费者与身份要求保留。
- **最终验证**：Debug/Release × Bannerlord 1.3/1.4 + Bootstrap 六构建 0 warning/error；J12 lifecycle 68、owner source 3、Intent 1176、Compression 297、PolicyHistory 95、ResultSettlement 453；J09 wiring 25、Native 91/184、Scene 71、Courier 34/32/39；四实现 DLL API/metadata 1060；Persistence Profile/Chunk 与 Identity contract 5、Bridge bindings/runtime、Phase8 readiness 73、repository source inventory 7、代码地图 429 recorded/working-tree 全部通过。ResultSettlement 原 net6 runner 因本机无 net6 reference pack，使用相同 Program/生产规则源码的临时 net8 host 完成 453 项。
- **负向证据**：queue selector 的 `running job` 单变量变异可编译并命中具名断言失败；旧 Direct 主权、虚假战争事实、WorldMap 迟到回调等红例保持通过。
- **未验证**：直接 Persistence identity audit（需当前 Stage 实现）、Phase8 Stage DLL parity、真实 Campaign/Mission、旧 SAVE round-trip、live Economy/Diplomacy/WorldMap/AFEF、付费 provider、音频和帧性能均 `NOT-RUN`。本轮未 Stage/Deploy/Package，未写游戏目录或存档。
- **回滚**：先定向 revert 后续文档提交，再 revert `5c3e7b0e`；若还需回退审查修复再 revert `a50ab3ad`。不得 reset/rebase/强推，代码回滚不等于逆转已发生的游戏状态。

详细坐标、命令、保留项与下一步见 [J12 最终 HANDOFF](handoffs/2026-09-22-j12-final-closeout.md) 和 [J12 实施计划](plans/j12-domain-owners-plan.md)。

<a id="j12-review-repair-20260921"></a>
## J12 审查修复与重新开放（2026-09-21）

**当前状态：J12_REOPENED_PARTIAL；J13 暂缓。** 复审确认 `1c62c2c9` 的 Direct/World/WorldMap R100 只能证明目录、类型和 ABI 保真，不能替代原计划 b2–b4/c1–c4 的真实职责与生命周期退出门。旧 `J12_OFFLINE_VERIFIED` 结论撤销；J12a 与 J12b1 仍保持已有验证。

- 产品修复 `a50ab3ad`：Direct 七类动作实算法移入 `DiplomacyBehavior.Actions.cs`；补非国王玩家宣战拒绝和 Apply 后真实战争观察，阻止虚假 WorldDiplomacy 成功事实。
- `WorldDiplomacyRequestLeaseCoordinator` 成为单一进程内 request owner，冻结 jobId/generation/maxTokens/timeout；worker 不再读取可变 job，旧代 completion 不能释放新 lease。
- `WorldMapDelayedRequestCoordinator` 为同伴建队 UI 分配一次性票据，旧/重复 callback 不再清新请求 busy 或重复触发转金/建队；普通队列 `AddedCommandCount` 改为实际接受数。
- 新增 `J12DomainLifecycleRegressionTests` 32 与完整 Intent 1175 断言；两个外交旧红例、正常宣战对照和 WorldMap 旧回调注入均按预期；Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；四 DLL API/metadata 1060、Persistence 142/168/13/44、Identity 5、Bridge 23/12、J09 wiring 25、Phase8 readiness 73、source inventory 7 通过。
- 剩余：WorldDiplomacy 完整 queue/start/completion/commit；WorldMap c1 协议、c2 受理、c3 队列/事件及 c4 governor 延迟请求；完成后再跑同一候选 Release/API/Persistence/三渠道/Bridge/Phase8/代码地图。详见[技术 HANDOFF](handoffs/2026-09-21-j12-review-repair-handoff.md)。

真实 Campaign/Mission、旧 SAVE、live Economy/Diplomacy/WorldMap/AFEF、provider、音频和性能仍 `NOT-RUN`。本节优先级高于下方保留的旧 J12 完成回执；旧段仅作历史审计，不再表示当前状态。

<a id="j12-offline-verified-20260921"></a>
<a id="j12b-diplomacy-start-20260921"></a>
<a id="j12a-economy-start-20260921"></a>
# 历史回执：J12 曾被过早标为离线闭合（已由上节纠正）

**历史状态（不再有效）：J07–J12_OFFLINE_VERIFIED。** 此段记录 `1c62c2c9` 当时的验收说法，不能覆盖上方 `J12_REOPENED_PARTIAL`。Economy 产品链与 Diplomacy 四规则仍有效；Direct/World/WorldMap 的完整职责结论已撤销。

- **J12b1 四规则**：`WorldDiplomacy{OfferCooldown,ThreatState,ResultSettlement,PolicyHistory}Rules.cs` 以 Git `R100` 迁入 `src/modules/AF.Module.Diplomacy/Rules/`；namespace、public/internal 可见性、DTO/JSON、输入 mutation 与所有生产消费者不变。三个 smoke csproj 和 Intent 源码文件定位已切真实新路径。
- **J12b1 证据**：PolicyHistory 94、ResultSettlement 453、Intent rule-focused 1143；Debug 1.3/1.4/Bootstrap 0 warning/error；416 锚点绑定 `0ac279fc`。两个原 net6 runner 在 SDK8 离线环境缺 ref pack，临时 net8 wrapper 保持同一 Program/规则源码；当时完整 Intent 套件仍读取旧 PermanentAlliance 注册路径；本轮已跟随真实 `StartupPatchComposition`，完整 1175 断言通过。
- **J12b2–b4 实际结果**：`DiplomacyBehavior.cs` 967 行真实 direct owner 与 `WorldDiplomacyBehavior.cs` 20,454 行真实 world owner 均以 Git `R100` 归入 `src/modules/AF.Module.Diplomacy/{Direct,World}`。七类动作、资格/Prompt、job queue、请求冻结、in-flight、completion、传播、Vassalage/Annexation 调用和 Campaign/save 状态全部保持同一类型/实例；根路径不存在 facade 或第二份状态。
- **J12c 实际结果**：`WorldMapPartyCommandBehavior.cs` 9,993 行真实 owner 以 Git `R100` 归入 `src/modules/AF.Module.WorldMap/Runtime`。协议、STOP/普通/非 Hero/建队/远征受理、queue/event lifecycle、delayed request、嵌套 `WorldMapOrderApplyResult` 与四个 save key 同一 owner 保真；没有复制 J09 parser 或新旧双执行。
- **J12d 最终证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；四实现 DLL metadata 1060；Persistence/Profile 142/168/13/44；Bridge 23、Phase8 73；Diplomacy 94/295/453/1143；J09 wiring 25、Native Action 91、Native Completion 184、Scene 71、Courier 34/32；repository source inventory 7。代码地图 recorded/working-tree 419 均通过。
- **完成边界**：这是 J12 domain ownership 与离线兼容收口，不是 J17 全仓细粒度拆分。World/WorldMap 内部仍是大型 cohesive state owner；未为了行数制造转发壳、复制状态或更改 save identity。更细内部 partial 分片可在后续清理阶段进行，但不能声称是本次实机证据。
- **已知工具项**：两个原 net6 diplomacy smoke 因本机无 net6 ref pack，使用相同 Program/生产源码的临时 net8 wrapper；该历史提交只记录 rule-focused 1143；本轮修复测试路径后完整 Intent 1175 通过。Memory terminal 的既有 `TagSceneSessionHistoryLine` 缺失仍不计 PASS。
- **未验证**：真实 Campaign/Mission、旧 SAVE round-trip、live Economy/Diplomacy/WorldMap/AFEF、provider、音频与帧/网络性能均 `NOT-RUN`。
- **下一步**：J13 Social/Weekly/Duel/Encounter/Issue 等领域 owner；不重开 J12，除非出现具体回归或相关源码变化。

## J12a Economy 已闭合回执

- **兼容边界归位**：`EconomyRewardDebtContracts.cs` 100% 原样迁入 `src/AF.Contracts/Compatibility/Economy/`；planner 和 main-thread port 100% 原样迁入 `src/modules/AF.Module.Economy/{Planning,Execution}`。namespace、public 类型/构造器/接口、enum 数值与实际 consumers 不变，旧三个路径已删除，测试 Compile 路径同步。
- **真实职责抽取**：新增 `Projection/EconomyPromptProjection.cs`。`RewardSystemBehavior.BuildTrust*` 与两个 Debt Hint 仍在所属线程完成 live trust、账目 normalize、价格/期限捕获，再把 string/int-only `EconomyDebtPromptLine` 交给 detached formatter；没有将 `NormalizeDebtRecord` 或游戏对象搬到 worker，也没有第二 Prompt 拼装链。
- **Replay/Authorization/Batch owner**：Hero/Party/Merchant 三个完整 partial 100% 内容迁入 `Execution/{Hero,Party,Merchant}`；11 个 live authorization 声明精确归入 `Authorization`，零消费者 resolver 删除。`EconomyReplayBatchCoordinator` 统一 null/reject/applied 计数、fact retention、partial/unknown 和 unknown 后停止；三个 domain step delegate 是唯一 live mutation host，原生 inventory/gold/settlement 操作有意留其后方。J12a2 `DONE`。
- **Trust/Debt normalization owner**：`EconomyTrustPolicy` 唯一拥有 clamp、十级映射及三组 AI 语义文本；`EconomyDebtNormalizationPolicy` 唯一拥有旧 aggregate→line 迁移、line clamp、note、aggregate/date 重建和幂等归一。原 `DebtRecord` nested 全名、字段、JSON/SyncData 和 game-thread capture 语义不变。
- **Debt schedule/Quest owner**：`EconomyDebtSchedulePolicy` 唯一拥有 due day、提醒节奏、finite/unlimited overdue trust/relation 算法；pending quest HashSet 与 ID、load/import reconciliation、deferred creation、deadline sync、agreement completion共 10 个声明逐声明原样迁入 `DebtPromiseLifecycle`，后续 ledger/daily 包补齐其余生命周期。
- **Debt ledger/daily owner**：`DebtLedger` 现拥有嵌套 public DTO/private schema、运行账本、提示/导入导出、查询、创建、约定结清等 35 个逐声明原样迁移；`DailyEconomyLifecycle` 直接拥有已注册的 `OnDailyTick`，没有留根转发壳。`_debtStorage` 与 `DailyTickEvent` 注册仍留 Campaign host，嵌套类型全名和 SyncData key/type 不变，Debt 生命周期已归位。
- **Trust state 与 Reward capture**：`TrustState` 直接拥有 progressive carry、个人/公共/定居点/商人 apply 及 battle/quest event 共 75 个原声明，删除零调用 `ClampLong`；`InventoryPromptCapture` 直接拥有 Hero/merchant 授权快照、完整/可见候选和 Prompt capture 8 个原声明。根 `RewardSystemBehavior.cs` 当前 18,682 行；数字只作导航，实际完成依据是声明、消费者和门禁。
- **兼容保留**：`ApplyRewardTags` 仍有 Native/Scene/Courier 共 5 个真实 mixed-domain 消费者。J09 executor 已确保 Economy typed plan 单次执行并排除重复 delegated raw；该入口必须等 J12b/J13 的外交/入队等分支接走后再删，当前保留是活动兼容责任，不是遗漏清理。
- **证据**：Reward capture 8 exact / ProductionReward 11；projection/trust 15 + 2 有效变异；Trust state/event 75 exact；Debt normalization/schedule 48 + 2 有效变异；Quest 10、ledger/DTO 35、daily 1 exact；HeroAssetScope 67 + 5 有效变异；GiveAsset stress 80562；Economy port/executor；J09 default wiring 25；Production owner current-DLL；Phase8 73。Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；四实现 DLL 1060 metadata；Persistence/Profile 142/168/13/44；代码地图 412 锚点绑定 `7f2fffba`。
- **已知工具项**：`MemorySummaryMainThreadBoundaryTests/run_terminal.py` 读取新契约路径后，在既有非 J12 符号 `TagSceneSessionHistoryLine` 提取处失败；基线 `9af7e8cf` 的 `MyBehavior.cs` 同样没有该符号。未修改 Memory 业务或降低断言，不能计为 PASS；后续按它的真实 owner 单独处理。
- **下一步**：按既定顺序开始 J12b Diplomacy；不重开 J12a，除非出现具体回归或相关源码变化。J12c WorldMap 与 J12d 整包仍待完成，不能标 J12 DONE。
- **未验证**：真实 Campaign/Mission、旧 SAVE、live inventory/gold/merchant/debt/trust、AFEF、provider、性能均 `NOT-RUN`。

详细顺序、保留符号和退出门见 [J12 计划](plans/j12-domain-owners-plan.md)；当前代码位置见[范围图](architecture/af-framework-code-scope.md)与 412 锚点代码地图。回滚先文档/地图，再按 `7f2fffba` → `fa26d430` → `616ba892` → `3d2b636e` → `54b55aa3` → `deb421ae` → `07feb572` → `3f2c454e` 定向 revert；不 reset/rebase/强推。

## 以下 J12 计划节为实施依据；当前进度以上方回执为准

<a id="j12-planned-20260921"></a>
# 当前接续：J12 计划已编写，尚未施工（2026-09-21）

**状态：J07–J11_OFFLINE_VERIFIED；J12_PLANNED。** 规划基线 `60499d44`（J11 验收修复已推送），本轮只有文档；生产代码、测试、构建、默认入口和自动化均未改。自动化继续暂停。

实施清单见 [J12 Economy / Diplomacy / WorldMap 计划](plans/j12-domain-owners-plan.md)。顺序为 G0 有限基线 → J12a 捕获/资产执行/债务信任 → J12b 规则/直接外交/世界外交作业 → J12c 协议/受理/队列/延迟请求 → J12d 整包离线验收。

本次依据当前源码纠正原提纲：Economy public ABI 必须保留；债务 capture 可能规范化状态；外交 J08 transport 已接线；外交 Rules 的 DTO mutation 不等于纯只读；WorldMap STOP 后失败、部分效果和排队必须独立表述。不得承诺“任何失败都零副作用”，不得以四 Rules 归位或薄 wrapper 冒充整个领域拆分。

本轮仅完成源码/消费者/测试入口和关键风险的规划核对，不新增产品 PASS、不重复执行六构建。文档 5 个本地链接/锚点、代码围栏与唯一当前入口检查通过；现有代码地图 working-tree 394 锚点通过（仅定位，不是功能验收）。执行首轮按计划核实环境与相关门禁，J12 必要离线验收完成前不得标 DONE；LIVE/SAVE/provider/性能独立 NOT-RUN。本次计划只本地提交，不自动推送/部署/Stage/打包或恢复自动化。

## 以下 J11 为已完成阶段回执；当前下一步以上方 J12 计划为准

<a id="j11-team-seams-20260921"></a>
# 当前接续：J11 制作组内部模块接缝离线整包闭合（2026-09-21）

**状态：J07/J08/J09/J10/J11_OFFLINE_VERIFIED；J12 尚未开始。** J11 只治理编入 `AnimusForge.dll` 的 Policy / Gathering / Siege internal typed contracts、薄 adapter 和装配证据；没有迁移或重写制作组玩法，也没有开放独立子 MOD API。

## 产品结果

| 责任 | 当前 owner / 路径 | 结果 |
| --- | --- | --- |
| Internal contracts | `src/AF.Contracts/Internal/TeamModules/{IPolicy,IGathering,ISiege}ModulePort.cs` | 3 文件 / 13 方法；namespace、internal 可见性、参数、默认值、ref/out 和返回不变；没有状态或实现 |
| Policy adapter | `src/bridges/Policy/PolicyModuleAdapter.cs` | 4 方法原样转发 `KingdomAgendaCustomPolicyBehavior` / `NpcRulerPolicyBehavior`；8 个生产调用点 |
| Gathering adapter | `src/bridges/Gathering/GatheringModuleAdapter.cs` | 5 方法原样转发 `NobleGatheringBehavior`；12 个生产调用点；facts/notifications 仍由调用方提交 |
| Siege adapter | `src/bridges/Siege/SiegeModuleAdapter.cs` | 4 方法原样转发 `AfGcczShoutBridge`；11 个生产调用点；`conversation-siege` 与 active-stage gate 保持原 owner |
| Composition | `src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs` | 每种 port 一个静态无状态实例；无反射/扫描/每次字典查询/热替换 |
| 清理 | 原 `Refactor/Modules/TeamModulePorts.cs`、`TeamModuleAdapters.cs` | 替代实现和真实消费者全部接线后删除；无旧声明、Link、复制实现或直连绕过 |

完整 owner/consumer/gate 矩阵见 `docs/architecture/af-team-module-seam-matrix.md`。物理归位不是新 FeatureBridge：InternalModuleDirectory `Ready` 仍只表示 adapter 已装配；实际 Campaign、目标、资格、模块开关和失败语义继续由原领域 owner 决定。

## 最终离线证据

- **精确迁移与 ports**：6 个 interface/class declaration 与 J11 前逐声明相等；TeamModulePortParity 13 方法 / 31 call expressions、308 行为断言；资格反转、Siege selected 反转、player/speaker text 交换 3 个可编译行为变异全部拒绝。
- **Policy 双版本**：Bannerlord 1.3 与 1.4 各自 `policy-all-modules-contract-only` 1406（18 modules）和 `policy-history-only` 1115 通过；未改 MCM retrieval、active instance、save codec、execution/rollback。
- **Composition / Bridge**：Campaign composition 42 + 6 可编译变异（含真实入口重复注册拒绝）；CompositionMatrix 18 cases / 24 invariants；Bridge 16 bindings（12 wired / 4 declared-only）、23 单测、runtime isolation 12；ModuleFramework public API 119 + 256 concurrent reads。
- **三渠道影响面**：Scene parity 71；J09 default wiring 25；Courier Prompt 550/76、postprocess 39、domain commit 32；Native action 91、completion 184。未复制 Prompt、parser、动作或事实提交。
- **构建/API/存档**：Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/0 error；四实现 DLL 1060 metadata/API；Persistence/Profile 142 literal / 168 typed / 13 chunked / 44 flattened，Identity 5、Chunk replay 8。
- **结构**：394 锚点代码地图 recorded / working-tree 通过，绑定产品 `cdbd077af3abb4614594eff4b198052a4841e63e`。

## 清理、性能和限定

adapter 只有静态实例的一次直接接口调用，不缓存 Hero/Mission/session，不 catch、retry、写历史/AFEF、显示通知、读取文件/网络或扫描程序集。相关新目录无冲突标记、TODO/HACK/TEMP、重复接口/adapter 或无消费者占位方法；AF 主体未发现 13 个领域 owner 方法的直接绕过。`TeamModuleServices` 和 `TeamModuleRegistration` 因仍是活动装配入口保留。

产品提交 `cdbd077a`；最终文档/地图以本节所在提交为准。回滚按文档/地图 → `cdbd077a` 定向 `git revert`，不 reset/rebase/强推。真实 Campaign/Mission、旧 SAVE、制作组玩法结果、真实 GCCZ 场景、provider、音频和性能仍 `NOT-RUN`；无 Stage/Deploy/Package，未写游戏、存档或 `G:/AFMOD/GCCZ`。下一阶段先规划 J12，不重开 J11，除非有新的具体复现或相关源码变更。

## 以下为上一阶段 J10 回执

<a id="j10-scene-owners-20260921"></a>
# 当前接续：J10 Scene / Courier 离线整包闭合（2026-09-21）

**状态：J07/J08/J09/J10_OFFLINE_VERIFIED。** J10 的 Scene/Courier 渠道 owner、生命周期、提交时点与失败语义已完成必要离线验收；不是全项目 J17、实机、旧档或发布完成。下一步先制定 J11 制作组内部模块接缝计划，不在本节迁移 Policy/Gathering/GCCZ 玩法。

J11 已形成有限实施计划 `docs/plans/j11-team-module-seams-plan.md`，状态 `PLANNED / NOT_STARTED`：13 个 internal 方法、31 个生产调用点按 Contracts → Policy/Gathering/Siege thin adapters → 原玩法 owner 分包；本次只写计划，没有开始搬文件或修改玩法。

## 产品结果

| 责任 | 当前 owner / 生产接线 | 结果与保留边界 |
| --- | --- | --- |
| Scene audience / request | `Channels/Scene/{SceneShoutConversationScope,ScenePlayerShoutRequestOwner}` | Mission/epoch/Agent identity、冻结输入与 one-shot claim 唯一；旧完成不能发布到新会话 |
| Scene pending / speech | `ScenePendingAfefFactsOwner`、`SceneSpeechQueueOwner<T>` | pending AFEF oldest-first/one-shot；FIFO/单 worker lease/clear/reset；payload/TTS/动作仍归游戏线程 host |
| Scene group/relay/passive/reaction | `ShoutBehavior.SceneConversationChains.cs`、`ShoutBehavior.ScenePostprocess.cs` | 正文先显示，统一后处理后动作；queued speech/action 完成后才发布 relay；live Agent/movement/audio 副作用留 host |
| Courier prompt / message | `PromptPreparation`、`PromptSchedule`、`PromptMessages` | 同一五阶段 Prompt/历史/规则；source/session/generation 逐跳复核；没有 Courier 私有缩水管线 |
| Courier generation / transport / delivery | `GenerationLifecycle`、`SessionTransport`、`RouteTransport`、`DeliveryLifetime` | pregeneration 只产文本；到达/回信后才 commit；旧 retry/迟到 completion 不能改新 session |
| Courier session / letter | `SessionRegistry`、`SessionCreation`、`RuntimeTick`、`ProactiveLetters`、`LetterInventory` | load reset/copy-on-publish index、主动来信有界扫描、库存与既有 discard guard 保持；SyncData/DTO/key 留宿主 |
| Courier domain / wait | `DomainCommit`、`ReplyWait` | `DeliveryApplied` 与 request identity 后唯一领域提交；Economy one-shot；history/AFEF/notoriety 单次；最后一个 waiter 才恢复时间锁 |
| Shared J09 / team seams | `AF.Module.Actions`、`TeamModuleServices` | 三渠道共用 Tags/Plan/Execute/Receipts；Policy/Gathering/GCCZ 仍只走 typed thin port，不迁玩法 |

最终物理结果：根 `CourierDeliveryBehavior.cs` 从 J09 的 10,514 行变为 3,477 行（20 个 Courier partial）；根 `ShoutBehavior.cs` 从 39,190 行变为 37,062 行（6 个 Scene 文件）。行数只作导航，不是验收标准；根文件保留的 DTO/SyncData/Harmony/UI/live-object adapter 均有活动责任。

## J10 最终证据

- **构建**：Debug/Release × Bannerlord 1.3/1.4 + Bootstrap 六项，0 warning / 0 error；SDK 8.0.422，引用 1.3.15.110062 / 1.4.6.115628；无 Stage/Deploy。
- **API/存档**：四实现 DLL 1060 metadata/API；Persistence/Profile 142 literal keys / 168 typed bindings / 13 chunked / 44 flattened；Chunk 8、Identity tool contract 5。唯一 Scene key `_sceneHeroRevisitDays_v1` 名称/类型/数量未变，只修正拆分后的源码坐标。
- **Scene**：Parity 71、Queue 37、Request lifetime 30、Scope 5、Pending AFEF 5、Speech queue 6。
- **Courier**：Prompt 550/76、liveness 59/16、History 122/30、owner phase 16、postprocess 39、commit 34、domain commit 32，及 inbound/delivery/proactive/letter/route/session 套件通过。
- **共享/Native**：ChannelCutover 132、J09 wiring 25、InteractionPipeline、GiveAsset 80,562；Native Action 91、Admission 44、Completion 184。
- **制作组接缝**：13 个 typed-port 方法、31 个真实调用点完整 receiver/参数顺序对照；308 行为断言与资格/selected/参数交换 3 个变异通过。旧 Memory/GameLifetime fixture hash 不再阻断无关 port 证据，也未用刷新旧 hash 冒充通过。
- **Bridge/Phase8**：16 bindings（12 wired/4 declared-only）、23 tests、10 fixtures、12 isolation；Phase8 readiness 73 tests。
- **负例**：Courier delivery gate、reply-wait release 两个具名源码变异；Courier commit 4 个可编译运行变异；pre-delivery commit；TeamModule 3 个行为变异全部按预期失败。
- **结构**：392 锚点代码地图绑定产品 `f6c95ac37b7bdc21392c14627f7e529437465a57`，recorded / working-tree 两模式通过。详细证据见 `docs/handoffs/2026-09-21-j10-scene-courier-offline-closeout.md`。

## 清理、回滚和限定

相关 Scene/Courier 目录没有冲突标记、TODO/HACK/TEMP、重复 owner 或新旧双执行。最后产品包为 `14283c3f`（Prompt/fact）与 `f6c95ac3`（domain commit/reply wait），验收工具/地图为 `7d70f528`；回滚用定向 `git revert`，先文档/测试后产品，不 reset/rebase/强推。更早 J10 包的回滚链见 `docs/plans/j10-scene-courier-plan.md`。

真实 Campaign/Mission、旧 SAVE round-trip、provider、live Economy/外交、子 MOD CLR、真实 TTS/audio、帧/网络性能仍 `NOT-RUN`。Stage 依赖的 ProductionDuel/PhaseEight actual-DLL replay 本轮因明确禁止 Stage 而未运行，不能复用旧 Stage 冒充当前证据。自动化保持 PAUSED。

## 以下为历史回执，当前状态以上方为准

<a id="j09-offline-verified-20260921"></a>
# 当前接续：J09 Actions / 事实提交离线整包闭合，下一阶段 J10（2026-09-21）

**状态：J07/J08/J09_OFFLINE_VERIFIED。** J09 的唯一标签协议、ActionPlan 完整性、typed/legacy execution adapter、动作终态、历史/confirmed fact 提交和默认三渠道接线已完成必要离线验收。真实 Campaign/Mission、旧 SAVE、live Economy/外交、provider、音频及帧成本仍独立 `NOT-RUN`；这不是全项目 J17 完成。

## 产品结果与缺陷修复

| 责任 | 当前 owner / 接线 | 结果 |
| --- | --- | --- |
| Tags | `AF.Module.Actions/Tags` | 共享 finite catalog/parser；raw 第 65 项、未授权协议、`ACTION:*`、顺序/参数篡改均 fail closed |
| Plan / Execute | `AF.Module.Actions/{Plan,Execute}` | strict raw/plan、request/action fingerprint；Economy/Duel typed port 保留，尚未归位领域走 request-bound compatibility adapter，不把玩法搬入通用层 |
| Receipts | `AF.Module.Actions/Receipts` | 动作 success/reject/partial/unknown 与可见历史/confirmed facts 分离；owner-started 异常不可重试，reservation 仍只能由 owner 完成 |
| Native 默认 | `ShoutBehavior.NativeActionCommit.cs:20` | action-only boundary 后复用原 live core；completion、TTS、WorldMap exit、pending player history 不变；detached factory直接调 legacy core，避免嵌套执行 |
| Scene 默认 | `ShoutBehavior.ScenePostprocess.cs:482` | 保持 relay 先解析、mood→GCCZ→direct→follow/speech；最终验收发现 helper 返回后曾提前发布 relay，`2a1fc124` 修为 queued speech/action 完成并重验后才发布 |
| Courier 默认 | `CourierDeliveryBehavior.CommitDispatch.cs:22` | `DeliveryApplied` 前立即拒绝；默认 wrapper 与 detached core 分离，预生成不执行、到达提交不提前、历史不双写 |
| 性能 | `LegacyInteractionSnapshotAdapters.CaptureActionCommit:574` | action commit 只冻结身份/candidate/facts，不重读或分配整段 Prompt history；真实帧耗时仍 NOT-RUN |

## 最终离线证据

- 正常：ActionProtocol 14；InteractionPipeline/receipt/host；Economy；Duel dispatch 16/16 与 outcome 18/18；Weekly/Notoriety；Scene parity 71、Queue 37；Courier owner 39、commit 34；Native action 91、admission 44、completion 184；默认 wiring 25。
- 负向：ActionProtocol 5 个可编译变异、Scene Queue 7 个可编译变异、Courier owner 8 个可编译变异均命中具名断言，不以编译/路径错误冒充红例。
- 构建：最终源码 Debug/Release × Bannerlord 1.3/1.4 + Bootstrap 六项，均 0 warning / 0 error；引用版本仍为 1.3.15.110062 / 1.4.6.115628，没有 Stage/Deploy/Package。
- API/存档：四个实际实现 DLL 1060 metadata/API；public V1/legacy memory 相同、internal 隔离；Persistence/Profile 142 literal / 168 typed / 13 chunked / 44 flattened；Identity 5、Chunk replay 8 通过。
- 结构：Bridge bindings 16（wired 12 / declared-only 4）及 23 单测、Phase8 inventory 11 单测；334 锚点代码地图 recorded/working-tree 通过。地图只证明导航。
- 清理：旧 catalog/parser/executor/committer/cache 路径均不再 tracked；共享 Actions owner 无 TaleWorlds 依赖；默认渠道不存在新旧并行执行；历史文档中的旧路径只作为历史快照保留。

回滚按完整包定向 revert：`2a1fc124`（Scene queued relay 修复）→ `beb7dd38`（minimal identity）→ `449227a9`（默认渠道接线）→ `f4f022a3`（compat executor）→ `f61ec13e`/`bb223aec`/`65a14421`/`21206ec6`/`dbe87c4`/`fd01974b`/`20ba9527`。不 reset/rebase。下一包从 J10 开始，不重开 J09，除非出现新的具体复现或相关源码改动。

## 以下为历史回执，当前状态以上方为准

<a id="j09-shared-action-boundary-20260921"></a>
# 当前接续：J09 shared action boundary 已闭合，默认三渠道接线进行中（2026-09-21）

**状态：J07/J08_OFFLINE_VERIFIED；J09_IN_PROGRESS。** J09a–J09c 与 J09d shared core 已闭合，不能再写成“尚未开工”；但默认/兼容 Native、Scene、Courier 尚未全部改接，故不能标记 `J09_OFFLINE_VERIFIED`。自动化 `af-7-8` 保持 PAUSED，当前线程按用户要求继续。

| 责任 | 当前 owner | 产品提交 / 真实结果 |
| --- | --- | --- |
| 标签与授权 | `src/modules/AF.Module.Actions/Tags` | `20ba9527` 原样归位；`21206ec6` 阻断第 65 个 raw 动作绕过；Prompt/parser/domain/channel 矩阵见 `docs/architecture/af-action-protocol-owner-matrix.md` |
| 计划完整性与执行 adapter | `src/modules/AF.Module.Actions/{Plan,Execute}` | `fd01974b` 分离 strict ordered raw/plan policy；Economy/Duel typed owner 和 legacy adapter 边界保留 |
| 终态与事实提交 | `src/modules/AF.Module.Actions/Receipts` | `dbe87c4` 归位；`65a14421` 将动作 success/reject/partial/unknown 从历史/AFEF transaction 剥离，owner 抛异常继续是不可重试 unknown |
| 三渠道共享动作边界 | `LegacyChannelActionCommitter` → `ActionExecutionCommitter` | `bb223aec`、`f61ec13e`；detached Native/Scene/Courier 生产提交经同一 canonical request/action identity 与终态 receipt，无动作不调用 owner，disallowed/overflow fail closed |

**已验**：ActionProtocol 正常 14 项和 5 个可编译有效变异；InteractionPipeline、Economy、Duel 16/16、Courier/Channel 相关回归；最新 shared-core 产品候选 Debug 1.3/1.4/Bootstrap 0 warning/0 error。没有访问 provider、Stage/Deploy/Package、游戏或存档。

**下一有限包 J09d**：逐渠道把默认/兼容动作尾改接 action-only boundary。Native 保留 captured completion、TTS、WorldMap exit 与 pending history rollback；Scene 保留 target/session/epoch、mood、GCCZ、direct、speech/relay 相对顺序；Courier 保留 DeliveryApplied 后才 commit。每个渠道只能有一个权威执行点，不能新增 memory 写入，也不能把 J10 的 group/relay/transport 状态机偷渡到 J09。三者接完后才进入 J09e Release/API/存档/代码地图与清理门禁。

真实 Campaign/Mission、旧 SAVE、live Economy/外交、真实 provider/音频/性能仍 `NOT-RUN`。回滚按 `f61ec13e` → `bb223aec` → `65a14421` → `21206ec6` → `dbe87c4` → `fd01974b` → `20ba9527` 定向 revert；不 reset/rebase。以下 J08 闭合节保留为历史依据。

<a id="j08-offline-closeout-20260921"></a>
# 当前接续：J08 离线责任包闭合，自动化保持暂停（2026-09-21）

**状态：J07_OFFLINE_VERIFIED、J08_OFFLINE_VERIFIED；J09 已完成实施计划但尚未开工。** 用户要求暂停自动化并在当前线程完成 J08；`af-7-8` 已确认 PAUSED。J08 产品提交：非流 `5dc17947`、流式 `4776b691`、ModelCatalog/TTS 归位 `1e1fdfad`、Policy/World transport 收敛 `5a2df9d6`。原 `eb4b0bee` 在整体 transport 扫描前过早写成完成，本节以实际修复和最终验证纠正该结论。

## 已落地责任

| 责任 | 当前源码（完整一基坐标以 322 锚点图为准） | 完成与保留边界 |
| --- | --- | --- |
| 非流 attempt/lifetime | src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs | Primary、Configured、Policy、WorldDiplomacy 共用认证、send/body/dispose 与 detached status/reason/header/Retry-After；一次调用只做一次 attempt，不隐藏领域重试 |
| Primary 非流 adapter | ShoutNetwork.CallApiWithMessages | 设置/model/UI/token 诊断留宿主，不搬 220 行造壳；修复成功 response 未释放 |
| SSE attempt/lifetime | src/modules/AF.Module.Llm/Streaming/LlmStreamingTransport.cs | 唯一 SSE send/read owner，处理 data/DONE、content/reasoning、raw 上限、取消/stale 与资源释放，不隐藏 retry |
| Primary/Configured stream | ShoutNetwork.CallApiWithMessagesStream、LegacyConfiguredChatGateway stream branch | 两个真实消费者共用 owner，保留可见过滤、thinking fallback、typed status/metadata；已显示部分正文后禁止再次 stream/非流 fallback |
| 模型目录 | src/modules/AF.Module.Llm/ModelCatalog/LegacyModelCatalogGateway.cs | 100% rename，namespace/ABI/实现不变；解析/排序/UI/stale 仍归 Onboarding/MCM |
| TTS transport | src/modules/AF.Module.Llm/Tts/LegacyVolcTtsGateway.cs | 100% rename，ITtsGateway/payload/header/audio/error 不变；voice、Hero/Agent/Mission、播放停止仍归 TtsEngine |
| Policy/WorldDiplomacy | `PolicySystem/Npc/PolicyLlmClient.cs`、`WorldDiplomacyLlmClient.cs` 及两个 typed Gateway | 实际 POST/read/dispose 改接共享 transport；Policy hard-wall/兼容降级与 World thinking fallback/route/retry/Prompt/结果解析仍归领域 owner |
| Configured compatibility | Refactor/Adapters/LegacyConfiguredChatGateway.cs | 仍有真实调用和 manifest 责任；底层 transport 已模块化，保留路径不是第二条发送链 |

**真实缺陷修复：**
1. 旧 Primary 非流成功/错误 response lifetime 不完整；TrackedContent 复现后由共享 owner 全终态释放。
2. 旧 Primary stream 已调用 onChunk 后遇读取异常仍进入第二次 stream attempt，并先尝试非流 fallback；实际 Debug DLL 回放得到“部分部分”。现在已有可见内容时立即退出 attempt loop，直接完成部分正文，不再次请求。
3. 整体复核发现 Policy/WorldDiplomacy 仍各自持有 live `HttpResponseMessage` 和重复请求/read/dispose；`5a2df9d6` 改为共享 attempt owner，并让 response status/reason/header/body 在释放前脱离。Policy 超时后的迟到任务仍由观察者收尾，不再让调用方提前释放尚在发送中的 request。

## 验证和限定

- 非流 240 检查、5 项可编译变异；Primary 15 组及 Configured 旧/新请求、次数、接受点和输出对照通过。
- 流式 owner 17 检查、5 项可编译变异；Unicode/reasoning、坏 chunk、raw 上限、429、header/line 拒收、observer、部分异常、取消与资源释放通过。
- 实际 Debug DLL Primary replay：非流/stream、两类 thinking fallback、Unicode、增量/最终不重复、部分不重放、credential、两类取消通过；Configured 和 ModelCatalog replay 通过。
- J01 协议 13、LegacyShoutGatewayResult 40、NativeMainReply 179/19 场景通过。source projection 只投影已由新 suite/实际 DLL 覆盖的两个 Primary consumer 差异。
- ModelCatalog rename 后 replay PASS。最终 Debug 1.4 artifact 通过 Policy/WorldDiplomacy 实际 Gateway loopback 回放：caller cancellation、retry backoff cancellation、hard timeout、credential boundary 均 PASS；使用 `.tmp/j08-domain-replay` 的本地 runner 和显式依赖，不创建 Stage、不访问 provider。TTS 为 100% rename并由双构建与历史回放覆盖；真实音频仍 NOT-RUN。
- 最终 Debug/Release × 1.3/1.4/Bootstrap 六构建 0 warning/0 error；四实现 DLL 1060 metadata/API、Persistence/Profile 142 literal / 168 typed / 13 chunked / 44 flattened、Phase8 inventory、322 锚点双模式通过。
- `PersistenceIdentityAudit.py` 的默认扫描会因本工作树位于祖先 `.tmp` 而排除全部源码，不能把其原始 FAIL 当产品回归或 PASS；临时按仓库相对路径纠正扫描后只报告已登记的 WarStats 47 key / 1 behavior 相对旧 `d4cb1467` baseline 增量。本轮未修改任何存档键，J16 应修正该工具的路径判断。
- 本地日志在 .tmp/j08-nonstream-20260921、.tmp/j08-stream-20260921、.tmp/j08-final-20260921。真实 provider、Campaign/Mission、旧档、音频播放、网络背压和帧成本仍 NOT-RUN。

## 清理、性能、回滚

已删除 Primary、Configured、Policy、WorldDiplomacy 的重复 HTTP request/read/dispose 与 Configured 旧 SSE reader；TTS/ModelCatalog 因协议不同保留独立 owner。未新增队列、扫描或隐含 retry；每 attempt 只分配请求/响应与有限 raw sample并复用 HttpClient。真实网络吞吐未量测。

回滚按包 focused revert：`5a2df9d6`（Policy/World 收敛）、`1e1fdfad`（纯归位）、`4776b691`（stream）、`5dc17947`（non-stream）；本轮检查点 `6240ff6`。没有 push、部署、Stage、打包、游戏/存档或其他工作树修改。

## 下一步

下一阶段按 [`docs/plans/j09-actions-facts-plan.md`](plans/j09-actions-facts-plan.md) 执行：G0 标签/owner 矩阵 → Tags → Plan/typed ports → Receipts → Native/Scene/Courier 接线 → 清理/整包验收。自动化保持 PAUSED；不要因 J08 主类仍大或还能追加相似测试重开本包，只有新的具体运行证据或相关失败才定向回归。

## 以下为历史回执，当前状态以上方为准

<a id="j08-j09-overnight-continuous-20260921"></a>
# 当前执行纠正：J08 通过后立即做 J09（2026-09-21 01:40 北京时间）

用户纠正上一版通宵目标：不能在 J08 收尾后暂停，必须在同一自动化工作流立即进入 J09，并优化长期钻研单个子阶段的问题。本节取代下方“通宵目标只到 J08”的自动化说明；产品状态未因本次规则/自动化更新改变，仍是 `5dc17947`、J08_IN_PROGRESS。

- 双 SKILL 增加**有限责任包**原则：真实 owner 接实际消费者、关键行为/失败证据和最终兼容门禁通过后立即进入下一阶段。不得按每个 helper/partial 复制 fixture，不因主类仍大、历史事项仍多或还能增加证明而滞留；新的具体运行证据或相关失败才增加门槛。
- 自动化 `af-7-8` 更名为 **AF J08→J09 通宵连续收尾**，ACTIVE、每 30 分钟接续、北京时间 09:00 截止。J08 的非流→stream→model/Gateway→TTS 有限门禁通过后不暂停，连续做 J09 Tags→Plan/Execute/Receipts→三渠道接线。
- J09 只收通用标签/计划/唯一执行/回执和渠道接缝；领域业务、制作组玩法、J10 Scene/Courier 会话、J12 各域和 J14 新 public submit 不偷渡。本轮仍不自动 push/部署/Stage/打包/写游戏或存档。
- 到 J09 必要离线门禁通过、09:00 截止、用户停止或仅剩外部输入时，才更新交接并暂停。截止未完成需逐项列真实状态，不能把速度要求变成删断言、伪完成或零 BUG 承诺。

## 以下为当前产品回执

<a id="j08-overnight-skill-automation-20260921"></a>
# 当前执行：J08 通宵工作窗与双 SKILL 收敛（2026-09-21 01:37 北京时间）

用户询问当前进度并要求睡前优化 SKILL 与自动化。本次不改产品源码、不将配置更新冒充 J08 新功能：当前产品仍为 `5dc17947`，J07 保持 `OFFLINE_VERIFIED`，J08 为 `IN_PROGRESS`；准确进度及下一包以下方 `j08-nonstream-transport-20260921` 回执为准。

- `.claude/skills/animusforge-maintainer/references/llm-transport.md` 新增稳定、非提交绑定的方法：transport 一次调用只拥有一次 attempt；retry/错误文案/Prompt/显示留真实 policy owner；所有 request/response/content/reader/linked source 由创建边界释放；caller cancel、timeout、stale、provider failure 分开；非流与 SSE 状态不能假统一；TTS 网络和游戏音频生命周期分开；确定性 HttpMessageHandler 必须执行真实生产 transport/消费者。
- 通用维护 Skill 增加按需路由，AF 核心框架 Skill 增加跨层约束；没有修改全局 Skill、AGENTS、政策玩法或外部 API，也没有把当前路径/提交写成永久架构。
- 自动化 `af-7-8` 更新为 **AF J08 通宵收尾**、ACTIVE、每 30 分钟接续。新窗口截止 UTC `2026-09-21T01:00:00Z`（北京时间 09:00）；目标只到 J08 必要离线验收，不自动扩到 J09/J10/J17。
- 自动化从当前 Git 继续 J08a 上层编排→J08b streaming→J08c model/Gateway→J08d TTS；按完整责任包集中验证，不重复已通过的 J07/非流 240 检查和六构建，除非相关源码/依赖改变。完成、截止、用户停止或仅剩外部输入时，准确交接并通过工具暂停。
- 权限边界不变：单代理，无 push/merge/rebase/reset/stash/部署/Stage/打包/游戏或存档写入/全局安装；`.dotnet-cli-home/` 和本地 `.tmp` 资料保留，不提交制作组简版。

## 以下为当前产品回执

<a id="j08-nonstream-transport-20260921"></a>
# 当前接续：J08a 共享非流 HTTP 责任包已验收，J08 仍在进行中（2026-09-21）

**产品提交 `5dc17947`；检查点 `36de0b4`。J07_OFFLINE_VERIFIED 保持；J08_IN_PROGRESS，不是 J08 完成。** 本节取代上一节“J08 尚未实施”。唯一工作树/分支/交付远端、三 SKILL、禁止推送/部署/游戏/存档写入等边界不变；没有改自动化截止时间。

## 本包交付与代码位置

| 位置（一基行号） | 已接线责任 | 仍保留 / 后续 |
| --- | --- | --- |
| `src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs:29-51` | 一次请求的认证、POST JSON、sender 调用、响应/body 接受点、脱离 response 的结果、request/response 释放 | 每次调用只发一次；不隐藏自动重试、不持有 HttpClient 生命周期、不存凭据 DTO |
| 同文件 `:55-78` | 原 Configured linked timeout 与 Retry-After 解析归同一 owner；流式错误路径复用后者 | 保持调用方 token 所有权、原超时语义、原 date/header 处理；未新增 provider 行为 |
| `ShoutNetwork.cs:665-884`（调用 `:733,:755`） | 主请求与 thinking fallback 请求均改用新 owner；两个响应/body 代际接受点顺序和中文错误文案保持 | 上层设置/显示名/UI 重试/空回复补救/诊断编排仍在宿主，不把 220 行方法称为已全部模块化；stream 未迁 |
| `Refactor/Adapters/LegacyConfiguredChatGateway.cs:497-550,647-665` | 非流 Generate/Validation/PreparedJson 共用同一真实 transport；HTTP 失败分类统一给非流与流式复用 | 公开 Gateway/result ABI 未改；流式读取/解析以及适配器策略仍保留真实消费者 |
| `tests/modules/AF.Module.Llm/NonStreamingTransport/` | 当前/旧 Primary 与当前/旧 Configured 的 source-linked 差分 + 真 HttpMessageHandler 替身 | MCM/姓名/日志/UI/存档代际是明确替身，非实机/真实 provider |

**实际修复**：旧 Primary 成功响应未 Dispose，离线 TrackedContent 复现原版本未释放；新 transport 在成功、HTTP 失败、代际拒收和接受回调异常时释放响应与请求。保留原正文、错误、thinking 400、空回复一次补救和用户确认后重试；这是资源生命周期修复，不是简化请求/取消功能。

**清理**：删去两处 Primary 的重复 request/auth/read/dispose 代码、Configured 中已不可达的旧非流分支、原私有 timeout/Retry-After 实现。旧 SendPrimaryNonStreamingRequestAsync 保留为 Debug scoped replay sender/真实 GlobalClient 的有效接缝；stream 路径不是废代码，不删除。ShoutNetwork 原文件是混合行尾，本次从原字节按局部差分替换，未整文件统一行尾；未修改 ShoutBehavior.cs。

## 本轮验证（限定离线范围）

- 新 suite **240 检查**：Primary **15** 组旧/新最终文本、完整 payload、请求次数、代际检查顺序对照；Configured **7** 组旧/新状态/正文/错误码/request 对照 + caller cancel/timeout；Primary caller cancel 和 response 接受点清理。协议、认证与正文提取使用实际生产 LlmApiCompat，网络为确定性 HttpMessageHandler。
- **5 项有效红测**：泄漏 response、跳 header 接受门、丢 caller token、thinking retry 没去 controls、丢 Retry-After，全部编译成功并命中具名断言；不是编译/路径失败。取消反例检查 caller cancel 是否真正到达 send token，不能等另一个 timeout 后也返回 Cancelled 而假绿。
- ConfiguredChatGatewayReplay、ConfiguredChatValidationReplay、KnowledgeRagGatewayReplay **全部 PASS**（原 localhost replay，非真实 provider）；包含 streaming 既有行为、prepared JSON、桥开关独立性、超时/取消/credential 边界。
- J01 协议正常 suite **13 用例 PASS**，协议实现/fixture 未改变，历史 7 变异不因本包重复运行。原完整 J01 源码逆向检查仅对已经单独执行差分的 J08 Primary 方法作精确投影，不删除周围源码/调用次数断言。
- LegacyShoutGatewayResult **40**、NativeMainReply **179 / 19 场景** PASS。J07 其他 owner/UI/动作/历史源码和输入未变，复用 `e5c14b8a` 已绑定证据，不重开整包调查。
- Debug/Release × 1.3/1.4/Bootstrap **6 构建通过，0 warning / 0 error**；SDK 8.0.422，引用 1.3.15.110062 / 1.4.6.115628；原构建脚本、无 Stage/Deploy。
- 当前四个 DLL **1060** API/metadata 断言 PASS；存档契约 **142 literal keys / 168 typed bindings / 13 chunked / 44 flattened** PASS。315 锚点图绑定 `5dc17947`，记录提交/工作树两模式通过。
- 日志：`.tmp/j08-nonstream-20260921/`，构建日志 `.tmp/j08-nonstream-build-debug.log` / `...-release.log`，全部仅本地。第一次 J01 调用漏传必填 CLI 参数，补齐原参数后 PASS，未改流程。fixture 初版假定坏 JSON 必为失败，实际旧 LlmApiCompat 会保留可读原文；已改用真实旧 adapter 对照，未改生产兼容策略。

**未验证/不冒充**：真实 provider/游戏/旧档/音频/帧成本未测。旧 `PrimaryLlmGatewayReplayTests` 的 Stage/引用/日志路径仍未在本包处理，没有运行该真实 DLL Host 回放；新的 source-linked 差分不能冒充它。实际 DLL 检查是 metadata，不是 CLR/游戏加载。

**性能与回滚**：每个 HTTP attempt 一个脱离响应的短生命周期结果；仍复用既有 HttpClient，不新建连接池、扫描、队列或隐含 retry。释放泄漏的 response 降低了滞留风险，未量测真实网络/内存成本。回滚用 `5dc17947` 的定向 inverse/revert，保留检查点 `36de0b4`；不 reset、不强推、不覆盖其他工作树。

## 下一步：继续 J08，不回退 J07

1. **J08a 剩余上层编排**：主对话与 Configured 仍有各自配置/重试/错误/诊断宿主责任。明确保留的 UI/姓名游戏适配与应迁入模块的协议策略/请求状态 owner，接真实消费者后删除旧实现；不能仅把整段 220 行搬进另一文件或把两套不同策略强行压平。本次只关闭共享 HTTP attempt/lifetime 责任包，不把它抬成 J08a 全部收尾。
2. **J08b** 流式 request/SSE/可见输出/Unicode/取消；保留已输出内容后的失败与回退时序，不允许误重放或在游戏线程等网络。之后 **J08c** 模型目录/Gateway、**J08d** TTS 传输。J09/J10/J14 留后续，不顺手开放新的 public submit。
3. 原窗口仍截止 UTC `2026-09-20T17:48:36Z`；本轮结束时尚未截止，自动化维持 ACTIVE。到期若未完成，写清本包已验、上层编排/stream/model/TTS 未完，更新两份交接并暂停，不能把时限用完记 J08 DONE。

## 以下为历史回执，当前状态以上方为准

<a id="j07-offline-closeout-20260921"></a>
# 当前接续：J07 离线整包验收闭合，下一包 J08（2026-09-21 北京时间）

**状态：J07_OFFLINE_VERIFIED；J08 尚未实施，接下来交给自动化。** 本节取代下方历史“J07b 进行中 / C1 未完成 / 继续拆第一阶段”的接续指令，但保留历史缺陷证据。不是全项目 J17 收尾，也不是实机、旧档或真实 provider 通过。

- 修改前检查点 `e110d562`；实现/测试提交 `e5c14b8a3cb471a30a07783d7fe3700ac8d9cbe3`。代码图 310 锚点绑定该实现，记录提交/工作树两种模式通过。
- 唯一施工目录 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`；本地分支 `codex/af-modularize-j04-20260918`；GitHub `https://github.com/klfwdf/AnimusForge.git`，比较/未来交付分支 `origin/codex/af-main-refactor-continuation-20260831`。本轮 fetch 后远端仍 `b9a52c8f`，无远端新增分叉；没有推送。
- 原计划 C1/C2/C3 已按本节限定离线范围闭合；G1/G2/J07a 的不变证据沿用，不再重开调查。当前用户要求“直接完成，再接自动化做 J08”，因此下一轮从 J08a 开始，不再把文档/源码投影维护当成 J07 产品进度。

## 1. 本次真正改变了什么

| 责任 | 当前代码位置（一基行号；完整坐标以代码图为准） | 实现 / 保留边界 |
| --- | --- | --- |
| 唯一入口与四阶段编排 | `ShoutBehavior.cs:20081-20085`；`src/modules/AF.Module.Conversation/Channels/Native/NativeConversationTurnCoordinator.cs:10-21` | 454 行混合回合体被替换为实际协调器；顺序为准备→Prompt/历史→正文/展示→后处理/提交。每个阶段的停止结果阻止下游，默认结果拒收；异常原样向原 admission/UI owner 传播，禁止重试提交 |
| 请求身份与游戏捕获 | `ShoutBehavior.NativeTurn.cs:69-105,107-178` | 每个已准入回合独立 host；原 admission/Hero/Character/manager/token 不重新从槽取得。共享捕获边界在主线程先验身份，再执行；携带/恢复 ExecutionContext，捕获异常不被通用调度 fallback 吞掉 |
| Prompt/历史阶段 | `ShoutBehavior.NativeTurnPrompt.cs:66-130,132-190,192-233` | 保留唯一历史 Task.Run fork/join、join 后再验身份、原五步 Prompt、pending 输入与 AFEF 消费。真实领域资格、物资/队伍上下文、消息装配回主线程。非 Hero 显示名在组消息时捕获，展示 worker 不再触发冷 Culture/NameGenerator 缓存读取 |
| 正文/raw/可见输出 | `ShoutBehavior.NativeTurnPresentation.cs:23-109` | 继续调用已验 MainReplyStage；保留 raw meeting taunt→scene taunt→观察→清理→提前 TTS 顺序与原条件；后处理前先发布主文，失败提示/开场/无语音情况不改 |
| 后处理及权威完成 | `ShoutBehavior.NativeTurnCommit.cs:48-160,162-233` | 领域规则资格、直接 GCCZ 桥调用、决斗筹码、目标 metadata 在校验后捕获；复用原 Scene 的 Prepare/Request/Complete，不造第二条管线。Prepare 与 Complete/Normalize 在游戏线程，只有 detached prompt strings 进入后台网络；再走原唯一 action dispatch/completion |
| 类型与兼容 | `NativeConversationTurnCoordinator.cs:25-44` | internal 同 DLL 端口/值类型阶段结果，不是外部 API；未新增 SyncData key/持久类型、未切入口开关。UI、public V1、存档与各业务 owner 未改 |

**不是只换文件名**：模块协调器实际决定阶段推进/停止/终态，宿主只保留请求作用域的游戏适配和既有 Prompt/领域 helper 接线；原主入口不再拥有整回合控制。各职责方法为 9–113 行（参考值，不是验收目标）。主文件现在 39,190 行，仍有其他渠道/领域职责；不能称整个 Shout 大类或全项目“拆干净”。

**C2 安全修正**：此前后处理同步 wrapper 把 Prepare 与 Normalize 也放在 worker。新 Native 路径使用它已有的三阶段真实实现，准备/完成分别重验原身份，不把同步 LLM 放主线程。已有 busy 后消费 opening、只释放自己的票据、ConversationEnded epoch、已领取等待真实结果、提交 unknown 不重试、捕获上下文/revision 的迟到关窗保护不改。所有动作、历史和 AFEF 仍由原权威 owner 负责；没有把政策/宴会/GCCZ 玩法搬入 Conversation。

## 2. 验收清单与证据等级

本地日志目录 `.tmp/j07-closeout-20260921/`（不进 Git），可执行测试入口留仓库。所有数字只是对应 fixture/构建范围，不能相加当玩家功能覆盖率。

| 类别 | 本轮结果 | 限定 |
| --- | --- | --- |
| Native 五组 | Admission 44、Preparation 589、Pending 111、Dispatch 91、Completion 184：PASS | 原真实 owner/动作队列/回执边界；领域与游戏端口有明确替身 |
| 新回合与线程 | NativeTurn 98 检查；6 个变异编译成功并命中指定失败断言；4 个来源/接线测试：PASS | 实际协调器、实际 Capture helper、实际后处理调用片段；物理独立队列线程与 fake provider，不是 Bannerlord Host |
| 既有 Native 交互 | MainReply 19 场景/179；raw 37；presentation 46；TTS fallback 14：PASS | raw 的 TTS 仍是线程 spy；不是声音实际播放 |
| 历史 / 生命周期 | HistorySnapshot 852 + Native 27；InteractionRequestLifetime 51；GameLifetime 36；主线程队列 132：PASS | 保留原 fork/join 与 started/timeout 行为，不以新增 callback 取消已领取工作 |
| Pipeline / 共同接缝 | Pipeline 40、native commit failure 4、commit boundary 69、receipt 39、async owner 18、anonymous prompt 13；Scene postprocess 71；ChannelCutover 132：PASS | 旧领域 helper 替身明确；Scene/Courier 业务源码未改，未开放新公开提交能力 |
| Prompt 接线 | ProductionConsumers、Scene/Native wrapper 3：PASS | 直接读取新 host 方法的调用顺序/参数；原五步 Native 与 Courier schedule 断言保留 |
| 子 MOD / 内部隔离 | NativeModuleSubmission 41 + 外部 internal 访问 CS0122 拒收；实际 4 个 DLL metadata 1060：PASS | 不等于真实子 MOD 加载或 CLR/Bootstrap 实机时序 |
| 构建 | Debug/Release × 1.3/1.4/Bootstrap：6 项均 0 warning / 0 error | 原构建脚本，仅环境 wrapper；SDK 8.0.422，真实引用 1.3.15.110062 / 1.4.6.115628 |
| 存档 / 地图 | 142 literal keys、168 typed bindings、13 chunked、44 flattened：PASS；310 锚点双模式 + Phase8 inventory：PASS | 存档契约不是加载旧存档的实测 |

**测试来源透明**：`tools/NativeConversationAdmissionTests/turn_extraction.py` 从当前阶段体还原算法，比较完整 token 顺序/参数及主文件周围内容，允许的差异只有已验证的调度边界、typed 返回、请求局部变量存储和显示名捕获。既有夹具通过该算法投影适配搬迁，继续验证原 owner；它们不承担新增阶段调度证明。新增 `tests/modules/AF.Module.Conversation/NativeTurn/` 才执行新协调器/捕获/后处理片段。另有负例证明**即便刷新新增文件摘要，删改 TTS 传参仍会被算法对照拒收**；没有只刷 hash 消红。

**本轮定向修正记录**：集中门禁发现 ProductionConsumers 仍寻找旧单方法中的 wrapper 调用，已改为读取实际 BuildPromptAsync 与 PostprocessAndCommitAsync，并断言 Prepare→Request→Complete；没有删掉顺序断言。一次手工运行 HistorySnapshot 漏传已有 `DOTNET_EXE`，默认机器 dotnet 无 SDK；仅补环境变量后 852 通过，未安装 SDK或改生产引用。

**性能边界**：每次 Native 提交增加一个短生命周期 host/阶段状态和最多 7 个串行、有身份校验的 capture callback；没有每 Tick 扫描或新无界队列。物资/历史/资格 work 本来就存在，现在回到正确线程；游戏帧成本未量测，不能宣称“零成本”。已有 NativePerf 记录各 callback ms，可供真实 Campaign/Mission 后续采样。

## 3. 删除与保留、回滚

- 删除旧 454 行单体编排；只有一个 Native 协调入口，没有保留并行旧回合实现或新 feature flag。
- 保留原通用 `TryRunSceneUnifiedActionPostprocess`：Scene 仍有真实同步消费者；不能因为 Native 已改三阶段就删除。
- 保留 NativeAdmission/PendingHistory/ActionDispatch/Completion/MainReply 游戏适配、原公开 facade、UI/Harmony/存档身份。它们有真实调用与兼容责任，不用删除活路径冒充清理。
- 仅暂存本轮文件；新测试 `.generated` 已显式忽略，未纳入提交。`.dotnet-cli-home/` 是原有本地未跟踪目录，保留。没有写旧 AF-REFACTOR、NEW-10、GCCZ、游戏或存档，没有推送/部署/Stage/打包/默认切换。
- 回滚点 `e110d562`；需要回滚时对 `e5c14b8a` 做有针对性的 inverse/revert，并同步还原依赖本次文件位置的测试/地图/文档。不要 hard-reset、强推或覆盖其他作者。

## 4. J08 接棒的有限清单

J08 执行沿用[原 J08 总计划](#j08-llm-传输--模型目录)。**只在出现新的具体复现或实际源码变更时重开 J07 的相关测试，不再重跑无关历史调查。**

| 包 | 真实责任与入口 | 退出门 |
| --- | --- | --- |
| J08a | `ShoutNetwork.cs` 非流请求与真实 Gateway 消费者→`src/modules/AF.Module.Llm/Transport` | 唯一发送/响应/取消/超时 owner；fake HttpMessageHandler 下同输入请求与回复、400 thinking fallback、空回复一次补救保持 |
| J08b | 同文件流式请求→`Streaming`，复用既有 `LlmVisibleReplyNormalizer` | SSE 分片、Unicode、取消/关闭、过滤和回调次序覆盖；不能每字符拆坏 Unicode 或新开第二条流管线 |
| J08c | 既有 ConfiguredChat/ModelCatalog/Policy/WorldDiplomacy Gateway 与配置适配 | 收拢真实 transport/model owner；领域 prompt 与业务不重写；内部 typed ports 与外部 public DTO 分离，所有真实消费者接通后才删旧实现 |
| J08d | TTS gateway 与传输 owner | 保留 Native/Scene 的 voice capture 和播放 owner、回退/取消；只迁传输，不把游戏音频生命周期当网络实现删除 |

J08 按完整责任包推进，最终跑相关 LLM/三渠道/Native 回归、双版本/Bootstrap、API/存档；小改动按风险定向验证，文档不触发六构建。真实 provider、Campaign/Mission、旧档、音频播放、帧成本仍 **NOT-RUN**。J09 标签/ActionPlan/领域回执与 J10/J14 留后续，当前自动化专注 J08，不顺带开放 Scene/Courier public 提交。

自动化复用 `af-7-8`、每 30 分钟接续；原截止 UTC `2026-09-20T17:48:36Z`（北京时间 9 月 21 日 01:48:36）保留，不擅自延长。J08 必要离线验收通过或到期/用户停止时记录真实完成度、技术/本地简明交接并暂停；剩余未完不能标 DONE。自动化当前配置以工具成功回执为准，文档本身不启动任务。

## 以下全部为历史回执，当前状态与接续以上方为准

<a id="j07-closeout-course-correction-20260920"></a>
# 当前执行要求：停止 J07b 微切片循环，整包收尾后进入 J08/J09（2026-09-20）

用户要求主动检查是否在钻牛角尖，并自主完成 J07 收尾、调整自动化接 J08/J09。审查确认问题在执行粒度与重复验证：此前票据/线程修复真实有效，但每个小改动都维护大量逆变换/变异并反复六构建，进度不应继续由测试工具驱动。具体 diff 统计、有限退出门及流程修订见[计划第 0.2 节](plans/j07-conversation-native-plan.md#j07-finite-closeout-20260920)。

- **本次只纠偏计划与自动化，不宣称产品新增或 J07 完成。** 产品基线仍 `d9e9aae1`；既有 298 锚点/正常回归/构建证据不因这次文档改动重复运行。
- J07 只剩 C1 完整回合编排、C2 Native 当前链的线程/pending/终态收口、C3 整包验收三个退出门。C1 未完成，C2 部分完成，C3 待最终候选；达到后记 `J07_OFFLINE_VERIFIED`，不是 LIVE/SAVE/全项目 DONE。
- 修改前检查点与审阅 diff 保留，但改为完整责任包集中验收、失败项定向复跑；保留已有断言，只为新风险补必要证据。第 5 节 25 条编号仍在；仅工程节奏第 18–19 条依新授权更新，其他安全/兼容边界不放松。
- 自动化仍使用现有 `af-7-8`，已更新为 **AF J07整包收尾 → J08/J09**、ACTIVE、每 30 分钟接续；J10/J14 保留总计划，不在当前自动化擅自开新 public API。
- 原工作窗 UTC `2026-09-20T17:48:36Z` 保留；截止/完成 J09 必要离线验收/用户停止时做两份交接并暂停。本地简明版改为 `.tmp/af-j07-j09-team-handoff-20260920.md`，不进 Git。不承诺在剩余窗口硬赶完三个包。
- 唯一施工树/本地分支/未来交付目标不变；不推送、部署、Stage、打包、切默认、操作游戏/存档或其他工作树。三份仓库 SKILL 和全局 AF 安全/清理规则继续适用，不更改全局 Skill/AGENTS。

## 以下为已有产品回执

<a id="j07b-mainreply-thread-boundary-20260920"></a>
# 当前接续：J07b 首个正文接收阶段已抽取，raw 展示线程问题已修复（2026-09-20）

**J07b_IN_PROGRESS，仍不是 J07/J10 DONE。** 本轮检查点 `aeb1b53`；阶段实现 `00574541c7982e85b1565960ee92667aab41b2b7`；线程缺陷复现 `e478f699`；修复源码 `d9e9aae156c208d599f95354d10ba45e142155a0`。只在指定本地施工树实施，无推送/部署/Stage/打包/默认切换/存档操作。自动化继续 ACTIVE，工作窗仍截止 UTC `2026-09-20T17:48:36Z`（北京时间 9 月 21 日 01:48:36）。

## 真实职责与源码坐标

| 责任与一基坐标 | 本轮落地 | 保持 / 未覆盖 |
| --- | --- | --- |
| `src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyStage.cs:13-47` | 从主编排提取正文接收：完整回复归一化、provider await 后代际检查、目标验证、必要的 pending 撤销、空/错误回复终止，真实入口只调用一次 | 仍用原 `CallNativeConversationApiAsync`，没有第二条缩水管线；Ready 仅准许进入下一阶段，不是动作成功/commit 回执 |
| `NativeConversationMainReplyContracts.cs:8-49`（同目录） | 窄 typed internal 端口、验证结果、阶段状态/结果；默认 NotStarted 不准许下游执行 | 不是 public 子 MOD 契约，不新增存档类型/键或配置面 |
| `ShoutBehavior.NativeMainReply.cs:11-57` | private adapter 捕获原 admission、history key/sequence；将验证与撤销交回已有游戏线程操作；保留文本清理与失败提示 | 游戏身份不复制到纯 owner；没有按当前槽重新取目标；正常文字、四类错误前缀和提示标题原样保持 |
| `ShoutBehavior.cs:20302-20307` | 原 36 行接收算法由 6 行真实阶段调用/停止门接替 | `SubmitNativeConversationTextInternalAsync:20081-20534` 当前 **454 行（原 484）**，不是主编排拆完；全文件 **39,639 行、CRLF、无 BOM** |
| `ShoutBehavior.cs:20311-20349`、`:20536` observer、`:3803` TTS helper | raw 挑衅→自然动作观察→显示清理→提前 TTS 留在已有 `main_reply_action_validation` 回调；移除 worker 上的观察/提前 TTS 调用 | 两个 helper 方法体未改，没有新增排队点；`:20350` postprocess-start 复验仍在其后；统一后处理 `:20474` 仍先于最终动作派发 `:20506` |

## 发现与修复的线程问题（不冒充实机）

旧 Native 入口在 Task.Run 中运行，raw-action 主线程回调返回后的续体直接调用观察器；观察器读取 Mission/Agents。使用**真实旧调用点 + 真实 observer 方法**、拒绝 worker 游戏读取的可控 Mission/Agent 端口，复现一次跨线程读取被 observer 的 best-effort catch 吞掉、观察没有提交。验收用例在修复前确实 FAIL，移入已验证主线程回调后 PASS。

相邻的提前 TTS 调用也位于 worker；现有 TTS helper 包含 Hero/Character/voice/Mission 捕获。第二个复现使用真实调用点 + **明确标注的 TTS 线程亲和 spy**，不是运行真实语音引擎：修复观察器后 tableau 用例仍红，提前 TTS 调回同一游戏线程回调后通过。保留原提前触发条件、先于后处理的顺序和既有标志语义；没有把音频实际播放完成伪装成后端 Task 完成。

**性能边界**：正文阶段每请求增加一个短生命周期 adapter 与有限 async 状态，纯字符串判定，无新扫描/锁/Task.Run。raw 修复不增队列，最多增加本次已接受 Native 回复原有的 Mission-agent 查找及可选提前 voice/TTS capture 到正确主线程；不是每 Tick 轮询。真实 Agent 数量/帧耗时及音频耗时仍 NOT-RUN，不声称免费或 O(1) 扫描。

## 验证与证据范围

- **当前源码正常回归**：Native Admission **44**、Preparation **589**、Pending **111**、ActionDispatch **91**、Completion **184** 全部 PASS；正文阶段 **19 场景/179 检查**在 raw 修复后再次通过；TTS fallback **14** PASS。NativeModuleSubmission **41** + 外部访问 internal 的 CS0122 拒收在正文阶段候选通过，raw 改动没有修改 API host/端口。
- **正文对照**：执行真实新 stage、真实 private adapter、真实 6 行 consumer stop gate、真实 LlmVisibleReplyNormalizer；对照 `dabee763` 的原 36 行阶段，比较输出、调用顺序、pending 身份、提示、单次 provider 与异常身份。强制 provider yield 后改变 owner/epoch/ticket/target/generation；游戏队列/目标判定和 prefix/leak 清理 helper 是明示替身。`presentation-only` 不凭空给 backend admission 添加 revision 守卫，UI scope 仍按原 owner 另验。
- **正文 10 项变异**均编译并在指定场景以具名断言拒收：跳代际/目标、漏撤销/归一化/错误处理、错误 pending key/目标端口/失败标题、先空回复后验证、consumer 忽略停止门。初版 skip-generation 因诊断轨迹提前在正常场景失败，已调整变异保留观测调用，确认真正命中 load 场景；没有降低正常断言。
- **raw 边界 37 项 + 4 项变异**通过：普通/fallback/迟到目标/关闭观察/缺 Mission/缺 Agent/tableau/静默回复/解析器故障。移回 worker、重复观察、去掉目标校验、提前 TTS 移回 worker 均按预期红。旧 `00574541` 的两类线程端口失败保留可重现；真实游戏、真实 TTS/provider 全部 NOT-RUN。
- **pending 旧回归不失效**：第一失败分支改执行真实新阶段 rejection block + 源码提取的捕获 rollback port；其余四分支、旧 original 模式及所有断言保留。12 项 pending 变异通过；没有恢复旧主编排片段来冒充新阶段行为。
- **严格来源/格式**：两个新 source-review packet 与之前票据/claim 逆变换组合；旧 full-file/API/lifetime 断言仍保留。正文 5、raw 4、既有 ticket 4 / claim 3 / lifecycle inverse 5 项源证明通过，未知生产差异会被拒收。Native 最终请求 old/current fixture 本轮只重新编译；不声称完整请求差分全集本轮重跑。
- **环境小修**：TTS fallback runner 原先固定 ROOT.parent 下 SDK，嵌套 worktree 报 WinError 2；仅增加已有约定 `AF_DOTNET` 覆盖，保留原 fallback。随后 14 项通过；不安装 SDK、不改生产引用。
- **双版本/产物**：沿用原 `.tmp/build-local.ps1` 只传引用参数，生成根先检查边界/重解析点/非产物；最终源码 Debug/Release × 1.3/1.4/Bootstrap 六构建 0 warning / 0 error。实际引用仍 1.3.15.110062 与 1.4.6.115628，SDK 8.0.422；不是别的 1.4 补丁实机证明。最终四个实现 DLL 的 **1060** 项 API/metadata 断言 PASS，public V1/旧 memory 签名及 internal 隔离不变；存档契约 **142 literal keys / 168 typed bindings / 13 chunked / 44 flattened keys** PASS，见本地 `api-delivery.log` / `persistence-delivery.log`，不是 CLR/旧档加载实测。
- **导航**：298 锚点地图绑定 `d9e9aae1`，recorded / working-tree PASS；Phase8 entry inventory 检查 PASS，不提高准备态证据等级。日志位于本地 `.tmp/j07b-mainreply-20260920/` 和具名套件 `.generated/`，不纳入 Git。

## 下一安全切片与收工边界

1. **继续 J07b**，将已线程归位的 raw/展示阶段抽成实际 typed 阶段，或提取后处理上下文/资格的捕获责任；逐个阶段编译两 API + Native 五组。Prompt 文本仍由原 J04 owner/适配接缝承担，不能把同步后处理 LLM 请求直接塞到主线程而制造卡顿。
2. 主编排的准备、历史 fork/join、消息装配、后处理资格/直接命令、最终提交与其余回滚决策仍未完成分层。对剩余 worker 的游戏属性读取继续按实际调用链核验；**本次两处修复不等于整个 Native 已线程安全**。完整 NativeStageSequence / Lifecycle / Rollback 包验收、J07c/d 均未闭合。
3. 当前第一阶段只允许 one provider call、异常传播、原终止顺序；未来调整新增文件时也要独立证明其变化，不能直接刷新旧 addedFiles hash 使历史逆变换失去意义。无需重开已闭合 G1/G2/J07a 或重复搬旧路径。
4. **J08/J09/J10 尚未完成**，不为了时限跳过所需离线行为或删仍有消费者的旧实现；J14 Scene/Courier 公开提交不在本轮擅自开放。实机/旧档/真实 provider/音频/帧耗时仍 NOT-RUN。
5. 回滚采用 focused inverse/revert：线程修复 `d9e9aae1` → 接收阶段 `00574541`，保留或单独处理复现测试 `e478f699`；初始检查点 `aeb1b53`。不 hard-reset，不操作其他工作树。
6. 当前施工树/分支不变；代码和详细台账仅本地。窗口仍在未来，自动化继续；到 J10 必要离线门槛全过、截止或用户停止，再写技术/本地制作组两份最终交接并暂停，不能把本轮输出当暂停。

## 以下为此前准入/claim owner 回执

<a id="j07b-native-ownership-20260920"></a>
# 当前接续：J07b 准入身份与排队领取已抽取，完整阶段编排仍未完成（2026-09-20）

**当前状态：J07b_IN_PROGRESS；不是 J07/J10 DONE。** G1/G2 与 J07a 复用上一节已绑定证据，不重新调查。检查点 `a893fea`；票据 owner `42ac364d1e53d28ebd6d422c9e29236ac439c3ad`；排队 claim `2835d1a56ab6e447a31473a802101e8a33efac5d`。全部本地提交；未 push、Stage、Deploy、打包、切默认或写游戏/存档。自动化 `af-7-8` 保持 ACTIVE，截止 UTC `2026-09-20T17:48:36Z`（北京时间 9 月 21 日 01:48:36）。

## 实际迁移、消费者与仍保留的职责

| 当前源码坐标（一基） | 已接线责任 | 未迁移 / 保留原因 |
| --- | --- | --- |
| `src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs:11-43` | 唯一后端票据槽、会话 epoch、展示 revision；引用身份、只释放自己、ConversationEnded 失效；替换宿主原三字段 | 不读取游戏对象，不持有 SaveRuntimeGuard generation；这些检查仍由宿主所属线程负责，不制造第二份带 Hero 的 DTO |
| `ShoutBehavior.NativeAdmission.cs:14-15,96-155,182-234` | 普通 / 主动开场 / Overlay 准入与 scope 使用同一 owner；busy 仍先于 opening 消费，异常与 finally 只释放本请求 | 实体快照及当前 Mission / Manager / Target 检查保留 adapter；不能删仍被 UI / API 调用的宿主入口 |
| `ShoutBehavior.ModuleNativeSubmission.cs:9-47` | 子 MOD 排队提交读取同一 epoch/revision，领取前原样重验；外部签名、结果语义未变 | Scene/Courier 新公开提交仍不开放，归 J14，不借本轮改 NotSupported |
| `ShoutBehavior.NativeCompletion.cs:68-72,123-137`；`ShoutBehavior.cs:15530-15534` | completion/晚关窗与 pending 撤销共用 revision 判定；仍按捕获上下文，不依赖已释放的 busy 槽 | 历史、AFEF 与动作副作用仍在原权威 owner；没有新增 SyncData key / 类型 |
| `src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs:5-30`；`ShoutBehavior.NativeAdmission.cs:102-120`；`ShoutBehavior.cs:19576-19619` | 原 0/1/2 CAS 收为显式 Queued/Started/ExpiredBeforeStart；真实 admission capture 与 action dispatch 共用；超时、retirement、队列异常都只能争抢尚未开始项 | 已领取项等待真实结果，unknown-after-start 不改为可重试；pending-history / 通用调度自身的其他 claim 暂未迁移，不为统一名字扩大改动 |
| `ShoutBehavior.cs:20081-20564` | 本次未移动 484 行真实主编排，先稳定其会话身份与终态基础 | 人物就绪、准备、历史 fork/join、五步 Prompt、正文、提前特例、后处理、动作/完成阶段仍待逐段抽取；不可用新增 owner 数量冒充“拆薄完成” |

**性能与清理**：票据 owner 每个 ShoutBehavior 一次分配，各次读写仍 O(1) / 原 Interlocked 与 Volatile 语义；没有新增每帧扫描、反射或锁。dispatch claim 是原共享闭包内 int 的值类型替代，不增加独立 heap 对象；发布后不得复制该值。宿主旧三字段、两处裸 dispatchState 及 completion 未用 using 已删除。未测真实帧成本，不声称全链路无 worker 游戏读取：后续主编排拆分仍须沿真实 Task.Run/await 逐段审查。

## 真实验证（离线分层，不冒充游戏）

- **最新候选 `2835d1a5`**：Native Admission **44**、Preparation **589**、Pending **111**、ActionDispatch **91**、Completion **184**；Presentation **46**；NativeModuleSubmission **41** + 外部消费者访问 internal 的 CS0122 拒收，全部 PASS。
- **新增 owner 行为**：NativeTicket **21**（真实异步 gate、同值不同引用、late finally、独立 owner）；NativeDispatchClaim **13**（真实 yield、已领取后过期尝试、共享闭包并发领取）。各 **7 / 4** 项变异均编译成功并因指定具名断言失败，不能把编译/路径错误算红例。
- **相关既有负例**：最新 claim 候选 Admission **7**、ActionDispatch **9** 项全部行为拒收。前一票据候选 `42ac364d` 的 Presentation **6**、Pending **12**、Completion **15** 以及 API generation/epoch/revision **3** 项行为拒收；后续 claim 切片对这些 runner 的正常项重新验证，上述负例不混写成最新候选全量再跑。
- **严格源码证明**：两个独立 review packet 分别绑定 `d2b167cc` / `42ac364d`，可将 4 个宿主文件精确恢复到前一切片；当前三项 claim 接线/字节检查、四项票据源检查、五项历史 inverse 反例通过，旧 API / game lifetime 完整源码断言仍保留，没有直接刷新旧 hash 消红。
- **fixture 修正**：改用真实 EndConversation 后会清槽，Pending 旧 fixture 在 Reject 时重读槽会空指针；现保留 Prepare 时捕获的 ticket 再拒绝，保持原断言，历史 original 模式不改。旧 Admission 并发/晚捕获与 Pending 越线程/错 key 缺陷仍可重现。不是修生产断言来凑 PASS。
- **构建**：两切片均执行现有 `.tmp/build-local.ps1`，仅传环境/引用参数，无 Stage/Deploy；最终 Debug/Release × 1.3/1.4/Bootstrap **六构建 0 warning / 0 error**。实际引用：1.3.15.110062 `_deps_auto`；1.4.6.115628 本机 build_check，不能写成 1.4.7 实测。SDK 8.0.422 / 同 SDK Newtonsoft；未安装新 SDK。四个生成根每次重置前已核实工作树内、无重解析点、无 tracked 或非产物文件。
- **实际产物 / 存档契约**：最终四个实现 DLL **1060** 项元数据断言通过；公开 V1/旧 memory 签名不变，internal 隔离保持。Persistence **142 literal keys / 168 typed bindings / 13 chunked / 44 flattened keys** PASS。不是 CLR/真实外部 MOD/旧档加载证据。
- **源码格式 / 地图**：ShoutBehavior 仍 **39,669 行，全 CRLF、无 BOM**，只是具名行替换；地图 **294 锚点**绑定 `2835d1a5`，recorded / working-tree 检查。地图 hash 使用和 verifier 相同的 CRLF 规范化，不抹平其他文件既有的裸 CR；没有修改那些生产文件。
- 日志仅本地 `.tmp/j07b-20260920/`：`*-claim.log`、`build-*-claim.log`、`api-claim.log`、各 `*-mut.log` / `api-mutations.json`；新套件 `.generated/*/run.log` 与 `mutations.json` 均忽略 Git。实机、旧档、真实 provider/TTS/帧耗时全部 **NOT-RUN**。

## 下一步、回滚与工作窗

1. **继续 J07b 真实阶段编排**：先选一个完整具名阶段，从 `SubmitNativeConversationTextInternalAsync:20081` 开始逐段抽取；每次两 API 编译 + Native 五组，不一次重排整段。当前 9 个主线程往返与所有重验点仍在真实入口；不得为满足结构断言放宽测试。
2. 票据三字段与 dispatch claim 已有唯一 owner，不再造重复 Ticket 或名义 AdmissionPolicy 转发壳。游戏对象仍在 host adapter，纯阶段 owner 只收冻结数据/明确的 typed 端口；Prompt 文本构造属于 J04，不搬成 Conversation 的新文本工厂。
3. 保持 busy→opening、ConversationEnded→epoch、历史 fork/join→重验，以及统一后处理→动作派发的顺序；原文挑衅/场景观察/提前 TTS 特例保持。后续可以组合目前两个严格 inverse packet，再给真实阶段迁移独立证据，不反复重开 J06/G2。
4. J07c/d、J08 LLM、J09 Actions、J10 Scene/Courier **未完成**；新增 Lifecycle/完整 StageSequence/Rollback 证据仍待落地。不能用本页状态代替最终全范围收尾，J17 不在本工作窗目标内。
5. 回滚按后进先出的 focused inverse/revert：claim `2835d1a5` → admission `42ac364d`；切片前检查点 `a893fea`。不得 hard-reset / 改写历史；若当前又有后续改动先检查依赖。
6. 唯一施工树 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`、本地 `codex/af-modularize-j04-20260918`；未来交付仍 `origin/codex/af-main-refactor-continuation-20260831`，本轮不推送。保留 `.dotnet-cli-home/`。期限未到保持自动化 ACTIVE；到 J10 离线门槛全过、截止或用户停止，再写详细/本地制作组两份交接并暂停。

## 以下为已完成 J07a 及更早回执

<a id="j07a-relocation-20260920"></a>

## 2026-09-20：G2 实体/请求补强与 J07a 归位已落地，继续 J07b

**提交与范围**：G2 测试 `059006a4`；J07a 产品路径/消费者 `fb5dc1ca756153889c0eb25a59419cafb5dfc08e`；严格历史逆变换修复 `30b90716`。本轮全部本地提交，未 push。J07a 是原样归位，不是 Native 编排已经拆薄；J07b/c/d、J08–J10 尚未完成。自动化保持 ACTIVE，截止仍为 `2026-09-20T17:48:36Z`，不能将这次输出当成暂停。

### G2 已验证范围（源码仍为原 J06 实现）

- `tests/modules/AF.Module.Knowledge/EntityTextDifferential/{Program.cs,WorldCases.cs,run.py}`：从两个源码版本提取实际非 Hero formatter、常驻添加、可见队伍筛选/排序、ID 投影与捕获/匹配/恢复方法，替换原空列表/throw 桩。19 场景、57 个主文/后处理/计数字段对照通过；覆盖定居点、家族、王国、混合对象、玩家/对话者常驻、主文禁王国但后处理保留、附近可见/过远可见及拒绝类别、capture-null 与 worker-null 回退。
- 10 项变异均编译后因具名断言拒收：原 4 项，另加删除定居点/家族/王国正文、常驻后处理、捕获可见队伍和显式王国 ID。日志 `.tmp/j06-g2-20260920/mutations.json`；测试源码固定在 `059006a4`。
- `SharedCompletionDifferential` 增加 `world_shared` 家族和 `--world-only`：同一问题、名词列表及 npc_1/Alda 身份投影用于 Lore/规则、生产派生世界实体正文、共享 completion、Native 最终消息、Courier 来信/回信请求。总计 16 场景 PASS；新非 Hero 家族单独执行 drop-lore/entity/rule 三个负向变异，两渠道完整请求均按预期变化。ID 集合和实体计数也进入共享结果，不能只验证非空字符串。
- 仍是生产方法提取＋可控游戏替身/文本端口组合，不是完整 Bannerlord Host 或真实 provider。位置、外交/舰船外部端口为受控数据；实机耗时、实际旧档及未枚举语义变体不由这些用例覆盖。J06 父包不因测试数增长就宣称全功能完美。
- 修复此前 `2946bf3d` 引入的 Courier production-context 测试分支未更新固定摘要：先追溯 stored hash 到 `3aaece30`，审阅新增 fixture 分支；保留全部普通断言与全源码 inverse，叠加本轮可选共同输入后仅更新 Harness 摘要。`source_review.py` 的完整逆变换与 3 项未审改动拒收通过，不是刷新产品 hash 凑 PASS。

### J07a：10 个文件 100% rename，职责和身份不变

命名空间、类型、可见性、方法体、接口/存档身份全部原字节不变；没有第二份活动实现。旧 `Refactor/Contracts/InteractionContracts.cs` 和领域回执仍在原位。迁移映射、逐文件 SHA-256 与前后成员集原始证据在 `.tmp/j07a-20260920/`；Git commit 显示全部 10 个 rename 为 100%。下列行号属于 `fb5dc1ca`：

| 当前源码与类声明行 | 原位置 |
| --- | --- |
| `src/modules/AF.Module.Conversation/Internal/InteractionRequestCoordinator.cs:15` | `Refactor/Runtime/InteractionRequestCoordinator.cs` |
| `src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs:12` | `Refactor/Runtime/InteractionRequestLease.cs` |
| `src/modules/AF.Module.Conversation/Internal/DetachedInteractionHost.cs:16` | `Refactor/Runtime/DetachedInteractionHost.cs` |
| `src/modules/AF.Module.Conversation/Internal/InteractionResultCommitter.cs:16` | `Refactor/Runtime/InteractionResultCommitter.cs` |
| `src/modules/AF.Module.Conversation/Internal/InteractionCommitReceiptCache.cs:13` | `Refactor/Runtime/InteractionCommitReceiptCache.cs` |
| `src/modules/AF.Module.Conversation/Internal/NpcPersonaGenerationOwner.cs:10` | `Refactor/Runtime/NpcPersonaGenerationOwner.cs` |
| `src/modules/AF.Module.Conversation/Internal/PersonaGenerationWaiter.cs:9` | `Refactor/Runtime/PersonaGenerationWaiter.cs` |
| `src/modules/AF.Module.Conversation/Internal/RuntimeConfigSnapshotStore.cs:13` | `Refactor/Runtime/RuntimeConfigSnapshotStore.cs` |
| `src/modules/AF.Module.Conversation/Internal/Pipeline/InteractionPipeline.cs:12` | `Refactor/Contracts/InteractionPipeline.cs` |
| `src/modules/AF.Module.Conversation/Internal/Pipeline/FullInteractionPipeline.cs:14` | `Refactor/Contracts/FullInteractionPipeline.cs` |

- 同步真实项目/提取 runner/当前地图/Bridge manifest 路径。旧 pinned Git replay 仍读取旧提交的历史路径；这两个历史查找是明确保留的测试兼容，不是产品旧实现。源码 Compile 前后严格路径映射：1.3/1.4 各 **816 Compile / 7 EmbeddedResource**，无重复或遗漏；全部 10 个文件字节 SHA 不变。
- 原构建脚本经 `.tmp/build-local.ps1` 只传本机参数，Debug/Release × 1.3/1.4/Bootstrap 六项成功，均 0 警告/0 错误。实际引用：1.3 **1.3.15.110062**，1.4 **1.4.6.115628**（`G:/AFMOD/AF-REFACTOR/.tmp/build_check/1.4`），不要误写为验证了 1.4.7。预检四个固定 bin/obj 生成目录在本工作树、无 reparse、无 tracked 文件/非构建内容；无 Stage/Deploy。首次选用 Windows PowerShell 5.1 遇脚本策略阻止后，改用本来已启用 RemoteSigned 的当前 PowerShell 7.6.5 正常调用；未设置 Bypass 或修改执行策略。
- 生命周期 51 项、已按旧 main 编译且不重编的客户端替换运行、Pipeline 40、提交边界 69、回执 39、async owner 18、匿名提示 13 通过；DuelDispatch 16、Courier inbound completion、Notoriety 14、EconomyAware 执行契约通过。
- 实际消费者：Hero persona 125、Channel persona 169、Courier commit outcome 34、GameLifetime commit 19、Courier postprocess 39 通过。测试替身有 CS0649 警告，与产品六构建零警告分开。
- 实际四实现 DLL 的 API 元数据 **1060 项**、公开快照 5 个反例和 internal 不可访问编译拒收通过；Native 外部提交 41 项通过，未开放新的 Scene/Courier public 能力。PersistenceProfileConfig：142 literal keys / 168 typed bindings / 13 chunked keys / 44 flattened keys，状态/幂等与未知数据检查通过；IdentityAudit 工具自身 5 项通过，不将其当真实旧档验收。
- `entry_inventory.py` 原来把所有含 modules 的路径当部署目录，误排新 `src/modules`；现只允许该生产前缀，嵌套 bin/obj/部署 Modules 仍排除，新反例已覆盖。顺便发现既有 Courier 6 个入口未入清单，按工具既有规则补录并将其 entryCoverage 从 COMPLETE 降为 REPRESENTATIVE、owner 保持 ASSIGNED，留待 J10 重审；11 项 inventory 测试和 Bridge binding 16/12 wired/4 declared 检查通过，不假报域完成。

### G0 旧红根因及严格修复

Native 五组初跑为 Admission 44、Pending 111、ActionDispatch 91、Completion 184 PASS，Preparation 因 `Unreviewed J04 Shout target apply` 失败。`ShoutBehavior.cs` 字节 SHA 仍为 `973ac6d0ef172ce140de39b1306c93df4af07bfe63483c2aaca47e09a47c83e4`，本轮没有修改此主类。

`30b90716` 在 `tools/GameLifetimeTests/source_parity.py` 增加 **bd2582aa 已提交的两行 J06 capture/publication 的精确逆变换**，以及 J05 两条已核实 Memory 路径、J07a 一条 committer 路径的严格逆映射；固定旧摘要和最终全源码相等断言保持。新增 `test_source_parity.py` 的 5 项覆盖正常全文件还原、错误 target、删除 capture、无关源码追加、runner 非路径修改拒收。五份 lifecycle 全文件 inverse 全部 PASS；CaptureEligibility 12 项 PASS；NativePreparation 重跑 **589 项 PASS**。因此初跑旧红已解决，不再把它留成环境/实机阻塞。其 5 项既有生产方法变异全部编译执行并因预期具名断言拒收，记录在 `.tmp/j07a-20260920/preparation-mutations.json`；后台 capture 变异被现有主线程守卫拒绝为无快照，对应 healthy capture result 失败，不能把它误记成发生了后台游戏读取。

### 下一步 / 未覆盖

- **直接进入 J07b1**：按现有计划拆 Native 准入/票据身份，随后逐个抽 484 行方法的真实阶段；每个阶段编译＋Native 五组，保持先统一后处理后权威动作的主链、早期副作用特例、票据只释放自己与 unknown 不重试。不要再次把 G1/G2 或 J07a 当未做工作重新开盘点。
- J07a 只完成目录/消费者路径归位；Native 状态机未迁出、主类行数未减，J07 父包仍未验收。J08/J09/J10 按主台账继续；公开 Scene/Courier 提交仍归 J14，不扩大本轮开放范围。
- 源码地图绑定 `fb5dc1ca`，291 锚点 recorded/working-tree PASS。代码与离线证据不代替真实游戏、旧存档、provider、音频或外部 MOD 实际加载。自动化 ACTIVE；未 push、Stage、部署、操作存档或其他工作树。

<a id="j06-j10-automation-20260920"></a>

## 2026-09-20 自动接续：G1 专项通过，G2 待执行，目标 J10 离线验收

- 用户授权本地实施到 J10，并要求自动接续；已更新既有 `af-7-8`（30 分钟，ACTIVE），工作窗 `2026-09-20T11:48:36Z` 至 `2026-09-20T17:48:36Z`。到期或达到 J10 必要离线门槛后暂停并写两份交接；不是承诺六小时内全功能完美。
- 起点 `b9a52c8f` 与指定远端一致；唯一施工树 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`，本地 `codex/af-modularize-j04-20260918`。只有原未跟踪 `.dotnet-cli-home/`，没有同 owner 的外来 tracked 改动。单代理工作，禁自动 push/部署/Stage/打包/存档/其他工作树写入；Claude 同文件并发或远端分叉时暂停相关写入。
- 本切片意图：只改 `tests/modules/AF.Module.Knowledge/EntityTextDifferential/{Program.cs,run.py}`，统一 capture/worker/complete 的原文，补原文独立命中、王国限定、同称谓多国、长短称谓遮蔽和 worker 原文分支删除变异。旧侧仍从 `77a3d234` 提取真实方法，新側读取当前源码；保持其他行为断言，保留供共享最终请求 runner 使用的导出字段。
- 完成条件：正常原/新正文、ID/计数、顺序对照通过；仅新侧删除 raw 分支能编译并在对应具名案例失败，原有 3 项变异仍拒收；共享 12 场景请求回归不破坏。产品源码本轮不动。G2 非 Hero/常驻/可见队伍仍独立未完成，G0 全量基线审查不因 Git 已同步而算通过。
- 续作及结果补在本节；J07–J10 未开始，详细顺序沿现有 J07 计划和本台账 J08–J10 总计划。代码地图/源码未变时复用绑定证据，记录本次实际 runner，不无限重复全构建。

### G1 结果（测试源码 `9f9faea44878eac598ac2806638e7889c11f7f33`）

- 修改 `tests/modules/AF.Module.Knowledge/EntityTextDifferential/Program.cs:119-265`：旧 direct/title 统一共同 input 进入 capture、worker、complete；新增 `RenderRaw` 六场景（raw_only、raw_qualified、raw_ambiguous、raw_long_title、raw_distinct_titles、raw_overrides_mentions），用两国 King 与一国 High King 验证身份/计数、非空文本、候选覆盖和长短称谓遮蔽。旧生产同步与新 detached 及同步回退正文/后处理/计数/显式王国集合逐字段比较，原导出字段保留。
- 修改同目录 `run.py:15,148-151,171-178`：增加仅新侧生产 worker 的 `skip-worker-raw` 变异；完整导出必须有 8 场景 × 3 字段，逐键对照而非只比较数量。
- **先确认漏检，再补覆盖**：只增加该变异、还未扩展 fixture 时，删除新侧 worker raw 分支依旧 PASS（exit 0），证实旧测试盲区；补用例后它编译执行并在 `raw-title coverage failed scenario=raw_only` 失败。没有修改生产实现或放宽旧断言。
- 正常 `python -B tests/modules/AF.Module.Knowledge/EntityTextDifferential/run.py`：8 场景、24 字段 PASS；原 `drop-hero-main`、`drop-hero-post`、`drop-capture-fallback` 和新增 `skip-worker-raw` 四项均 exit 非零且命中预期业务断言，没有以 CS/NU 编译或依赖错误计作拒收。
- `python -B tests/modules/AF.Module.Prompt/SharedCompletionDifferential/run.py`：12 场景 PASS，旧/新共享完成体与 Native/Courier 最终请求兼容；本次没有重跑该套的三项变异，不冒称扩大了其中非 Hero/真实 Host 覆盖。
- 环境显式指定 `AF_DOTNET=G:/AFMOD/.dotnet-sdk/dotnet.exe`（8.0.422）、`AF_NEWTONSOFT=G:/AFMOD/.dotnet-sdk/sdk/8.0.422/Newtonsoft.Json.dll`，禁写 Python bytecode。本地日志 `.tmp/j06-g1-20260920/{mutation-results.json,*.log}` 不上传；Python AST、diff --check、冲突标记检查通过。产品源码/公开 API/存档/主类行尾未改，因此未执行产品双版本构建。
- 下一步 **G2**：扩大实际非 Hero 和常驻/可见队伍方法覆盖、共同输入的请求级接线；目前测试的中性 Hero/假游戏字段不证明真实游戏帧性能或全部实体等价。G0 仍要按 J07 实际改动核实必要五组/API/存档基线。自动化继续 ACTIVE，不暂停、不推送；回滚仅针对 `9f9faea4` 的测试差异，禁止回退生产版本。


<a id="j07-plan-review-20260920"></a>

## 2026-09-20：补推 Claude 三提交，修订 J06→J07 接续计划（PLAN_READY / NOT_STARTED）

本轮授权是核对/补推已有成果并制定后续计划，**不开始生产重构**。工作树 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`，本地分支 `codex/af-modularize-j04-20260918`；指定发布目标 `origin/codex/af-main-refactor-continuation-20260831`。fetch 后远端仍为 `2946bf3d`，本地领先 3、落后 0；已普通快进推送 `08699b4f`、`63e74e7d`、`b9b2215b`，并用 `ls-remote` 核实 `b9b2215b954d5f1c367fe66fd3d8092558a894fe`。无需开新代理、未修改网络代理配置；未强推、未动 main。

### 当前结论与修订

- **J06 仍 `VERIFY / NOT_ACCEPTED`，J07 仍 `PLAN_READY / NOT_STARTED`**。本节取代下方“J06 剩余仅实机”“最终生产文本对照全部闭合”的过宽解释，不否认已通过的限定 fixture 差分。
- `EntityTextDifferential/Program.cs:136` 后台匹配收到空原文，而 capture/complete 为非空；`:99-113` 非 Hero 与常驻实体有置空/throw 假端口。前者需补共同输入和 raw-only/限定/歧义/遮蔽反例，后者可增加生产方法＋可控假游戏对象的离线正文差分。真实属性成本、实机/旧档/provider 分开，不全部归为离线不可闭合。
- 后续唯一详细执行计划仍为 [J07 原计划文件](plans/j07-conversation-native-plan.md)：G0 基线→G1 原文分支→G2 非 Hero/最终请求→J07a 10 文件归位→J07b 准入/编排/终态→J07c 交互→J07d 包验收。安全纯 rename 可独立提前，相关证据缺口不在行为签收时豁免。
- 修正 J07 流程图：`ShoutBehavior.cs:20504` 统一后处理在 `:20536` 主线程动作派发前；`:20349` 原文挑衅、`:20376` 提前 TTS 等既有特例保持。不重排为“先动作再后处理”。Prompt 已是 J04/J06 五步，不再写三步。
- 搬迁清单为 8 Runtime＋2 Pipeline，共 10 文件；`InteractionContracts.cs` 保留原路径/类型身份。纯目录归位不改命名空间、可见性、API、程序集/存档身份。保留第 5 节 25 条约束；120 行为建议，不以压行数代替职责归属。

### 已有验证与本轮边界

同日审查复跑（产品源码 `70db6ec2` 未变）：291 锚点 recorded/working-tree PASS；SharedCompletionDifferential 12 场景、NativeKnowledgeSchedule 8 项/4 场景、Courier liveness 59 项/16 场景 PASS。后两 harness 存在 CS0649；未重跑全量构建、全部变异或游戏验收。本轮文档提交只验证差异/链接/源码字节不变，不冒用旧记录证明产品全部完成。

核实 `ShoutBehavior.cs` 为 39,669 行、全部 CRLF、无 BOM，SHA-256 `973ac6d0ef172ce140de39b1306c93df4af07bfe63483c2aaca47e09a47c83e4`，本轮未修改。已跟踪文件在开工前无差异；未跟踪 `.dotnet-cli-home/` 原样保留。计划与根 HANDOFF 文档单独提交，推送结果最终以远端 ref 为准，不提交本地直发版/产物/玩家内容。后续回滚用 focused revert，不 reset/rebase/强推；不部署、不恢复自动化、不修改政策/宴会/GCCZ 业务。

## 以下为历史回执；当前结论以上节为准

<a id="j07-plan-ready-20260919"></a>

## J07 计划就绪：PLAN_READY / NOT_STARTED（2026-09-19）

详细实施计划与注意事项清单见 **[docs/plans/j07-conversation-native-plan.md](plans/j07-conversation-native-plan.md)**，本节只记锚点：

- **与前几包的性质差别**：J04–J06 搬纯算法（可逐字节对照），J07 搬会话生命周期（时序不变量）。验收重心从"文本一致"转为"边界断言 + 变异拒收"。
- **实测盘点**：`Refactor/Runtime|Contracts` 里 9 个生命周期 owner 已无 TaleWorlds 依赖，J07a 是纯 `git mv` 归位；真正要拆的是 `ShoutBehavior.cs:20081-20564` 的 484 行 `SubmitNativeConversationTextInternalAsync`（9 次主线程往返、1 次 `Task.Run` fork/join、4 次 `IsStale`、5 处回滚）和 `ApplyNativeConversationGameActions*`（82+53 行）。五个 `ShoutBehavior.Native*.cs` partial 合计 693 行边界已清晰。
- **切片**：J07a Internal 归位（纯 rename，单独提交）→ J07b Native 阶段序列 owner（`NativeConversationTicket`/`AdmissionPolicy`/`StageSequencer`/`RollbackPolicy` 四个纯 owner，宿主方法体降到 ≤120 行）→ J07c 主动开场/关窗/失败文案固化。
- **硬门槛**：不新增任何 SyncData key（`ShoutBehavior.SyncData:10851` 仍只有 `_sceneHeroRevisitDays_v1`）；`AfDialogueClient` 与 `ModuleNativeSubmission` 对外签名不变；Scene/Courier 会话链归 J10 不在本包；Native 十组既有 runner + 新增四套契约全绿；原脚本等价六项构建。
- **15 条时序不变量**（票据只释放自己、capture 开始即必须接受、pending opening 在 busy 拒绝之后消费、`ConversationEnded` 才递增 epoch、已领取的主线程操作不可强制取消、提交只在主线程且异常即终态 unknown、仅 commit 前可 fallback、completion 校验捕获上下文而非当前槽……）已逐条落在计划文件第 5 节，附源码注释出处。

<a id="j06-differential-green-20260919"></a>

## J06 接续：远端红测已修复，最终请求差分全绿；仍 VERIFY / NOT_ACCEPTED（2026-09-19）

**接续基线**：本地 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918` 由 `1c45ba3d` 快进（`--ff-only`，无冲突、未 reset/rebase）到 GitHub `origin/codex/af-main-refactor-continuation-20260831` 终点 `2946bf3d`；产品源码终点仍为 `70db6ec2`，本轮**未改任何产品源码**，291 锚点代码地图继续绑定 `70db6ec2`。

### 红测根因与修复（测试提交 `08699b4f`）

| 项 | 现象 | 根因 | 修复 |
| --- | --- | --- | --- |
| `EntityTextDifferential` | 远端 HANDOFF 记录的“非空输入下生产 `BuildPromptContext` 主文/后处理变空（main=0/134, post=0/49）” | `2946bf3d` 把 `latestInput` 从 `""` 改为含“Alda the King”的整句，但 harness 里 `FindRawRulerTitleMatches` 仍是 `throw` 桩；生产 `BuildPromptContext` 的外层 `catch` 吞掉异常返回空上下文，旧/新两侧同时为空、期望非空 | 两侧改为提取各自基线（`77a3d234` / 当前）的真实 `FindRawRulerTitleMatches`、`FindBestRawQualifiedRulerTitleAlias`、`IsRawRulerTitleShadowed`（其依赖的 `RawTextContainsEntityPhrase`、`BuildRulerTitleCandidates` 等本已在提取集内）。Hero 直接/称谓两例主文、后处理、计数、显式王国 ID 旧新逐字节一致；`drop-hero-main` / `drop-hero-post` / `drop-capture-fallback` 三项变异仍拒收 |
| `SharedCompletionDifferential` | 同上（它以子进程复用实体差分 JSON） | 同上 | 修复后 12 场景 PASS：共享 `CompleteSharedPromptBuild` + Courier 完整请求 + Native 最终消息用生产派生 Lore/实体/规则文本一致；`drop-lore` / `drop-entity` / `drop-rule` 三项变异均 `EXPECTED_REJECT`（Courier 与 Native 最终请求同时差异） |
| `Knowledge/Index` | 本机 `error CS0246 Newtonsoft` | `66abbdbd` 在 csproj 硬编码 `local/dotnet/8.0.425/.../Newtonsoft.Json.dll`，本机不存在 | csproj 改 `@@NEWTONSOFT@@` 占位，runner 按 `AF_NEWTONSOFT` → 仓库本地 SDK 解析（与 `tests/AF.Persistence` 同约定）；70 项 PASS |
| `PersistenceProfileConfigContractTests` | `typed SyncData binding catalog drifted` | 远端 `MyBehavior.cs` 编辑使两条绑定行号漂移，catalog 未刷新 | 只刷新 2 条 `line`，168 条 source/key/ref/type 身份断言不变；PASS |

### 本机实际验证（全部退出码 0）

- 构建：`.tmp/build-local.ps1`（等价原 `build_single_module.ps1`，无 -Stage/-Deploy）Debug + Release × 1.3/1.4/Bootstrap 六项。
- Knowledge：Index/Lore/Import 70、Entities、EntityAllocationParity、PublishedSnapshot、CapturePerformance、LoreTextDifferential、EntityTextDifferential（+3 变异拒收）。
- Prompt：Composition、BuildPhases、CaptureEligibility、KnowledgePhases、ExtraRuleFallback、ExtraRuleTextDifferential、NativeKnowledgeSchedule、NativeFinalRequestDifferential、SharedCompletionDifferential（+3 变异拒收）、ProductionEntry/Evaluation/My/Reward/SceneNative/Consumers。
- 渠道：CourierPromptPreparation 550/76、CourierOwnerPhase、NativeConversationAdmission 44、NativeCompletionBoundary 184、ScenePostprocessParity 71、ChannelCutoverBoundary、CourierHistoryPreparation、NativePendingHistoryBoundary、NativeHistorySnapshot。
- Memory/Persistence：MemorySummaryMainThreadBoundary/RunOwner/Budget、GameLifetime memory、Records 34、OwnerJsonStorageCodec、PlayerExports、PersistenceProfileConfig、IdentityAuditContract、MigrationContract。
- HeroAssetScope、HeroPersonaGeneration。

### 为什么仍不标 `J06_OFFLINE_VERIFIED`

远端上一节列出的三项阻塞中，②“Lore/实体/规则进入最终 Prompt 的完整生产文本对照”已由本轮 `SharedCompletionDifferential` 全绿闭合；③ 原一键脚本六项构建由本机等价 wrapper 完成（远端已记录用户授权后原脚本通过，本机未再重置固定生成目录）。仍开放：① 逐候选关系/距离捕获的**实机帧耗时**样本（`CapturePerformance` 是离线方法级观察值，不是游戏帧证据）；实体差分仅覆盖中性 Hero 直接/称谓两例，定居点/家族/王国正文、可见队伍、常驻实体仍靠 fixture；实机、旧档、真实 provider `NOT-RUN`。因此 J06 维持 `VERIFY / NOT_ACCEPTED`，下一包按总计划进入 **J07 Conversation 核心 / Native**，J06 剩余为实机验证项而非源码项。


## 以下为远端交接与历史

<a id="j06-retrieval-cutover-20260919"></a>

## J06 检索线程收口进度：VERIFY / NOT_ACCEPTED（2026-09-19）

**本次 Courier 最终消息生产方法补强 `3aaece30`**：从 `77a3d234` 与当前 `CourierDeliveryBehavior.cs` 分别提取两种信件的最终消息构建器及历史消息转换 helpers，断言双方源码一致并编译执行；旧同步/新调度的完整序列化请求 76 场景、550 检查通过，system、上下文/历史、当前信件消息顺序有实际断言；仅新侧删除 Lore/实体/规则 fixture 文本三项变异分别失败。`source_review.py` 的固定依赖摘要按已核对的 J06 schedule 变更与 runner 补强更新，严格全文逆变换及三项反例通过；当前 Courier liveness 59 检查通过。后续 runner 兼容修复使 `run_liveness.py --old` 只编译旧 partial 所需文件，旧行为运行时 `wait timeout` 失败（预期旧红，非编译故障）。知识上下文仍由 fixture 制造，Native 最终请求全文与共享 `CompleteSharedPromptBuild` 同输入真实检索结果仍缺，故继续 `VERIFY / NOT_ACCEPTED`。

**实体生产上下文差分新增 `49441aa1`**：`tests/modules/AF.Module.Knowledge/EntityTextDifferential` 从 `77a3d234` 与当前源码分别编译 Hero 直接/称谓匹配、`BuildPromptContext` 主文和后处理事实块；当前侧还实际执行 `CaptureEntityCandidates` → `MatchDetachedCandidates` → `BuildPromptContext`，并对空 capture 的生产同步回退逐字段比较。相同假 Hero/王国输入下两例主文、后处理正文、匹配计数、显式王国 ID 逐字节一致；仅新侧丢主文、丢后处理标题、丢 capture 回退三项变异均拒收。样例仅覆盖中性 Hero（称谓 `King`）且定居点/家族/王国正文、可见队伍和常驻实体为不执行的假端口；不能代替所有实体类型、真实 TaleWorlds 字段成本或 Native/Courier 最终请求全文。J06 仍 `VERIFY / NOT_ACCEPTED`。

**本次聚焦复跑**：Prompt Composition、BuildPhases、KnowledgePhases、NativeKnowledgeSchedule、EntityAllocationParity、LoreTextDifferential、ExtraRuleTextDifferential、ProductionEntry、ProductionConsumers、Native Admission、Native Completion、Scene Postprocess 71 均退出码 0；Scene runner 首次未指定本地 `--dotnet` 时 `WinError 2`，按原 runner 选项指定 `local/dotnet/8.0.425/dotnet.exe` 后通过。代码地图 recorded/working-tree 各 291 锚点通过，绑定未变生产源码 `70db6ec2`。未新增产品源码，因此先前原脚本 Debug/Release 六项构建证据继续适用，未执行 Stage/Deploy 或清理 `.dotnet-cli-home/`。

**额外规则生产方法差分新增 `5e21115c`**：`tests/modules/AF.Module.Prompt/ExtraRuleTextDifferential` 各自从 `77a3d234` 与当前源码提取 `AIConfigHandler` 的语义/词法选择、sticky 合并与规则正文组装，并编译各自的生产 `PromptRuleRanking`、`PromptStickyRuleStore` 和规则模型。在同一假配置/语义结果下，预选语义命中、无预选词法回退两分支的规则 ID 与非空正文逐字节相同；仅新侧删正文、跳过词法回退两项变异均失败。游戏资格/运行时规则补文端口为确定性中性值，尚未与 Lore/实体结果接入最终 Native/Courier 请求，故不改变 J06 状态。

**Lore 生产方法差分新增 `1cd0b9df`**：`tests/modules/AF.Module.Knowledge/LoreTextDifferential` 从 `77a3d234` 与当前源码各自提取 `KnowledgeRuleIndex`、`LoreCandidateRetriever`、`KnowledgeLibraryBehavior` 的 Hero 命中/正文格式化方法及 `AIConfigHandler` 入口；相同假 Hero、同一条 Praven 规则、相同 mention 下，旧同步、当前预选、当前版本过期回退的非空 Lore 正文逐字节相同。检索调用次数验证预选与过期回退分支；仅新路径忽略版本检查、删除 Lore 正文两项变异均失败。假游戏端口不覆盖玩家外观、技能、文本映射或真实 TaleWorlds 读取；尚未接实体、额外规则和 Native/Courier 最终请求，故保持 `VERIFY / NOT_ACCEPTED`。

`da677af3` 在同一个生产捕获方法测试中增加硬预算停止契约：stub budget 于首次 64 项检查后标记超限，生产 `CaptureCandidates` 恰好返回 64 项、只检查一次；原两项负向变异继续失败。2,000 项耗时为不同运行间会波动的离线方法观察值，不作为实机帧预算证明。

**后续 Courier 全请求增量 `1fa1a4e1`**：现有生产请求体回放使用生产 `PromptExtrasComposer` 将六类 Lore/实体/额外规则 fixture 文本带入旧同步和新调度路径，对两方向的完整序列化请求逐字节比较；76 场景、390 检查通过。只在新请求体删去 Lore 文本的变异于 `knowledge_text_lore_hit` 被拒收。它覆盖 Courier 最终请求体的组装/调度回归，但游戏读取与知识检索结果仍是 fixture；Native 最终请求全文、真实生产 Lore/实体结果的旧新同输入文本对照仍未完成，故 J06 继续 `VERIFY / NOT_ACCEPTED`。

**本次 Courier 契约加固 `00dd1c6d`**：旧侧请求 wrapper 与两套最终请求组装方法均直接从指定旧基线 `77a3d234:CourierDeliveryBehavior.PromptPreparation.cs` 提取，不再共享新侧最终组装体。新侧独立删除 Lore、实体、预选规则正文的三项变异，分别在 `knowledge_text_lore_hit`、`knowledge_text_entity_hit`、`knowledge_text_rule_preselected` 的旧新完整序列化请求比较中失败；正常 76 场景 / 390 检查通过。知识检索与底层消息游戏端口仍为 fixture，**没有执行本计划要求的真实生产 Lore/实体/规则检索与 Native 全文差分**，因此保持 `VERIFY / NOT_ACCEPTED`；产品源码未变，先前六项构建证据仍适用。

**2026-09-19 接续增量（测试 `ae2cb4f4`、`7dd969d9`、`db9a899e`；产品源码仍 `70db6ec2`）**：获准并安全预检后，原 `build_single_module.ps1` 无 Stage/Deploy 的 Debug、Release 各 1.3/1.4/Bootstrap 均 `Build Result : success`、每项 0 警告/0 错误；四个被重置目录均在工作区且无 reparse/非构建文件。`CapturePerformance` 提取真实 `CaptureCandidates`/`CaptureDetachedMetadata` 方法，用假 Hero 2,000 个、9 轮测得无元数据 0.397 ms、含范围/距离元数据 1.436 ms、增量 1.039 ms（单机单次均值），每轮 31 次 64 项预算检查、2,000 项范围/距离捕获；去元数据/预算检查两项变异均按预期失败。这不是 TaleWorlds 实机帧耗时。`PromptAssemblyStage` 生产文件的完整 `Extras` 八组合逐字节期望及丢 Lore/实体/规则三项变异通过，但没有覆盖整个最终模型请求 Prompt，也没有让旧同步和新捕获路径的真实 Lore/实体/规则结果在相同输入上逐字节比较。当前 **仍 `VERIFY / NOT_ACCEPTED`**；仅剩的离线文本对照缺口不得用 formatter hash 或本次 `Extras` 契约替代。实机/旧档/provider `NOT-RUN` 不作为离线门槛。

本次聚焦回归：J03 Configuration 36、ProductionModels 18、Retrieval 150、ProductionEntry 7、ProductionEvaluation 22、ProductionMy 4、ProductionReward 11、ProductionSceneNative 3、ProductionConsumers PASS；J04 Composition 200、BuildPhases PASS；Knowledge Index/Lore/Import 70、Entities 32、PublishedSnapshot 7、EntityAllocationParity 4、KnowledgePhases 14、ExtraRuleFallback 21、NativeKnowledgeSchedule 8、Courier Prompt 270/64、Native Admission 44、Completion 184、Scene Postprocess 71 PASS。Scene runner 首次因未传 `--dotnet` 得 `WinError 2`，重跑指定本地 SDK 后通过。代码地图 recorded/working-tree 仍为 291 锚点 PASS，源图绑定 `70db6ec2`；新增测试不改变生产坐标。未推送、Stage、部署或触碰 J07/J10。

本节取代下方 J06d“当前状态”，不改写其历史测试记录。当前工作树 `E:/AnimusForge-refactor-continuation-20260831`、分支 `codex/af-main-refactor-continuation-20260831`；本轮本地检查点 `b383cadd`，产品切片 `34b033de`（Lore）、`8ca6c27e`（额外规则失败回退）、`0bf579f8`（实体）、`5bcb518d`（Lore 设置快照）、`0a0f7e54`（实体捕获预算）、`eae59e63`（每版本 Lore 规则快照）、`bda3e6a2`（实体关系/距离快照与后台全局分配）、`3be39594`（最终阶段不再恢复整库候选）、`70db6ec2`（Native/Courier 仅传纯 DTO 到后台）。没有 push、Stage、Deploy、游戏写入、J07/J10 扩展或公开 API/存档/配置格式修改。**实机、旧档、真实 provider 均 `NOT-RUN`，但不是 J06 离线门槛。**

| 源码责任与一基坐标 | 已接线 | 保留边界/未闭合 |
| --- | --- | --- |
| `KnowledgeLibraryBehavior.cs:508-635,711,1535,4065,4468`；`MyBehavior.cs:30285-30386` | 游戏线程每版本一次发布脱离原可编辑对象的 Lore 规则快照、预备索引并冻结 MCM 检索开关/TopK/MinScore；后台通过请求 scope 只读快照、复用已发布索引召回候选；最终线程补 Hero/Character 运行时事实。预选候选过期时走原兼容入口。 | 冷索引初始化和一次性规则复制仍在游戏线程；索引 hit/miss/Touch 已由 70 项 Index 契约覆盖、规则发布/深拷贝/版本失效由实际方法 7 项 + 2 负向变异覆盖；仍缺最终 Lore 文本生产对照。 |
| `WorldEntityRetrievalService.cs:270-445,514-650,668`；`MyBehavior.cs:30297,30348,30705` | 游戏线程沿原枚举捕获 Hero/Settlement/Clan/Kingdom 名称、别名、称谓及可见队伍，同趟捕获关系/距离元数据、每 64 项检查 3 秒预算；后台 DTO 执行原 `FindMatches`/称谓算法与唯一 `EntityInjectionAllocator.Select`；最终恢复已选 live 引用，成功路径不重复全量枚举、模糊评分或分配。同步 API 保留，同一算法无第二份评分实现。 | 实时实体事实块格式化属于最终 Prompt 组装；旧 `ApplyGlobalInjectionLimit` 仅供同步/失败回退。新增对所有捕获 Hero 的关系/位置读取可能增加游戏线程耗时，虽受原 3 秒预算限制，仍需生产性能样本及成功/回退文本对照。 |
| `AIConfigHandler.cs:5414-5432`；`MyBehavior.cs:28298,30364` | 正常规则预选沿原后台路由；无预选 ID 的语义/词法/sticky 回退也移到后台，游戏线程只按原格式补运行时指令；旧同步调用者不强迁。 | 实际回退方法提取契约 21 项与两项负向变异已覆盖语义命中、词法缺失、异常、sticky 目标和排除；完整 Prompt 文本对照未齐。 |
| `src/modules/AF.Module.Prompt/Composition/PromptRetrievalCapture.cs:34-57`；`ShoutBehavior.NativePromptBuild.cs:82-123`；`CourierDeliveryBehavior.PromptSchedule.cs:97-120` | Native/Courier 在路由与完成之间新增游戏捕获、只接收脱离 TaleWorlds 对象的 `PromptKnowledgeWorkInput` 后台检索，最终 owner 阶段才发布结果并逐跳重验；Scene 仍顺序执行，完整调度归 J10。 | Courier 新检索阶段 owner/generation/participant 迟到回放 6 例及最终守卫变异已覆盖；Native 实际调度方法提取 4 场景/8 检查覆盖正常、迟到 admission/generation、worker 异常，两项守卫变异按预期失败；完整 Prompt 文本对照未齐。 |

**本轮已运行**：J03 Configuration 36、ProductionModels 18、Retrieval 150（含 cache/perf 样本）、ProductionEntry 7、ProductionEvaluation 22、My 4、Reward 11、Scene/Native 3、Consumers PASS；J04 Composition 184、BuildPhases PASS，13 项源码边界/快照/分配/资格变异按预期拒收；Knowledge 生产方法提取执行 14 项 + 3 变异、Lore 发布 7 项 + 2 变异按预期拒收；Knowledge Index/Lore/Import 70、Entities 32（2,000 候选 × 4 mention，8,000 次评分 22 ms，本机一次观察值）；生产实体新旧全局分配提取契约 4 项 PASS，去 detached 家族范围/距离加分两项变异均按预期 FAIL（stubbed 游戏元数据，不是完整文本或实机帧性能）；额外规则生产回退提取契约 21 项和两项变异按预期 FAIL；Courier Prompt 270/64（含新 Knowledge 迟到 6 例及最终守卫变异）、liveness 59/16、OwnerPhase 16；Native Admission 44、Completion 184，实际新知识调度提取契约 4 场景/8 检查和两项变异按预期 FAIL；Scene Postprocess 71、deferred queue 37。七个实体文本 formatter 方法体与 `8ca6c27e` 逐字节规范化相同，仅证明 formatter 未改，不是端到端 Prompt 文本等价。`NativePreparationBoundaryTests` 的旧 `GameLifetimeTests/source_parity.py` 在未改动的 `ShoutBehavior.cs` 上因 J04 精确逆变换断言失败，本轮不以该项报 PASS。

当前 `70db6ec2` 的非删除性 **Debug/Release × BannerlordApi 1.3/1.4/Bootstrap 六个直接构建**均 0 警告/0 错误（1.3 `_deps_auto`，1.4 `local/bannerlord-refs/1.4.7.117484`，Bootstrap 1.3 引用），但它们**不等于当前源码的原一键脚本验证**。已预检脚本将重置的四个精确目录均在工作区、无 reparse 且只含构建产物；执行原 `build_single_module.ps1 -Configuration Debug`（无 Stage/Deploy）仍被自动审核拒绝，原因是本次递归重置缺少被审核认可的明确逐目录授权；没有绕过，Release 原脚本也未执行。此前 `77a3d234` 的原脚本六项 PASS 只属于旧源码，不能充当前源码验收。

**离线验收阻塞，不标 `J06_OFFLINE_VERIFIED`**：① 实体全局分配排序/分数已有生产方法对照，但仍缺成功/回退**完整文本**及新增逐候选关系/距离捕获耗时样本，不能以纯算法 22 ms 充当游戏帧证明；② Native 迟到/异常与辅助规则失败回退已有生产方法契约，但仍缺 Lore/实体/规则进入最终 Prompt 的完整生产文本对照；③ 当前源码原一键脚本 Debug/Release 六项需获准重置固定生成目录后运行。任一失败都必须列为具体阻塞，不以 LIVE/SAVE/provider `NOT-RUN` 代替。291 锚点[代码地图](architecture/af-framework-code-map.json)以 `70db6ec2` recorded/working-tree 通过，仅为源码导航；当前责任详见[范围图](architecture/af-framework-code-scope.md)。未跟踪 `.dotnet-cli-home/` 保留。

<a id="j06d-current-verification-20260919"></a>

## 当前状态：J06d 验证中，J06 父包未验收（2026-09-19）

本轮实际工作树 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，从 `1c45ba3d` 继续；J06d 修正切片 `61ff0875`、Knowledge 导入切片 `66abbdbd`、捕获异常隔离 `dc9c49fb`、负向变异契约 `d93bb1e9`。此节取代下面的“断线/WIP 当前状态”标题，但不改写其历史结论。未跟踪 `.dotnet-cli-home/` 保留，未推送、Stage、部署、打包或操作存档。

- **旧行为对照与已修复缺口**：RAG 先判四个玩家同阵营排除，再判场景移动，再判 GCCZ、附庸/外交/世界外交/王国议程，`scene_auto_group_relay` 与 `noble_deference` 禁用，`vanilla_issue` 要有真实目标，其余默认允许。前处理独立门控 GCCZ、附庸、外交、世界外交、王国议程、婚姻、`vanilla_issue`、NPC 重大行动、领主大厅；`kingdom_service` 始终允许，relay 在前处理仍按默认允许。新纯资格契约逐项覆盖上述顺序和差异（Composition 184 项；此前曾把 relay 误认为两个入口都禁用，红测据旧 switch 修正）。捕获领主大厅原先读到旧 ambient 目标，现临时绑定当前目标并恢复父 scope；六个旧 setter-only 入口改变目标时清除旧资格，仍走合法同步 live fallback。附庸资格捕获改用同一只读谓词，不在每次 Prompt 捕获时提前写 per-topic 诊断事件。相关源码锚点在[276 点代码地图](architecture/af-framework-code-map.json)，线程/回退 source-linked 契约和去 worker 资格变异已执行。
- **线程与 ambient 边界**：Native/Courier 的 Begin/Capture 和 Complete 经游戏线程调度；worker 只接 `PromptRuntimeTargetBinding` + `PromptRuleEligibility`，先走 captured 分支，不通过 `Hero.Find`/Mission/附庸等 live 资格。Scene 仍是同步/旧 setter 消费者，其完整调度属于 J10，不能借 J06 宣称三渠道线程重写。Retrieval 149 项含 eligibility 嵌套、异常、真实 yield、12 并发子请求、Clear 与 setter-only 隔离；BuildPhases source contract PASS，删除 worker eligibility 的变异按预期 FAIL。源码证明不代替实机主线程调度测量。
- **捕获故障边界**：`AIConfigHandler.CapturePromptRuleEligibility` 单独处理 Hero 解析与各 live gate 异常；正向授权事实失败保持 false，玩家同阵营交易限制和场景移动排除失败保持 true，不因一个模块异常隐藏其他主题，也不因异常放行受限规则。提取实际生产方法并用 fake 游戏线程端口运行的契约 12 项 PASS（当前目标 ambient 恢复、worker 无 live read、单门控失败及 Hero.Find 失败）；去掉独立异常捕获和把排除默认改成 false 的两项变异分别预期 FAIL。此契约不模拟真实 Bannerlord 异常来源、日志与提前评估副作用，不是实机通过证明。
- **J06a/b/c owner 与容量**：`KnowledgeLibraryBehavior` 以 `Index`/`Retriever` 消费 `KnowledgeRuleIndex`/`LoreCandidateRetriever`，仍持有 Campaign、规则存储、ONNX 生命周期及 Hero 文本事实；`WorldEntityRetrievalService` 消费 `EntityNameMatcher`、`EntityMentionList`、`EntityInjectionAllocator`，仍持有游戏候选枚举/位置与最终文本。Index 候选缓存上限 512、语义结果 hard cap 20；Lore mention term 上限 32、单词查询 80 字符、实体 query 数夹到 1–12；Entity 分配按配置夹限。检索只在请求/索引重建而非 Tick，每请求捕获一次资格，Courier 因前处理与正文各捕获一次；这些是源码容量边界，未量到游戏帧耗时。Knowledge Index/Lore 59、Entities 31、HeroAsset 67 项通过。
- **导入归属复核与迁移**：原 `MyBehavior` 相关静态簇并非单一职责。`GetKnowledgeKeywordsForCompare`、三个来源文件读取/查找方法、`TryFindDuplicateKnowledgeVariantCondition` 及三个 When 规范化方法共 8 个已迁到 `src/modules/AF.Module.Knowledge/Import/KnowledgeImportSupport.cs`，宿主 12 个真实调用点改接 owner，旧方法体删除。关键词规范化改用已核实与旧体逐符号相同的 `LoreCandidateRetriever` 实现；所迁 8 方法体除访问修饰符与该委托外，与 `61ff0875` 基线提取对照相同。`ValidateKnowledgeKeywordsForSingleRuleImport`、`ValidateKnowledgeKeywordsForImport`、`BuildKnowledgeRuleImportFailureMessage` 仍需读当前 KnowledgeLibrary/Campaign 导出与保留中文失败语义，留 `MyBehavior` 游戏线程薄适配，不把它们误算纯 Knowledge。Import fixture 覆盖去重、条件排序、重复 When、优先单文件、坏 JSON、缺目录（Knowledge Index/Lore/Import 共 70 项）；未实际导入玩家数据或旧档。
- **本轮实际验证**：J03 Configuration 36、ProductionModels 18、Retrieval 149、ProductionEntry 7、ProductionEvaluation 22、My 4、Reward 11、Scene/Native 3、Consumers PASS；J04 Composition 184、BuildPhases PASS；Courier Prompt 252/58、OwnerPhase 16、Scene Postprocess 71、Native Preparation 589/Admission 44/Completion 184、Knowledge Index/Lore/Import 70 + Entities 31、HeroAsset 67 PASS；捕获契约 12 及两项负向变异预期 FAIL。若 runner 自带 fixture `CS0649` 警告，不当作生产零警告。此前直接编译矩阵通过；经用户授权，最终源码用原 `build_single_module.ps1` 不带 Stage/Deploy 执行 Debug/Release，各配置的 BannerlordApi 1.3、1.4 与 Bootstrap 共六项均 0 warning / 0 error，脚本引用版本为 `v1.3.15.110062` / `v1.4.6.115628`，flavor、PDB、两实现哈希不同及构建标记通过；独立复核六份标记的角色/API/引用版本/flavor/SHA256 也通过。只重置已核对的工作树固定生成目录，未写游戏目录、Stage、部署或打包。地图 recorded/working-tree 均 276 PASS，仅证明源码定位。真实 Campaign/Mission、旧档、真实 provider 仍 `NOT-RUN`。
- **剩余门槛**：真实 Bannerlord 异常/提前评估副作用、真实 import/save 邻接矩阵未测；需补这些行为证据并重验最终候选后，才评估 `J06_OFFLINE_VERIFIED`。旧 `IsRuleCurrentlyEligibleForRag` 和 `CanInjectRuleTopicIntoPreprocessForExternal` live 分支仍供明确同步 setter-only 消费者使用，不能删掉；J10 Scene 调度不在 J06。

## 以下为本轮前已发布的交接与历史

<a id="j06d-github-delivery-20260919"></a>

## J06d 中断checkpoint GitHub交付（2026-09-19）

用户明确要求整理Claude Code断线内容、写双份HANDOFF并推送。发布前fetch确认目标分支不存在，旧远端重构分支仍25a89cea；待推送31提交/111路径，未含`.tmp`、generated、日志、DLL/压缩包或本地直发版，tracked工作树干净。已普通创建并推送`origin/codex/af-modularize-j04-20260918`，首次`ls-remote`核对`858de66e052d663c79b9c9888590449b178eeaaf`；不推main/旧重构分支、不强推。此发布只保证内容可恢复，不把bd2582aa WIP升级为验收完成。详细状态见[中断HANDOFF](handoffs/2026-09-19-claude-code-interrupted-modularization-handoff.md)。

## 以下为交付前当前状态与历史

<a id="j06d-interrupted-handoff-20260919"></a>

## 当前状态：J06d_WIP_CHECKPOINT / CLAUDE_DISCONNECTED（2026-09-19）

用户要求在Claude Code断开后整理实际改动，按三份仓库Skill写正在进行/未完成内容、详细及本地直发HANDOFF，并推送GitHub。本节是当前唯一入口；不把checkpoint写成验收完成。

- **工作区/分支/基线**：`G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`；`codex/af-modularize-j04-20260918`；基线25a89cea。Claude最后已提交切片157dc7f2（J06c）；中断留下9文件，未继续实现，原样checkpoint为bd2582aa。
- **已完成父包**：J04_OFFLINE_VERIFIED、J05_OFFLINE_VERIFIED。J06a 8337f0b7、J06b e66ba0d7、J06c 157dc7f2分别已提交纯Index/Lore/Entities owner，但尚未写J06父包最终回执或新地图。
- **正在做**：J06d将RAG/预处理资格从worker live解析改成游戏线程捕获的`PromptRuleEligibility`，通过`PromptBuildRequest`/`CourierPreprocessRequest`和ambient retrieval context传递。9文件及精确坐标见[详细HANDOFF](handoffs/2026-09-19-claude-code-interrupted-modularization-handoff.md)。
- **验证边界**：现有Prompt Composition 155项PASS，但无J06d专项行为断言；本机原脚本Debug/Release各1.3/1.4/Bootstrap 0警告/0错误，无Stage/Deploy。262点map recorded(d903df67) PASS，working-tree在ShoutBehavior stale FAIL；LIVE、旧SAVE、真实provider、完整三渠道/Knowledge矩阵NOT-RUN。
- **未完成/验收**：为11项资格事实补旧live oracle与旧红/新绿；证明worker无Hero.Find/Mission/live资格；检查捕获函数只读且无提前消费；验证ambient嵌套/异常/yield/并发/Clear；跑J03/J04/Knowledge/Native/Courier/Scene及双版本；清理替代旧体、更新owner matrix/范围图/代码地图。此前不得标J06_OFFLINE_VERIFIED。
- **三Skill约束**：维护Skill0.2.0要求双版本/存档/主线程与风险证据；框架Skill要求主体/internal/public分层且复用唯一权威；Policy Skill只允许未来检索MCM控制及模块隔离，J06不能吞Policy运行/存档/调度。全局clean guard继续清被替代路径，必要兼容壳写明理由。
- **后续**：完成J06后按J07 Conversation/Native、J08 LLM、J09 Actions、J10 Scene/Courier、J11 bridges、J12/J13 domains、J14三渠道public API、J15 content、J16 tests/tools/Bootstrap、J17结项。当前master plan的J14三渠道授权优先于下方历史默认NotSupported描述。
- **操作边界**：推新专用分支，不覆盖main或旧远端；本地直发文件不入Git。未部署/打包/写存档/切默认/安装全局Skill/恢复自动化。

## 以下为中断前已提交状态与历史

<a id="j05-offline-verified-20260919"></a>

## J05 离线回执：J05_OFFLINE_VERIFIED（2026-09-19）

**仅限源码与离线验收；实机、旧档读写、真实 provider 均 `NOT-RUN`。** 分支 `codex/af-modularize-j04-20260918`，生产终点 `d903df67`，地图绑定同提交（262 锚点，无悬空路径）。基线 `25a89cea` 至今 25 个本地提交，未推送、未 Stage/Deploy/打包、未写游戏目录、未动存档。

### 对照总计划 J05 四切片

| 切片 | 完成边界 | 证据 |
| --- | --- | --- |
| J05a 记录写入 | `NpcActionLedger`（常量/规范化/10 日窗口/跨窗口去重/同日序号/时间线比较/有序追加）与 `DialogueHistoryLedger`（场景会话标记、AFEF/NPC 行前缀、一次性事实过期、260 行扁平-截断-重组）成为唯一 owner；`MyBehavior` 11 个私有 helper 删除，`RecordNpcActionInternal`/`AppendDialogueHistoryById`/`RemoveExpiredSingleUseNpcFactLines`/`MemoryRecovery.TagSceneSession` 全部改调 owner | `tests/modules/AF.Module.Memory/Records` 34 项 + 变异 `window-off-by-one`、`expiry-keeps-old` 被拒；提交 `3cc6f0d7` |
| J05b 摘要 owner 归位 | 9 个已有 owner 由 `Refactor/Runtime|Contracts` 纯 rename（git 100% 相似度）到 `src/modules/AF.Module.Memory/{Summary,Records,Recovery}`；24 个 tool 工程路径更新；命名空间/类型名不变 | `MemorySummaryMainThreadBoundary`、`MemorySummaryRunOwner`、`MemorySummaryBudget`、`NativeHistorySnapshot`、`GameLifetime run_memory` PASS；提交 `8b712247` |
| J05c 保存编解码 | `src/AF.Persistence/OwnerJsonStorageCodec`：SyncData 七处 owner→JSON 列表循环收敛为一处，键策略（IsNullOrEmpty/IsNullOrWhiteSpace）、空列表跳过、预存 sanitize、键规范化、逐 owner 失败隔离全部参数化保持原样；存档 key/chunk helper/字段/日志通道不变；`syncdata-binding-catalog.json` 只刷新行号（168 条 source/key/ref/type 身份断言不变） | `tests/AF.Persistence/OwnerJsonStorageCodec` 8 项 + 变异 `abort-on-error` 被拒；`PersistenceProfileConfig`/`ChunkReplay`/`IdentityAuditContract`/`MigrationContract` PASS；提交 `321c7318` |
| J05d 导入/导出/Dev 编辑器 | 只做 owner 接线：`PlayerExportsStore`（模块根/PlayerExports/文件夹名/JSON 读写清理/最新导出/导入路径解析）与 `NpcDataFileName`（`heroId__name.json` 解析/构造/兼容规则）成为唯一 owner；`MyBehavior` 16 个 helper、`ModOnboardingBehavior` 3 个、`KingdomStrategicProfileBehavior.DevUi` 2 个重复副本删除；菜单/询问框/各 scope 导入导出正文未改 | `tests/AF.Persistence/PlayerExports` 25 项 + 变异 `name-mismatch-passes`、`latest-oldest` 被拒；提交 `d903df67` |

### 行数

`MyBehavior.cs` 58,033 → 57,378（−655）；`ModOnboardingBehavior.cs` −58；`KingdomStrategicProfileBehavior.DevUi.cs` −47。新增 owner：`NpcActionLedger` 123、`DialogueHistoryLedger` 179、`OwnerJsonStorageCodec` 98、`PlayerExportsStore` 200、`NpcDataFileName` 128。

### 构建

每个切片后 `.tmp/build-local.ps1`（等价 `build_single_module.ps1`，无 -Stage/-Deploy）Debug + Release × 1.3/1.4/Bootstrap 退出码 0，产物在 `bin/<Config>/single_module_artifacts/versions/{1.3,1.4}` 与 `bootstrap/`。

### 明确保留（不在 J05）

- `SanitizeDailyMemoryDrafts`/`SanitizeCompressedMemoryBlocks`/`SanitizeMemorySummaryQueue`/`SanitizeMemoryOverview*` 与五个记录类型（`DailyMemoryDraft` 等）仍是 `MyBehavior` 私有嵌套类型：它们是存档类型（`MyBehaviorSaveableTypeDefiner`），Skill 要求 Saveable 身份不变，且 sanitize 内含 `TWParallel.IsMainThread` 分支与 `CloneMemorySummarySource`。移动它们需先解决嵌套类型的存档命名，归 **J16/J17 存档类型评估**，不在本包冒进。
- `BuildCompressedMemoryExportBundle`/`ApplyCompressedMemoryExportBundle`/`HasCompressedMemoryDataForHero` 直接读写 5 个 `MyBehavior` 字段并调用 `MarkMemoryOverviewDirty`，随上条。
- 159 个 `Import*/Export*/OpenDev*` 方法体（约 6k 行）按总计划只做 owner 接线，未重写 UI；`OpenDevRootMenu`/`ReturnToDevRootMenu` 门禁原样。
- `MemorySummaryDispatcher` 等 owner 只搬位置，"逐 record/字符/耗时预算替换每帧 N 回调"未在本包实施（现有 `MemoryMaintenanceWorkBudget` 已是 record 级预算，替换收益需实机数据），保留到 J07/J13 复评。
- `KnowledgeLibraryBehavior` 导入校验（`ValidateKnowledgeKeywordsForImport` 等 8 个静态方法）留在 MyBehavior → **J06 Knowledge**。

### 预先存在的失败（与 J05 无关，未修改）

- `tools/PersistenceIdentityAudit.py`：在未改动的 `25a89cea` 快照同样失败（基线 `d4cb1467` 清单缺 WarStats 键）。
- `tools/MemoryFailureUiBoundaryTests`：在 `25a89cea` 快照同样失败（memory-run parity 期望）。
- `validate_persistence_profile_config.py` 的绝对路径 `.tmp` 排除已在 `416da085` 改为仓库相对（tracked runner 的唯一修改，行为对远端布局等价）。

### 环境偏差

同 J04：`local/dotnet/8.0.425` 与 Newtonsoft 路径以 `AF_DOTNET`/`AF_NEWTONSOFT` 环境变量或内存替换提供；`PersistenceChunkReplayTests` 直接以本机 SDK `dotnet run`。

<a id="j04-offline-verified-20260919"></a>

## J04 最终离线回执：J04_OFFLINE_VERIFIED（2026-09-19）

**本节取代上方所有 J04_PARTIAL 状态，仅限源码与离线验收；实机、旧档、真实 provider 均 `NOT-RUN`。** 分支 `codex/af-modularize-j04-20260918`，生产终点 `8faf5fbe`，地图绑定同提交（253 锚点两模式通过）。基线 `25a89cea` 至今 20 个本地提交，未推送、未 Stage/Deploy/打包、未写游戏目录。

### J04 完成边界（对照总计划 J04 三条完成标准）

| 标准 | 证据 |
| --- | --- |
| 三个默认渠道入口调用唯一组合 owner，旧算法退出、无第二副本 | Native `ShoutBehavior.NativePromptBuild.cs`、Courier `CourierDeliveryBehavior.PromptSchedule.cs`、Scene 八个调用点与两个静态门面均经 `BeginSharedPromptBuild / RunSharedPromptRouting / CompleteSharedPromptBuild`；`git grep` 无第二套路由/装配实现；`PromptComposer.cs`、8 个规则 ID helper、7 个 sticky 成员、6 个规则块 helper、3 个大内联块全部删除 |
| worker 内零 live 游戏对象读写；owner/generation/目标重验后才接受 | 步骤2（`RunSharedPromptRouting`）只读 detached `PromptBuildRequest` + 配置/缓存/网络；BuildPhases 契约断言该步骤不含 Reward/Duel/Team/Entity/MobileParty/Clan/Hero.MainHero 读取；Native 两次 admission + 两次 generation、Courier 三次 run/source 重验由 ProductionConsumers 契约断言 |
| 原话题/规则资格/历史结构/记忆注入/PostprocessRules 同源/故障回退/Scene 多人语义保持 | Courier 252/59 + 5 变异（harness 改为断言每步线程位置）、Scene 71/37/30、Native 八组 runner、J03 六契约、HeroAsset 67 在每个切片后复跑 PASS；Composition 契约 155 项 + 2 变异、BuildPhases 契约 + 2 变异；`GameLifetimeTests`、`CourierPromptPreparationTests` 两套精确逆变换均记录 J04 差异 |

### 13 个 Composition owner（`src/modules/AF.Module.Prompt/Composition/`）

`PromptRuleIdPolicy`、`BuiltInRuleStickyCarry`、`PromptBuiltInTopicRouter`、`PromptPreprocessRuleIdAssembler`、`PromptExtrasComposer`(+`PromptExtrasSections`)、`PromptRuntimeTargetBinding`、`PromptRuleBlockText`、`PromptTopicRoutingStage`(+`PromptRoutingInput/Ports/Result`)、`PromptBuildRequest`(+`PromptExclusionSets`)、`PromptContextDecisions`、`PromptAssemblyStage`(+`PromptEntityCapture`)、`PromptRetrievalCapture`(+`PromptBuildPhases`)、`PromptRuleInstructionComposer`(+`PromptRuleInstructionSections`)。目录不引用 TaleWorlds / AIConfigHandler / Logger（BuildPhases 契约断言）。

### 行数

`MyBehavior.cs` 58,669 → 58,033；`ShoutBehavior.cs` 39,698 → 39,668；`AIConfigHandler.cs` 7,707 → 7,717（+两个发布口）；新增 `ShoutBehavior.NativePromptBuild.cs` 110、`CourierDeliveryBehavior.PromptSchedule.cs` 122。共享 builder 771 行单体 → 三步 + 五阶段，最大阶段 `CapturePromptSections` 207 行（全部为游戏线程段落捕获）。

### 明确保留（归后续包，不在 J04）

- `CapturePromptSections` 内 `AIConfigHandler.GetLoreContext`（ONNX lore，`KnowledgeLibraryBehavior.BuildLoreContextInternal` 读 12 处 Hero 状态）与 `WorldEntityRetrievalService.BuildPromptContext` 仍在游戏线程 → **J06 Knowledge**。
- `BuildExtraRuleInstructions` 中 `AIConfigHandler.BuildMatchedExtraRuleInstructions` 语义检索仍在游戏线程（它依赖 `ResolveConversationTargetHero` 的 `Hero.FindFirst` live 读）→ **J06d**。
- Scene 五个 async 调用点无 owner 调度器，继续走三步顺序组合 → **J10a**。
- `BuiltInRuleStickyCarry`（duel/reward/loan）与 J03 `PromptStickyRuleStore`（kingdom_service/marriage）仍是两个状态 → J06 或 J07 合并评估。
- 四个游戏派生 `Add*RuleExclusions*` adapter 仍在 MyBehavior（读 Hero/Mission）→ 随 J07 归 Conversation 适配层。

### 环境偏差记录

- `tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py` 在本 worktree 报 `symbolic SyncData source inventory drifted`：原因是排除集合按绝对路径 parts 匹配，而本 worktree 路径含 `.tmp`，导致全部源码被排除。以内存改为相对路径后，本 worktree 与**未改动的 `25a89cea` 快照**报同一 `chunked key mismatch: missing=[13 keys]`——即该 runner 在远端基线上已不通过，与 J04 无关（`git diff 25a89cea` 四个大文件 SyncData 行数变化 = 0）。登记为 J05 首项待查，不在本包修改 runner 或 catalog。
- J03 runner 的 `local/dotnet/8.0.425` 路径、`NativeTtsFallbackBoundaryTests` 的 `ROOT.parent/.dotnet-sdk` 路径以内存替换运行；未改 tracked 文件。

<a id="j04f-receipt-20260919"></a>

## J04f 回执：Native/Courier 执行位置已搬移（2026-09-19，J04_PARTIAL）

**叠加于总计划与前两批回执；J04 仍未 OFFLINE_VERIFIED（剩 J04g/J04h）。** 生产 `72f8d342`（Native）、`52247a51`（Courier）；地图绑定 `52247a51`，250 锚点两模式通过。未推送、未 Stage/Deploy。

### 已迁

| 坐标（源码 `52247a51`） | 内容 |
| --- | --- |
| `MyBehavior.cs` `BeginSharedPromptBuild` / `RunSharedPromptRouting` / `CompleteSharedPromptBuild`（`internal`） | 共享构建拆为三个可调度步骤：步骤1 游戏线程捕获请求 + 排除列表；步骤2 任意线程话题路由 + mention 检索（`PromptRetrievalCapture` 预取）；步骤3 游戏线程段落捕获 + 纯装配 + 追加。旧 `BuildShoutPromptContextForExternalInternal` 变为三步顺序组合，Scene 八个调用点与静态门面行为不变 |
| `ShoutBehavior.NativePromptBuild.cs` `BuildNativePromptContextScheduledAsync` | Native：`RunNativeConversationMainThreadFuncAsync("prompt_build_begin")` → 后台 slot（复用既有超时/迟到完成守卫，以 marker 实例占位）→ `RunNativeConversationMainThreadFuncAsync("prompt_build_complete")`；两次 `IsNativeConversationAdmissionCurrent` 重验、两次 `SaveRuntimeGuard.IsStale`。旧 `RunNativeConversationBackgroundPreprocessAsync(() => BuildShoutPromptContextForExternal(...))` 整段后台调用删除 |
| `MyBehavior.cs` `BeginCourierRulePreprocess` / `RunCourierRulePreprocessRetrieval` | Courier 前处理同样拆为游戏线程捕获 + 任意线程检索；旧 `RunCourierRulePreprocessInternal` 变为顺序组合 |
| `CourierDeliveryBehavior.PromptSchedule.cs` `BuildCourierPreparedPromptScheduledAsync` | Courier：owner 阶段（前处理请求）→ `Task.Run`（检索）→ owner 阶段（共享请求）→ `Task.Run`（路由）→ owner 阶段（段落/装配）；每个 owner 阶段重验 `IsCourierPromptRunCurrent` + `IsCourierPromptInputCurrent`。`PromptPreparation.cs:172` 的单个 `Task.Run(() => BuildCourierPreparedPrompt(input))` 删除 |

### 验证（本机，全部退出码 0）

- Courier prompt 252 / liveness 59；harness 桩改为断言每步线程位置（Begin/Complete 必须主线程，Retrieval/Routing 必须工作线程），Program 断言不变；五个变异（`worker_assembly`/`main_preprocess`/`skip_accept`/`wrong_direction`/`skip_source`）仍拒收，`main_preprocess` 已重定向到调度器；`source_review.py` 与 `liveness_review.py` 精确逆变换通过（新增 J04f hunk）。
- Native 八组 runner（preparation 589 / admission 44 / completion 184 / pending 111 / history 852+27 / module submission / action dispatch / TTS 14）PASS；`GameLifetimeTests/source_parity.py` 新增 J04f 精确逆变换。
- Scene parity 71 / queue 37 / lifetime 30、Courier history/owner-phase/commit、HeroAsset 67、J03 六契约、Composition 142、BuildPhases（改为三步形状，2 变异拒收）、ProductionConsumers（新增 Native/Courier 调度形状断言）PASS。
- 原脚本 Debug + Release × 1.3/1.4/Bootstrap 六项 0 警告/0 错误。

### 未完成

- 步骤3 仍在游戏线程调用 `AIConfigHandler.GetLoreContext`（ONNX/lore 检索）与 `WorldEntityRetrievalService.BuildPromptContext`；lore 依赖步骤3 内合并后的 mentions，需先把 mentions 合并前移到步骤2 才能把 lore 也搬到后台（J04g 一并处理）。
- `BuildTriggeredRuleInstructions` / `BuildExtraRuleInstructions` 未段落化（J04g）。
- Scene 五个调用点未接三步调度（它们各自在 async 流程中，无 Native/Courier 那样的 owner 调度器；按总计划归 J10）。
- 实机、旧档、真实 provider `NOT-RUN`。

<a id="modularization-master-plan-20260919"></a>

# AF 主体完整模块化总计划（2026-09-19，PLAN_READY / J04 继续 ACTIVE）

本节是用户要求的"一次大任务"总计划：按三份仓库 Skill（maintainer 0.2.0、af-core-framework、policy-effect-module）把 AF 主体拆完，最终交付 J17 全仓结项。它替代上方 2026-09-17 路线表的粗粒度描述，**不替代各包实施时的详细执行单**；每包开工前仍按 J03/J04 的做法写意图节、逐切片提交、逐切片回归。基线：分支 `codex/af-modularize-j04-20260918`，源码 `d6824d9d`，241 锚点地图两模式通过；原始基线 `25a89cea`。

## 0. 不变约束（来自 Skill，不重复解释）

1. 一套源码 → `AnimusForge.dll` 双版本（1.3/1.4）+ Bootstrap 唯一加载；`AF.Foundation/Module/Bridge` 是逻辑分层，**不拆 DLL**，namespace/程序集/存档类型/SaveableTypeDefiner 身份不变。
2. 三层架构：AF 主体 → 同 DLL typed `internal` 制作组接缝（`Refactor/Modules/TeamModulePorts.cs` 的 `IPolicyModulePort`/`IGatheringModulePort`/`ISiegeModulePort`）→ 独立版本化 public API（`src/AF.Contracts/PublicApi/V1` + `src/modules/AF.Module.PublicApi`）。政策/宴会/GCCZ 玩法不重写。
3. 每包必须：真实算法+状态迁到新 owner → 接通全部真实消费者 → 删除旧实现（不留转发壳）→ 直接编译生产源码的契约 + 变异拒收 → 相关三渠道 runner 复跑 → 原脚本 Debug/Release 双 API + Bootstrap → 代码地图两模式 → 台账回执。Stage/Deploy/打包/推送分别授权。
4. 游戏对象读写在所属线程；后台只处理 detached 输入；回写重验 owner/generation/目标。
5. 三渠道（Native/Scene/Courier）共享同一话题、规则、历史、记忆、后处理、动作、AFEF 语义。**用户已授权 Scene/Courier 公开提交为最终目标（J10+J14）**，与远端 J14 默认范围不同，不静默降级。
6. 性能按真实频率与工作量判断；不新增 tick 轮询、全量扫描、重复反射；不为普通抽取制造 manifest/Host/热卸载。

## 1. 现状盘点（源码 `d6824d9d`，本轮实际统计）

| 家族 | 行数 | 已迁出 owner | 主要残余簇（按方法名聚类，方法数/行数） |
| --- | ---: | --- | --- |
| `MyBehavior.cs` + 16 partial | 62,325 | Memory dispatch/summary/sealing/budget partial、Prompt Composition 12 owner、Persona owner | Weekly 352/9.7k、Memory 252/6.8k、Kingdom 120/3.2k、Dev/Import/Export 159/6.3k、Prompt 79/2.4k、Persona 63/2.2k、History 72/2.1k、Party 72/2.1k、UI 92/2.1k、Settlement 67/1.5k、Action 59/1.4k、Sync 7/1.4k |
| `ShoutBehavior.cs` + 9 partial | 41,656 | Native admission/preparation/pending/completion/dispatch partial、ScenePostprocess partial | Scene 313/9.4k、Native 115/4.4k、Prompt 95/3.2k、History 61/2.6k、Postprocess 42/1.9k、Group 21/1.6k、Party 37/1.1k、TTS 19/0.9k、Passive 19/0.7k、Memory 25/0.7k |
| `CourierDeliveryBehavior.cs` + 7 partial | 11,925 | PromptPreparation/HistoryPreparation/CommitDispatch/InboundCompletion/DetachedPostprocess partial | Courier 141/4.1k、Party 32/0.7k、UI 20/0.6k、Npc 15/0.6k、Load 9/0.5k |
| `ShoutNetwork.cs` | 1,367 | Protocol 三文件（J01） | 真实 HTTP 发送 `:133-158`、普通/流调用、取消/重试、姓名过滤 |
| `AIConfigHandler.cs` | 7,717 | Configuration 5 owner、Retrieval 23 owner | ONNX/辅助网络适配、`IsRuleCurrentlyEligibleForRag:1728`、`ResolveConversationTargetHero:6352` live 读 |
| `KnowledgeLibraryBehavior.cs` | 13,791 | — | ONNX 索引 `EnsureOnnxIndex:1390`、`BuildLoreContext:5670`、Saveable |
| `Refactor/Runtime` 22 文件 | 9,883 | 已是 owner 形态 | 四个 OutcomeReceipt（2.1k/1.6k/1.5k/0.4k）、`InteractionResultCommitter`、`DetachedInteractionHost`、`FeatureBridgeRuntime` 待归位到 `src/` |
| `Refactor/Adapters` 21 文件 | — | Legacy* 适配器 | 待按 owner 归位或删除 |
| 领域大类 | Reward 22.6k、WorldDiplomacy 20.5k、SiegeAi 17.2k、SceneTaunt 10.6k、WorldMapParty 10.0k、LordEncounter 9.4k、Duel 8.6k、Vassalage 8.6k、ProactiveNpc 8.2k、NobleGathering 5.9k、VoteDeal 4.8k | — | 30 个文件含 SyncData/Saveable；Harmony 密集：MilitaryExercise 17、Duel 13、TroopInspection 12 |

## 2. 包序列（依赖顺序；每包给出真实入口、目标 owner、完成边界、验收）

### J04 Prompt 组合（ACTIVE，剩余三切片）

| 切片 | 内容 | 完成边界 |
| --- | --- | --- |
| J04f 执行位置 | Native：`ShoutBehavior.cs:20080` `SubmitNativeConversationTextInternalAsync` 改为 主线程 `CapturePromptBuildRequest` → 后台 `PromptTopicRoutingStage`（+ lore/mentions 检索）→ 主线程 `CapturePromptSections`/`ApplyPromptRuntimeAppendices`（重验 admission/generation）→ 任意线程 `PromptAssemblyStage`；沿用 `RunNativeConversationMainThreadFuncAsync`。Courier：`CourierDeliveryBehavior.PromptPreparation.cs:156` 的 `Task.Run` 改为同样分段，沿用 `RunCourierOwnerPhaseAsync`。Scene 五个调用点（`13831/17831/20760/27415/27906/30727/38869`）按现有线程语义接入，不改 Scene 多人接力/旁听。 | 后台阶段零 Hero/Clan/Reward/Duel 读；`GetLoreContext`/`GetAuxiliaryMentionedEntitiesForExternal` 从阶段 3 移到后台阶段；三渠道 runner 全绿 |
| J04g 规则指令段落化 | `BuildTriggeredRuleInstructions`（141 行）、`BuildExtraRuleInstructions`（~100 行）拆为"host 捕获规则正文 → `PromptRuleInstructionComposer` 纯拼装"；Reward/Loan/Duel/Taunt 运行时正文由 host 捕获 | 与 `PromptRuleBlockText` 合并为一个 owner；旧两方法删除 |
| J04h 验收 | Courier 前处理与主链共用 `PromptExclusionSets` + `CapturePromptBuildRequest`；更新地图/范围图/回执；记 `J04_OFFLINE_VERIFIED` | 全部具名消费者接唯一 owner；Composition 契约 + BuildPhases 契约 + 三渠道 runner + 双版本构建 |

### J05 Memory / Persistence

- **真实入口**：`MyBehavior.DialogueHistoryCommit.cs:12` `CommitDialogueHistoryWithScene`（唯一运行期接受）；`MyBehavior.cs:15451` `RecordNpcRecentAction` / `RecordNpcMajorAction`；`MyBehavior.cs:30874` `AppendExternalLoreHistory`；`MyBehavior.MemorySummary*.cs`（已有 dispatcher/run/planning/input/sealing/budget owner）；`MyBehavior.MemoryRecovery.cs:1463`；`Refactor/Runtime/MemorySummaryDispatcher.cs`、`InteractionMemoryRecoveryLedger.cs`、`InteractionResultCommitter.cs:718`。
- **目标 owner**：`src/modules/AF.Module.Memory/{Records,Summary,Recovery,Afef}`；`src/AF.Persistence/` 只放通用保存基础（chunk replay、identity audit 已有工具契约）。存档类型/key/`MyBehaviorSaveableTypeDefiner:58658` 原地保留，只迁算法。
- **切片**：J05a 记录写入（Recent/Major/Dialogue/Lore 四类写者收敛为一个 `MemoryRecordWriter`，AFEF 事实语义统一）→ J05b 摘要（已有 owner 归位到 `src/`，逐 record/字符/耗时预算落实，替换"每帧 N 回调"）→ J05c 恢复账本 → J05d 导入/导出/Dev 编辑器（`Import*`/`Export*`/`OpenDev*` 159 方法 6.3k 行）只做窗口门禁与 owner 接线，不重写 UI。
- **验收**：`tools/PersistenceProfileConfigContractTests`、`PersistenceChunkReplayTests`、`PersistenceIdentityAudit` 严格 runner 保持 PASS；旧档字段/类型审计不变；新契约覆盖四类写者去重、预算、恢复幂等；三渠道 memory 写入契约相同。

### J06 Knowledge

- **真实入口**：`KnowledgeLibraryBehavior.cs:5670` `BuildLoreContext`、`:1390` `EnsureOnnxIndex`、`AIConfigHandler.cs:7630-7640` `GetLoreContext`；`WorldEntityRetrievalService.cs`（4.2k）`BuildPromptContext`；`Refactor/Adapters/LegacyKnowledgeRagGateway.cs`。
- **目标 owner**：`src/modules/AF.Module.Knowledge/{Index,Lore,Entities}`；ONNX 引擎生命周期归 `Index`，静态知识/百科归 `Lore`，世界实体检索归 `Entities`。ONNX 文件与 `ModuleData` 不随源码搬（J15）。
- **切片**：J06a 索引构建/失效/只读查询 owner（脱离 `KnowledgeLibraryBehavior` 的 Campaign 生命周期）→ J06b Lore 检索纯算法 + host 适配 → J06c 实体检索 → J06d `AIConfigHandler.IsRuleCurrentlyEligibleForRag:1728` / `ResolveConversationTargetHero:6352` 的 live 读改为 J04 `PromptRuntimeTargetBinding` 传入的 detached 资格（消除 J03 遗留）。
- **验收**：Lore/实体检索确定性 fake embedding 契约；J04 BuildPhases 契约扩展"后台阶段可调用 Knowledge"；`HeroAssetScopeRegressionTests` 保持。

### J07 Conversation 核心 / Native

- **真实入口**：`ShoutBehavior.cs:20080` `SubmitNativeConversationTextInternalAsync`（Native 回合唯一编排）；`ShoutBehavior.Native*.cs` 五个 partial（admission 260/preparation 83/pending 134/completion 138/dispatch 78）；`ShoutBehavior.ModuleNativeSubmission.cs`；`Refactor/Runtime/InteractionRequestCoordinator.cs`、`InteractionRequestLease.cs`、`DetachedInteractionHost.cs:389`；`Refactor/Adapters/LegacyNativeConversationFacade.cs`、`LegacyNativeConversationOptInRunner.cs`。
- **目标 owner**：`src/modules/AF.Module.Conversation/{Internal,Channels/Native}`；`Internal` 持会话/请求/lease/generation/取消；`Native` 持准入、准备、pending 历史、完成、动作派发。UI 覆盖层 `AnimusForgeNativeConversationOverlay*.cs` 留 adapter。
- **切片**：J07a 请求生命周期（coordinator/lease/host 归位 `src/`）→ J07b Native 五 partial 归位并把 `SubmitNativeConversationTextInternalAsync` 拆为"准入 → J04 五阶段 → LLM（J08 接缝）→ 后处理（J09 接缝）→ 提交"→ J07c 主动开场/关窗/失败文案保持。
- **验收**：Native 五组 runner（589/44/184/111/852+27）+ `NativeModuleSubmissionTests`、`NativeActionDispatchOutcomeTests`、`NativeTtsFallbackBoundaryTests` 全绿；`AfDialogueClient` 契约不变。

### J08 LLM 传输 / 模型目录

- **真实入口**：`ShoutNetwork.cs:133-158` 两个 Send、`:889-1128` 普通调用、`:1130-1590` 流调用（含 400 thinking fallback、空回复一次补救、SSE 回调时点、逐字符 Unicode 旧缺陷）；`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`LegacyModelCatalogGateway.cs`、`LegacyPolicyLlmGateway.cs`、`LegacyWorldDiplomacyLlmGateway.cs`、`LegacyVolcTtsGateway.cs`；`DuelSettings.GlobalClient`；`Logger` token 统计队列 `:769-845`。
- **目标 owner**：`src/modules/AF.Module.Llm/{Transport,Streaming,ModelCatalog,Tts}`；唯一请求/重试/超时/取消/流状态 owner；配置与实时姓名过滤分界。
- **切片**：J08a 非流传输 → J08b 流传输 + `LlmVisibleReplyNormalizer.StreamFilter` 接线 → J08c 五个 Legacy*Gateway 收敛到一个 typed gateway 接口（Policy/WorldDiplomacy 只保留各自 prompt，不复制第二条链）→ J08d TTS。
- **验收**：J01 协议 13 用例 + 7 变异保持；新增传输契约用确定性 fake `HttpMessageHandler` 覆盖取消/超时/重试/400 fallback/空回复；真实 provider `NOT-RUN` 单列。

### J09 Actions / 事实提交

- **真实入口**：`Refactor/Adapters/LegacyActionTagParser.cs`、`LegacyActionTagCatalog.cs`、`LegacyNativeActionPlanExecutor.cs`；`ShoutBehavior.ScenePostprocess.cs:1033`（Scene 唯一权威后处理 work item）；`ShoutBehavior.cs:22991` `TryPrepareCourierActionPostprocessForExternal`；`CourierDeliveryBehavior.DetachedPostprocess.cs`；`Refactor/Runtime/InteractionResultCommitter.cs`、`InteractionCommitReceiptCache.cs` 及四个 `*OutcomeReceipt.cs`；`docs/directive_tag_output_case.md`。
- **目标 owner**：`src/modules/AF.Module.Actions/{Tags,Plan,Execute,Receipts}` + 领域 typed 执行端口（`AF.Contracts/Internal`）。规则资格 → `tag_rules` → 解析 → 唯一执行 → AFEF/receipt 全链保留；计划/执行/失败/部分成功分清。
- **切片**：J09a 标签目录/解析 owner（三渠道共用同一解析）→ J09b 计划/执行/回执 owner，四个 OutcomeReceipt 归位 → J09c Scene/Courier 后处理调用改接唯一执行入口，删除各自重复解析。
- **验收**：`ScenePostprocessParityTests` 71/37 + `run_mutations`、`CourierPostprocessOwnerRegressionTests` 39 + 8 变异、`CourierCommitOutcomeTests`、`NativeActionDispatchOutcomeTests` 全绿；新增"三渠道同一标签同一执行"契约。

### J10 Scene / Courier 渠道整包

- **真实入口**：Scene：`ShoutBehavior.cs:27280` `HandleGroupResponse`、`:27693` `HandleGroupResponsePerHeroIndependent`（接力/旁听/去重）、`:20715` `GetPassiveNpcResponse`、`:13796` `GenerateGroupConversationTurnLineAsync`、`:30685` 即时反应、TTS 19 方法 0.9k；Courier：`CourierDeliveryBehavior.cs:4398/4851` 两个生成入口、`PromptPreparation.cs:156` `PrepareCourierPromptRequestAsync`、`CommitDispatch.cs`、`InboundCompletion.cs:556`、旧 retry 按钮。
- **目标 owner**：`src/modules/AF.Module.Conversation/Channels/{Scene,Courier}`；各渠道保留自己的队列/会话/代际/提交时点，共享 J04–J09 主体。Scene pending AFEF 消费、玩家去重/距离/旁听不能纯函数化；Courier 到达提交不改为预生成提交。
- **切片**：J10a Scene 会话 owner（group/relay/passive/reaction 四条链归位；可等待的生命周期与真实结果）→ J10b Courier 会话 owner（运输/预生成/到达/来信/retry 身份收拢；删除剩余后台 session live 读）→ J10c 三渠道对齐审计（`docs/free_conversation_scene_shout_alignment.md`）。
- **验收**：Scene parity/queue/lifetime + Courier 五组 runner 全绿；新增"独立调用方可等待 Scene 请求真实结果"与"Courier 旧 retry 不改新会话"契约；**为 J14 开放 SceneSubmit/CourierSubmit 准备完整调用链证据**。

### J11 制作组接缝（Policy / Gathering / Siege）

- **真实入口**：`Refactor/Modules/TeamModulePorts.cs:7,17,27` 三个 internal port、`TeamModuleAdapters.cs`、`src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs`；`PolicySystem/`（77 文件，policy-effect-module Skill 权威）、`NobleGatheringBehavior.cs`、`AnimusForge.SiegeAftermathIntervention/` + `AfGcczShoutBridge.cs` + `Gccz*Bridge.cs`；`Refactor/Runtime/FeatureBridgeRuntime.cs:421`。
- **目标 owner**：internal 契约 → `src/AF.Contracts/Internal`，薄桥 → `src/bridges/{Policy,Gathering,Siege}`，玩法仍在各自根目录（不迁 PolicySystem 内部结构，不改 MCM 检索语义）。
- **切片**：J11a Policy 桥（`AfGcczShoutBridge` 中 AF 侧调用与 `IPolicyModulePort` 归位）→ J11b Gathering 桥 → J11c Siege 桥（`AfGcczShoutBridge` 的 prompt 注入/排除/bypass 改用 J04 ports）。`G:/AFMOD/GCCZ` 同步另获授权。
- **验收**：`PolicyEffectModule.ContractTests --policy-all-modules-contract-only` + `--policy-history-only` 1115 保持；`CampaignCompositionTests` 42+5、`CompositionMatrixContractTests`；三桥各自源码接线契约。

### J12 Economy / Diplomacy / WorldMap

- **当前细化计划**：[J12 领域 owner 实施计划](plans/j12-domain-owners-plan.md)（`60499d44` 源码基线；计划完成，生产未开始）。该文替代本小节早期笼统施工提纲，具体责任/消费者/保存与公开接口保留项见其第 4–6 节。
- **顺序**：G0 → J12a Economy → J12b Diplomacy → J12c WorldMap → J12d 整包验收。目标仍为 `src/modules/AF.Module.{Economy,Diplomacy,WorldMap}`，领域状态不进通用 Actions。
- **已校正边界**：现有 public Economy 契约保 ABI；capture 可能带主线程规范化副作用；WorldDiplomacy client 已接 J08；partial/unknown/STOP 已执行与 queued 不可简化成“失败不改状态”。
- **验收**：真实入口/owner 和三渠道消费链、相关旧行为回归/有效反例、双版本/Bootstrap/API/存档/地图；LIVE/SAVE 独立。不是只迁目录或只补接口。

### J13 其他领域

- **当前细化计划**：[J13 领域 owner 实施计划](plans/j13-domain-owners-plan.md)（`0624d502` 规划基线，2026-09-24，尚未施工）。具体责任、首包步骤、后续包退出门和安全/环境条件以该计划及本台账置顶回执为准；下列计数为历史盘点。
- **真实入口**：WorldEvents/WarStats（`AFWarStatsTerminal` 适配）、`PlayerNotorietyBehavior.cs`（3.9k）、`RomanceSystemBehavior.cs`（3.7k）、`DuelBehavior.cs`（8.6k，13 Harmony）、`SceneTauntBehavior.cs`（10.6k）、`LordEncounterBehavior.cs`（9.4k）、`SettlementEntryTroopSelectionBehavior.cs`、`TroopInspectionBehavior.cs`（12 Harmony）、`MilitaryExerciseBehavior.cs`（17 Harmony）、`ProactiveNpcRequestBehavior.cs`（8.2k）、`MeetingBattleLockMissionBehavior.cs`、`ModOnboardingBehavior.cs`、Weekly 报告（MyBehavior Weekly 簇 352 方法 9.7k）、Kingdom 簇 120/3.2k、UI 簇。
- **目标 owner**：各领域独立子包 `src/modules/AF.Module.{Social,Duel,Encounter,Settlement,Weekly,Onboarding,...}`；UI 只适配。**Weekly 簇是 MyBehavior 最大残余，单列 J13a 首包**；伤害/敌对/百科/军团目标四个案例文档作为对应包的验收清单。
- **切片**：J13a Weekly 报告（`MyBehavior` 352 方法 → `AF.Module.Weekly`，`WeeklyMemoryMaterialOutcomeReceipt` 归位）→ J13b Kingdom 簇 → J13c Persona（已有 owner 归位 + 63 方法）→ J13d Social/Notoriety/Romance → J13e Duel/Taunt/Encounter/Settlement/Exercise（按案例文档逐包）→ J13f UI/Overlay/Onboarding 适配层。
- **验收**：每包一个源码接线契约 + 相关 runner；Harmony 补丁调用点核对表；不迁 Harmony 类本身。

### J14 public API 收尾（含用户授权的三渠道开放）

- **真实入口**：`src/modules/AF.Module.PublicApi/V1/AfApi.cs:14-62`（`SceneSubmit`/`CourierSubmit`/`ActionExecute`/`MemoryWrite`/`ExtensionRegister` 现 NotSupported）、`AfDialogueClient.cs`、两个 Projection；`src/AF.Contracts/PublicApi/V1/AfApiContracts.cs`；`ShoutBehavior.ModuleNativeSubmission.cs`。
- **目标**：在 J07/J09/J10 完成后，按 af-core-framework Skill 门槛开放 `SceneSubmit`、`CourierSubmit`：完整调用链、线程/生命周期/失败语义、可探测能力、兼容证据齐全才把 `Unsupported` 改为 `Available`；`ActionExecute`/`MemoryWrite`/`ExtensionRegister` 未获授权不开放。
- **切片**：J14a Scene 公开提交（票据/结果/取消，复用 J10a 可等待结果）→ J14b Courier 公开提交（区分运输阶段与实际提交阶段）→ J14c 契约版本与投影回归。
- **验收**：`NativeModuleSubmissionTests` 扩展为三渠道；能力快照契约；不破坏 `ContractVersion = 1` 语义（若需破坏性变化另开 V2）。

### J15 content / profile

- 静态资源（`AnimusForge/ModuleData`、GUI、7 项 EmbeddedResource、prompt JSON、ONNX）按 owner 唯一归属到 `content/`；运行/安装路径与 `LogicalName` 默认保持；PlayerExports 分 curated/用户变更且先闭合 G0.3；不全量覆盖用户配置。

### J16 tests / tools / scripts / docs / Bootstrap

- 测试随 owner 迁 `tests/`（`tools/*Tests` 中已有 30+ runner 逐包归位）；工具只留源码，输出进 `artifacts/`；文档一事实一权威入口；**一键脚本与 Bootstrap 只在另获授权时迁**；`.tmp/build-local.ps1` 类机器专用包装不入库。

### J17 全仓结项

- 重新分类全部 tracked/untracked/ignored，UNASSIGNED=0；`Refactor/` 目录清空（全部归位 `src/`）；混合大类剩余职责逐符号清零或写明兼容壳；无双核心/重复编译；offline/LIVE/旧 SAVE 单列；仍 HOLD 的资产明确"全仓未完成"。

## 3. 执行方式

- **顺序**：J04f→J04g→J04h → J05 → J06 → J07 → J08 → J09 → J10 → J11 → J12 → J13a（Weekly，可与 J11/J12 并行）→ J13b–f → J14 → J15 → J16 → J17。J05/J06 可并行；J11/J12/J13 可并行但各自独立提交。
- **每切片**：意图检查点提交 → 实施提交 → 契约/回归/双版本构建 → 地图/范围图/回执提交。回滚用定向 inverse，不 reset。
- **不做**：不推送、不 Stage/Deploy/打包、不写游戏目录/存档、不安装全局 Skill、不改一键脚本、不清理未知资产，除非用户另行授权。
- **本机环境记录**：SDK `G:/AFMOD/.dotnet-sdk` 8.0.422、1.3 引用 `_deps_auto`、1.4 引用 `G:/AFMOD/AF-REFACTOR/.tmp/build_check/1.4`、Harmony Workshop 2859188632；J03 runner 的 `local/dotnet/8.0.425` 路径以内存替换运行。

<a id="j04-slice2-20260919"></a>

## J04 第二批切片回执：J04_PARTIAL / 五阶段边界已显式化（2026-09-19）

**本节叠加于下方首批回执；J04 仍未 OFFLINE_VERIFIED。** 生产切片 `6315fd26`→`d6824d9d`（6 个提交），地图绑定 `d6824d9d`，241 锚点 recorded / working-tree 均通过。同一分支，未推送、未 Stage/Deploy/打包。

### 本批迁出（源码 `d6824d9d`，一基行号）

| 新 owner / 坐标 | 真实迁移与接线 | 旧实现处置 |
| --- | --- | --- |
| `Composition/PromptRuleBlockText.cs:11` | `【附加规则:id】` 块格式唯一 owner：Append/Has/Count/ReplaceBody/Remove/AppendIfMissing/PrependDisclaimer；`BuildExtraRuleInstructions`、`BuildTriggeredRuleInstructions` 及 36 处调用改用 | MyBehavior 6 个 helper 与 6 处内联标记字符串删除 |
| `Composition/PromptTopicRoutingStage.cs:58,26` | 辅助路由→强制预选→八话题→sticky 消费/prime 成为一个阶段；输入 `PromptRoutingInput` 全部 detached，网络/ONNX/门控/日志经 `PromptRoutingPorts` 五个委托由 host 提供；`PreprocessFormatException` 透传、其他辅助失败回退语义路由 | 主链 ~80 行路由/日志块删除；host 只建输入、供 ports、写日志 |
| `Composition/PromptBuildRequest.cs:12,49` | `PromptBuildRequest` 承载一次构建的 detached 身份/标志/排除集合；`PromptExclusionSets` 拥有 explicit/runtime/preprocess 三层布局、不可用配置规则过滤、有序列表 | 主链内联集合构造删除；host 只提供四个游戏派生 adder；玩家部族等级读取收为 `ResolvePlayerClanTierForPrompt` |
| `Composition/PromptContextDecisions.cs:29` | reward/loan 提升（party transfer 资格、已消费决斗结果）、澄清提示门控、lore 来源选择与旧诊断标签、语义触发日志行 | 主链对应分支删除；决斗结果消费保留在旧位置以维持 TrustPrompt 判定不变 |
| `Composition/PromptAssemblyStage.cs:35` | 纯装配：Extras、后处理块合并、显式王国 ID、上下文标志、preprocess ID | 主链尾部装配删除；实体/议程检索结果由 host 捕获成 `PromptEntityCapture` |
| `MyBehavior.cs:30474` orchestrator；`:30557` `CapturePromptBuildRequest`；`:30648` `CapturePromptSections`；`:30862` `ApplyPromptRuntimeAppendices` | 共享 builder 拆为五阶段：捕获请求（游戏读）→ 路由（detached）→ 捕获段落（游戏读）→ 纯装配 → 运行时追加（游戏读）；orchestrator 80 行 | 原 771 行单体不再存在；**执行仍在调用线程顺序进行，行为不变** |

### 本机实际验证（全部退出码 0）

- Composition 契约 142 项（新增 RuleBlockText 12、RoutingStage 14、ExclusionSets 10、ContextDecisions 12、AssemblyStage 8）；`sticky-limit`、`router-excluded` 两个变异仍拒收。
- 新增 `tests/modules/AF.Module.Prompt/BuildPhases/run.py` 源码接线契约：orchestrator 五阶段顺序、orchestrator 不直接读游戏服务、三个捕获阶段不跑规则检索、Composition 目录不引用 TaleWorlds/AIConfigHandler/Logger；`--mutate assembly-reads-game`、`--mutate routing-before-request` 均拒收。
- J03 六契约、Courier 252/59/39、Scene 71/37/30、Native 589/44/184/111/852、HeroAsset 67 在每个切片后复跑 PASS。
- 原脚本 Debug + Release × 1.3/1.4/Bootstrap 六项 0 警告/0 错误（无 Stage/Deploy）。

### 未完成 / 不能外推

- **线程边界只是显式化，尚未搬移执行位置。** Native 仍在 `RunNativeConversationBackgroundPreprocessAsync` 后台线程执行整个五阶段，Courier 仍在 `Task.Run` 内；`CapturePromptBuildRequest`/`CapturePromptSections`/`ApplyPromptRuntimeAppendices` 里的 Hero/Clan/Reward/Duel/Party 读取因此仍发生在后台。下一切片：渠道调度改为“主线程阶段 1 → 后台阶段 2 → 主线程阶段 3/5（含 generation/owner 重验）→ 后台/任意线程阶段 4”，并把 `AIConfigHandler.GetLoreContext`、`GetAuxiliaryMentionedEntitiesForExternal` 从阶段 3 拆到阶段 2 或独立后台阶段。
- `BuildTriggeredRuleInstructions`（141 行）与 `BuildExtraRuleInstructions`（~100 行）仍在 MyBehavior，含 Reward/Duel/Taunt 实时读取；已改用 `PromptRuleBlockText`，未段落化。
- `AIConfigHandler.IsRuleCurrentlyEligibleForRag` / `ResolveConversationTargetHero` 在规则检索内部通过 ambient 目标 ID 做 `Hero.FindFirst` / `Campaign.Current.ConversationManager` 读取——这是 J03 遗留的检索侧 live 读，不在本包。
- 实机、旧档、真实 provider `NOT-RUN`；Scene/Courier 公开提交未开放。

<a id="j04-slice1-20260918"></a>

## J04 首批切片回执：J04_PARTIAL / OFFLINE_VERIFIED_SLICE（2026-09-18）

**本节取代上方 J04 意图节的 ACTIVE 状态，仅限源码与离线验收；不是 J04 整包完成。** 生产切片 `e0aa8142`、`be91c047`、`27ec5e26`、`2a191526`，测试/工具 `11f90fec`，地图绑定 `11f90fec`。分支 `codex/af-modularize-j04-20260918`，基线 `25a89cea`；未推送、未 Stage/Deploy/打包、未写游戏目录。

### 已迁职责（源码 `11f90fec`，一基行号；符号为追踪依据）

| 新 owner / 坐标 | 真实迁移与消费者 | 旧实现处置 |
| --- | --- | --- |
| `src/modules/AF.Module.Prompt/Composition/PromptRuleIdPolicy.cs:12` `PromptRuleIdPolicy` | 规则 ID 集合/排除/规范化、运行时门控、companion/family 四项排除、`noble_deference` 排除、Courier 命中排序、辅助命中收集与强制合并；MyBehavior 39+ 调用点及 `RunCourierRulePreprocessInternal` 接新 owner | MyBehavior 8 个 private static helper 删除，两段内联 LINQ 删除 |
| `.../BuiltInRuleStickyCarry.cs:12` `BuiltInRuleStickyCarry` | duel/reward/loan 跨回合 carry 的唯一状态 owner（短确认识别、目标 key、2/2/3 回合上限、消费/prime/clear）；MyBehavior 持一实例，存档加载 `ResetLocalTransientRuntimeForLoadedSave` 调 `Clear()` | `_ruleSticky*` 四字段、`IsShortAckForRuleFollowup`、`ResolveRuleStickyTargetKey`、`GetBuiltInRuleStickyTurnLimit`、`ClearRuleStickyCarry`、`TryConsumeRuleStickyCarry`、`UpdateRuleStickyCarryFromHits` 删除；与 J03 `PromptStickyRuleStore`（kingdom_service/marriage）是不同状态，未合并 |
| `.../PromptBuiltInTopicRouter.cs:24` `PromptBuiltInTopicRouter` | 8 个内置话题（duel/reward/loan/surroundings/kingdom_service/marriage/party_transfer/worldmap_party_command）的“辅助路由权威 → 语义评估 → sticky 兜底”算法；语义评估通过 `PromptTopicSemanticEvaluator` 委托仍由 `AIConfigHandler.IsGuardrailSemanticHit` 提供 | 主链内 ~170 行八段重复 if/else 删除 |
| `.../PromptPreprocessRuleIdAssembler.cs:17` | preprocess 规则 ID 收敛（辅助 ID + 路由标志 + `persistent_adp_debt` + `noble_gathering` 注入块检测 + 排除集合差集）；`ShoutBehavior.PersistentAdpDebtPostprocessRuleId` 由此常量取值 | 主链 11 处 `preprocessRuleIds.Add` 删除 |
| `.../PromptExtrasComposer.cs:12,47` `PromptExtrasSections` / `PromptExtrasComposer` | Extras 21 段 canonical 顺序、每段空白策略（保留旧 IsNullOrEmpty / IsNullOrWhiteSpace 差异）、决斗/原版战败/释放三条固定模板、实体检索规则集、后处理块合并、8 个注入标记检测 | 主链 22 处 `stringBuilder.AppendLine` 与三条内联模板删除；host 按原顺序捕获段落文本 |
| `.../PromptRuntimeTargetBinding.cs:10` `PromptRuntimeTargetBinding`；`AIConfigHandler.cs:5666` `ApplyGuardrailRuntimeTarget` / `ClearGuardrailRuntimeTarget` | 六值检索目标身份（hero 回退、troop=character、soldier/commoner rank）唯一派生与发布口；MyBehavior 3 处、ShoutBehavior 4 处、ScenePostprocess 1 处接线 | 8 组六 setter 发布/清理块删除；旧 public setter 保留给现有 `ShoutBehavior.cs:28008` 等四 setter 局部调用 |
| 删除 `PromptComposer.cs` | `git grep` 零调用者（仅 owner matrix 文档提及） | 死代码删除 |

`MyBehavior.cs` 58,669 → 58,078；`ShoutBehavior.cs` 39,698 → 39,673；四文件净 −680 行。共享 builder `MyBehavior.cs:30526` `BuildShoutPromptContextForExternalInternal` 现约 470 行，仍为 `mixed-host`。

### 本机实际验证（全部退出码 0；命令见 `.tmp/reg/`、`artifacts/tests/prompt-j04-composition/`）

- 新契约 `tests/modules/AF.Module.Prompt/Composition/run.py`：直接编译七个生产 owner 文件，86 项断言；`--mutate sticky-limit`（loan 上限 3→2）、`--mutate router-excluded`（去掉排除门控）均编译成功后断言失败（退出码非 0）。
- J03 生产契约复跑：ProductionEntry 7、ProductionEvaluation 22、ProductionMy 4、ProductionReward 11、ProductionSceneNative 3、ProductionConsumers 五类源码边界；均 PASS。
- 三渠道回归复跑：Courier prompt 252 / liveness 59 / postprocess 39；Scene parity 71 / queue 37 / lifetime 30；Native preparation 589 / admission 44 / completion 184 / pending 111 / history 852 及 `--native` 27；HeroAssetScope 67；均 PASS。`tools/GameLifetimeTests/source_parity.py` 增加 J04 精确逆变换（1 apply + 4 clear + 1 常量），Scene queue / Courier postprocess 桩增加 Apply/Clear 并链接 binding 源；未改任何生产断言。
- 原脚本 `一键编译覆盖推送/build_single_module.ps1` Debug 与 Release 的 Bannerlord 1.3、1.4、Bootstrap 六项各 0 警告/0 错误，未带 `-Stage/-Deploy`。本机参数：SDK `G:/AFMOD/.dotnet-sdk` 8.0.422、1.3 引用 `_deps_auto` 1.3.15.110062、1.4 引用 `G:/AFMOD/AF-REFACTOR/.tmp/build_check/1.4` v1.4.6.115628、Harmony 取 Workshop 2859188632；与制作组记录的 8.0.425 / 1.4.7 不是同一安装，仅证明本机双 API 编译通过。
- 231 锚点[代码地图](architecture/af-framework-code-map.json) recorded / working-tree 两模式通过；26 个既有锚点仅因行号漂移按符号重定位并刷新文件 hash，无符号/路径/状态改动。

### 环境偏差（不是产品失败，已记录不掩盖）

J03 runner 硬编码 `local/dotnet/8.0.425` 与 `.tmp/nuget-packages` Newtonsoft 路径在本机不存在；以内存替换（`.tmp/run_with_local_sdk.py`，未改 tracked runner）指向 `G:/AFMOD/.dotnet-sdk` 与 SDK 自带 `Newtonsoft.Json.dll` 后通过。`NativeHistorySnapshotTests` 默认 `DOTNET_EXE` 指向 `C:\Program Files\dotnet`（仅运行时），需显式环境变量。

### 未完成 / 不能外推

- **J04 未 OFFLINE_VERIFIED。** 共享 builder 仍在调用线程内直接读取 Hero/Clan/Reward/Duel/MobileParty/TeamModuleServices/WorldEntityRetrievalService 并调用 lore 检索；Native 后台执行、Courier `Task.Run` 执行的线程现状未改变。下一切片：`PromptExtrasSections` 之前的“主线程捕获输入 DTO → 后台组合 → 主线程重验接受”边界，以及 `BuildTriggeredRuleInstructions`（约 560 行）的段落化。
- `RunCourierRulePreprocessInternal` 与主链的规则排除集合构造仍各自内联（同一四个 Add* helper），未合并为一个 owner。
- 未触碰 Scene/Courier 公开提交、Memory、Knowledge、Actions；`AfApi` 能力表不变。实机、旧档、真实 provider `NOT-RUN`。
- 本地专用脚本 `.tmp/build-local.ps1`、`.tmp/run_with_local_sdk.py`、`.tmp/refresh_code_map.py`、`.tmp/j04*_rewire.py` 不入库；不改一键脚本。

<a id="j04-intent-20260918"></a>

## J04 执行意图：共享 Prompt 组合责任闭包（2026-09-18，历史；状态见上节）

工作区 `G:/AFMOD/AF-REFACTOR/.tmp/modularize-20260918`，分支 `codex/af-modularize-j04-20260918`，基线 `25a89cea`（= 当时 `origin/codex/af-main-refactor-continuation-20260831`）。旧工作区 `G:/AFMOD/AF-REFACTOR` 及其两份未提交草稿不动、不合并。用户本轮授权：继续按 J01–J17 路线拆分 AF 主体；未授权推送、部署、Stage、打包、写游戏目录、安装全局 Skill。

**用户已明确的目标差异（登记，不静默降级）：** 用户此前要求 Native / Scene / Courier 三渠道对外开放。远端 J14 默认“只接已开放能力，Scene/Courier 不因整理开放”与此不同；本台账将 Scene/Courier 公开提交登记为已授权目标，映射到 J10（渠道真实生命周期与结果）+ J14（公开能力开放，须完整调用链、线程/生命周期/失败语义、可探测能力与兼容证据）。J04 不实现该开放。

**J04 已核实入口（源码 `25a89cea`，一基行号）：**

| 源码 / 符号 | 责任 | 真实消费者 |
| --- | --- | --- |
| `MyBehavior.cs:30794-31564` `BuildShoutPromptContextForExternalInternal` | 共享主链 Prompt 上下文：规则排除集合、辅助/语义/强制话题路由、内置 sticky 兜底、关系/军队/周报/政策/规则/lore/实体 Extras 组装、preprocess 规则 ID 收敛 | `ShoutBehavior.cs:13831,17831,20185,20760,27415,27906,30727,38869`；`CourierDeliveryBehavior.PromptPreparation.cs:149` |
| `MyBehavior.cs:30651-30698` `RunCourierRulePreprocessInternal` | Courier 前处理话题命中并按优先级/分数排序 | `CourierDeliveryBehavior.PromptPreparation.cs:146` |
| `MyBehavior.cs:28483-28644` 规则 ID 集合/排除/规范化/运行时门控静态 helper | 纯字符串集合算法，被 MyBehavior 内 39+ 处调用 | `BuildTriggeredRuleInstructions`、preselected 规则注入等 |
| `MyBehavior.cs:1798-1804,19841-19865,19893-20007` `_ruleSticky*` 与 `TryConsumeRuleStickyCarry`/`UpdateRuleStickyCarryFromHits`/`ClearRuleStickyCarry` | 内置 duel/reward/loan 话题跨回合 sticky 状态（与 J03 的 `PromptStickyRuleStore` 是不同状态：后者管 kingdom_service/marriage） | 主链 `:31130,31160`；存档加载复位 `:2503` |
| `PromptComposer.cs:7-76` | 固定层缓存/拼接；`git grep` 零调用者 | 无（死代码候选） |

**线程现状（不冒称已解决）：** Native 在 `RunNativeConversationBackgroundPreprocessAsync` 后台执行整段构建，只有 WeeklyPromptSnapshot 在主线程预捕获；Courier 在 `Task.Run` 中执行；Scene 调用点位于各自 async 流程。整段仍有大量 live 游戏读取（Hero/Clan/Reward/Duel/MobileParty/TeamModuleServices）。本包采用 J03 计划已定的过渡接线：旧适配器按原调用域准备输入 → 新 owner 执行纯算法/持有状态 → 旧适配器继续原业务；完整“主线程捕获→后台→主线程接受”改造按切片推进，不在首切片一次完成。

**J04 切片顺序：** J04a 规则 ID 策略 + 内置话题路由 + sticky 状态 owner（纯算法/状态迁出，旧实现删除，接通主链与 Courier 前处理；删除零调用者 `PromptComposer`）→ J04b Extras 分段组装与 preprocess ID 收敛 owner → J04c 主线程捕获输入 DTO 与后台组合边界 → J04d 集成：契约测试、Courier/Scene/Native 回归、原脚本 Debug/Release 双 API + Bootstrap（无 Stage/Deploy）、地图/范围图/回执。每切片本地提交，可用定向 inverse 回滚。

<a id="j03-delivery-20260918"></a>

## J03 远端交付范围核实（2026-09-18）

用户明确要求将当前完成内容全部推送；目标 `origin/codex/af-main-refactor-continuation-20260831`，fetch 后远端基线 `4ae94412fd23f8e20b20107bbe5f5f67acda3cc5`，本地代码/地图 HEAD `3c1ae5aa`，远端落后 52 提交且为本地祖先。本次将工作树中 HANDOFF 与本台账的 J03 规划/过程/最终验收增量一并提交，不遗漏源码或文档。此前实施阶段的“未推送”不是本次禁止交付指令；历史正文保留。

已检查这 52 个提交新增可达的 164 个 blob：未命中受保护本地交接、PlayerExports、Stage、`.dotnet-cli-home/`、二进制/归档/日志路径或所检查的私钥/provider token/AWS key 模式；这是有限发布筛查，不是完整秘密审计。未跟踪 `.dotnet-cli-home/` 保持本地，不做清理或批量暂存。当前代码地图 working-tree 221 锚点 PASS，文档差异检查通过；沿用同源 J03 离线验收，不重跑产品构建，不声明实机、旧档或真实 provider 已验证。只普通快进推送当前分支，不推 main、不部署/Stage/打包；最终成功以推送退出码及远端 ref 与提交相等为准。

<a id="j03-implementation-status-20260918"></a>

## J03 实施现状：J03_OFFLINE_VERIFIED（2026-09-18）

### 最终离线验收回执（生产 `e6c82d8d`；测试 `d1d3407a`）

**本节取代下方所有 `PARTIAL / NOT_ACCEPTED` 的历史实施回执，仅限源码与离线验收。** 从起点 `602df8fa` 经意图检查点 `7fe79311`，`01dd8267` 在 `PromptConfigurationSnapshot.cs:7-49` 对六份模型发布时及兼容读取时作深拷贝，原有 public 签名及 JSON/资源身份不变，`AIConfigHandler.cs:113-118` 的旧 owner 热路径只借用内部视图，避免逐次 JSON 分配。真实六模型（含 `JObject`）18 项 JSON 身份、源修改/读者修改隔离通过；直接编译生产 loader/store/registry 的配置 36 项覆盖六文件缺失/损坏 fallback、磁盘覆盖顺序、同 ID 末项覆盖、并发 reload、异常默认换代、捕获旧 revision 稳定。生产规则命中入口 7 项验证命中与规则正文同代；去掉外层 pin 的变异编译成功、断言失败（预期退出码 1）。

`AIConfigHandler.cs:36-43,4720-4883` 的 `PromptRuleEvaluationPorts` 是**仅内部、逐调用**的确定性资格/embedding/rerank/辅助结果接缝；普通游戏调用传 `null`，仍走原 ONNX/辅助网络、游戏资格和日志适配，无进程级全局测试 provider。实际提取并编译该生产方法及完整 Retrieval owner 的 22 项契约覆盖语义与辅助入口、辅助/重排失败回退、provider 异常、MCM/目标/资格/排除 cache key、缓存 hit、旧代晚结果不发布。session 调用、mission 所属线程 seed、RAG 完成回传及旧 worker 跨 reload 门控/缓存隔离均按生产 warmup 方法与 coordinator 执行。检索 owner 135 项另覆盖嵌套、异常、提前返回、真实 `Task.Yield()` 后的 scope 恢复、mentions 交付、80-key、私装超展示 cap、排序/原索引及 agent/settlement 隔离。

具名消费者分别验收：My 实际提取 `BuildSettlementTransferRuntimeInstructionForExternal` 4 项（全量/展示 scope 与原授权顺序），Reward 实际提取三个候选方法并执行 Scene 原始 hero/merchant 候选片段 11 项（公私装备、候选 cap、授权全量、scope 隔离）；Scene/Native 实际提取两个角色包装方法 3 项验证同一 mentions 与 trade/party 标志交付，且原始 Native 调用处及 My/Reward/Scene/Policy 生产调用边界由 `ProductionConsumers/run.py` 逐条核对，三个断线变异均被断言拒绝（预期退出码 1）。Policy `PolicyHistoryRetrievalService.TryRetrieveDialogueByMentions` 在实际 1.4 生产程序集上以 `--policy-history-only` 通过 1115 项断言；原 source-wiring 契约不冒充整段游戏域可执行测试。Reward/My/Scene 的游戏物品/角色读取、Native 大方法及真实 provider 保留边界替身，实机另列未运行。

代表性最终命令均退出码 0：`python -X utf8 -B tests/modules/AF.Module.Prompt/ProductionEvaluation/run.py`（22）、`ProductionEntry/run.py`（7）、`ProductionMy/run.py`（4）、`ProductionReward/run.py`（11）、`ProductionSceneNative/run.py`（3）、`ProductionConsumers/run.py`（五类源码边界）；SDK 8.0.425 的 `dotnet build`/运行 `ProductionModels/PromptProductionModelsTests.csproj`（18）、`Configuration/PromptConfigurationLoaderTests.csproj`（36）与 `Retrieval/PromptCandidateSelectionTests.csproj`（135）。Policy 测试 `tools/PolicyEffectModule.ContractTests/bin/Release/net472/PolicyEffectModule.ContractTests.exe --assembly bin/Release/net472/AnimusForge.dll --policy-history-only`（1115）。Courier prompt 252/liveness 59/postprocess 39、Scene parity 71/queue 37/lifetime 30、Native preparation 589/admission 44/completion 184/pending 111/history 852及 `--native` 27、HeroAssetScope 67、PersistenceProfile 严格 runner，最后均退出码 0；fixture/game-domain 边界仍按各 runner 输出保留 `STUBBED`。旧机器硬编码 SDK 路径的几个 runner 仅在内存中改为仓库 SDK 路径执行，未改断言或生产源码。

检索 Release net8 本机样本：同规模单值缓存各 20,000 次 hit 9.728 ms、miss 0.347 ms、clear+publish 0.700 ms；5,000 候选输入选 10 项用 31.030 ms，100 seed 暖启动工作项用 0.047 ms。仅为离线 fixture 量测，不当作游戏帧预算或稳定基准；候选未截断、sticky 与私装规则保持。构建前逐一预检获准的四个精确生成目录均在工作区、非重解析点且只含可再生产物；随后原 `build_single_module.ps1` 不带 `-Stage/-Deploy` 的 Debug/Release Bannerlord 1.3/1.4/Bootstrap 六项各 0 警告/0 错误，无打包。两 API 的 MSBuild 实际求值各 789 Compile/7 EmbeddedResource，两 Prompt LogicalName 及其余五项集合原样。221 锚点[代码地图](architecture/af-framework-code-map.json) recorded/working-tree 通过；当前责任/残余见[范围图](architecture/af-framework-code-scope.md)。所有切片仅本地提交；起点已未提交的本台账/HANDOFF 差异保留叠加，`.dotnet-cli-home/` 未跟踪且未清理；未推送、部署、Stage 或写游戏目录。**实机、旧档、真实 provider 均 `NOT-RUN`**，此状态不解除任何资产/许可 HOLD，也不扩展 J04/J06。

### 以下为 J03 历史部分实施回执

### 本次继续实施回执（起点 `602df8fa`；生产 `01dd8267`，契约 `2c741536`）

本次先作本地意图检查点 `7fe79311`。`PromptConfigurationSnapshot.cs:7-49` 现在发布六份模型时逐一深拷贝，普通属性读取再次拷贝，避免源模型或 getter 调用者改动已发布 revision；`AIConfigHandler.cs:103-108` 的六个私有热路径入口改用 owner 独占借用，避免每次提示词读取触发 JSON 分配。配置契约先以源/读者修改六模型嵌套成员旧红复现，再修复，最终 36 项通过；同 ID 覆盖、六配置缺失/损坏回退、并发 reload、异常默认换代、已捕获旧代、命中正文同代由该契约与 `tests/modules/AF.Module.Prompt/ProductionEntry` 的实际提取方法共同覆盖。后者直接编译生产 `BuildRulePromptRegistry`、`GetGuardrailSemanticRuleHits` 及配置 store，7 项通过；移除外层 revision pin 的编译成功变异被同代断言拒绝（预期退出码 1）。借用对象仍是内部可变模型，尚不能声称编译期深层不可变视图或完整真实 getter 并发契约。

检索契约直接编译生产 facade、管线、warmup coordinator，新增真实 `Task.Yield()` 后 scope/mentions 交付与父上下文恢复，合计 132 项通过。Courier prompt 252／liveness 59／postprocess 39、Scene parity 71／queue 37／lifetime 30、Native preparation 589／admission 44／completion 184／pending 111／history 852 与 `--native` 27、PersistenceProfile 严格 runner 均退出码 0。Courier prompt/liveness 及四个 Native runner 仅在内存中把已不存在的 `G:\AFMOD\.dotnet-sdk` 路径替换为仓库 `local/dotnet/8.0.425`；未改生产源码或断言。Courier postprocess 的 Newtonsoft 路径指向现有 `local/bannerlord-refs/1.4.7.117484/Newtonsoft.Json.dll`。这些替身契约不等于 My／Reward／Scene／Native／Policy 每个生产候选调用链的端到端验收。

按用户限定范围逐一预检 `bin/Debug/single_module_artifacts`、`bin/Release/single_module_artifacts`、`obj/single_module/Debug`、`obj/single_module/Release` 均在工作区内、非重解析点且只含可再生产物，才由原 `一键编译覆盖推送/build_single_module.ps1` 重置。使用 SDK 8.0.425、`_deps_auto` 1.3.15.110062 与 `local/bannerlord-refs/1.4.7.117484`，Debug/Release 的 Bannerlord 1.3、1.4、Bootstrap 六项均退出码 0、各 0 警告/0 错误；未带 `-Stage`/`-Deploy`，未打包。两版 MSBuild 求值各 789 Compile／7 EmbeddedResource，七个 LogicalName 未改变。220 锚点地图 recorded/working-tree 均通过；源码职责及残余见[范围图](architecture/af-framework-code-scope.md)。起初无离线 NuGet 配置的基线遇 `NU1301`，改用仓库离线 NuGet.Config；首次默认 sandbox 生产构建/项目求值因现有 Windows SDK 读取权限报 `MSB4184`，获工具权限后原命令成功，未采用伪造 SDK 属性绕过。最初 Courier postprocess 缺失旧 Newtonsoft 包路径、配置测试项目名写错，定位并改用现有确切路径/项目后通过。

**仍为 `PARTIAL / NOT_ACCEPTED`，不得写 `J03_OFFLINE_VERIFIED`。** `AIConfigHandler.TryGetGuardrailEvalSnapshot` 的确定性 provider/资格内部接缝、真实语义与辅助入口完整故障矩阵、五类消费者的实际生产候选链及全部具名 scope 闭包、session/mission/RAG 三入口真实线程与迟到 worker、同规模 hit/miss/reload/暖启动/大候选性能数据尚未齐。实机、旧档、真实 provider 均 `NOT-RUN`。原有未提交的本台账和 HANDOFF 改动继续保留，`.dotnet-cli-home/` 未跟踪且未清理；本次只聚焦提交代码/测试/地图，无推送、Stage、Deploy、打包或外部写入。

### 最新继续实施回执（生产源码 `9242bcfa`，地图 `81b6de2a`）

在下方已验证切片之上，`313b6133` 把生产意图拆分与 2+2 输入 embedding 批次迁往 Prompt Retrieval，并删掉旧类未被调用的 built-in 证据私有实现；`52cd7e47` 将召回、逐意图重排、跨意图聚合和最终评估编排接成唯一 `PromptRuleRetrievalPipeline`，生产 `AIConfigHandler.TryGetGuardrailEvalSnapshot` 只负责 MCM／资格／provider 回调、日志及缓存发布；`3586331e` 修复公开后处理规则 getter 返回内部可变列表的问题。`9242bcfa` 修复跨 reload 时评估命中与规则正文可能混用两代 registry 的外层入口：整个 `GetGuardrailSemanticRuleHits` 固定一个 revision，配置契约增至 34。检索管线直接编译生产文件，以确定性 embedding／rerank 验证语义选择、重排与失败回退，`d34d74f3` 再补双意图聚合与配置关键词脱离契约，检索检查总数 128。Courier prompt 252／liveness 59／postprocess 39、Scene parity 71／queue 37／lifetime 30、Native preparation 589／admission 44／completion 184／pending 111 均复跑通过；Native History 原 runner 因本机 apphost 8.0.30 包缺失失败；`dfe6b12c` 只改 runner 构建／启动方式、不改断言，现原 runner 普通 852 项与 `--native` 27 项均 PASS。PersistenceProfile 严格 runner 复跑 PASS。原构建脚本预检精确生成目录后，最终生产源码 Debug／Release 的 1.3、1.4、Bootstrap 六项均 0 警告／0 错误，未 Stage/Deploy。源码 Compile glob 自动纳入三个新 `.cs`，既有 EmbeddedResource LogicalName 未动；220 锚点代码地图 recorded／working-tree 均通过，详情见[范围图](architecture/af-framework-code-scope.md)。

**仍为 `PARTIAL / NOT_ACCEPTED`。** 新管线已真实接线，但六份模型的深层只读性、所有 My／Reward／Scene／Native／Policy 消费者端到端契约、真实辅助网络／provider、所有具名 scope 的异常／yield／mentions 生产闭包仍未充分验证。实机、旧档、真实 provider 分别 `NOT-RUN`，不能标记 `J03_OFFLINE_VERIFIED`。本节叠加于任务起点已未提交的主台账差异；未整文件暂存，未跟踪 `.dotnet-cli-home/` 未删除/提交，无推送或部署。

### 最新接续增量（生产源码 `3ff315ba`，测试 `79c6dbb8`）

在既有切片之上，本轮本地提交 `0916b60c`、`d74f3e0c`、`3085f8cd` 将逐意图召回截断／重排失败回退、规则 seed／rerank 文本、评估模型与最终命中组装、辅助主题资格与评分迁至 Prompt Retrieval owner；旧类删除相应重复算法并接通新 owner，保留真实游戏资格、ONNX／辅助网络、日志及版本化缓存发布。`3ff315ba` 将实际捕获的 revision、MCM、资格、目标与路由输入交唯一缓存键 owner。`84e65df7` 修复内置 RP 默认模型被 Lazy 跨代共享：旧红用例先失败，再改为只缓存资源原文、每次 fallback 新建模型。`c707b476` 直接编译完整生产 `PromptListRetrievalService`，验证 MCM 热改、全量授权／展示 scope、私装超 cap、81 keys 和目标 agent 隔离；`79c6dbb8` 直接编译生产 `RagWarmupCoordinator` 验证 mission seed 由所属调用者传入后台完成回调。无新 public API、DLL、Host、资源或用户数据变化，未推送／部署。

配置生产 loader 31、Retrieval 119、Courier 252／59／39、Scene 71／37／30、Native 589／44／184／111／852 均通过。最终生产源码按原 `build_single_module.ps1` 先预检精确生成目录，Debug／Release 的 Bannerlord 1.3、1.4 和 Bootstrap 六项均 `0 warning / 0 error`，无 `-Stage`／`-Deploy`；原 Prompt EmbeddedResource LogicalName 仍由 `AnimusForge.csproj:81-86` 明定且未修改。[代码范围图](architecture/af-framework-code-scope.md)与 217 锚点地图按上述真实删除／新增迁移更新，recorded／working-tree 两模式通过。PersistenceProfileConfigContract 起初报 `extra=['synthetic-only-key']`，定位为全仓 `*.cs` 扫描误纳入未编译的 `tests/modules/AF.Module.Llm/Protocol/Program.cs`；排除测试源码后暴露 52 个旧行号漂移。`de6bd963` 只修生产扫描边界，并在 168 条 key/ref/type/source 完全相同前提下刷新精确行号，严格断言、行号／类型反例均保留；最终 runner **PASS**，未更改生产存档实现。

**继续 `PARTIAL / NOT_ACCEPTED`，不记 `J03_OFFLINE_VERIFIED`。** 生产六配置的深层只读发布仍未闭合，完整旧红矩阵及真实 getter 并发契约不足；My／Reward／Scene／Native／Policy 的端到端候选调用链、语义／辅助真实网络入口和所有 scope/mentions 异常闭包仍未逐条生产契约覆盖；`AIConfigHandler.TryGetGuardrailEvalSnapshot` 仍有网络、游戏资格及评估编排残余。实机、旧档、真实 provider 分别 `NOT-RUN`。本节与 HANDOFF 的原未提交规划内容继续保留，未被任一本地切片整文件暂存；未跟踪 `.dotnet-cli-home/` 未删除或提交。

### 继续实施回执（源码 `e4f94429068af012dad29955afe7c6d2279c4fd4`）

从 `3e180ca0` 后已作本地逐片提交：`2aa4edb7` warmup 所属线程 seed／旧代门控，`aeee48f1` 六配置生产 loader 与规则 registry，`517e87ed` 辅助实体和向量缓存，`6dfec4c1` 请求配置 pin，`76fb1c46` 排序预算，`6532fbc8` 单评估缓存，`00178d28` 派生缓存，`bca67d26` 单次 MCM/目标资格 cache key，`cb8f83ec` sticky，`0e22a7d7` 语义召回，`2d2b2a31` 跨意图聚合，`2f11b09e` 保留 sticky 跨 reload，`e4f94429` 最终命中／诊断排序。`a39cc411` 精确逆变换审查 Scene 生命周期源码；`9c57d392`、`a75bbb21` 分别修正 Scene/Courier 的狭窄测试 fixture，未更改生产断言。单 DLL/旧 public 签名、JSON 与资源路径身份未改；MSBuild 两线各 780 Compile／7 EmbeddedResource，两个 Prompt LogicalName 原样。

验证：生产 loader 22、Retrieval 88、Courier prompt 252／liveness 59／postprocess 39、Scene parity 71／queue 37／request lifetime 30、Native preparation 589／admission 44／completion 184／pending 111／history 852；Debug／Release 原 `build_single_module.ps1` 的 1.3、1.4、Bootstrap 均成功，运行前检查精确生成目录，**无 `-Stage`／`-Deploy`**。代码地图 211 锚点 recorded／working-tree 通过。PersistenceProfileConfigContract 首个失败仍为 `extra=['synthetic-only-key']`（runner 扫入测试合成 key）；临时排除此目录后还出现 `typed SyncData binding catalog drifted`，试验改动已撤销，未放宽生产断言，不能把该 runner 算通过。

**仍为 `PARTIAL / NOT_ACCEPTED`，不可写 `J03_OFFLINE_VERIFIED`。** 六份 loader 的完整旧红／并发生产契约、配置模型深层只读、My/Reward/Scene/Native/Policy 候选消费者完整契约、语义与辅助真实入口双路径及所有 mentions/scope 异常闭包未全覆盖；`AIConfigHandler.TryGetGuardrailEvalSnapshot` 仍掌握 ONNX／辅助网络接缝和评估编排。实机、旧档、真实 provider 均 `NOT-RUN`。sticky 目标总量保持既有无硬上限，未擅自淘汰。未推送、部署、迁资源/用户数据或扩展 J04/J06。原先未提交的本节与 HANDOFF 规划差异仍留工作树，未纳入任何本地代码提交；`.dotnet-cli-home/` 亦未跟踪、未删除。

### 以下为较早的三切片部分实施回执（历史）

本节取代下方规划标题的“IMPLEMENTATION_NOT_STARTED”状态；下方规划原文保留为实施范围，不视为已全部完成。基线 `062c5939` 后有三个已核实本地切片：`3ef5e7e9` 候选纯匹配排序、80-key/10 分钟索引及唯一 `IntentQueryOptimizer` 归位；`848fc4c2` 六模型本地加载后原子发布 revisioned 快照，配置门面及公开签名保留；`d11eb572` 请求 ambient/scope 归位并在 My/Scene/Native 入口接线，子 scope 的 mentions 合并后恢复父值。均为同 DLL 内部接缝，未推送、部署、Stage 或改资源路径。

**未完成，不能标 `J03_OFFLINE_VERIFIED`：** 六份 loader/错误回退的真实生产矩阵与旧红用例尚缺；模型深层只读性未闭合；规则召回、评分、配置派生缓存、辅助实体 store 与 sticky 仍在 `AIConfigHandler`，warmup seed 所属线程捕获未实施；MCM/目标资格全量缓存隔离、所有 Courier/Reward/Policy 消费者及并发 reload/warmup 的实际生产行为未完整证明。J04 全线程改造与 J06 知识索引仍不在本包。

**已验证范围：** PromptJ03 Release 聚焦 `34` 检查通过；Scene postprocess parity `71` fixtures、deferred queue `37` fixtures 通过，后者 game-domain helpers 为 `STUBBED`；Debug/Release 原 `build_single_module.ps1` 的 `BannerlordApi=1.3/1.4` 与 Bootstrap 均已执行成功，未带 `-Stage/-Deploy`，运行前核查脚本重置的精确生成目录。地图 `197` 锚点以源码 `d11eb572` 两模式通过，旧锚点仅针对源码真实变化重定位/重算哈希，新增锚点明确残留 owner。Courier 源检查通过；PersistenceProfileConfigContract runner 当前失败 `extra=['synthetic-only-key']`（测试合成 fixture 被扫描），不能计入通过；其他受影响回归尚未全跑。实机、旧档、真实 provider：`NOT-RUN`。这些通过项不替代 J03a–J03e 整体验收。

本节及根 HANDOFF 在本任务开始前已有未提交规划改动，新增状态直接叠加于工作树，未将原改动纳入本任务切片提交；后续提交须保护原作者差异。下一步继续按下方 J03a–J03e 清单迁移旧 owner、补真实回归，再做全量验收。当前保留所有原用户改动。

<a id="j03-current-plan"></a>

## J03 当前计划：Prompt 配置与检索（2026-09-18，PLAN_READY / IMPLEMENTATION_NOT_STARTED）

本节取代下方路线表 J03 的粗粒度描述，作为执行模型直接接续的工作单；历史回执不改写。调查基线：工作区 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，HEAD `062c5939ddf436ccab81f6e7eec33d67906701a3`，开始时工作树干净。依赖复用 [J02 源码/离线验收](#j02-full-completion)，不重做 J02 或为无关 HOLD 开全仓盘点。**本轮只交付计划，产品尚未实施；接手实施的模型核对工作区后直接执行 J03a–J03e，不先返回重写计划、申请开始或逐包等待调度。**

### 1. 目标与非目标

- 在同一 `AnimusForge.dll` 内，把配置加载/版本/只读视图归到 `src/modules/AF.Module.Prompt/Configuration`，把意图规范化、规则召回/排序、请求上下文和检索缓存归到 `src/modules/AF.Module.Prompt/Retrieval`。迁移真实算法与状态并接通旧入口，不以 partial、目录或反向转发壳报完成。
- 保留 `AIConfigHandler`、`PromptListRetrievalService` 的既有签名及 JSON/资源身份作为兼容入口；它们仍含必要游戏适配或未迁职责，不能整文件标为已模块化。
- 非目标：J04 的完整 Prompt 组合/三渠道主线程捕获改造，J06 的知识库/索引，J05 记忆和 J08 网络传输；不重写资格/玩法/标签，不新增 public API、独立 DLL、Host 或 manifest。默认资源先确认归属和引用，**本包不搬运行 ModuleData、PlayerExports、ONNX 或改一键脚本**；物理内容布局留 J15，不阻塞本源码包。

### 2. 已核实代码与真实消费者

以下为基线修订的一基行范围，符号为追踪依据；不是逐方法搬迁清单。

| 源码 / 符号 | J03 责任及真实消费者 | 保留接缝 |
| --- | --- | --- |
| `AIConfigHandler.cs:240-328,2301-2426,3031-3140,9024-9303`：六配置字段、`BuildRulePromptRegistry`、两内置默认 loader、`ReloadConfig` | Configuration；`MyBehavior.cs:18863-18864` 会话加载、`:57900-57944` 导入后 reload；`SubModule.cs:221-225` 控制台 reload；DuelSettings 辅助连接测试仍消费原入口 | 文件定位依赖 `AnimusForgeModulePaths.cs:29-43`；导入写盘不归配置 reader |
| `AIConfigHandler.cs:1672-1779,2537-2701,3722-3759,5063-5269,5386-6191,6360-6995`：意图拆分、warmup、向量/排除提示缓存、辅助实体、评估与 sticky 合并 | Retrieval；`MyBehavior.cs:30650-30696,30792-31561` 前处理与共享 builder，后续正文和后处理读取同一话题结果 | `IsRuleCurrentlyEligibleForRag:2107`、runtime instruction/constraint、辅助 HTTP、Lore 调用不能随大段代码混入纯计算 owner |
| `AIConfigHandler.cs:290-302,326,2031-2058,7187-7258`：七个 guardrail AsyncLocal 与 latest entities | 请求级 owner；MyBehavior 上述入口及 `:28780-28835`；`ShoutBehavior.ScenePostprocess.cs:176-193`，`ShoutBehavior.cs:19956-19982,20507-20524,22968-22988,23018-23512,27999-28010` 都有 set/finally-clear | `MyBehavior.cs:31552-31560` 只清目标而未清 semantic；不能宣称现有 finally 已完整隔离。latest entities 另有显式 clear 和跨步骤读取 |
| `PromptListRetrievalService.cs:16-219,221-318,320-413,415-1093`：候选缓存、匹配/排序、资产策略及别名 | Retrieval 接收纯候选描述；生产链含 `MyBehavior.cs:24064-24086,24436-24509,24773-24776`、`RewardSystemBehavior.cs:18114-18549,19836-19874`、`ShoutBehavior.ScenePostprocess.cs:731-891`；`PolicySystem/History/PolicyHistoryRetrievalService.cs:225` 复用 mention terms | 原 payload 是 RewardItemInfo / MyBehavior 嵌套条目，含游戏句柄且只复制 List，不是深不可变快照；全量授权与展示清单不可混并 |
| `IntentQueryOptimizer.cs:9-228`：`OptimizeSplitIntents`、2/4 意图上限 | 实际被 AIConfigHandler 拆分和 `MyBehavior.cs:32187-32248` 历史召回共同消费，保持同一实现 | `git grep IntentAnalyzer` 只命中旧路线文字：**不存在该生产类/文件**，不凭名字新建另一分析器 |
| `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs:24-111` 与 `Refactor/Runtime/RuntimeConfigSnapshotStore.cs` | 已有 Native/Courier detached provider 配置快照；reload 后保留原通知 | 不是六份 Prompt 配置快照，不能复用其“失败保留旧值”替换 AIConfigHandler 的失败默认值策略 |

### 3. 职责、状态与关键设计决定

**Configuration：一个权威 owner，不再分散六个可变全局根。** loader 负责既有解析、校验、默认/错误结果和静态 registry 规范化；提供内部 `CaptureConfiguration()` / `ReloadConfiguration()` 形态的窄读写口（名称由执行者定），返回带 revision 的只读配置集合。原模型类型/字段身份保留，发布后的集合不向消费者暴露可修改引用；原 getter 需要 List 时返回兼容副本。registry 只缓存静态规范化，不缓存当前场景资格。

- 当前事实：reload 顺序写六字段，无总发布锁；成功路径清两向量 cache 和单条 `_lastGuardrailEval`，再递增 `_guardrailConfigVersion`、重置 warmup；外层异常直接换六个默认模型，跳过这段失效。该 version 不是 JSON schema version，也不保证一致快照。
- 计划选定：低频 reload 在独立串行加载边界构造完整 replacement，随后一次发布配置及其 revision；任何实际替换（包括既有异常默认结果）都换代。读取不长时间持锁，既有 in-flight 检索只持本次捕获引用；旧版本计算只能写旧版本缓存，不能污染新版本。**这是本计划明确的并发一致性修正，不冒称旧代码已有保证**；不改变合法配置结果与各类失败 fallback。只固定单次检索/构建操作，不在 J03 强行把整个三段对话钉死到一个配置版本。
- 不把 credential、MCM 对象或游戏活对象塞进快照；现有 provider 配置通知继续走原 adapter，两个 revision 不互相冒充。reload 不写用户文件，不做自动修复/覆盖。

**覆盖顺序不能统一成臆造的“默认→用户→MCM”。** 实际是：活动模块 `ModuleData` 文件→各 loader 专有回退；registry 先四类 legacy，再按 `RulePrompts` 顺序同 ID 覆盖。Preprocess 读严格 UTF-8，磁盘 schema 旧于内置版本时整份用内置；缺失/损坏保持错误状态，不能误改成一律默认。RP introduction 缺失/无效可用内置默认，内置也坏则停用。知识开关/semanticFirst/topK 在 MCM 可读时优先，其中 DirectTopN>0 优先于 SemanticTopK，夹到 1–12；无 MCM 才用 Guardrail。规则 cap 来自 MCM、默认 4、夹到 1–12；候选 cap 默认 10、夹到 1–30。辅助路由由 UseAuxiliaryRuleApi 或 MemoryPreprocessMode 1/2 决定。MCM 值按操作捕获，不因文件 revision 未变而永远缓存。用户导入是 `ImportKnowledgeFromDir` 按 overwriteExisting 复制 `knowledge/AIConfig.json` 后 reload，不是 loader 自动 merge PlayerExports；保留这一顺序及用户选择。

**Retrieval：按状态寿命分 owner，而非一个万能缓存。**

- 配置派生：语义 phrase/input cache、单条评估 cache、排除提示列表及 warmup 门控归检索 owner，绑定所捕获的配置 revision。锁只保护读写/发布，不包 HTTP、embedding、rerank 或游戏回调；不把原多个锁合成全局大锁。评估 key 保留输入/secondary/context、aux/rag、排除列表，补充实际影响结果的捕获 MCM 值、目标/资格签名与 revision，防跨目标或热改配置误复用；不是靠每次清空取消缓存收益。
- 请求态：内部 detached `PromptRetrievalContext` 持 semantic 文本、六目标标识（agent 缺省 -1）、必要资格值与本次 options；一个 AsyncLocal scope 负责保存/恢复父上下文，正常/异常/提前返回均经 finally/Dispose。兼容 Set/Get 暂留但转接唯一 owner；迁完具名 set/clear 消费者才算闭包，不能换成 ThreadStatic。可变 latest entities 不共享父子引用，应 clone/显式返回；保留原跨步骤累计效果，先把结果通过现有 out `mentionedEntities` / prompt context 接到后续清单和后处理，再退出 scope，不能因“自动清理”丢失 mentions。
- 跨调用态：辅助实体 FIFO（64）及 sticky 按目标状态归 Retrieval；保持现有 merge、clone、回合衰减与排除语义，**不顺便随 reload 清空它们**。sticky 每目标最多 3 项，但目标总数无硬上限；这属于已发现的既有容量风险，不偷偷新增淘汰而丢规则。当前 J03 不承诺解决全局 sticky 生命周期。
- 候选态：匹配核心用 ID/原序号/别名/计数/私装标志等 detached 描述，返回选择 ID/索引；别名中 Hero/ItemObject/CharacterObject/Settlement 读取和有效性判断留薄 adapter。TTL/80-key 容器逻辑抽入专用候选 store，但带游戏句柄的兼容 payload 仍由 adapter 持有并只按原调用域使用；不得把它标为后台安全、配置快照或新增动作授权。J03 不改 key 的 scope/entity/agent/settlement/discriminator 语义，不把所有 scope 拆成各 80 个。

**J04/J06 依赖止于接口。** J04 将消费配置视图、detached 检索输入、RuleSelection（规则 ID/命中数据/mentions）、scope 和候选选择结果；资格捕获、完整主线程→后台流水与正文/后处理组合仍归 J04。暂留旧适配调用域并标注现有 Courier/shared builder 的后台 live-read 风险，不在 J03 偷换 Task.Run/dispatcher。J06 继续通过现有 `GetLoreContext → KnowledgeLibraryBehavior.BuildLoreContext`、embedding/reranker 服务接缝消费 options；不迁知识索引、ONNX 生命周期或 Memory 状态。纯检索 owner 不反调旧 AIConfigHandler 算法，允许暂由旧适配器提供资格结果、网络结果和游戏描述，禁止“新服务全部转回旧大类”。

### 4. 有依赖顺序的小包

| 顺序 | 完整交付单元 | 完成边界 |
| --- | --- | --- |
| J03a 基线/契约 | 固定当前合法输出、坏配置 fallback、覆盖顺序、排序/容量与真实消费者；为三类状态写针对性生产切片测试，标出上述并发修正的旧红用例；记录本地实施意图 | 测试不读真实用户配置、不发网络；不重复 J02。将当前源码证据与待改行为分开 |
| J03b Configuration | 提取 loader/静态 registry/配置发布 owner，转接全部旧配置 getter 和 reload；保留原 path、JSON 类型、7 个 EmbeddedResource 集合及两 Prompt LogicalName | 原配置状态退出旧类；并发 reload、异常默认换代、捕获引用稳定与 MCM 覆盖可测；不迁资源文件 |
| J03c 纯检索与候选 | 归位 IntentQueryOptimizer；提取意图/候选规范化、匹配排序及候选 store 机制，薄适配器承接原 payload/别名/授权名单；接通 MyBehavior、Reward、Scene/Native 后处理和 Policy consumers | 公私装备、全量/展示 scope、fallback 和原索引稳定；共享历史召回仍用同一 optimizer，不归入 J05 |
| J03d 规则检索/上下文 | 在 b/c 上迁规则评分/召回/rerank、配置派生缓存与 warmup、辅助实体/sticky 状态；接通配置版本、请求 scope、目标/资格/网络窄接缝及所有原 set/finally 消费者 | 旧类不再拥有已迁状态/算法；完整 mentions 传递、嵌套/yield/异常恢复、迟到 warmup 不污染新代；不搬 shared builder 和 HTTP 实现 |
| J03e 集成收口 | 聚焦回归、双 API 原构建与 Compile/资源集合验证；补 owner matrix/代码地图/本节回执，HANDOFF 仅摘要 | 分别标结构、职责、离线与实机；遗留游戏适配、J04/J06/J08 符号仍明确。普通接线/编译/回归由执行者处理，不逐步重问规划者 |

执行模型先核对实际 Git 与本节基线差异，保留本轮两份规划文档和其他作者改动，记录本地意图检查点，然后连续完成 a→b→c→d→e，每个验证切片本地提交。包间不设置人工验收等待点，也不因名称、拆文件方式、内部 DTO 形状、测试桩、接线或普通编译/回归问题返回规划者；执行者在本节契约内自行解决并复验。b/d 中的原子发布、换代拒收、scope 恢复和 cache key 完整性已作出设计决定，直接落实。出现业务输出差异时先按既有行为修正适配与算法，不自行扩大为玩法改写；只有无法同时满足本节明确约束的真实矛盾才记录具体反例与受阻项，继续其余独立工作，不以泛泛“待确认”代替实施。

### 5. 必须保持的行为、性能、线程与兼容约束

- 规则 ID/code、优先级/分组、资格和排除、正文/`PostprocessRules` 同源、辅助 API 失败后的原召回退路均保持；不拿 LLM 结果当动作事实，不触碰唯一执行/AFEF 提交。
- 查询发生于对话/前处理/后处理，不新增 Tick 轮询。配置只在既有加载/显式 reload/导入时读盘；warmup 入口为 session_launch、mission_start、rag_warmup_complete（`MyBehavior.cs:18864`、`ShoutBehavior.cs:10993`、`RagWarmupCoordinator.cs:57`），仍按版本门控，后台只用预先捕获 seeds，不在 worker 调 `GetAllEnabledRulePrompts` 读取实时资格。尤其 RagWarmupCoordinator 的完成入口本身在 worker，不能简单把捕获前移一层便称主线程安全：复用所属线程调度做一次带版本检查的 seed 捕获，再后台计算；不等待网络、不新增逐帧扫描。旧 worker 不得将新代 warmup 门控清回 0。
- 保持向量 cache phrase 1024/input 256 的满容量清空策略、单条 eval、64 实体 FIFO；排除提示按版本懒建。每说话者最多 2 intents、合计 4；rerank 总预算 clamp(3×returnCap,8,36)，每意图 4–12，召回 10–30。这些是实际工作量边界，不是整次耗时保证。
- 候选 store 为 80 keys / 10 分钟，publish 时淘汰、get 时查过期；不是每个列表最多 80 项。候选选择保 0.66 匹配阈值、score→mention priority→原序号、fallback 原顺序；`FilterNpcRewardItemsForAssetTransfer` 的私装在普通 cap 之外，不能用硬截 30 项改变玩法。单列表、alias 和 sticky 目标总数现无全局硬上限，性能测量须报告输入规模，不伪称已有严格帧预算。
- 不在锁内捕获游戏对象/做网络；不新增跨线程读取。已有混合共享 builder 的线程缺口按 J04 接口留下，不因此把请求网络搬主线程；对仍混合游戏读取的入口，采用“旧适配器按原调用域准备输入→新 owner 执行纯算法→旧适配器继续原业务”的过渡接线，完成 J03 所列算法/状态转移；不把尚待 J04 的完整捕获阶段迁移当作 J03 的常规停工点，也不把旧线程风险冒称已解决。
- 同 DLL、namespace/既有 public 签名/模型 JSON 身份、原资源 LogicalName/安装路径、双 1.3/1.4 和单 Bootstrap 保持。无存档迁移；不把 RAG/候选缓存变成持久化数据；不扩增记录玩家全文或凭据的日志。

### 6. 验证与完成标准

实施时新增的聚焦测试必须运行**实际迁移实现**：配置六文件缺失/损坏/旧 schema/内置失败；legacy→custom 同 ID 覆盖和 MCM 有/无/热改；reload 与请求/warmup barrier 交错（旧结果晚返回、失败换代）；scope 嵌套、真实异步 yield、异常及提前返回、mentions 跨步骤消费；候选 TTL/81 keys/全量与展示隔离、私装超 cap、稳定排序；语义/aux 两路径、排除/sticky、2+2 intents 和预算。网络/embedding/游戏捕获用确定性 fake，不能只测新 DTO 或更新 source hash。

受影响回归复用 `tools/CourierPromptPreparationTests/{run.py,run_liveness.py,source_review.py}`（其 shared builder 是 stub，不能代替新检索测试）、`tools/ScenePostprocessParityTests`、Native/coordinator 既有测试；`tools/PersistenceProfileConfigContractTests` 只证明原配置/身份契约，不能证明新 Prompt 快照。PromptLab 可作格式补充，不用其网络评测替代生产接线测试。性能比较缓存 hit/miss/reload、同规模大候选列表与暖启动的实际工作项/耗时，不设未经测量的毫秒承诺。

全部具名消费者接入唯一 owner、兼容壳有清晰残余职责、上述行为/并发测试及两个 BannerlordApi 构建通过，才记 `J03_OFFLINE_VERIFIED`。集成验证固定使用 `一键编译覆盖推送/build_single_module.ps1`，分别完成 Debug/Release 的双 API 与 Bootstrap；不带 `-Stage` / `-Deploy`，不打包。执行者从 J02 既有构建证据读取参考目录与依赖参数并核实当前存在性，不要求用户重新提供已在仓库可查的信息。该脚本会重置仓库内 `bin/{Debug,Release}/single_module_artifacts` 与 `obj/single_module/{Debug,Release}` 并修剪生成物；调用前核对这些精确路径只含可再生成产物、非重解析点，工具层必要安全审批按环境处理，不另设规划回问。日志/测试输出留仓库忽略的 artifacts 或 runner 生成目录，不写系统临时查询文件。绝不把过去 J02 的构建当作变更后 J03 构建。更新代码地图并运行 recorded/working-tree 两模式；实机、旧档、真实 provider 仍单列 NOT-RUN，许可/玩家数据 HOLD 不解除。

**直接执行结论：** 本包没有要求执行模型返回规划者选择的关键设计项；按上述范围、顺序和完成标准做到 J03 源码/离线验收收口，再报告实际结果，不只交付下一份计划或停在首个小包。J04/J06 不构成等待条件。不存在待寻找的 IntentAnalyzer 或待选择的全局用户配置合并方案。sticky 总目标限额/跨游戏候选生命周期、完整三渠道线程捕获明确不在本包，保留原行为并登记后续责任，不借此缩减本包已列实现与验证。

本轮未实施上述任何 owner、并发修正或测试。规划验证：`git diff --check` 通过；新增显式源码路径/行范围、当前入口锚点与 HANDOFF 链接已检查；两文件原历史正文按换行归一后完整保留；Git 差异仅本台账与 HANDOFF，HEAD 未变。没有执行游戏构建或产品测试。调查时曾误把源码引用查询结果重定向到系统临时文件 `C:/Users/PC/AppData/Local/Temp/af-j03-refs.txt`，这是违反本轮仓库外只读约束的操作偏差；未继续修改或擅自清理该文件，不声称全程零仓库外写入。

## 以下为既有规则、交付与实施历史；J03 当前计划以上节为准

<a id="skill-plan-execution-20260918"></a>

## 当前任务：精简规划与执行规则补充（2026-09-18，SKILL_VERIFIED）

- 基线 `4ae94412`，工作区干净；仅补充仓库维护 Skill 的 `references/module-and-bridge-workflow.md` 及本台账/HANDOFF，不新建 Skill 或竞争计划。
- 规则：只请求计划不自动实施；计划解决关键设计而不逐行翻译代码；近期细化、远期保留路线；获准执行者自行处理普通实现/回归，改变设计、行为或授权范围才请求决策，不削弱验收。
- 验证：`python -X utf8 -B .claude/skills/animusforge-maintainer/scripts/verify-af-skill.py` 和系统 `skill-creator/scripts/quick_validate.py` 对仓库维护 Skill 均 exit 0；`git diff --check` 通过。仅验证结构、元数据、链接与差异格式，未做独立模型行为测试，未重跑 Bash 全套或产品构建。
- 本次为仓库局部补充，外部主源与全局副本未同步；原安全、性能及兼容约束不变。生产仍为 J02_OFFLINE_VERIFIED；J03、实机/旧档、全仓 HOLD 状态不变，不推送、部署、打包或安装。

## 以下为既有交付与实施历史，不构成本轮执行或发布授权

<a id="full-delivery-20260918"></a>

## 当前交付：R2 / J01 / J02 与 Skill 0.2.0 完整源码文档（2026-09-18）

用户明确要求“推送远端仓库”，并纠正不得遗漏 J01/J02 源码。本轮交付全部已完成源码、测试、代码地图及配套文档，不再缩成仅 Skill。目标 `origin/codex/af-main-refactor-continuation-20260831`，核实远端起点 `d92c4b3e5c63ec1b16dca5b976c123eb56016cca`；普通快进，不操作 main、不强推、不重写历史、不部署或上传本地构建产物。

- 源码：R2 `64eaa7a8`；J01 `156e6836` / `02f1747c`；J02 `102eab84`、`82660997`、`469e3712`、`9d14a1ec`。真实实现、消费者和测试都保留，不因文档交付而排除源码提交。必要历史源码引用因此继续可达。
- 文档：维护 Skill 0.2.0、框架 Skill、协调说明、仓库结构、AGENTS、HANDOFF 和主台账一起收齐。此前为避免混入旧改动而未提交的六份工程文档，现作为用户要求的完整配套文档交付；不涉及其他作者源码或外部副本。
- 历史核实：`ae8e6b89` 仅误纳两份工程文档的既有规划/环境/验证记录，`3a57007d` 曾撤回相关增量。不能据此断言含密钥或玩家存档正文。31 个待推送提交的 161 个新增 blob 已进行路径与凭据模式筛查：未命中受保护交接、Stage/PlayerExports、本地缓存、DLL/压缩包或凭据模式。筛查是有界检查，不宣称通用隐私证明；明确仅留本地的资料仍不上传。旧整分支禁推标记在本次实际核实和明确授权下不再适用于这些普通工程文档。
- 验证：当前生产 C#/工程/构建入口与已验 `9d14a1ec` 无新增差异，沿用此前双 API + Bootstrap 的同源验收；本次代码地图 recorded/working-tree 各 185 PASS、Skill 11 tests / OK、最终差异检查。此次不重跑实机/旧档/provider 或游戏构建，也不把发布当作 G0.7 或整个重构完成。
- 发布核对：推送前再次核对远端起点及快进关系，成功与否以远端 ref 和本地提交相等为准；完整检查与回执仅保存在忽略的 `artifacts/af-push-20260918/`，不随源码上传。

## 以下为实施与验收历史（旧未授权/禁推状态已由本节取代）

<a id="skill-restructure-20260918"></a>

## 当前任务：AF Skill 与配套文档重整（2026-09-18，SKILL_VERIFIED）

本节是唯一当前执行入口。用户已批准以 `D:/下载/af-skill/af-skill` 为主源重整维护规则，并对齐本仓库实际读取的维护 Skill、框架 Skill 与入口；不修改产品代码、工程配置、一键构建或 J01/J02 的历史结果。保留已落地的一套源码、1.3/1.4 双实现、单模块与 Bootstrap 唯一选择约束。

本地起点 `aea89eb5`，空检查点 `56fc1205`；修改前六份既有 dirty 文档及相关 Skill 已保存至 `artifacts/af-skill-restructure-20260918/before/`，原差异见同目录 `preexisting.patch`。验收为结构/链接/YAML/模板、脚本语法、五类真实路由及失败反例、主源与仓库副本的一致性和源码未变。完成证据集中在本节与该私有本地产物目录，HANDOFF 只链接摘要。

以下所有旧“当前/调度/执行意图”段落均为历史，不再发出操作指令；其中代理数量、模型分工、总控及核验安排不再适用，不由另一套人数规则替代。生产状态仍为下方 J02_OFFLINE_VERIFIED；J03 不自动启动，LIVE/旧 SAVE/provider 未验、G0.7 与许可/用户数据 HOLD 不变。本分支历史含本地私密记录，仍禁止推送/发布；本次不部署、打包或全局安装。

### 本轮完成与验证

- 主源与仓库维护副本更新为 0.2.0；主入口只作识别/约束/路由。架构区分双版本运行现状、逻辑职责和独立平台目标；工作包、仓库依赖、验证、台账与模板已统一。两份历史审查分别原字节保存到各自 `references/history/*.txt`，不再作为默认执行清单。仓库保留 framework coordination 定制；`agents/openai.yaml` 的发现策略未改。
- 主源/仓库 27 个非入口共享文件按规范化文本一致，入口差异仅为仓库协调元数据与链接；四份历史快照与各自修改前原件 SHA 一致。当前入口与框架的 8 处 Markdown 链接/锚点通过；原 HANDOFF/台账全文仍作为历史后缀保留。
- 主源与仓库各执行 `scripts/test-af-skill.py`：**11 tests / OK**（包含五类路由、身份拒绝、坏相对链接与锚点、错误版本/YAML/双版本模板、措辞替换、脚本语法、相对入口、安装 dry-run 与拒绝覆盖）。完整日志为 `artifacts/af-skill-restructure-20260918/{master,repository}-tests-verified.log`；fixture 均隔离本地，未实际安装。
- 三个 Skill 的 `quick_validate.py` 均输出 `Skill is valid!`；`git diff --check` 通过。原校验器对新文档依赖九条固定英文措辞，迁移前回归记录为 6 failures，已由结构/解析/行为检查替代；没有删掉失败用例来通过。
- 另一次直接 PowerShell 启动非登录 Git Bash 暴露 `dirname: command not found`，已把 Skill 脚本路径/大小写处理改成 Bash 内建；直接入口与回归复验成功。Skill 内 `.gitattributes` 仅固定 `*.sh eol=lf`，避免 Windows 检出破坏工具入口，不修改产品/全局 Git 配置。
- 变更范围与 SHA 记录见本地产物 `audit.json`；产品 C#、工程、模块 XML、一键构建、资源和玩家数据未改。源码地图内容未变，不为说明更新重跑 185 项产品地图或游戏构建。实际宿主重新发现、实机、旧档、部署、ZIP、上传、全局安装均 NOT-RUN；这些不是本次 Skill 验收项。
- 提交只包含干净文件和能独立应用于原索引的本轮 hunk。框架 SKILL、framework-coordination 与 repository-structure 三份文件和先前未提交修改重叠，已在工作树完成合并但不整份暂存；其余原有 dirty 内容也不混入提交。不得把单个提交误称为包含全部本地定制的可发布包。

恢复时使用本轮修改前精确文件备份核对并生成定向逆补丁；不恢复整个目录、不覆盖后续作者改动、不改写历史。本任务规则与工具已完成，下一产品工作仍按新的明确请求选择，不自动启动 J03。

## 历史执行与验收记录（保留原文）

<a id="parallel-controller-handover"></a>

<a id="j02-full-completion"></a>

## 当前回执：完整 J02 源码与离线验收完成（2026-09-18，J02_OFFLINE_VERIFIED）

用户要求“那你做完J02啊”，本节取代下方仅目录生命周期子包/ACTIVE状态。J02原行约定的Foundation/宿主通用责任、实际消费者、有限目录归位均完成；不是完整项目、G0.7全仓清理或实机验收完成。起点3706e87d，意图5c97bfb0/overlay补充108a7c15；本地生产切片B `82660997`、C `469e3712`、A `9d14a1ec`。同一工作区/分支，原六份dirty正文保留；不推送、部署、打包、改一键流程或源玩家数据。

### 原J02逐责任闭包

| 责任 / 已核实路径和一基坐标（源码 `9d14a1ec`） | 实际迁移与消费者 | 保留 / 未覆盖 |
| --- | --- | --- |
| `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs:7-95` | 目录4状态字段、Initialize/Shutdown/CaptureSnapshot唯一owner；Runtime门面接入；本项沿用102eab84并已集成复验 | 目录不等于通用插件热卸载Host |
| `src/AF.Foundation.Runtime/Diagnostics/DiagnosticTraceContext.cs:6-54`、`MetricWindow.cs:6-57` | Logger的AsyncLocal trace/父scope及180秒指标窗口真实退出旧根；Logger BeginTrace:280、Metric:508调用 | Logger记录文本/路径/MCM、TraceScope输出门面保留；RecordHitRate:522-616为J03/J06领域观测 |
| `src/AF.Foundation.Runtime/Diagnostics/BoundedLogWriteQueue.cs:10-147` | 4096/8192背压、drop计数/摘要节流、唯一worker门控/批量/排空；Logger EnqueueLogWrite:1366调用 | UTF8文件sink/清理原位置；Logger tokenStats队列:769-845属J08 LLM消息dump，隐私与领域队列不冒称已解决 |
| `src/AF.Foundation.Runtime/Diagnostics/PerformanceWindow.cs:8-264`、`FreezeWatchState.cs:9-215` | Perf帧/桶/事件/30秒窗口与Freeze心跳、scope、256事件环/缓存唯一owner；根门面接入 | Perf保250ms MCM缓存；Freeze保唯一实际监控线程、游戏现场读取、OS dump、文件sink；无新逐tick委托/扫描 |
| `src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs:6-65`、`GameLifetimeCoordinator.cs:7-39`；`src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs:9-88` | 已提取owner原字节归位：generation、实际Game身份、退役/准入暂停/Seal/reset-clear不重写；四迁移raw SHA相同 | Registry不是第二业务队列；J05记忆预算/J07渠道调度/请求lease与业务SyncData不在J02 |
| `src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs:11-65` | 实际My/Shout/Courier实例捕获/退役；SubModule GameEnd:98-103、Unload:105-112、InitializeGameStarter:120-140继续接线 | 不接管团队业务存档/整个游戏生命周期平台 |
| `src/AF.GameAdapter.Bannerlord/Composition/{CampaignComposition,CampaignModelComposition,TeamModuleRegistration,TeamModuleServices,ModuleFrameworkRuntime}.cs` | 五原owner归位，namespace/类型/内容不变；Runtime:11-36仍工厂与静态兼容门面，未造第二Host | Git100% rename/归一原文证明；迁前raw SHA未捕获，不冒称双向raw哈希 |
| `src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs:7-548`、`ApplicationTickComposition.cs:7-137` | Startup注册顺序/逐组catch与36相位fast/watched/异常finally/WarStats真实移交；SubModule:114-118、142-145仅引擎壳 | SubModule UIExtender/欢迎/Mission/WarStats适配仍必要引擎或J13领域责任；不重写玩法，不改公开ABI |

### 本次真实验证与证据

- B：四迁移前后raw SHA一致；24个非overlay runner仅逆路径替换、5个历史校验适配保留原hash。隔离GameLifetime 36 checks+12编译后变异、bindings15、commit19、真实SaveGuard8及Courier/Memory历史链通过。证据 `artifacts/g0-closure-20260917/j02-b-lifecycle/verification.json`。bindings旧默认G: SDK不可用，隔离wrapper只注入本仓SDK/HERE/原fixture；不声称旧入口直接通过。首次Guard缺本地DOTNET_CLI_HOME失败，改本地环境后通过，原失败日志保留。
- C：完整SubModule严格逆组3706e87d后接旧历史链；五composition归一原文相等；36相位Tick replay+5编译后指定反例、Campaign42+5、Team308+3、Scene71、Native正常/enum重排各41+8指定反例通过。证据 `artifacts/workspace-j02-host-composition/after/{receipt,native-mutation-results}.json`。这不证明真实Harmony/游戏运行。
- A：新owner合成控制6组、24个旧Logger/Freeze声明的可执行旧新oracle4组、6个编译成功且指定行为失败的突变通过；总控独立复跑均exit0。受保护领域/平台区段原文、Perf4方法体及操作顺序检查通过；**不是三根整个文件严格inverse**。实测涵盖异步trace、窗口清理、背压/drop/worker失败重启、scope/ring；不执行真实日志、玩家文本或OS dump。证据 `artifacts/workspace-j02-completion-20260917/integration/diagnostics-parent-verification.json` 及其日志，底层oracle/mutations在 `artifacts/tests/j02-diagnostics-a/`。
- 原脚本完整Debug与Release各1.3+1.4+Bootstrap+本地Stage成功（已授权六固定生成根，重验不越界/无reparse）；两侧日志有Build/Stage success，invocationStatus=true/scriptThrew=false。JSON保留的lastExternalExitCode=1是robocopy复制成功返回值，不能把此字段写成0；外层执行结束0。6组artifact/Stage DLL SHA一致，两实现不同，XML只载Bootstrap。源码与构建归一SHA相等；Logger仅恢复原混合换行，raw SHA不同明确记账。
- 双API各762 Compile/7 EmbeddedResource=旧755+7新owner+9路径映射；完整Identity/Link/LogicalName和既有Reference HintPath等集，Bootstrap3/0。实际当次4实现DLL/1060 API元数据、API snapshot36/public119与并发128/256、5 API变异通过。证据集中 `artifacts/workspace-j02-completion-20260917/integration/{stage-debug,stage-release,fresh-after-artifacts,member-set-verification,api-actual-artifacts}.json` 及日志。
- overlay只调用build_file_set：J01的297→302，精确SaveGuard路径映射+从已列Logger/SubModule抽出的5个直接正文owner，类别等集；未调用create_package/写dist。旧overlay非独立完整工程，未额外扩整树。`tools/HostCompositionTests/.generated/`新增精确ignore，生成物不提交、不清理。
- 代码地图绑定源码 `9d14a1eca2c25075975134605c24d48666ee123a`，185锚点recorded/working-tree两模式均PASS；旧167中17定位更新、150项逐字段保留，新增18个owner/门面/残余边界锚点。独立只读复核未发现本包离线结项阻断。

### 保留风险与停止边界

J02源码/离线完成，LIVE、代表性旧SAVE、provider网络、真实Harmony与纯1.3运行环境仍NOT-RUN；既有1.3混合引用只证明原选择与本次构建，不冒称纯1.3实机。资产/许可/用户数据/全仓cleanup HOLD未解除；Stage含PlayerExports私密本地副本，严禁打包/上传。历史ae8e6b89仍含本地专用记录，本分支禁止推送/发布；回退只可按A/C/B做聚焦inverse提交，不reset/rebase。下一计划包J03需另按原依赖/门禁细化，不在本次自动启动。

## 以下为 J02 目录生命周期子包与此前历史

### 当前回执：局部源码准入闭合，J02-Lifecycle OFFLINE_VERIFIED（2026-09-17）

- 已从盘点转入交付：分类工具提交 `648bb084`，生产/测试切片 `102eab84134ee8e2ab2edb2e25d9f9aa7f560837`；本包真实状态 owner 提取、接线、两源归位及完整离线验证完成。**不是完整 J02 或全仓 G0.7 完成**，不得解锁未核实的后续宽包。下方 ACTIVE/意图为过程记录，以本回执为准；本轮不留虚假 ACTIVE。
- G0 可机械证据已落地：分类 7 tests 通过，标准库 trace 工具行覆盖 193/209（92.34%，非分支覆盖）；初始索引 22,186 项 UNKNOWN=0，业务 owner 单独标 UNVERIFIED，不用文件平面替代职责证明。依赖 SHA/来源、原参数求值、隔离离线 Restore+ReferencePath 全通过：1.3/1.4/Bootstrap 分别 86/87/15 个实际引用（含机器框架），无工程或构建脚本修改；1.3混合版本引用/未知分发许可仍明确保留。
- 源码坐标均绑定 `102eab84`：`src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs:7-95`（4字段，Initialize:14-56、Shutdown:58-66、CaptureSnapshot:68-94）；旧 `Refactor/Modules/ModuleFrameworkRuntime.cs:11-36` 仅工厂/静态转接、RegisterCampaign:22-25保留。两既有源 `InternalModuleDirectory.cs:8-420`、`ModuleFrameworkSnapshot.cs:8-47` 迁入同目录，Git 100% rename；未捕获迁前工作树 raw SHA，不将归一正文相等冒称物理hash对照。旧API/类型/namespace/程序集/存档身份保持；加载/卸载/显式查询频率不变，无tick扫描/新Host。
- 聚焦回执 `artifacts/workspace-j02-directory-lifecycle/after/receipt.json`：目录44、API snapshot36/public119及并发128/256、Composition42、Native正常/重排各41通过；5 API+5 Composition+8 Native变异均编译成功后指定行为拒绝。总控独立复跑 API与Campaign源码inverse通过，新owner/门面严格逆组60072f07后继续955a原历史守卫；独立Sol实diff核验无源码阻断。
- **当次集成通过：** 用户再次明确授权六个固定生成根重建及本地Stage（不改源数据/游戏/打包上传）后，原脚本 Debug→Release 均 invocationStatus=true、无throw、总命令exit0；每侧1.3/1.4/Bootstrap完整构建与Stage success。首次UTF-8管道助手错误发生在脚本启动前，修正编码后才执行；没有绕过权限。六组artifact/Stage DLL SHA一致、两个实现不同、XML只载Bootstrap；`stage-{debug,release}.{json,log}`及`fresh-after-artifacts.json`记录证据，Stage私密副本不上传。
- 总控当次 `ModuleFrameworkApiTests/run.py --dotnet local/dotnet/8.0.425/dotnet.exe --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts` exit0：4实现DLL/1060元数据及5行为反例通过（实参为工作区绝对路径，见`api-actual-artifacts.json`）。原参数迁后成员集合：两API各755 Compile/7资源=迁前754+新owner/两路径映射，Bootstrap3/0，无重复且全部Reference HintPath不变，见`member-set-verification.json`。地图绑定源码修订，7定位更新+4新增，其余156项不变；recorded/working-tree各167 PASS。
- 边界/下一包：资产/个人设置/日志/PlayerExports/原版参考/ONNX/工具产物原位HOLD；无清理/停跟踪/全局安装/推送/部署/ZIP。LIVE/旧SAVE/provider均NOT-RUN；本次本机混合引用构建不等于纯1.3或真实游戏验收。下一精确任务为 **J02余项的游戏生命周期/调度预算 owner闭包**（以SubModule真实消费者及既有runtime组件为起点），不是重做目录状态；先核既有实现和依赖，再派完整有限包，不以这次局部完成直接启动J03/J05/J08或宣称全仓gate已关。

#### 以下为本轮意图与门禁过程记录

最新用户要求“把他闭合再继续做”，取代本轮仅只读等待的执行状态。现在直接完成可验证的分类/本地依赖证据，再推进无资产/用户数据变更的具名源码子域；不逐微步请求继续。该决定不生成第三方分发权，也不批准递归清理、资产/玩家数据迁移、外仓写入或发布。G0 全仓结项与本轮有限源码准入分开记录，未解决的许可/数据/产物 HOLD 不谎报 CLOSED。

执行意图（G0-CLOSE）：A 负责 `tools/repository_source_inventory.py` 与聚焦单测，以 Git 元数据建立可重放互斥分类和未知项拒绝；B 负责复验固定本地 SDK/实际引用/hash，证据仅写新建 `artifacts/g0-closure-20260917/dependencies/`；C 细化目录生命周期真实 owner 提取包，待本地意图检查点/直接依赖核验后连续实现接线与聚焦测试。A 的本地清单仅在 `artifacts/g0-closure-20260917/inventory/`，不输出玩家路径/内容；独立 Sol 核验器审分类、依赖与源码边界。无删除、停跟踪、脚本/CI/CD 变更或游戏写入。总控独占索引/提交及共享构建；已有六份 dirty 只追加本轮 hunk，不能整份暂存。

本段取代下方旧任务的交接等待状态，不改 J01 离线验收结论。当前 Git 根为 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，接管 HEAD `41a12bbbc183ce3a64c930b9253448acaab44f33`；六份既有 dirty 文档与空索引已核实。

执行意图：Astra 总控接管；首轮三个 Sol 只读包分别负责 A 元数据分类/owner 缺口、B 实际依赖来源/许可与数据产物 HOLD、C 下一实施波次的符号/状态/消费者/冲突；第四个 Sol 独立核验。总控仅追加本台账与 `HANDOFF.md` 当前入口，集中记录结论，不另建总计划。验收为实际路径/一基坐标、可复查元数据、逐类门禁决定与有限工作包；复用 J01 证据，不重跑无改动的构建。

首轮不写生产、测试、资源、索引或共享产物；不读玩家文本，不移动/删除/停跟踪文件，不操作游戏/存档/外仓。后续源码实施须先明确包边界与相关门禁；G0.7 及各 HOLD 未自动解除。总控独占后续索引/提交/集成构建，本分支仍禁止推送/发布。J02 尚未实施，LIVE/旧 SAVE/provider 仍 NOT-RUN。

#### G0-CLOSE 门禁事实与本轮源码准入（2026-09-17）

- 路径平面分类已可重放：`python -X utf8 -B tools/repository_source_inventory.py` 对接管索引 22,186 项逐项互斥归类，UNKNOWN=0；敏感 HOLD 优先，3,586 项虽命中 ignore 仍被跟踪。7 项聚焦测试与总控独立复跑通过。旧 P2 的 22,182 是历史分母，不回写历史。这里只闭合文件平面证据，不把 source 类别冒充逐符号业务 owner；本包 Runtime→Foundation 的 owner/消费者另按下段精确闭合。
- 依赖证据 `artifacts/g0-closure-20260917/dependencies/{verification,references-evaluated-original}.json`：固定 SDK 8.0.425/官方 ZIP、63 个 1.4 manifest DLL 与原来源、18 个 1.3 overlay DLL、Bootstrap 与运行时来源 hash 已复验。无平台属性绕过的真实提权 `msbuild -getItem:Reference,Compile,EmbeddedResource` 三项目求值 exit 0；双 API 各 754 Compile/7资源，Bootstrap 3/0，迁前集合冻结。诊断中的 SDK 访问拒绝和临时属性求值另留记录，不冒充原参数成功。
- **来源边界：** 1.3 的 71 个 HintPath 中仅 18 个来自 1.3 overlay，其余 49 个来自当前游戏/模组、4 个来自 AF 私有运行时；1.4 的 72 个为 overlay63+游戏/模组5+私有4。这是既有选择，不在本包替换依赖或修改工程；可核实本次前后来源一致，但不能声称纯 1.3 依赖、clean-clone 或实机兼容。未知分发权均 `UNKNOWN_LOCAL_ONLY`。
- 数据/产物隔离决定：原版参考树、PlayerExports、ONNX、资产来源、tracked缓存/工具dist/个人设置等继续原位 HOLD，不移动/删除/停跟踪。`deploy_module.ps1:720-733` 的 Stage 复制 PlayerExports，`package_mod.ps1:752-764` 的 ZIP 过滤不排它；因此 Stage 按私密本地验证副本处理，本轮禁 ZIP/上传/发布。未读取玩家载荷或凭据。
- **本轮有限源码准入已具备，J02-Lifecycle ACTIVE。** 用户最新闭合后继续指令、明确源码 owner/无数据接触、固定依赖与迁前输入、检查点 `60072f07` 构成这个包的准入；不再等待全仓资产处置。G0.7 全仓整理/clean-source/分发结项仍未 CLOSED，剩余 HOLD 不被这个局部决定豁免，也不阻断此具名纯源码包。后续各包继续按真实依赖判断，不机械推导 J03 等已放行。

#### J02-Lifecycle 有限联合包执行意图（2026-09-17，ACTIVE）

用户本轮要求闭合后继续；本包仅拆清现有目录生命周期 owner，不启动全 J02 或 J03/J05/J08。基础源码 `41a12bbb` 的 `Refactor/Modules/ModuleFrameworkRuntime.cs:14-17,19-61,74-109` 四状态字段及 Initialize/Shutdown/CaptureSnapshot 实现转入 `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs`；原 Runtime 保留同签名静态门面、`TeamModuleRegistration.CreateDirectory` 工厂选择与 `RegisterCampaign:65-71` 原样接线。`Refactor/Modules/{InternalModuleDirectory,ModuleFrameworkSnapshot}.cs` 原字节归位同目录，namespace/type/程序集/存档身份不改。目录上限、锁覆盖、Ready/Degraded 重入、Stopped 保留目录、冻结快照与失败码不变；不新增 Host/反射/扫描/队列。Initialize 为加载频率、Shutdown 为卸载频率、Snapshot 为显式查询，非 tick 热路径。

Sol C 白名单：上述生产文件；`tools/InternalModuleDirectoryTests/InternalModuleDirectoryTests.csproj`；`tools/ModuleFrameworkApiTests/{run.py,source_boundary.py,SnapshotBoundaryChecks.cs}`；`tools/CampaignCompositionTests/run.py`；`tools/NativeModuleSubmissionTests/run.py`。测试先于提取补齐重入/工厂失败/停止转发反例；保留原源码逆变换链，不刷新 hash 掩盖漂移。总控独占相关现存 README/内部指南、代码地图/范围图/owner matrix/台账/HANDOFF 与集成验证。固定 `.generated` 和共享 bin/obj/Stage 串行；新证据根 `artifacts/workspace-j02-directory-lifecycle/`，不重置旧 J01 证据，不打包/部署/上传。路径移动仅以上两源码，非目录批量搬迁。

准入/退出：实际本地依赖与无敏感内容变更闭合后实施；迁前/迁后源码 inverse、完整 Compile/资源集合、目录/API/Composition/Native 正常及编译成功后指定变异、双 API+Bootstrap 原 Stage 验证。资料/缓存/许可仍隔离 HOLD，局部源码通过不记全仓 G0.7 CLOSED；LIVE/旧 SAVE/provider 保持 NOT-RUN。检查点 `67971c97`；失败保留证据并定向逆补丁，不 reset 或删除输出。

## 当前调度：交接新总控，J01 已完成 / 下一波未实施

用户最新要求先交接再创建新总控任务，采用 3 个 Sol 实施工作包 + 1 个独立核验代理、Astra 统一规划整合；详见[交接文件](handoffs/2026-09-17-j01-parallel-controller-handoff.md)。本次 intent 仅该交接及两入口链接，不改生产/测试/构建，不在旧任务续包。并行只能用于依赖满足且文件不重叠的包；Git 索引/提交/共享输出归总控独占。原 G0.7 及各类 HOLD 保留，新任务先用有界核验给出门禁事实和可执行波次，不将 J02–J17 路线当直接批量搬迁许可。下方 J01_OFFLINE_VERIFIED 结论保持；新任务接管不是历史工作已完成或高风险授权的替代证据。

<a id="j01-current-status"></a>

## 当前状态：J01_OFFLINE_VERIFIED / STOPPED_AFTER_J01 / J02_NOT_STARTED

用户最新“你直接改呗”明确扩大本次 J3 白名单：`tools/package_policy_system_source_overlay.py` 的 `runtime_assets` 两条旧路径改为 `AnimusForge/CustomPrompts/Policy/{CustomPolicyEvaluatorPrompt,NpcRulerPolicyPrompt}.json`，删除 `CustomPrompts/CustomPolicyEvaluatorPrompt.json` 已废弃重复项；原 J01c/d/e 有界授权保持。本段取代旧 BLOCKED/ACTIVE 当前入口，下方过程记录不回写。该修复仅固定文件清单，不改提示词 JSON、Policy loader/玩法、`create_package()`、dist、打包或一键脚本；未访问玩家文本。历史 `4c3e8d94` 为两资源迁入 Policy 子目录并删除三旧项，`DuelSettings.cs:5030-5055` 路由到 Policy，Policy ContractTests `Program.cs:8958-8974` 禁止旧根副本。

| J01 验收维度 | 当前状态 | 边界 |
| --- | --- | --- |
| 结构 | VERIFIED | 两协议原字节归位、根旧副本退出；地图源绑定 `02f1747c`，原 142 锚点不改 |
| 职责 | VERIFIED（仅协议） | 8 方法/4 常量归 policy、13 直连；旧宿主真实网络/SSE/配置/姓名仍混合 |
| 离线 | VERIFIED | 协议正常/变异、Courier/Legacy、双 API Stage 与 ABI/Composition/Native；地图两模式 163 PASS |
| LIVE / 旧 SAVE / 真实 provider | NOT-RUN | Stage/合成测试不可替代实机、历史存档或真实网络验收 |

- 解阻脚本提交 `0e6be2963a1d6aaea8e1b99f57d69608cfe5c011` 仅两路径替换/一重复项删除；迁前失败 `build_file_set()` exit 1 的日志仍保留。修复后迁前/迁后真实 `build_file_set()` 均 exit 0、297 文件/297 类别，两 Policy 资源各唯一且为 `runtime_assets`；迁后只把 `LlmApiCompat.cs` → `src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs` 的 `host_integration` 路径映射，完整集合/类别相等。证据 `artifacts/workspace-j01-llm-protocol/after/overlay-{pre,post}-relocation.json`；未调用 `create_package()`。
- 迁移与消费者源码提交 `02f1747c4e226d9c8e187f2503c6197ed6148156` 仅两份 100% Git rename 与三处路径替换：Compat `src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs:1-720`（原始 SHA-256 `95911a1ffbb2324529e4fa1156a864e13091d3c2020555c30194f76a8b1b8a74`）、Normalizer `src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs:1-485`、`StreamFilter:62-144`（原始 SHA-256 `76a660ee99846d4c4251dc00bf4af1a1ec472d7772f53d06765eefc48533e440`）；根旧副本退出。`CourierPostprocessOwnerRegressionTests/run.py:31`、`test_extraction.py:26` 与 overlay `host_files:102` 只改物理路径，namespace/公开 API 与 J01b policy 算法未动。旧 `ShoutNetwork` 网络/配置/姓名/统计及 Scene/Courier/Npc 消费者不因本次迁移扩写；完整责任/13 接线见上方 J01b 回执。
- 固定 SDK 8.0.425 求值两 API 的完整 `Compile Identity/Link`：迁前 753 经两路径映射并仅加入 policy = 迁后 754，无重复/遗漏、tests 未混入；7 个 `EmbeddedResource Identity/Link/LogicalName` 全等。真实 after/relocated 同一 Program restore/build/run `0/0/0`、13 PASS，7 个独立变异各 restore/build `0/0`、run `1` 且指定 `FAIL`；12 块及宿主整文件 inverse 通过。Courier 单测 8、LegacyShout 3、Courier 正常 39 PASS，8 原变异均成功编译/运行指定拒绝。首个 Courier 变异验收包装层误以为必须抛异常的假失败保留，已按原 runner 的 `PASS/FAIL` 汇总格式复核，未改断言/源码。证据在 `after/member-set-verification.json`、`artifacts/tests/llm-protocol/j01cd_*` 和 `after/courier-*`。
- 原 `build_single_module.ps1 -Stage` 的 fresh Debug/Release 均完成 1.3、1.4、Bootstrap，各 0 错误，Stage success；直接脚本 `$?=true`、无 throw，最终命令各退出 0。Debug 首次包装层把 `deploy_module.ps1` 成功 robocopy 剩余 `LASTEXITCODE=1` 错判为失败，原日志保留；重新预检 Debug 三根并用正确的直接脚本状态重跑通过，Release 同样通过。六个产物/Stage DLL SHA 一一相等、1.3 与 1.4 不同、`SubModule.xml` 只加载 Bootstrap；全部六个完整 SHA 与日志在 `after/fresh-after-artifacts.json`、`stage-{debug,release}-final.{log,json}`。六输出根每次重置前逐级/内部检查无 reparse 或未知项，旧 before DLL hash 已冻结；Stage 含私密 PlayerExports 副本，不上传，也未 Deploy/写游戏。
- 使用上述**当次** Debug/Release artifact-root 的 API runner exit 0：4 实际 DLL、1056 ABI 元数据、3 snapshot 反例与外部 `CS0122`；Composition exit 0：42 断言与 5 反例；Native 正常/重排各 exit 0、41 断言与外部 `CS0122`，8 原变异各 exit 1、与迁前相同指定运行 `FAIL` 且无编译错误。各真实命令/退出/日志在 `after/gate-*.{log,json}`。此为离线源码/产物验收，不等于实机加载或真实三渠道网络行为。
- **J01e 结构/责任回执：** [163 点代码地图](architecture/af-framework-code-map.json)的 `sourceRevision=02f1747c4e226d9c8e187f2503c6197ed6148156`，历史 `remoteBaseline` 不动；原 142 锚点逐字段保持，新增 21 项覆盖 policy 8 方法/4 常量、Compat/Normalizer/每流 StreamFilter 与旧宿主实际 send/入口保留。`python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py` 的 recorded 与 `--working-tree` 两模式均 `anchors=163`、exit 0。 [代码范围图](architecture/af-framework-code-scope.md)和[owner matrix](animusforge-owner-matrix.md)仅更新本包职责；ShoutNetwork 仍是混合宿主，不将 transport、三渠道或整个 LLM 标完成。
- **离线验收/停止点：** 真实源码算法提取、两文件原字节路径迁移与 13 接线均完成；13 协议用例/7 可编译变异、Courier/Legacy、双 API 完整 Debug/Release+Bootstrap+Stage、实际 DLL ABI/Composition/Native 均已离线验收，上述 after 日志与源码 hash 绑定。逐字符 Unicode 流发射 `a4F6` 的既有缺陷保留未修；LIVE、旧 SAVE、真实 provider 网络、游戏部署均 NOT-RUN，不由 Stage 冒充。用户要求完成 J01 后停止，**J02 NOT_STARTED**，不自动续包、推送、发布、部署或建自动化。
- **本地 Git 安全回执：** 文档 `ae8e6b89` 因 `git commit -- paths` 意外纳入既有 dirty 文档增量，`3a57007d` 已用 focused inverse 撤销该部分，保留本包顶部回执；两文档工作树 SHA-256 纠正前后不变，六份原 dirty 恢复、索引空，净提交差异仅顶部 14 增/5 删。误提交仍在本地历史且含本地专用材料，**本分支禁止推送/发布**；修正不代表新的推送授权。J01e 本轮只改代码地图/范围图/owner matrix/台账及 HANDOFF 五个白名单文件，由独立验收方负责定向索引/提交。

## 以下为本轮过程与原计划

# 当前任务：按真实模块与责任整理本工作区（2026-09-17）

## J01a 执行意图（2026-09-17，ACTIVE）

- 最新用户已批准 J01/J3/J5 限定范围、本地切片提交和六个指定输出根的受控重置；本步仅执行 J01a 基线，不提取/迁移生产算法，不修改地图或 overlay，不自动继续 J01b/J02。执行者 Sol；owner 为 `AF.Module.Llm/Protocol` 测试准备。
- 本步写入白名单：`AnimusForge.csproj` 的 tests Compile 排除；`tests/modules/AF.Module.Llm/Protocol/{run.py,Program.cs}`；Courier runner/test 的 Newtonsoft 参数与缺失校验；本台账与 `HANDOFF.md` 的增量 hunk。可写产物仅 J5 指定测试/证据根、原 runner 本轮 `.generated`、固定 SDK 缓存及六个指定构建输出根；不动游戏、PlayerExports 源、外仓、全局配置。
- 起点为分支 `codex/af-main-refactor-continuation-20260831`、HEAD `99360142b9b4fa5ca309cadf2cf62b627b1cdda8`；六份既有 dirty 文档保护且暂存区为空。三生产文件原始 SHA-256 与 J3:88 一致；五个新目标不存在。先冻结六文档 hash、依赖来源、迁前 Compile/资源成员，再做仅本意图的本地检查点。
- 验证门槛：原 revision 与当前 before、合成协议行为和 7 个可编译且被指定断言拒绝的变异；Courier 原 8 个变异与单测；fresh Debug/Release × 1.3/1.4/Bootstrap+Stage、API/Composition/Native 既有门禁。任何来源、路径、正常断言、变异、输出根或依赖失败按 J6 停止。LIVE、旧 SAVE、真实 provider 网络独立 NOT-RUN；Stage 视为含私密副本，不上传。

### J01a 基线回执（2026-09-17，BASELINE_VERIFIED；J01b NOT_STARTED）

> J01b ACTIVE（2026-09-17）：Astra 已独立验收 J01a；沿用本地检查点 `1d7d2cbf`，本轮仅新增 `src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs`、修改 `ShoutNetwork.cs` 的 J2 清单片段/13 接线及本台账/HANDOFF 增量。不迁两个协议原文件，不改 tests/csproj/overlay/地图；先跑真实 `after --layout extracted` 与 7 变异/inverse，再作 focused commit，等独立验收后另行调度迁移。旧六份 dirty 保持未暂存；LIVE/旧 SAVE/provider 网络仍 NOT-RUN。

<a id="j01b-extraction"></a>

> **J01b 提取回执（2026-09-17，EXTRACTION_VERIFIED / J01c NOT_STARTED）：** 源码提交 `156e6836cf0ba2345791a6964acc387551ccf46a` 仅含新 policy 和 `ShoutNetwork.cs`。以 `99360142` 原源码为基准，12 块原字节（除 7 个 `private static`→`internal static` 可见性词）相等；旧宿主只删除 5 段并资格化 13 调用，逆向插回后整文件按 BOM/CRLF 归一完全相等；故障模拟的 host 调用漂移及 policy 算法漂移均拒绝。当前源码坐标（均为 `156e6836`，一基）：policy `:9` 为 `internal static class`，4 private 常量 `:11,13,33-34,36-37`；`HasEmptyResponseRetryMarker:15-31`、`IsBattleSpeechRequest:39-55`、`GetLastMessageRole:57-77`、`EnsureFinalUserTurn:79-105`、`BuildEmptyResponseRetryMessages:107-137`、`ContainsAnyIgnoreCase:139-155`（private）、`LooksLikeThinkingControlError:157-167`、`TryReadMessage:169-234`。旧宿主 13 调用位于 `ShoutNetwork.cs:260,381,408,424,450,670,751,793,800,911,1018,1253,1256`；`LogNormalizedMessageTail:251-267`、真实普通调用 `:665-905`、流调用 `:906-1365` 与其他网络/姓名/配置/统计责任留原宿主，无 wrapper/重复算法。
> 同一 Program 的实际源码 `after --layout extracted --output-name j01b_current_01` restore/build/run 退出 `0/0/0`，13 PASS；7 个 `j01b_mutant_*_01` 各 restore/build `0/0`、run `1` 且仅指定 `FAIL`，runner wrapper 各退出 `0`。日志在 `artifacts/tests/llm-protocol/j01b_current_01/` 及七个 `j01b_mutant_*_01/`，均为本地合成输入。两协议根文件原始 SHA-256 保持 J3:90；它们**尚未迁移**，Courier LINKS/overlay/地图仍旧路径，J01c/d/e 和迁后 Stage/API/Composition/Native 均 NOT-RUN。LIVE、旧 SAVE、真实 provider 网络同样 NOT-RUN；等待 Astra 独立验收，不自动续迁。

> J01c/J01d ACTIVE（2026-09-17）：Astra 已独立验收 J01b；起点 `5611ad886f0eaf27bcb9eb78e769dd4d2a6d0b0d`。本轮仅原字节迁移两份协议文件、更新 Courier LINKS/期望及 overlay Compat `host_files` 路径，按 J5/J6 比对完整 Compile/资源、文件集、协议/Courier 与双 API Stage/旧门禁；台账/HANDOFF 仅本包增量。不改策略算法、项目、测试规则、地图或其他生产文件；先留基线 hash/输出证据，再动源码。六 dirty 文档保留，索引不整份暂存。

> **J01c_BLOCKED / J01b_EXTRACTED_VERIFIED / J01_OFFLINE_NOT_COMPLETE（2026-09-17）：** 本段明确取代上方 J01c/J01d ACTIVE 与迁前 STOPPED 初报；J01b 提取仍已验证，但 J01c 未开始，J01 整包未完成。实际只读 `build_file_set()` 退出 `1`（`FileNotFoundError: AnimusForge/CustomPrompts/CustomPolicyEvaluatorPrompt.json`），日志/退出 JSON 在 `artifacts/workspace-j01-llm-protocol/after/overlay-build-file-set-before.{log,json}`。AST 固定清单有三条陈旧路径：`AnimusForge/CustomPrompts/CustomPolicyEvaluatorPrompt.json` → 已跟踪 `AnimusForge/CustomPrompts/Policy/CustomPolicyEvaluatorPrompt.json`；`AnimusForge/CustomPrompts/NpcRulerPolicyPrompt.json` → 已跟踪 `AnimusForge/CustomPrompts/Policy/NpcRulerPolicyPrompt.json`；`CustomPrompts/CustomPolicyEvaluatorPrompt.json` → 同一现存 Policy/CustomPolicyEvaluatorPrompt 候选（无独立根目录 tracked 文件，后续须确定去重/意图）。这是既有 overlay 清单未跟随 Policy 目录迁移，**不是资源丢失或未知来源**；历史相关提交 `889b1b04/4c3e8d94`。当前 J3 白名单只准改 `host_files` Compat 路径，**不准修 `runtime_assets`、不豁免或 mock `build_file_set()`**。后续须另行精确扩展 J3 修这三条路径/去重并运行实际文件集与类别前后对照，不能默认授权。两协议原文件 SHA-256 仍为 J3:90、目标不存在，未迁移、未重置六构建根、未跑 Stage/迁后门禁；只将本阻断记录写入文档索引，原六 dirty 仍保留。

- 本段为当前授权与执行状态，**取代下方批准前的 `EXECUTION_NOT_AUTHORIZED` 规划快照**；仅 J01a 完成，不声明 J01 生产提取或全仓 G0.7 完成。意图提交 `1d7d2cbf`，代码/测试切片 `26444eb0f7f6731c11a65336328455eb67c228d4`。白名单实改仅 `AnimusForge.csproj` tests Compile 排除、`tests/modules/AF.Module.Llm/Protocol/{run.py,Program.cs}`、Courier runner/test 的 Newtonsoft 参数与缺失校验；三生产文件未改，原六份 dirty 文档保留。源码基准仍为 `99360142`；12 个提取块、13 接线、原三文件 SHA-256 与 J3:88 一致。
- 协议 runner 以固定原 revision 与当前源码分别运行 before：`baseline_04`、`baseline_current_04` 均 restore/build/run 退出 `0/0/0`，13 组合成断言 PASS，12 块 hash 与 Program 指纹一致。当前 before 的 7 个变异均 restore/build `0/0`、run `1` 且命中各自指定 `FAIL`，未用编译失败冒充行为拒绝。`after` extracted/relocated/inverse 入口已实现，但生产目标尚不存在，**未执行迁后正常测试**。失效依赖/非法输出名/缺迁后目标/无效 Git ref 均先于写入退出 `2`；祖先与既有子项 reparse 的无写 mock 反例均拒绝。
- Courier `test_extraction.py` 8 OK；原 runner 正常 39 PASS，8 个原变异均编译成功并按预期运行拒绝；LegacyShout compatibility 3 OK。`python -X utf8 -B` AST 检查与 `git diff --check` 退出 `0`。测试仅用固定本机 SDK `local/dotnet/8.0.425/dotnet.exe`、其 Newtonsoft.Json.dll（SHA-256 `dd8c541806cea6ed4bfd32ba772ce18100c6b5c887f00e7e7c1ac360b5e3b9a0`），清空 NuGet sources，不访问真实 provider/玩家文本。
- 迁前 1.3/1.4 Compile 均 753、EmbeddedResource 均 7；排除前后完整 `Identity/Link/LogicalName` 集合全等，tests 不进入生产输入。1.3/1.4 各 18 个引用解析通过；`local/bannerlord-refs/1.4.7.117484/manifest.json` 的 63 DLL 与来源/目标大小及 SHA 全等。manifest `projectSha256=777b719f1cc43525613a679e7cd2f172d59f327dbef476360345cbb00997de67` 复算匹配 B0 `e3a02cc5` 项目原始字节（UTF-8 BOM，第 12 行 LF，其余 CRLF）；`99360142` 的 Git 项目内容与该版相同，当前项目新增测试排除不刷新 manifest。六输出根重置前记录元数据且无 reparse，范围未扩张；Stage 含私密 PlayerExports 副本，证据不上传。
- 原 `build_single_module.ps1 -Stage` fresh Debug/Release 各完成 1.3、1.4、Bootstrap 与 Stage，退出均 `0`，六 DLL 的 artifact/Stage SHA 逐一相等且 XML 只载 Bootstrap。API 门禁退出 `0`（4 实际 DLL、1056 元数据、3 反例与外部拒绝）；Composition 退出 `0`（42 断言、5 反例）；Native 当前/重排退出 `0`（各 41 断言），8 反例均编译成功并运行拒绝。日志与 JSON：`artifacts/workspace-j01-llm-protocol/before/`；协议生成证据：`artifacts/tests/llm-protocol/`。首次未提升沙箱的 MSBuild/Stage 因 Windows SDK 路径 Access denied 退出非零，诊断后经审批按原命令重跑成功；原失败日志保留，不充作产品回归。
- 已知原行为：逐字符 Unicode 转义流会发射 `a4F6`，虽然最终 `NormalizedText` 是 `a你\nq`；`baseline_03` 原断言失败记录保留，`baseline_04` 锁定该既有行为且正常 `hello` 分片仍验无重复。本包不修生产协议。LIVE、旧 SAVE、真实 provider 网络与迁后验证均 NOT-RUN；等待独立验收后另行调度 J01b，不自动继续。

<a id="workspace-joint-execution-plan"></a>

## 当前入口：全仓联合路线与 J01 精确执行单（2026-09-17）

**意图完成：Astra 只补本台账与 HANDOFF；`ROADMAP_READY / J01_PLAN_READY / EXECUTION_NOT_AUTHORIZED`。** 计划的静态交付已验证，不等于未来测试已通过或清理 gate 已闭合。本节接替下方“规则已修订、完整计划待补”的接续状态；原规则、P1 分类、P3 五文件映射与 P9.9 实际回执保留。J 编号是本台账的联合交付包，不另建总计划，不恢复旧 B2–B7 先全部搬目录、最后才提取职责的调度。

本轮基线：`E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，HEAD `99360142b9b4fa5ca309cadf2cf62b627b1cdda8`。原六份 dirty 文档与空暂存区保护；本轮不修改 Skill、生产 C#、游戏 Prompt/JSON、测试、项目或脚本，不构建、提交、推送、部署或派发代理。下列新文件、改动和命令均为 **Sol 后续获准后的执行规格，不是本轮完成记录**。

### J0．交付顺序、清理 gate 与解除条件

**总路线已给出；首包选择 LLM 消息协议子域，而不是再做一个纯搬目录包。** 该包同时归位已有协议实现，并从 `ShoutNetwork` 提取实际算法。没有游戏对象/存档状态迁移，适合先建立联合包证据。它不代表整个 LLM、共享 Prompt 或三渠道完成。后续各包仍要补齐自己的符号/状态/消费者表，不允许依据路线表直接批量搬迁。

| 门禁 | 当前证据 / 缺口 | Sol 的有界动作与放行条件 | 阻挡范围 |
| --- | --- | --- | --- |
| G0.1 工作区与恢复点 | Git 根/分支/HEAD 已核实；六份 dirty 文档不能整份暂存；B0/B1 有本地提交 | 复核无外来源码差异，记录六文件原始 hash、精确白名单和目标不存在；先做仅本包意图的本地检查点，确认可逆补丁。不 reset、不重写历史 | 一切新增源码包 |
| G0.2 源码/资产归属 | 现有 P1 分类和 owner matrix 只是导航；全仓逐项归属未闭合 | 用 `git ls-files -z` 的路径/大小/后缀先做元数据清单，连同 untracked/ignored 的分类数量，不批量读取玩家日志。每个 tracked 项唯一落入 source/content/tests/tools/scripts/docs/references/design 或明确 HOLD；逐包核实真实读取者，最终 UNASSIGNED 必须为零 | 大范围提取；J01 只能以列明路径的独立批准运行，不能替全仓结项 |
| G0.3 用户数据/旧SAVE | PlayerExports、日志/会话、旧存档和外仓备份未形成新的迁移授权 | 只列路径、所有者、备份责任和恢复检验。取得用户对具名数据/备份位置的批准后才复制或调整；源数据、游戏目录及其他 worktree 保持 HOLD | content/持久化迁移、实机与旧SAVE；不阻碍只读规划 |
| G0.4 来源/许可 | 1.4.7 平铺 63 DLL manifest 已有；1.3 18 项门禁有证据，但全部 fallback 引用来源/分发权未闭合 | 对每个实际解析引用记录版本/hash/本地来源与合法获取方法。原版/反编译树、DLL、ONNX、设计资源分别决定保留本地、合法获取或可分发；不得将“机器上有”当分发许可 | clean-clone、参考树/二进制清理与发布；禁止借局部构建宣布全仓可复现 |
| G0.5 缓存/产物分离 | `local/`、`artifacts/` 已排除；工具 dist、旧缓存/归档仍有历史 HOLD | 对每一类给出依赖、备份与停跟踪清单；用户批准该清单后才处理索引/物理路径。`git rm --cached` 也需授权；不清整个 `.tmp` 或 tools | 全仓 cleanup gate；不把保留项记为 DONE |
| G0.6 构建与包布局 | B0/B1 Debug/Release × 1.3/1.4/Bootstrap+Stage 已离线验证；非本包新基线 | J01 先验证原 SDK/63 manifest/18 项引用及新测试基线，再在获准的六个输出根重跑原 Stage；冻结迁前 Compile/资源/日志证据，迁后消费当次产物 | J01 生产改动与后续结构包 |
| G0.7 全仓放行 | G0.2–G0.5 尚未闭合 | 在本节逐类填写事实与明确决定，完成 clean-source 准备/包布局验证，才将 repository gate 标为 CLOSED。未批准清理时继续标 HOLD，不自行删除也不谎报 CLOSED | **所有大范围提取**；没有默认 J02→终包连续执行权 |

执行分两种授权，不混淆：① Sol 可先获准做 G0 只读核验；② 用户可明确批准 **G0 限定核验 + J01 有限路径联合包**，历史其他分类继续 HOLD。第二种是具名局部许可，不豁免来源失败、输出越界或任一 J01 验收门槛，更不闭合全仓 gate。后续大范围提取必须先关闭 G0.7；门禁工作有具体退出条件，不以“以后清理”无限延期主体拆分。

### J1．覆盖全仓的联合工作包路线

全仓目标沿用下方 P1 的 `src/content/tests/tools/scripts/docs/references/design/local/artifacts`，根项目/README/AGENTS/HANDOFF 与用户入口暂留。每包包含“真实 owner 提取 + 该 owner 的路径/资源/测试归位”，拆成可逆小步而不是两个长期独立工程。纯契约与公共 API 五文件 B1 已完成，**直接复用，不重做**。下表除 J01 外为 `ROADMAP / DETAIL_REQUIRED`；文件族是已识别线索，未声称整文件和所有消费者已审完。

| 包 / 依赖 | 真实职责与现有入口线索 | 目标 owner、状态/壳边界与该包交付 |
| --- | --- | --- |
| J01 协议；G0 限定放行 | `ShoutNetwork` 消息尾部/空回复/错误判定，`LlmApiCompat`，`LlmVisibleReplyNormalizer` | `AF.Module.Llm/Protocol`；8 方法/4 常量真正退出旧实现，协议适配及每流可见回复状态归位；详见 J2–J6 |
| J02 Foundation/宿主；G0.7 | `SubModule`、`Refactor/Modules/ModuleFrameworkRuntime`、directory、composition，Logger/Perf/Freeze/SaveRuntimeGuard 的通用部分 | `AF.Foundation.Runtime` + `AF.GameAdapter.Bannerlord/Composition`；复用既有目录/装配，不新增第二 Host；运行代际、队列/取消/诊断状态各有唯一 owner；引擎注册壳原身份保留。通用层不得吞并业务 |
| J03 Prompt 配置/检索；J02 | `AIConfigHandler` 配置版本、锁/缓存、AsyncLocal 上下文；`PromptListRetrievalService`、`IntentAnalyzer` | `AF.Module.Prompt/{Configuration,Retrieval}`；先明确配置快照、请求上下文与缓存失效 owner，再迁实现及默认资源/测试；实际 MCM/用户覆盖顺序、容量与 finally 清理不变 |
| J04 Prompt 组合；J03 | `MyBehavior.RunCourierRulePreprocessInternal:30650-30696`、`BuildShoutPromptContextForExternalInternal:30792-31562`、`PromptComposer` | `AF.Module.Prompt/Composition` + Bannerlord 捕获适配；主线程读 Hero/Clan/规则资格，后台只拿 detached 数据；不把含实时对象的 771 行整体叫“纯 Prompt”。共享正文/后处理同源，历史 PR1 纳入本包 |
| J05 Memory/Persistence；J02，Prompt 读口与 J04 协调 | `MyBehavior` 记忆/摘要/保存责任，已有 dispatcher、run owner、预算/index/digest/sealing 组件 | `AF.Module.Memory` 的记录/摘要/检索/AFEF owner 与 `AF.Persistence` 通用保存基础分开；M1/M2/M3 与现有组件复用；逐 record/字符/耗时预算，不能以每帧回调数冒充深预算。保存类型/key/迁移壳不随路径改 |
| J06 Knowledge；J03，J05 读契约 | 世界/百科知识、Lore 与 embedding 检索消费者 | `AF.Module.Knowledge`；静态知识、领域事实与 Memory 分开，不再藏在 MyBehavior；索引构建/失效/只读查询状态归 owner；ONNX 文件不随源码包搬 |
| J07 Conversation 核心/Native；J04、J05 | `Refactor/Runtime` 已有 interaction host；`ShoutBehavior.Native*` 的 admission/preparation/pending/history/completion | `AF.Module.Conversation/{Internal,Channels/Native}`；提取真实会话/运行/提交责任，不仅 partial 改目录；复用开始前取消、receipt/AFEF、generation 和 enum 投影门禁；场景对象/存档壳留 adapter |
| J08 LLM 传输/模型；J01、J02 | `ShoutNetwork:889-1590`、`Refactor/Adapters/LegacyConfiguredChatGateway`/model catalog、DuelSettings 网络配置 | `AF.Module.Llm/{Transport,Streaming,ModelCatalog}`；唯一请求、重试、超时、取消和流状态 owner；配置/匿名消息转换与实时姓名过滤分界；原 Primary replay 的旧引用/日志写入隔离在此闭合，不另造缩水请求链 |
| J09 Actions/事实提交；J04、J05、J07 | `LegacyActionTagParser`/catalog、Shout 后处理 work item、指令标签及权威执行入口 | `AF.Module.Actions` + 领域 typed 执行端口；规则资格→tag_rules→解析→唯一执行→AFEF/receipt 全链保留；计划/执行/失败/部分成功分清，不能正文成功代替动作成功 |
| J10 Scene/Courier 适配；J07、J08、J09 | `ShoutBehavior` 接力/旁听/历史；`CourierDeliveryBehavior` 生成、到达、后处理及既有 PromptPreparation | `Conversation/Channels/{Scene,Courier}`；保留两渠道自己的队列/会话/代际/提交时点，共享已提取主体。Scene pending AFEF 消费、玩家去重/距离/旁听不可纯函数化；Courier 到达提交不可改为预生成提交 |
| J11 制作组接缝；J09、J10 | `TeamModulePorts/Adapters/Services`、PolicySystem、NobleGathering、SiegeAftermathIntervention、Xihai 显式 Link | internal 契约→`AF.Contracts/Internal`，薄桥→`src/bridges`，玩法仍各 `AF.Module.*` owner。分 Policy/Gathering/Siege 三个具名子包验收；不重写数值/业务存档，不把 internal 与 public API 合并；`G:/AFMOD/GCCZ` 同步另获准确授权 |
| J12 Economy/Diplomacy/WorldMap；J09、J10 | Reward/Debt、WorldDiplomacy、地图命令等现有入口 | 各自 `AF.Module.Economy/Diplomacy/WorldMap`；以权威交易/债务/外交/地图状态为界逐域闭包，领域状态不迁进通用 Actions；相关三渠道规则和资源随包验证 |
| J13 其他领域；J02、J09，按实际接缝细化 | WorldEvents、WarStats、Social、Duel/挑衅、Settlement/俘虏/mission、UI 适配 | 各领域独立子包，不一批搬完；场景 allowlist、伤害/敌对关系、百科/军团目标等案例随影响面验证。UI 只负责适配，不成为新业务 owner |
| J14 public API 收尾；J07、J09–J11 | 已迁五个 V1 文件与对应投影/能力快照 | 默认只接原已开放能力、路径/owner 文档与契约回归；Scene/Courier 当前 NotSupported 不因整理开放。新增 SDK 能力需用户另定范围与兼容版本，不借重构扩产品 |
| J15 content/profile；随 J03–J13 owner | `AnimusForge/ModuleData`、GUI、7 项 EmbeddedResource、profile、用户配置 | 静态资源→`content/modules`/bridges/foundation/profiles，每项唯一归属；源码路径可迁，运行/安装路径与 LogicalName 默认保持。PlayerExports 分 curated/用户变更且必须先闭合 G0.3；不得全量覆盖用户配置 |
| J16 tests/tools/scripts/docs/Bootstrap；随对应 owner，最终在 J15 后收口 | 现有 tools 测试、独立工具、原一键脚本/包装入口、docs 案例/架构图、Bootstrap 项目 | 测试随 owner 迁 `tests`；工具只留 source，输出进 artifacts；文档一事实一权威入口，历史归档先修当前链接。脚本/Bootstrap 单独精确映射，**只有另获授权才迁**，根一键入口保留兼容且验证双实现单模块；不修改 CI/CD/部署语义 |
| J17 全仓结项；全部批准包 + G0.7 | 重新分类全部 tracked/untracked/ignored 和未迁过渡责任 | UNASSIGNED=0、每个源/资源/测试有 owner、混合大类剩余职责逐符号清零或写明兼容壳、无双核心/重复编译；清洁源码准备/包布局、文档/调用/反射/存档身份核验；offline/LIVE/旧SAVE 单列，不以目录整洁作业务完成证据 |

复杂包必须再按真实领域切小并沿用本表依赖，不按“大文件行数减少”评功。比如 J05 内 Memory 的写入、摘要、索引是独立切片；J11–J13 各领域分别审查，不授予跨所有玩法的批量重写权。被 HOLD 阻挡的资产不抹掉，最终验收如仍 HOLD 就明确全仓未完成。

<a id="joint-j01-execution"></a>

### J2．J01 的精确源码与 owner 闭包

以下均为 HEAD `99360142b9b4fa5ca309cadf2cf62b627b1cdda8` 的一基行号；三份生产文件工作树与该提交按 UTF-8 BOM/换行归一后相等。**纯路径移动仍校验原始字节，不用归一相等代替原字节相等。**

| 来源 / 范围 / 符号 | 目标及保留/退出规则 |
| --- | --- |
| `LlmApiCompat.cs:1-720`，`public static class LlmApiCompat` | 原字节移动到 `src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs`。URL、OpenAI/Anthropic payload、认证头、普通/流/推理文本解析全部保留；不改供应商协议或认证行为 |
| `LlmVisibleReplyNormalizer.cs:1-485`，`public static class LlmVisibleReplyNormalizer`；`StreamFilter:62-144` | 原字节移动到 `src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs`。`ReplyPropertyNames:14-25`、深度常量及每流状态随类原样保留，不改 envelope/增量语义 |
| `ShoutNetwork.cs:251-252,272-276`，4 个 continuation/retry 常量 | 必要新文件 `src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs`，`namespace AnimusForge; internal static class PrimaryChatMessagePolicy`；常量 private，原文本/标记逐字不变。本包不是调整游戏 Prompt 内容 |
| `ShoutNetwork.cs:254-270` `HasEmptyResponseRetryMarker`；`:278-294` `IsBattleSpeechRequest`；`:296-316` `GetLastMessageRole`；`:318-344` `EnsureFinalUserTurn` | 函数体原样移入上述新 owner，参数/返回/out 不改，供旧宿主调用的方法为 internal static |
| `ShoutNetwork.cs:364-394` `BuildEmptyResponseRetryMessages`；`:451-467` `ContainsAnyIgnoreCase`；`:486-496` `LooksLikeThinkingControlError`；`:602-667` `TryReadMessage` | 函数体原样移入新 owner；`ContainsAnyIgnoreCase` 仅 owner 内 private，其余 internal。原异常/空值/大小写/反射行为不改；8 个原方法和 4 常量在旧类中退出，**不保留委托回旧算法、同算法 wrapper 或第二套副本** |

接口保持既有 `IEnumerable<object>` / `List<object>` / `out string` 消息边界；这是兼容多供应商消息形状的窄适配器，不新增无消费者 DTO 层。所有旧公开类/成员、namespace、程序集名仍不变；private 方法转入 internal owner 是本包唯一计划内内部可见性调整，不扩 public ABI。

**全部直接接线点：** `ShoutNetwork.cs:355` GetLastMessageRole；`:538,674,894,1135` EnsureFinalUserTurn；`:565` IsBattleSpeechRequest；`:581` TryReadMessage；`:975,1242` LooksLikeThinkingControlError；`:1017,1477` HasEmptyResponseRetryMarker；`:1024,1480` BuildEmptyResponseRetryMessages，共 13 处。这些调用仅改为 `PrimaryChatMessagePolicy.<原方法>`，内部互调随提取自然归属新类。`LogNormalizedMessageTail:346-363`、payload 配置/统计、姓名处理、网络/重试调度和所有公开 API 留在旧宿主。发现额外 delegate/反射字符串/消费者即停，补清单后再继续，不能依赖地图的非穷尽性。

**状态与成本：** 新 policy 只有 4 常量、无实例/共享可变状态、无游戏对象、订阅、锁、队列或 I/O。输入 List 不原地修改；原匿名消息/JObject/dictionary 引用共享方式不变。Normalizer 的 `_candidate/_lastPreview/_passThrough/_emittedLength` 和 `NormalizedText` 由一个 StreamFilter 实例独占，Push/Complete/EmitNewSuffix 是其读写者；不是跨请求单例。policy 按请求构造/重试运行、parser 按响应或 chunk 运行，不引入每 Tick 扫描。现有 TryReadMessage 反射、消息线性枚举以及流缓冲的重复字符串开销保留，**不声称已优化或具备新增硬预算**；首包不增加枚举/反射次数、缓存或锁。性能修复另列证据，不夹在原样提取中。

**保留责任：** `ShoutNetwork` DEBUG override 锁/委托及 scope `24-130`、真实 send `133-159`、PlayerReferenceStreamFilter `161-217`、配置/统计/姓名与实时 Hero 读取、普通调用 `889-1128`、流调用 `1130-1590` 仍是过渡责任。取消/generation 检查、400 thinking fallback/空回复最多一次补救、SSE 回调时点均原样保留；J01 不宣称 transport 或整个类已拆完。没有存档字段、SaveableTypeDefiner 或引擎注册迁移。

### J3．路径、消费者与写入白名单

| 路径 | 后续获准的唯一改动 |
| --- | --- |
| 上表三份目标源码 + 根 `ShoutNetwork.cs` | 两个原字节 move；一个真正 owner 新文件；旧类只删 8 方法/4 常量并替换 13 个调用，不顺带格式化/usings 清理 |
| `AnimusForge.csproj:18-20` | 仅在既有 Compile Remove 中追加 `tests\**\*.cs` 并更新相邻说明，避免新测试进入实现；不改 References、EmbeddedResource、Link、版本条件/程序集 |
| `tests/modules/AF.Module.Llm/Protocol/run.py`、`Program.cs`（必要新测试文件） | J4 定义的基线/当前执行、纯协议用例、故障反例及提取 inverse；不建立第二份生产实现。生成 csproj/NuGet.Config/复制测试输入仅在 artifacts |
| `tools/CourierPostprocessOwnerRegressionTests/run.py:31,69-91` | 将 normalizer 的 LINKS 指向新路径；新增可选 `--newtonsoft` 绝对路径参数并在构建前校验文件，默认保持旧值以免破坏其他调用。其余抽取/计时 instrumentation/变异/断言原样 |
| `tools/CourierPostprocessOwnerRegressionTests/test_extraction.py:25` | 更新 LINKS 期望新路径；增加新依赖参数缺失时 fail-closed 的静态/单元核验，不削弱原断言 |
| `tools/package_policy_system_source_overlay.py:102` | 仅 `host_files` 的 Compat 读取路径换新路径。该脚本 `193-205,242` 按实际 repo-relative 路径产生 OVERLAY，保持该规则，不能继续把迁后源码映射成旧根路径。只验证文件集，**本包不执行 create_package/写 dist/发布** |
| `docs/architecture/af-framework-code-map.json`、`af-framework-code-scope.md`、`docs/animusforge-owner-matrix.md` | 增补/刷新本包符号范围/职责和未覆盖项；地图以真实源码提交更新，保留其他锚点与历史 baseline；owner matrix 不把整个 ShoutNetwork 标完成 |
| 本台账、根 `HANDOFF.md` | 逐子步 intent/证据/当前坐标/剩余责任；只暂存本包 hunk，不吸入原六文件其他变更 |

两份迁移文件原始 SHA-256：Compat `95911a1ffbb2324529e4fa1156a864e13091d3c2020555c30194f76a8b1b8a74`；Normalizer `76a660ee99846d4c4251dc00bf4af1a1ec472d7772f53d06765eefc48533e440`。旧 ShoutNetwork 原始 SHA-256 `61b14e93ea872a70798a3516868c50692cd84dbde6259077859959c6eef3745e`。实施前重新比对，不匹配不能用刷新 hash 放行。

已查明的生产符号消费者无需修改（namespace/类型不变）：Compat → `AIConfigHandler:2791,2803`、`DuelSettings:5998,6366,6402,6683,6844,6852,6955`、`ModOnboardingBehavior:1988,2970,3585`、`MyBehavior:34097,34102,34147`、`PolicySystem/Npc/PolicyLlmClient:181,206,222,272,424,437,527,850`、`Refactor/Adapters/LegacyConfiguredChatGateway:226,245,385,507,588,639,643`、`LegacyModelCatalogGateway:171,205`、`WorldDiplomacyLlmClient:133,165,223,298,304` 及 ShoutNetwork。Normalizer → `CourierDeliveryBehavior:9380,9402`、`RewardSystemBehavior.RpItemIntroduction:679`、`ShoutBehavior:9384,13876,17288,17305,18038,20317,20654,20714,20867,27561,28076,28106`；上述省略后缀者均为 `.cs`。它们是编译/行为影响面，不是扩大生产写入清单。

定向检查 root、tools、scripts、项目/配置、Refactor/src、Skill 与当前 architecture 中的两文件路径引用，现有执行型路径命中为本表 Courier 两项和 overlay 一项；历史文档不全局替换。迁前还须核对本包 8 符号的 delegate/反射引用为零或有明确消费者，并检查工作树新增文件；静态文本搜索不冒称发现所有动态反射。

### J4．测试规格、真实依赖与判定

**已核实的环境修正（仅测试，不能改生产引用）：** `.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll` 当前不存在。可使用已有固定 SDK 内的 `local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll`，FileVersion `13.0.3.27908`，SHA-256 `dd8c541806cea6ed4bfd32ba772ce18100c6b5c887f00e7e7c1ac360b5e3b9a0`；新 runner 和 Courier 显式传该路径，不下载/伪造 NuGet 缓存。存在/hash 已核实，实际测试加载仍须由 G0/J01 基线验证，不能凭文件存在报 PASS。

新 `run.py` 的确定接口：`--dotnet <existing exe>`、`--newtonsoft <existing dll>`、`--source-ref <commit>`（可省略表示工作树）、`--phase before|after`、`--layout extracted|relocated`（仅 after，默认 relocated）、`--output-name <single safe name>`、可选 `--mutation <id>`。根从脚本 ancestors 找含本项目的已核实 Git 根，不沿用旧 tools 的 parents[2]；全部写入限定 `artifacts/tests/llm-protocol/<output-name>/`，输出名仅 `[A-Za-z0-9_-]+`，拒绝空/绝对/含斜杠/`..` 输出名与 reparse/越界路径。不静默覆盖既有不同源码指纹的输出，不递归清理。使用 net8.0、显式本地 Reference、清空 packageSources，无线上 provider 请求、真实配置/玩家文本或游戏对象。

- `before` 从固定 revision 读原 Compat/Normalizer 和 ShoutNetwork，复用 `tools/ChannelCutoverBoundaryTests/run.py:25-51` 的只读 source/declaration 提取能力；签名必须唯一，4 常量按具名声明读取（包括跨行字符串），将 8 原方法放入 **测试生成物** 的同形 policy 壳。只为测试调用调整原 private 可见性，不改函数体。记录原块 hash、提取来源和任何 instrumentation。
- `after` 直接编译三份实际目标源码，不用手写“等价实现”代替；同一 Program/同一 fixture 在 before/after 执行。测试 runner 自身存在性/目录失败、编译失败都不能当行为反例成功。
- after 必须有 inverse（before 记录原块/原文件基线）：对当前 ShoutNetwork 只反替 13 个限定调用，按固定原邻接锚点插回 8 方法/4 常量；与基线整文件按 BOM/CRLF 归一后完全相等。新 owner 的方法体/常量逐项与基线相等、仅可见性/类容器变化；不得以整段转发/新算法通过 inverse。

| 用例组 | before/after 必须保持的行为；全部人工合成输入 |
| --- | --- |
| 消息读取/尾部 | null、空、JObject、dictionary、小写/大写属性匿名对象、属性 getter 抛错；最后有效 role/content、非空 user 不追加、空 user/assistant/system/空集合补 user；原 List 元素/顺序不被就地改写；普通与阵前演讲两种指令逐字保持 |
| 空回复标记 | 首个 system 合并或补 system；已有非目标消息保留；标记精确大小写、出现后可检测；尾部始终归一。调用次数限制属于旧调度，不把纯 builder 自己谎称为网络“只重试一次”证明 |
| thinking 判定 | control 关键词与拒绝关键词必须同时命中；空白、普通 400 文本、仅一个类别不能误判；大小写行为不变 |
| 请求协议/认证 | OpenAI 克隆不改输入、Anthropic system/developer 提取/相邻 role 合并/空消息兜底/max_tokens/thinking；URL 组合；官方 Anthropic、代理和普通请求头行为原样。仅构造 HttpRequestMessage，不发送；合成 key 不进请求 JSON/输出日志 |
| 响应文本 | 普通 choices、Anthropic content、Responses output、Gemini candidate、推理/finish/usage/空事件、坏 JSON/null；保留现状的聚合行为，发现现有怪异输出另记 bug，不趁提取“纠正” |
| 可见回复/流 | 纯文本、代码围栏、命名 JSON/嵌套/数组/未知 JSON/坏包、转义/Unicode/换行；分片跨 key/引号/转义边界、Complete 有/无 finalText、两个独立 filter 交错；不重复发射、不跨流混状态，NormalizeComplete 与现有流回退分别按原行为断言 |

至少 7 个**独立生成物变异**：`drop-tail`→`tail-required`；`mutate-input`→`input-list-unchanged`；`drop-marker-detection`→`retry-marker-detected`；`thinking-or`→`thinking-needs-both`；`visible-bypass`→`visible-envelope`；`stream-duplicate`→`stream-no-duplicate`；`anthropic-empty`→`anthropic-empty-user`。每个修改必须唯一命中、成功编译、由对应明确行为断言拒绝；任意异常/缺 DLL/构建错不算抓到变异。mutant 不写生产文件，不刷新基线以掩盖失败。

Courier 测试正常路径及原 8 个变异全部保留；只替换 LINKS 和测试 DLL 参数。原 `tools/PrimaryLlmGatewayReplayTests/Program.cs:20-29` 还写死 `.tmp/build_check/1.4`，且调用真实设置/Logger；**本包不直接运行或修它，不把旧回放记录当当前验收**。J08 才细化隔离及真实 HTTP/SSE 调度回放。本包依靠全部原算法差分、全旧调用者 inverse、Courier 可见解析消费者回归与实际双版本构建证明限定等价，不冒称网络/实机覆盖。

### J5．Sol 获准后的顺序、命令与产物

1. **J01a / baseline**：先复核 G0.1/G0.6、manifest/源码 hash 和白名单；本地 intent 检查点只包含本包意图。增加测试 Compile 排除、两个新测试文件及 Courier 依赖参数（此时其 LINKS 仍读根文件）；运行 before 与当前 before 模式、正常/变异基线，**并在算法提取前跑一次下述完整 Stage/既有 API 门禁，冻结 fresh before 证据**。项目排除前后完整 Compile 集合应一致；若既有 `tests` 已含生产输入，立即停止而非默默排除。
2. **J01b / extract**：只新增 policy、原类移除 8 方法/4 常量并替换 13 调用。两个既有协议文件仍在根。跑 inverse/协议 before 对比；此时 after 显式传 `--layout extracted` 使用“根两文件 + 新 policy”的固定映射，最终 after/relocated 仅用三个目标文件，不能用 glob 找碰巧同名副本。验证后一个 focused 本地切片提交；不把此步当联合包 DONE。
3. **J01c / relocate**：两个原文件直接原字节 move，更新 Courier/overlay 路径；后续成员应由基线 753 变为 **754**（只新增 policy），两次 API 都按“两路径映射 + 一新增文件”逐成员/Link 比较，无重复/遗漏；7 个 EmbeddedResource 的 Identity/LogicalName 保持。测试 Program 不得混进实现。overlay 只调用无写入的 `build_file_set()`，按该一路径映射比较迁前后文件集/类别相等，不调用 `create_package()` 或扫描输出真实秘密内容。
4. **J01d / verify**：每个子步先聚焦检查，最终按下述原 Stage 矩阵与 affected tests 完整验收；正常基线/变异识别不通过即停。保存源码 hash、diff、编译成员、资源、运行日志和真实退出码；本包新的证据根为 `artifacts/workspace-j01-llm-protocol/<before|after>/`，不得覆盖 B0/B1 历史证据。
5. **J01e / handoff**：只有结构、算法 owner、消费者接线、逆变换/行为/构建全部通过才标 `J01_OFFLINE_VERIFIED`；LIVE/旧SAVE/实际网络调度回放独立 NOT-RUN。源码切片提交后地图 sourceRevision 绑定该真实提交，更新实际行号/hash并跑 recorded 与 working-tree；地图提交不伪造源码 revision。若提交权限受限，停在 working-tree 验证，不谎报 recorded 新版本通过。

在 P9.5 定义的固定进程环境中执行，所有下列路径均以 `$root` 为本工作区绝对根；`$dotnet = $root\local\dotnet\8.0.425\dotnet.exe`。**这些新 runner 参数是待实现规格，本轮未运行。** 每条正常命令执行后检查 `$LASTEXITCODE`，非零立即停；变异另捕获编译成功证据与指定失败标识。

```powershell
$newtonsoft = Join-Path $root 'local\dotnet\8.0.425\sdk\8.0.425\Newtonsoft.Json.dll'
$protocol = Join-Path $root 'tests\modules\AF.Module.Llm\Protocol\run.py'
python -X utf8 -B $protocol --dotnet $dotnet --newtonsoft $newtonsoft --source-ref 99360142b9b4fa5ca309cadf2cf62b627b1cdda8 --phase before --output-name baseline
python -X utf8 -B $protocol --dotnet $dotnet --newtonsoft $newtonsoft --phase after --output-name current
python -X utf8 -B -m unittest discover -s tools/CourierPostprocessOwnerRegressionTests -p test_extraction.py
python -X utf8 -B -m unittest discover -s tools/LegacyShoutGatewayResultRegressionTests -p test_compatibility.py
python -X utf8 -B tools/CourierPostprocessOwnerRegressionTests/run.py --dotnet $dotnet --newtonsoft $newtonsoft --output-name j01_after
```

协议 runner 的 7 变异逐项在 after 增加 `--mutation <上表ID> --output-name mutant_<ID>`；Courier 同理逐项 `--mutation visible-protocol-bypass|raw-parser|sync-authority|prepared-main-recompose|post-budget|late-callback|same-id-recipient|owner-one-shot`（竖线不是 shell 管道）。Courier 预期失败必须匹配其现有 `EXPECTED_FAILURES:59-64`，不能弱化计时/一次性/权威边界断言。

构建调用**严格复用 P9.5:298-350 的已核实原脚本/参数**：`$root\一键编译覆盖推送\build_single_module.ps1 -ProjectRoot $root -BannerlordRoot <已核实D盘游戏根> -Bannerlord13ReferenceDir $root\_deps_auto -Bannerlord14ReferenceDir $root\local\bannerlord-refs\1.4.7.117484 -RuntimeDependencyDir <D盘模块runtime> -HarmonyCorePath <D盘Harmony DLL> -Configuration Debug -Stage`；Debug 成功才同参数跑 Release。不传 Deploy、不改脚本、不下载补依赖；参数完整值已在 P9.5 逐项固定。Debug/Release 各 1.3、1.4、Bootstrap 与 Stage 缺一不报通过。

随后按 P9.5 同 SDK 跑 `ModuleFrameworkApiTests`（传两组真实 artifact-root）、`CampaignCompositionTests`、`NativeModuleSubmissionTests` 正常/枚举重排与全部 8 原变异；目的为防止新 Compile 排除/成员迁移破坏已有 API/组合，不重做 B0 的环境下载/安装或五文件迁移。所有 Python 命令使用 `python -X utf8 -B`，不要沿用机器 cp936。

**需在执行授权中逐项包含的可写/重置位置：**
- 当前工作区的 6 个原脚本根：`bin/Debug/single_module_artifacts`、`obj/single_module/Debug`、`bin/Debug/single_module_stage/AnimusForge`、`bin/Release/single_module_artifacts`、`obj/single_module/Release`、`bin/Release/single_module_stage/AnimusForge`。每次递归重置前解析绝对路径、核对仍在工作区且不含 reparse/未知用户数据，旧 before 证据先冻结；不能扩大到父目录。
- 新的 `artifacts/tests/llm-protocol/`、`artifacts/workspace-j01-llm-protocol/`；原 API/Composition/Native/Courier runner 各自 `.generated` 下的本轮输出、固定 SDK 进程所需 `.tmp/dotnet-cli`/`.tmp/nuget-packages`（允许构建缓存生成，不允许清整个缓存树）。复核输出被 ignore，不上传。
- Stage 含 PlayerExports 副本，属于私密测试产物；此许可不是备份/覆盖源 PlayerExports 或允许发布。禁止改游戏目录、外仓、用户 SAVE、全局 Skill/配置、CI/CD、一键入口或自动化。

### J6．停止、回滚与交给 Sol 的批准句

任一源码/hash/引用来源变化、出现清单外消费者、目标已有文件、Compile/Link/资源不等价、inverse 失败、正常断言失败、变异未编译或未被正确断言拒绝、输出越界即停止当前子步并报告真实信号。**不要靠修改玩法、跳过失败测试、放宽历史守卫、下载任意 DLL 或删除缓存推进。** 新问题所需额外路径先补本台账并获得相应授权。

回滚只逆 J01 当前切片：撤销本包调用替换并插回原方法/常量，两个文件按本表反向迁回并恢复对应路径引用；新测试/owner 文件的删除必须限于本包且按批准处理，不能递归删除目录。不覆盖六份原 dirty 文档，不 restore 整仓/整份文档，不 reset/rebase。已提交切片用 focused inverse commit，失败产物保留诊断，不“清环境重试”。

**可直接用于下一步的限定批准句：**“交给 Sol，先核验 G0.1/G0.6，再执行 J01a–J01e 的有限路径联合包；允许 J3 白名单、本包所需本地切片提交、J5 指定测试生成物及六个原脚本输出根重置。其余分类 HOLD；不做 J02 后续包、数据清理、游戏写入、外仓同步、安装、推送、发布或自动化。遇到本计划停止信号即停。” 用户只要求只读复核时，Sol 只能做 G0 只读，不将这段模板当作已批准。

### J7．本轮计划交付验证

- 两文件增量检查通过：台账原 `workspace-joint-module-planning` 锚点以下、HANDOFF 原 B0/B1 历史入口以下的原始字节 hash 均与本轮开始相同；其余四份既有 dirty 文档原始字节不变。HEAD/分支/六文件 dirty 集合及空暂存区不变，无生产文件差异。
- 8 个方法声明和 13 处直接调用的一基坐标逐项命中；3 份生产文件及 csproj 与 HEAD 归一比较相等，两个既有协议文件总行数/原始 hash 已核对，5 个计划中新目标文件仍不存在。本轮没有偷偷创建 owner/测试或提前迁移。
- `git diff --check` 通过（只有 Git 的 LF/CRLF 提示）；当前 HANDOFF 的 2 个本地链接/显式锚点及新增代码围栏闭合检查通过；原代码图 recorded-revision / working-tree 均为 142 锚点 PASS，继续绑定 `64eaa7a8`，不是新源码验证记录。
- 固定 SDK/Newtonsoft 路径/hash 与 1.4 引用 manifest 存在已核实；缺失的旧 Newtonsoft 路径和旧 Primary replay 环境依赖已明确处理或排除出 J01 必需验收。未运行新测试、变异、build/Stage、真实网络、游戏或旧SAVE，未验证实际依赖加载，不把已有 B0/B1 数字移作 J01 PASS。
- **现在可交 Sol 按本单核验；实施只差用户对 J6 有限范围的明确批准，以及 Sol 按序跑过本单真实基线。** 后续各包仍须各自细化，不能因此要求先把全仓每行审完才允许获准的 J01，也不能由 J01 计划反推全仓实施许可。

## 以下为已完成的 Skill／文档规则修订记录；当前接续以上方执行计划为准

<a id="workspace-joint-module-planning"></a>

## 当前入口：联合模块工作包规划规则（2026-09-17）

| 意图 / 状态 | 本次写入范围 | 保护与验证 |
| --- | --- | --- |
| 用户批准四文件最小文档修订；`DOC_RULES_VERIFIED`，产品实施未授权 | 本台账、根 HANDOFF、仓库整理 reference、AF 框架 Skill 的两条实施边界 | 保留原六份 dirty 文档；仅做增量说明和静态检查，不构建、迁移、提交、推送、部署、安装或启动子代理 |

- 实际工作区 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，起点 HEAD `99360142b9b4fa5ca309cadf2cf62b627b1cdda8`，暂存区为空。本次“提示词调整”只指代理工作指令，不修改游戏 Prompt/JSON、生产 C#、项目或测试 runner。
- **B0 与五文件 B1 已离线验证，不重做**；源码/消费者为 `64eaa7a8`，地图绑定提交为 `99360142`。实际执行证据仍见 [P9.9 所在的 R2 执行记录](#workspace-structure-r2-execution)，不把当时准备阶段的 NOT-RUN 改写为 PASS，也不追溯扩大五文件包的结构迁移范围。
- 本节取代下方 P0/P2/P5/P6/P7 中仍将新任务限定为“先全部整理目录、职责另开 D 阶段”的调度解释；P1 目标分类、P3 已完成映射、P9 执行回执和保护规则保留。B2–B7 的文件分类/消费者线索继续作为规划输入，不是新的批量移动授权；历史职责蓝图只作参考，不恢复其中的自动执行待办。
- 用户总目标是**全仓目录整理与复杂代码真实职责拆分**，以同一模块的完整工作包交付。本任务由 Astra 编写计划、Sol 在后续明确授权后按包执行；此分工只适用于当前任务，不写成通用 Skill 的永久模型约束。当前只修订规划规则，完整全仓执行计划和首个联合源码包均未达到 `PLAN_READY`，不自动派发或实施。

### 规划规则的唯一来源与门禁

长期规则统一放在[仓库整理 reference 的联合工作包流程](../.claude/skills/animusforge-maintainer/references/repository-structure.md#joint-module-packages)；[AF 框架 Skill](../.agents/skills/af-core-framework/SKILL.md)只引用并约束框架接缝，不另建 Skill 或第二本总计划。原 `src/content/tests/tools/scripts/docs/references/design/local/artifacts` 分类不变；同一实现 DLL 内继续区分主体领域、internal 制作组接缝和 public 版本化 API，Bootstrap 双版本单模块、namespace/ABI/存档身份、三渠道语义和性能边界保持，不借整理重写制作组玩法。

清理 gate 仍是大范围提取的前置。后续计划必须逐项列明未闭合证据、分类 HOLD、受阻包和解除条件；不能把 HOLD 当 DONE，也不能只写“以后清理”而无限延期职责拆分。文档授权不释放任何生产、数据、参考树、外仓或脚本操作权限。

### Astra 补计划、Sol 执行前的必填清单

每包在本台账中完成以下闭包；代码图/owner matrix 仅作导航，未读或未验证项标记 `UNVERIFIED`，不按文件名猜归属。

| 项目 | 必须写清 |
| --- | --- |
| 实际职责 | 旧路径、一基范围、方法/类型、源码 revision、可变状态及全部读写者；普通入口、delegate、反射、保存和工具消费者不能遗漏 |
| owner 与接口 | 唯一状态/执行 owner，跨 owner typed 输入输出、线程/生命周期、失败/取消/部分成功语义；不得复制第二套核心 |
| 全局顺序 | 模块依赖与清理前置、哪些已完成组件直接复用；相关资源/测试随 owner 包归位，共享工具/脚本/参考/产物面另列依赖，不按目录顺序盲搬 |
| 旧→新映射 | 实现、资源、测试和直接消费者的具名路径；glob/Link/LogicalName、运行时/安装相对路径、用户覆盖优先级及源码守卫的历史语义 |
| 包内步骤 | 方法/状态/调用者核实 → owner/接口 → 实现提取与路径归位的独立小步 → 接线及必要兼容转发 → 验证后移除被替代实现；每步可审查、可回滚 |
| 兼容与退出 | 逐符号列明必须保留的引擎/ABI/序列化壳；partial、转发回旧业务、保留重复实现不能当成职责提取完成 |
| 验收与成本 | 已核实 runner/参数/依赖/输出范围，正常与行为故障反例；实际 job/record/字符/耗时和运行频率、缓存/分批策略；双版本/Bootstrap、存档与渠道检查按影响面列明 |
| 停止与回滚 | 未授权路径、来源不明、调用/身份不等价、反例失效或清单外修改即停；只逆本包差异，不 reset、不覆盖他人改动、不以删除数据救环境 |

完成标准：结构归位、职责提取、离线验证、LIVE/旧SAVE 分栏记录。联合包缺少必需项不能报整包 DONE；获准的纯结构子步仍可如实记录其局部完成。首包必须有可直接执行的精确清单，后续未核实包不得冒称全量确认。

### 本次文档验证

- 四文件增量检查 PASS：两份 Skill 文件仅作列明替换；台账/HANDOFF 的历史正文可逆恢复到修改前内容，P9.9 未改；其余两份既有 dirty 文档字节不变。HEAD 仍为 `99360142`，暂存区为空，无生产文件差异。
- `git diff --check`、9 处本地链接/显式锚点、`python -X utf8 -B C:/Users/PC/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/af-core-framework` 均 PASS；代码图脚本的 recorded-revision / working-tree 各 142 锚点 PASS，仍绑定源码 `64eaa7a8`，未改地图或源码。
- 人工推演通过：只读请求不落盘；文档授权不触发产品；联合包不以目录/partial 冒充完成也不绕过清理 gate；历史结构包不追溯扩权。未运行独立代理行为测试。补丁工具曾拒绝 Skill 路径，核实为工作区普通文件后，经权限审批完成两文件定向写入，未改全局 Skill。
- 本次仅文档规则验证完成；生产/生成式测试、构建、实机/旧SAVE 均 NOT-RUN。完整全仓执行计划和首个联合源码包仍待补齐，不能由本回执标为 PLAN_READY 或 DONE。

## 以下为 R2 方案及执行历史；当前授权与后续规划以上方为准

<a id="workspace-structure-astra-plan"></a>

## 历史：Astra 规划复核（当时 R2 PLAN_READY；执行未开始）

| 意图 | 范围与保护 | 验证 |
| --- | --- | --- |
| 2026-09-17，Astra；复核并完善 WORKSPACE-STRUCTURE-20260917，后续交 Sol 实施 | 只追加本台账和根 HANDOFF；保留起始六份 dirty 文档及全部生产文件；不迁移、不构建、不提交、不改 Skill、不写仓库外 | 定向源码/消费者/依赖核对、文档链接和映射一致性、只读文件哈希与 Git diff |
| 2026-09-17，Astra 第二轮；修正 Sol 只读复核发现的 B0 执行缺口 | 仍只改本台账/HANDOFF；具体化 1.4 引用平铺准备、固定本地 SDK、Native runner 修复与验证命令；不执行安装/构建/迁移 | 只读推演引用清单、官方 SDK 来源/hash、脚本实际参数和写入边界；复验文档与生产保护 |

本节替代下方 Sol 初盘中的首包目标和环境判断；原盘点及原始验证结果保留为历史证据，不作为当前执行指令。**R2 的入口为 P9：先按其明确步骤完成 B0，再按 P3/P6 做 B1；P1/P2/P5 的整体整理方向不变。P9 修正旧版 D 盘 bin 直接作 overlay 的错误，固定 SDK 准备方案，不要求 Sol 再设计解决路线。当前只授权规划，执行仍须用户明确批准 P9 所列范围。**

### P0．当前结论与适用边界

- 本轮只做规划。整体整理为 **PLANNED / NOT MIGRATED**；环境和授权是后续执行门槛，不阻断本轮文档交付。Astra 规划、Sol 后续执行，不自动派发、不启动子代理。
- 实测 Git 根、分支和 HEAD 为上文工作区、`codex/af-main-refactor-continuation-20260831`、`d92c4b3e5c63ec1b16dca5b976c123eb56016cca`。起始六份 dirty 文件为两份台账/HANDOFF、AGENTS 及三份 Skill/引用；不是本轮新改六份文件。`main` 与 `origin/main` 均为 `96a1c60f`，祖先检查通过；`0a641aab` 对象本机不存在，未查最新远端、不合并 main。
- 主标准仍是 [repository-structure](../.claude/skills/animusforge-maintainer/references/repository-structure.md) 的原 Target planes、分类、清理顺序、源码/产物隔离和文档组织；Module-oriented 小节只细化执行。历史 `f898eb4c` 的分类 HOLD 不自动解除，也不把整理永久封死。大范围职责提取必须在 repository gate 闭合之后，有限路径切片需逐包特批和基线。
- 单一 `AnimusForge.dll` 内保留 AF 主体领域、internal 制作组接缝、public 版本化子 MOD API 三层；Foundation 不吞并对话/Prompt/动作/记忆或制作组玩法。Bootstrap 仍独立且只加载一种实现。目录整理不新增 Host、manifest、程序集、namespace、存档类型/key、协议或默认路由。

### P1．完整目标树：原分类的落地草案，不创建空目录

下列是全局目标及过渡位置；仅已有文件经过 owner 核实才创建对应节点。花括号表示候选分组，不是批量移动通配授权。

```text
AnimusForge.csproj / myaimod.sln / AGENTS.md / HANDOFF.md / README*  # 根入口暂留
src/
  AF.Contracts/
    PublicApi/V1/                     # 纯公开契约；首包仅 AfApiContracts.cs
    Internal/                         # 经核实不依赖实现的跨 owner 契约
  AF.Foundation.Runtime/
    {Scheduling,Diagnostics,Lifecycle,ModuleDirectory}/
  AF.GameAdapter.Bannerlord/
    {Composition,Compatibility,Paths}/ # 游戏接入与宿主装配，不承接全部玩法
  AF.Persistence/                     # 通用分块/身份/迁移基础，不收全部领域存档
  AF.Bootstrap/                       # 既有 Bootstrap 项目，独立迁移批次
  modules/
    AF.Module.PublicApi/{V1,Internal}/ # 公开门面及 internal 投影，待确认目录名
    AF.Module.Conversation/{Internal,Channels/Native,Channels/Scene,Channels/Courier}/
    AF.Module.Prompt/                  # 组合、规则、检索，混合实现仍过渡
    AF.Module.Llm/                     # 网络/兼容/回复规范化
    AF.Module.Memory/                  # 真实 Memory owner，不等同 MyBehavior
    AF.Module.Knowledge/               # 知识/RAG/模型 provider
    AF.Module.Actions/                 # 标签解析/授权/分派；玩法仍归业务 owner
    AF.Module.{Policy,Gathering,SiegeAftermath,SceneActions}/
    AF.Module.{Economy,Diplomacy,WorldMap,WorldEvents,WarStats,Social,Duel,Settlement}/
  bridges/
    AF.Bridge.ConversationSiege/       # 有实际跨 owner 玩法才归 Bridge
content/
  foundation/                         # 经证明跨模块共享的默认资源
  modules/<module-id>/                # Prompt/GUI/业务默认数据按唯一 owner
  bridges/<bridge-id>/
  profiles/                          # 组合配置，不把玩家 profile 混进来
tests/
  {contracts,foundation,composition,persistence,compatibility,fixtures}/
  modules/<module-id>/
  bridges/<bridge-id>/
tools/                               # PlayerExportsEditor 等工具源，不含 dist
scripts/{build,deploy,package,development,verification}/
docs/{architecture,modules,operations,compatibility,cases,handoffs,reference,archive}/
references/                          # 来源/版本/hash/许可 manifest、提取验证脚本
design/                              # 有许可的源素材
local/                               # ignored：本机依赖/参考快照/私有设置/玩家资料
artifacts/                           # ignored：stage/packages/logs/tests/diagnostics/dist
```

**原标准与真实职责的待确认点**：原 `src/AF.Contracts` 只适合契约，当前 API 的 4 个文件依赖内部运行态。本计划建议利用原有 `src/modules/AF.Module.<Name>` 槽位放 `AF.Module.PublicApi`，这是接入适配 owner，不是新玩法或新 DLL；不把它解释成 Contracts 的例外。用户需确认这一物理目录名；若不认可，4 文件留 `Api/` 等待另定目标，不自动改成 Foundation 或新增顶层架构。`AfDialogueClient.cs` 自身混有公开 enum/DTO 与操作句柄：首包整文件归 PublicApi 运行适配，未来若拆纯 DTO 是独立源码重构，不能本轮顺手拆。

**过渡区不建另一棵源码树**：`MyBehavior*`、`ShoutBehavior*`、`CourierDeliveryBehavior*`、`AIConfigHandler.cs`、`SubModule.cs` 以及尚未逐项核实的文件暂留原路径，标记 `TRANSITION / owner 待按符号闭合`；大类仍运行、保存或被 Harmony/外部调用。以后逐个已独立 owner 移走，直到每个剩余文件都有明确保留理由或独立迁移包。不会把整棵根 C# 放入 `Memory` 或 `Legacy` 后宣布完成。

| 层 | 允许的依赖/消费者 | 禁止与当前过渡债务 |
| --- | --- | --- |
| Contracts | BCL、稳定 DTO/ID；主体/内部桥/public adapter 消费 | 不引用 ModuleFrameworkRuntime、CoreDialogueClient、具体 Behavior、游戏 live 对象；契约中的 public 可见性保持，不因 internal 文件夹改修饰符 |
| Foundation / Persistence 基础 | 契约、窄宿主端口；生命周期、调度/分块被领域消费 | 不拥有政策/宴会/GCCZ/Prompt 规则；领域保存仍随领域 owner，不能只按 SyncData 名字迁移 |
| GameAdapter / Composition | 游戏 API、Foundation、模块入口；组合根可以装配各 owner | 游戏适配不反向变为领域实现；装配中的现存 Team 注册属于组合依赖，不能装成无业务依赖的纯 Foundation |
| 主体领域与 Channels | 复用现有 Prompt/LLM/Action/Memory 服务；Native/Scene/Courier 保留各自时机/会话/运输语义 | 不制造三套核心，不扩大 SDK；现存跨私有实现依赖如 CoreDialogueServices → ShoutBehavior 作为过渡接缝保留，不用物理迁移伪造依赖倒置 |
| internal 制作组接口/薄桥 | TeamModulePorts → 对应模块实现；AF 侧只做资格、线程、转换和调用 | 不把 TeamModuleAdapters 混合文件硬塞一个业务模块；共管玩法 Bridge 与无状态 adapter 区分；不暴露 public DTO 作为内部反向依赖 |
| public API adapter | Contracts + 内部目录快照、CoreDialogue 服务 | 内部主体不能依赖 public API；不直接另起 LLM、重复执行动作/记忆；Scene/Courier NotSupported 保持 |
| 内容/测试/工具/脚本/参考/产物 | 内容由 owner 消费，脚本做固定安装映射；测试显式依赖源或新鲜产物 | 不让生产 glob 包含 tests/local/artifacts，不让旧 DLL/缓存伪造源码测试通过，不将私密用户文件打包 |

### P2．原标准 → 当前分组 → 目标/批次 → 门槛

全局分组覆盖原分类；**不是 22,182 文件逐项最终归属认证**。先前 owner matrix 只作导航；除 P3 精确首包外，下列组还须生成每包具名清单与消费者闭包。未匹配的根文件统一 `UNASSIGNED / 原位保留`，不得按名称猜测迁走。

| 原标准要求 / 当前组 | 建议目标与批次 | 真实责任/消费证据及门槛 |
| --- | --- | --- |
| 唯一源码树、可恢复基线 | B0 原位盘点/恢复验证；保留根项目与现有入口 | 当前 Git 已确认；起始 dirty 分开保护。历史 18 hash / 142 坐标、缺 6 产物/17日志属于此前验证，非本轮构建基线；不复制所谓干净树 |
| root C# / `Api/**` | B1 按 P3 分 Contracts 与 PublicApi adapter | 5 文件内容保持，全部编入同一实现；不能全部归 Contracts |
| `Refactor/Contracts/**` | B2 逐文件核实后 `src/AF.Contracts/Internal/` 或领域内 `Contracts/` | 名为 Contracts 不保证无实现依赖；例如动作协议/领域结果属于领域契约，禁止整目录无审查移动 |
| `Refactor/Runtime/**` | B2 调度/生命周期→Foundation；Memory/History/Persona owner→对应主体模块；DuelOutcome→Duel | 142 点代码图和范围图定位现有调用，不能把全部 Runtime 当基础设施；每包补游戏依赖、实际 caller 和测试清单 |
| `Refactor/Modules/{CoreDialogueContracts,CoreDialogueOperation,CoreDialogueClient,CoreDialogueServices}.cs` | B2 `src/modules/AF.Module.Conversation/Internal/`，文件不拆 | CoreDialogueServices:7-10 → ShoutBehavior.SubmitModuleNativeDialogue；Contracts 文件是否应上提 AF.Contracts 要先验证类型闭包，暂保留领域内 |
| `Refactor/Modules/{CampaignComposition,CampaignModelComposition,TeamModuleRegistration,TeamModuleServices}.cs` | B2 `src/AF.GameAdapter.Bannerlord/Composition/` | ModuleFrameworkRuntime.RegisterCampaign → CampaignComposition；TeamModuleServices 创建三种 adapter。组合根能知道具体模块，Foundation 不能据此承接玩法 |
| `InternalModuleDirectory.cs`、`ModuleFrameworkSnapshot.cs`、`ModuleFrameworkRuntime.cs`、`TeamModulePorts.cs`、`TeamModuleAdapters.cs` | B2 目录/快照候选 Foundation.ModuleDirectory，纯 ports 候选 Contracts.Internal；Runtime/混合 adapters 暂留原位 | Runtime 同时依赖装配；Adapters 同时接政策/宴会/GCCZ，最终按 owner 分文件需单独重构授权。移个文件不消除这些依赖 |
| `Refactor/Adapters/**` | B2 随实际领域、Channels 或具名 Bridge 逐包；未核实留原位 | Legacy* 仍被真实调用，不因名字删除；跨 owner gameplay 才放 bridges |
| `MyBehavior* / ShoutBehavior* / CourierDeliveryBehavior* / AIConfigHandler / SubModule` | B2 仅已单责 partial 可独立候选；其余过渡原位；职责提取另开 D 阶段 | My 有历史/记忆/人设/保存，Shout 混 Scene/Native/Prompt/动作，Courier 混运输/LLM/回执/保存；不在结构包续跑旧业务待办 |
| `PromptComposer / PromptListRetrievalService / IntentQueryOptimizer / LlmApiCompat / LlmRetryPrompt / LlmVisibleReplyNormalizer` 等 | B2 按 P1 Prompt / Llm；RAG/Knowledge 家族→Knowledge | 旧 owner matrix 指明职责，尚需逐文件实读确认，不能本表直接批移。Prompt 与多渠道调用关系要成套验证 |
| `SaveRuntimeGuard / CampaignSaveChunkHelper / AnimusForgeModulePaths / Logger / TraceHelper` 等 | B2 通用保存→Persistence、路径→GameAdapter.Paths、日志→Foundation.Diagnostics | shared 命名不证明通用；确认 Saveable/反射、运行路径和线程调用后才迁。领域 SyncData 不跟通用 helper 搬 |
| `PolicySystem/**`、`NobleGatheringBehavior*`、`AnimusForge.SiegeAftermathIntervention/**` | B3 分别 Policy、Gathering、SiegeAftermath；GCCZ 可复用规则仍为同一 owner | 制作组需确认各包清单；规则/数值/状态机不改。GCCZ 原项目和 AF glob 双消费，排除 bin/obj 与项目引用须闭合；外仓同步不在本授权 |
| `SiegeAiInterventionBehavior* / CastleAftermath* / Gccz* / VillageAftermath*` | B3 AF 游戏接缝随对应领域；确属跨域玩法才归 ConversationSiege 等 Bridge | 不能把所有 Bridge 后缀文件当共管 Bridge。先区分 AF adapter、GCCZ 规则、领域保存；未实读文件继续 HOLD |
| `extensions/AnimusForge.XihaiAction/src/{CoreProject,Runtime}` | B3 SceneActions，保持既有模块内部结构和 72 显式编译成员 | 主 csproj:153-158 显式 Include/Link；extension 其他源码/项目/资源不等同这 72 文件，迁前逐消费者确认 |
| `RewardSystemBehavior* / DebtPromiseQuest / Diplomacy* / WorldDiplomacy* / Vassalage* / WorldMapPartyCommandBehavior*` | B3 Economy、Diplomacy、WorldMap 等原 owner；跨域 VoteDeal 暂留 | 不改变 owner 的保存与副作用；旧矩阵仅规划导航，需实际读引用、性能与兼容门槛 |
| `WorldEvents/** / WorldMessageTimelineMenuBehavior / WarStats/** / Social* / Duel* / Settlement* / Prisoner* / MilitaryExercise*` 等领域 | B3 对应领域模块逐包；混合 Mission/业务原位过渡 | 不能全归 GameAdapter；WarStats UI 跟 WarStats owner，通用 UI 才共享。尚未逐文件确认全组 |
| `UI/**`、根 Overlay/Encyclopedia/Widgets/设置文件、`Properties/**` | B2/B3 随业务或 GameAdapter；AssemblyInfo 暂留根原路径 | 反射/Gauntlet/Harmony/生成资源需专项；项目/程序集元数据不借目录整理改身份 |
| `AnimusForge.Bootstrap/**` | B4 `src/AF.Bootstrap/`，连同项目但不混产物 | 根 sln、主 csproj 排除、build_single_module 固定项目路径及引用闭包全部更新；本包后置且脚本修改单独批准 |
| `AnimusForge/ModuleData / GUI / CustomPrompts / SubModule.xml` | B5 静态默认内容→content 对应 owner；安装布局不变，XML 暂留入口 | 7 EmbeddedResource LogicalName 保持；Stage 当前依赖原资源布局。需要 source→installed manifest 后才移。自定义可写内容不可按默认资源覆盖 |
| `AnimusForge/PlayerExports`、用户编辑的 Prompt/知识/配置 | B0 标保护；B5 仅确认的 curated 默认项可转 content；可写项候选 local | deploy 有合并/保留/回写，约 3,139 文件不能统当素材。未逐项确认 owner/备份/存档引用的全部原位 HOLD；私密 key/玩家文字不得进入示例或日志 |
| `tools/*Tests*`、其他测试 `.py/.cs/.txt` 与 `docs/fixtures` | B6 tests 各分区；可复用开发工具源留 tools | runner→tests，验证启动入口→scripts/verification；变更 ROOT parents、动态 import、source guards、fixture路径与 .generated 输出。先加正确生产排除，否则默认 glob 吃进测试 |
| `tools/PlayerExportsEditor`、其他工具项目、其 `dist/bin/obj` | B6 工具源码留 tools；分发产物→artifacts/dist | 先证明可重建/许可和用户数据不在 dist，再批准停止跟踪。不能顺手删 EXE/ZIP |
| `一键编译覆盖推送/**`、其他开发脚本 | B7 scripts 对应子目录；原入口兼容策略待确认 | 不改默认一键入口；用户可选择原入口保留包装器或继续原位例外。后者明确记结构未完全闭合，不靠解释当达标 |
| `docs/**`、根历史说明 | B6 按 architecture/modules/operations/compatibility/cases/handoffs/reference/archive | 本台账为唯一事实入口；根 HANDOFF 简短链接。活文档迁引用，冻结历史坐标保留提交语义；AGENTS/Skill 引用变更需另授权 |
| 两套原版源码、`_deps_auto`、外部 DLL、参考项目 | B0 清单/许可；B7 references 仅 manifest/提取验证工具，实体快照候选 local | 原参考树不直接搬进可发布 references；既有依赖与版本 hash 须可复现。没有许可/用户授权不移动/取消跟踪 |
| `PNG/`、根图片、设计文件 | B7 design 仅许可源，预览→artifacts | 先核实 runtime/打包是否消费，不以图片首包替代模块化 |
| `.tmp / tmp / .codex_tmp / .dotnet / .dotnet_cli / bin / obj / Phase0_Local_Archive / _DeveloperPatch`、日志/模型/压缩包 | B0 分类；B7 按用途 local 或 artifacts，逐类 HOLD | .tmp 有实际依赖，旧归档和工具包可能是唯一副本；ONNX 许可/运行需求/当前包排除规则分别决定。严禁一条清理命令删除 |
| `.agents / .claude`、Skill draft、其他未分组文件 | 当前原位保护；后续每项查用途 | 不是生产模块；本轮禁止修改 Skill，不机械移入 docs/tools。未分类残余是显式未完成项 |

### P3．首个源码结构包 B1：公开契约与 API 接入适配（精确五文件）

坐标绑定当前 `d92c4b3e` 下未改源码；详见 [现有代码图](architecture/af-framework-code-map.json)。物理路径与 C# namespace 不必同名；以下只换编译输入路径，保留文件字节、公开/内部可见性、程序集和所有 ABI。

| 旧路径 | 唯一目标路径 | 实读证据与直接消费者 |
| --- | --- | --- |
| `Api/V1/AfApiContracts.cs` | `src/AF.Contracts/PublicApi/V1/AfApiContracts.cs` | :1-116，仅 System 集合及公开 ID/状态/快照；AfApi.GetCapability、AfV1SnapshotProjection.Create、外部测试客户端消费；无内部运行 owner 引用 |
| `Api/V1/AfApi.cs` | `src/modules/AF.Module.PublicApi/V1/AfApi.cs` | :14-62；GetSnapshot:34-37 → ModuleFrameworkRuntime.CaptureSnapshot + projection；CreateDialogueClient:56-57 → CoreDialogueServices.CreateClient。不是纯契约 |
| `Api/V1/AfDialogueClient.cs` | `src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs` | :8-64；Result/Operation/Client 依赖 CoreDialogueResult/Operation/Client 并转发 SubmitNative/Cancel；CoreDialogueServices:7-10 → ShoutBehavior.SubmitModuleNativeDialogue。混合公开类型暂整文件归接入层，不拆 DTO |
| `Api/Internal/AfV1SnapshotProjection.cs` | `src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs` | :12-57；Create 消费 ModuleFrameworkSnapshot/内部状态，生成公开 DTO；AfApi.GetSnapshot 调用，SnapshotBoundaryChecks 直接测冻结/映射；不是 Foundation 捕获 owner |
| `Api/Internal/AfV1DialogueProjection.cs` | `src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs` | :7-39；CoreDialogue 枚举→V1 枚举显式映射；AfDialogueResult/Operation 调用，Native enum 重排反例验证；internal 修饰符不是“内部制作组契约”归属证据 |

**完整引用更新范围（B1 不移动这些消费者）**：

1. `tools/ModuleFrameworkApiTests/run.py:14-20,57-59,95`：SOURCES、变异目标路径、CoreOnly 排除同步。特别是 `not p.startswith("Api/")` 在迁移后会失效；应从这 5 个已确认 API 输入集合显式排除，不让 CoreOnly 悄悄编入 public 层后仍声称反向依赖测试有效。
2. `tools/ModuleFrameworkApiTests/source_boundary.py:18,32` 更新当前读取位置；`:35-36` 的历史 inverse key/`git show` 仍用旧路径。
3. `tools/NativeModuleSubmissionTests/run.py:16` 更新当前源码输入；`source_boundary.py:8-26` 同时处理 REVIEW 旧 key→当前物理路径：依赖 hash 检查、live source 读取、main 遍历都要映射。保留 `source-review.json` 的旧 key/hash/hunks/基线及 `verification.json` 历史证据，不删断言、不刷新 hash 凑 PASS。
4. `tools/CampaignCompositionTests/run.py:20-21` 更新当前 source 列表；调用 snapshot inverse 仍保持旧来源语义。两个 API/Native runner README 的当前定位同步；测试外部 Client.cs 等只消费类型名，不需改业务代码。
5. `docs/architecture/af-framework-code-map.json` **6 个锚点、5 个不同文件**（Client 文件有两项），不是旧盘点写的 5 锚点；更新 live path，提交后记录对应新 revision，保留可查旧坐标。当前 `af-framework-code-scope.md`、`af-public-api-guide-v1.md`、本台账/HANDOFF 导航同步。历史 handoff/审计/源码 inverse 的旧提交路径不全局替换。
6. 主 `AnimusForge.csproj` 默认 glob 在新位置应各编一次，B1 原则上不需改项目、资源或一键脚本；若真实求值出现遗漏/重复，停止并扩大清单再批准，不能临时复制旧文件补编译。执行前在受控源码/tools/当前 docs 中再搜旧路径及 `Api/` 前缀，区分可执行读取、历史 key、文案；本清单不是保证未来工作树没有新增引用。

### P4．环境复核与验证准备（本轮只读，没有构建）

- 游戏根 **确实存在**：`D:/steam/steamapps/common/Mount & Blade II Bannerlord`，其 `bin/Win64_Shipping_Client/TaleWorlds.CampaignSystem.dll` 存在；Library 内版本串为 `v1.4.7.117484`。这比源码差异参考 1.4.5 新，只证明发现 1.4.x 候选，不证明 API 兼容全部成立。
- `D:/steam/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge/bin/Win64_Shipping_Client` 含原脚本要求的全部六文件：`Microsoft.ML.OnnxRuntime.dll`、`onnxruntime.dll`、`onnxruntime_providers_shared.dll`、`System.Buffers.dll`、`System.Memory.dll`、`System.Runtime.CompilerServices.Unsafe.dll`。Harmony 候选为游戏根 `Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client/0Harmony.dll`，已存在。仅只读核对，不从游戏复制或修改任何文件；位数/ABI/hash/包许可还需执行前验证。
- 工作区 `AnimusForge/bin/Win64_Shipping_Client` 不存在；`.tmp/build_check/1.4` 缺两个 native ONNX DLL，不能用作完整私有 runtime。`_deps_auto` 为既有 1.3 候选，按原 `Resolve-Bannerlord13ReferenceDir` 的18项和UTF-16版本规则核实。**D盘 bin 仅满足1.4 resolver的15/18项，不能直接作 `Bannerlord14ReferenceDir`**；原脚本还将 BannerlordBinDir/NativeBinDir/SandBoxBinDir 全指向 overlay。R2 固定用 P9 从同一1.4.7安装生成的63文件本地平铺目录，禁止暗中切至旧1.4.6 `.tmp`。
- 补充只读核实：按原 resolver 提取的 `_deps_auto` 18 项必需 DLL 均存在；UTF-16 唯一版本为 `v1.3.15.110062`，D盘 Library 的 UTF-16 唯一版本为 `v1.4.7.117484`。D盘 `Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client` 有 `MCMv5.dll` 和 `Bannerlord.MBOptionScreen.v1.4.0.dll`，`Modules/Bannerlord.UIExtenderEx/bin/Win64_Shipping_Client/Bannerlord.UIExtenderEx.dll` 存在，Native/SandBox bin亦存在。尚未执行 MSBuild 引用解析或证明这些 DLL 的全部 ABI/间接依赖，不再向用户索要这些已找到的目录。
- `C:/Program Files/dotnet/packs` 中 NETCore/WindowsDesktop/AspNetCore Ref 均有 **8.0.25 与 10.0.11**，没有8.0.30，不能满足**旧SDK10.0.400**的要求。R2已定位该版本要求来自本机 SDK 的 `Microsoft.NETCoreSdk.BundledVersions.props:273-278,332-375`，不是测试源码显式钉死8.0.30。按P9固定在仓库内准备官方SDK8.0.425及其8.0.31配套，再建立新基线；不手改SDK props、不把8.0.25伪称8.0.30、不降net8.0测试目标。本轮不运行restore/build。
- 新发现的 runner 阻碍：`tools/NativeModuleSubmissionTests/run.py:39-45` 硬编码 `G:/AFMOD/.dotnet-sdk/dotnet.exe` 与 DOTNET_ROOT，不能凭给 shell 设 DOTNET_EXE 解决。B0 应先单独批准最小 runner 可移植性修复，采用已有 API runner 的 `--dotnet`/environment 模式；新旧 SOURCE 验证不变，先于路径移动建立正常/enum 重排/8 个反例基线。本轮未改 runner。
- API runner 与 CampaignComposition runner 已有 `--dotnet`，分别在 `:79`、`:99`；源码守卫/代码坐标无需 .NET 构建。原脚本 `build_single_module.ps1:487-490` 会递归重置工作区 `bin/<Configuration>/single_module_artifacts` 和 `obj/single_module/<Configuration>`，`:537-541` 的 Stage 输出是 `bin/<Configuration>/single_module_stage/AnimusForge`；必须先解析这些目标并获得具体清理/覆盖授权，不能因 `-Stage` 就忽略风险。生产项目为 net472/x64，与聚焦测试 net8.0 是两套依赖门槛，8.0.30 缺包不等于生产编译已经失败。
- 具名保护清单：`docs/handoffs/2026-09-06-integrated-phase8-handoff.md`、`docs/handoffs/2026-09-06-team-brief.md` 本机存在，保持原文；历史本地专用 `docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md` 与 `.tmp/parallel-closeout-20260916/team-handoff.md` 本机不存在，不重建、不取消保护、不把缺失理解成可上传。既有 PlayerExports、自定义内容、其他 worktree、NEW-10/GCCZ 和参考快照一并保留；未来交付还须审查 outgoing 历史，本轮不发布。

### P5．批次、门禁、验收与回滚

| 批次 | 准确边界/前置 | 随包更新与验收 | 可逆退出 |
| --- | --- | --- | --- |
| B0 准备 | 不迁生产路径；按P9明确执行local排除、63引用平铺、固定本地SDK、Native runner适配；下载/输出重置须获准 | 迁移前原流程 Debug/Release 各1.3+1.4+Bootstrap、API/Native/组合聚焦基线；记录命令/源码版本/日志与DLL hash，验证local排除不改源码成员 | 只撤本包配置/runner/文档补丁，不回滚其他作者；副本/SDK删除另批，不改全局系统 |
| B1 API | 仅 P3 五源文件成批移动 + P3 具名消费者引用；用户确认 PublicApi 目录名、成批移动和必要测试/文档更新；B0 通过 | 五文件字节 hash 前后一致；编译成员按旧→新归一后集合相等且0重复/遗漏；六Stage、API元数据/独立客户端拒绝内部调用/CoreOnly、Native结果/取消/重排/反例、组合、6 API锚点与全图 | 不留源副本；对这五文件反向移动及本批引用逆补丁。已提交才做聚焦 inverse commit，不 reset；恢复后复验相同检查 |
| B2 AF 基础/主体/适配路径 | 仅 P2 中每次一个已经单责的 owner/契约组；**每包先补具名源和 caller 表**，不默认全部 B2 同时搬；混合类留原位 | 主 glob、显式 source guards、runner、地图、文档；相应契约/场景/保存身份测试 + 双线/Bootstrap。旧 namespace/行为字节保持，新增跨层依赖即停 | 该 owner 精确路径逆映射；不撤未涉及的前包；若发现必须拆方法，退出结构包另立 D 工作项 |
| B3 制作组与其他业务目录 | 一次 Policy/Gathering/GCCZ/SceneActions/其他已核实业务一个组；owner 与批量范围批准，独立项目/链接源码全确认 | 主/子项目 glob、Compile Link/排除、模块测试/组合矩阵/资源 owner；双版本；不写 G 盘。业务/保存重构不在包内 | 原组路径和项目引用一起逆向；不可把隔壁作者修改一起还原；外仓镜像另授权 |
| B4 Bootstrap | 仅已列 Bootstrap 源/项目与消费路径；构建脚本变更单独批准 | 主项目排除新Bootstrap路径，sln和原脚本指向新项目；双版本/Bootstrap选择元数据、Stage唯一模块布局 | 源/项目路径及消费者成套逆向，程序集/装载身份不动 |
| B5 content | 每次一个 owner 的**已确认静态默认资源集合**及 source→installed 映射；用户可写/PlayerExports HOLD | csproj资源/LogicalName、runtime/Stage/package引用；安装相对路径与字节清单等价、默认/覆盖优先级；双线Stage。生成安装树是产物，不是第二套源 | 文件与映射同时退回；不得用“重新部署”回滚用户数据；写入用户数据要另有备份和批准 |
| B6 tests/tools/docs | 每次一个测试域、工具或文档类别；排除/导入/读源/生成目录清单先写 | 生产glob排除 tests/artifacts/local；runner根定位/互相import/fixture/source-map；纯源检查不能依赖旧DLL，DLL测试显式依赖本次Stage。文档链接闭合 | 只逆本类别与消费者，保持历史证据不重写，产物清理另批准 |
| B7 脚本/参考/设计/本地与产物面 | 遵守原 Safe cleanup order：备份→清单→用户保护→许可→ignore设计→证明无偶然缓存依赖→逐类取消跟踪→可复现准备→小包文档/脚本/content。前面 B1-B6 是经特批的有限包，不提前宣称全局清理 gate 完成 | 入口位置决定、脚本固定相对路径、许可 manifest、依赖哈希、stage/ZIP allowlist、保留玩家数据。批量移动/取消跟踪/清理每类再确认；不做历史重写 | 本地备份可回读、index变化用精确inverse；不执行全仓恢复/清理，不用删除当回滚 |
| D 后续独立职责提取 | repository gate 闭合及用户另行授权；当前不执行 | My/Shout/Courier混合职责、线程/预算/内部反向服务/SDK分别按真实输入输出验收，不是目录包续点 | 行为切片独立审查、测试与inverse；LIVE/SAVE各自门禁 |

**每包共同门槛**：起点源码 revision + dirty 清单 + 具名旧→新映射 + 真实消费者 + 受保护数据 + 授权 + 可复现基线齐全；不准把“组名已定”当几百文件的批准清单。后续若允许提交，应先记录只含本包的 intent/checkpoint，并逐包精确暂存；本轮明确不提交，不能无差别 stage 上一作者的文档。

### P6．可直接交给 Sol 的 B0 → B1 执行单

1. 重读本节、实际 Git/dirty；保留本轮及前轮全部未提交文档。停止条件：HEAD/源文件内容变化、目标已有文件或用户数据、出现清单外写入、历史基线对象缺失。先复核差异再更新计划，不自选新树/合并 main。
2. 向用户一次展示 P3 五行目标与 P3 消费者清单，请确认 `AF.Module.PublicApi` 目录、五文件批移/引用修改；另列 B0 runner SDK 路径修复、Stage 会重置的两个输出树与 Stage 覆盖路径。用户只确认目录不等于授权清理/安装/部署。参考树、PlayerExports、G 盘、全局安装不包含其中。
3. **先准备验证，不先移动**。严格按P9.1–P9.4准备local排除、1.4.7平铺引用、SDK8.0.425与Native runner；随后P9.5运行原脚本/聚焦测试建立基线。缺依赖/NU1100/原脚本预检失败→B0未放行，不改生产代码救基线。
4. 命令选择：源码坐标用 `python -B .agents/skills/af-core-framework/scripts/verify_code_map.py`，加 `--working-tree` 检当前；源逆变换分别 `python -B tools/ModuleFrameworkApiTests/source_boundary.py` 与 `python -B tools/NativeModuleSubmissionTests/source_boundary.py`。三个runner统一显式传P9的 `local/dotnet/8.0.425/dotnet.exe`；Native须先完成B0的参数适配，再跑普通、`--reorder-core-enums`、全部8个 `--mutate`（失败必须是预期断言，不是编译失败）。不得悄悄回退到系统SDK10或旧G盘SDK。
5. 生产基线使用P9.5的完整命令：原一键脚本 + `_deps_auto` + **新准备的1.4.7平铺引用目录** + D盘私有runtime/Harmony + 本地固定SDK，Debug/Release顺序执行。旧 `-Bannerlord14ReferenceDir <D盘游戏bin>` 参数撤销。不加 `-Deploy`，不绕过来源门禁，不改原一键脚本；原脚本会重置目录，授权前不执行。
6. API runner 的 `--artifact-root` 可重复传本次 Debug/Release `bin/<Configuration>/single_module_artifacts` 来验真实 DLL 元数据；不得指向历史缺失日志或旧 `.tmp` DLL。静态 Compile 集合用原项目求值；若仍需诊断性 SDK 属性，则单独标记诊断，正式 Stage 必须独立通过。
7. 获准且 B0 全绿后，只执行 P3 五文件移动、列明路径消费者修改；标准化 hash 可辅助，**源字节 hash 相等是首选**。不拆文件、不增删公开类型、不改 enum 数值/using/namespace、不换内外协议、不做任何热路径性能修改；本结构包新增运行频率/分配/扫描/锁成本均应为零。
8. 复核 CoreOnly 排除、Native inverse 的旧key→live路径、6锚点，跑同一组迁前/迁后测试和六Stage。Compile集合按映射比较而非只比753；新增/丢失成员、资源LogicalName差异、ABI差异、反例失效、未归因的测试失败均停止放行。无法验证记 NOT-RUN，不能称迁移已验收。
9. 更新本台账、当前范围图/代码图/指南/两个runner README及根短HANDOFF；历史审计只链接，不全局替换旧路径。若后续获准提交，绑定新revision并跑地图记录/工作树模式；否则仅 working-tree 验证且明确记录模式未迁移到新提交。回滚只限本包 inverse，不回退其他作者 dirty。最后分别报告结构迁移、责任提取、LIVE/旧SAVE，不把其中一项当另外两项完成。

### P7．决策分栏与最终完成标准

| 已确认，不再问用户的事实 | 仍需用户决定/批准 | 环境或证据缺口，不是设计决定 |
| --- | --- | --- |
| 当前根/分支/HEAD；单实现DLL三层；D盘游戏/六runtime/Harmony存在；API5文件依赖；R2的63文件引用来源已核实 | 一次确认P9的本地依赖/SDK下载解压、两项配置与runner修复、Stage具体重置及B1迁移范围 | 本地SDK8.0.425/1.4.7平铺目录尚未实际生成；间接ABI/实际MSBuild解析/六Stage及聚焦基线尚待执行 |
| 默认资源与用户可写必须分开；不可复制第二源树；当前大类混合责任仍在 | curated PlayerExports边界、备份/数据移动授权；参考许可、ONNX交付方式、逐类取消跟踪 | 部分组仅旧矩阵导航，需每包逐文件实读/消费者；历史产物/日志缺失不能补成PASS |
| 现有一键入口不能擅改；本轮不提交/推送/部署/实机/旧SAVE/自动化 | 后续脚本移入scripts时是否保留原兼容入口；GCCZ外仓镜像精确授权（本轮无） | 清理和干净源准备门槛未闭合；LIVE/旧SAVE是独立游戏验证，不因文件整理自动完成 |

整理完成必须同时满足：①所有在范围内 tracked/受保护文件有唯一类别与owner，残余 UNASSIGNED 为零；②批准的目标分层已落地，未迁过渡文件逐项列职责、消费者和处理阶段，不把有过渡的目录称全部完成；③项目glob/Link/资源LogicalName/runtime相对路径/脚本/runner/当前文档与坐标全部闭合，历史坐标仍可按原提交复现；④从合法来源可准备依赖且不靠偶然缓存，源码/产物/玩家资料分离，stage/package allowlist和双版本/Bootstrap验证可复现；⑤路径包零语义/ABI/保存身份变化。即使这些完成，业务职责提取和 LIVE/旧SAVE仍分别记未完成，不复活旧HANDOFF待办。

### P8．首轮文档验收记录（历史；R2补充见P9）

- PLAN COMPLETE（仅本轮计划文档）：只增补本台账和根 HANDOFF；定向保护集（1,056个源码/项目/runner/脚本及既有规则文件）前后 SHA-256 聚合一致，`2cd37c578c6433c7442688f794addfcba083be9365bdc85f26affd5d7e935f01`。Git 仍只有起始六份文档 dirty，暂存区无差异，无非文档 tracked 改动；未读取机密配置正文。
- 本轮已执行：`python -B .agents/skills/af-core-framework/scripts/verify_code_map.py` 与 `--working-tree` 均 PASS（142锚点）；两项 `tools/{ModuleFrameworkApiTests,NativeModuleSubmissionTests}/source_boundary.py` PASS。文档新节的本地链接/显式锚点/代码围栏、P3五组唯一映射/源路径/一基范围/目标未创建检查 PASS；`git diff --check` 通过，仅 Git 提示未来 LF→CRLF，不是格式错误。
- 检查边界：初次尝试全 tracked 源哈希遇参考树长路径 FileNotFoundError，未改或删除该树，改用上述定向保护集与 Git 非文档差异检查，不宣称全参考树哈希验收；文档脚本第一次默认 GBK 解码失败，显式 UTF-8 后检查通过。无产品修复或构建重试。
- 本轮构建/restore/安装/迁移/Stage/部署/推送/LIVE/旧SAVE均 NOT-RUN（任务明确禁止）；只读发现依赖不等于环境闭包通过。

<a id="workspace-structure-r2-execution"></a>

### P9．R2：Sol 可逐项执行的 B0 → B1 工作单

**当前执行结果见 P9.9：B0 与五文件 B1 结构切片已离线验证；以下 P9.1–P9.8 保留获准前的方案和检查语境。** 本节原先消除已知方案缺口，不把当时的 NOT-RUN 记录改写成当时已运行；实际准备、失败诊断、追加授权与完成证据均以 P9.9 为准。任何新的生产编译错误仍须先报告，禁止为了推进迁移顺手修玩法。

#### P9.1 精确写入白名单与准备次序

| 子步 | 获准后允许写入 | 不允许/门槛 |
| --- | --- | --- |
| B0a 本地依赖隔离 | `.gitignore` 追加 `/local/`；`AnimusForge.csproj:12` 的既有 DefaultItemExcludes 末尾追加 `$(MSBuildProjectDirectory)\local\**` | 不改一键脚本、目标框架、编译符号、资源LogicalName或任何生产C#。当前local不存在；该目录不是永久架构新增，正是原分类的local面 |
| B0b 1.4引用准备 | 新建 `local/bannerlord-refs/1.4.7.117484/`，写P9.2的63个DLL及 `manifest.json` | 只从当前D盘游戏读；不用`.tmp`1.4.6；无通配复制整个游戏/源树；无游戏目录写入、无发布许可推定 |
| B0c 固定SDK | 新建 `artifacts/workspace-structure-20260917/downloads/` 存官方ZIP/校验回执；解压至 `local/dotnet/8.0.425/` | 只仓库内portable安装；不运行EXE安装器/winget，不改注册表/系统PATH/C盘SDK，不新建根global.json |
| B0d 最小runner适配 | `tools/NativeModuleSubmissionTests/run.py` 与该目录 `README.md`，仅SDK参数/路径验证/使用说明 | 不改任何Host、fixture、断言、变异内容、源码守卫、net8.0或结果预期；B1才改源路径 |
| B0e 实际验证 | 原三组runner的 `.generated/**`；既有 `.tmp/dotnet-cli`、`.tmp/nuget-packages`；P9.5的六个Stage/产物/obj根；`artifacts/workspace-structure-20260917/{before,after}/` 证据 | 新建/覆盖输出需在授权中明确；不清整个`.tmp`，不修改源PlayerExports。Stage会复制含玩家资料的源模块至本地stage，所得副本视作私密，不能上传或打包发布 |
| B1 | P3五源文件路径迁移、P3列明的5个runner/source-boundary文件、2个README、当前代码图/范围图/API指南与台账/HANDOFF | B0验收全绿及P3批量移动获准。不得扩展到B2整仓移动或大类职责拆分 |

执行前先记录HEAD、完整dirty差异与目标是否存在；保护文件不暂存、不回滚。若本地目录已被别人创建，只能按manifest/hash完整一致复用，否则停止，不 `-Force` 覆盖。准备包自身应可单独审查：生产源码无变化；主项目唯一变化是local排除。先用原源成员清单比较排除前后，确保生产 Compile 集合不变，再创建SDK/overlay。后续本地检查点按仓库规则精确提交本包文件；原六份dirty文档不能无差别暂存、冒充Sol新成果，本轮Astra不提交。

#### P9.2 1.4.7引用目录：不是“只补3个DLL”

**已实读** `build_single_module.ps1:216-265` 的 resolver 与 `:424-432` 的目录覆盖；它要求一个平铺目录，并把1.4的三种目录属性全部改到此处。D盘bin缺3项是第一个失败点；仅复制resolver的18项仍会漏主项目其他引用。选择**当前同一游戏安装**作为唯一1.4来源，本地生成最小完整直接引用集合。

源根固定为 `D:/steam/steamapps/common/Mount & Blade II Bannerlord`；目标固定为工作区 `local/bannerlord-refs/1.4.7.117484`。生成算法（一次性准备命令即可，不引入新的总构建脚本）：

1. XML解析当前 `AnimusForge.csproj` 的 Reference/HintPath，精确匹配 `$(BannerlordBinDir)\<file>`、`$(NativeBinDir)\<file>`、`$(SandBoxBinDir)\<file>`，保留各自真实来源目录；当前为**57个唯一文件**。不得把全项目Reference中的私有runtime/MCM/Harmony误收进游戏overlay。
2. 与原脚本 `Resolve-Bannerlord14ReferenceDir` 的18项RequiredFiles取并集；增补当前6个通过独立属性引用的文件：`TaleWorlds.Library.dll`、`TaleWorlds.Core.dll`、`TaleWorlds.MountAndBlade.dll`、`TaleWorlds.CampaignSystem.dll`、`TaleWorlds.GauntletUI.dll`（都来自游戏bin），以及 `TaleWorlds.MountAndBlade.View.dll`（来自Native bin）。不要按整个脚本任意DLL字符串取并集。
3. 结果必须为**63个唯一目标文件名：游戏bin 52、SandBox bin 6、Native bin 5**；本轮只读逐一 `is_file` 核对为63/63。SandBox六项为 `SandBox.dll`、`SandBox.View.dll`、`SandBox.ViewModelCollection.dll`、`SandBox.GauntletUI.dll`、`SandBox.GauntletUI.AutoGenerated.0.dll`、`SandBox.GauntletUI.AutoGenerated.1.dll`；Native五项为 `TaleWorlds.MountAndBlade.View.dll`、`TaleWorlds.MountAndBlade.GauntletUI.dll`、`TaleWorlds.MountAndBlade.GauntletUI.AutoGenerated.0.dll`、`TaleWorlds.MountAndBlade.GauntletUI.AutoGenerated.1.dll`、`TaleWorlds.MountAndBlade.Platform.PC.dll`。其余52项是第1/2步导出的游戏bin清单，生成manifest后先展示再复制；不需要用户手填这些文件名。
4. 所有来源必须是游戏根内普通文件，拒绝越界/reparse point；检查basename重名且来自不同路径的冲突，不能最后一个覆盖前一个。读取Library UTF-16 BuildInfo唯一值应为 `v1.4.7.117484`；版本变化→停止更新计划，不混旧版本。
5. 首次只新建目标/文件，不修改源；复制前后分别算SHA-256，并再读取来源hash确认复制期间游戏未更新。manifest逐项记源相对游戏根路径、目标basename、size、SHA-256，另记源游戏版本、HEAD、csproj/原脚本SHA-256。任一不匹配即未验收，保留失败证据，不能自动删目录重试。
6. 输出与manifest精确63/63一致，resolver18项全在；主项目57个直接目录HintPath及6个独立属性引用全可落到此目录。私有runtime仍明确从D盘AF目录读，Harmony/MCM/UIExtender仍按原项目条件读各模块，不偷偷混入overlay。overlay只作本地构建输入，不进入Git、stage或发行包。

补充只读检查：按D盘候选解析当前主项目72个带HintPath的Include条目，源文件均存在（不是实际MSBuild求值或间接依赖验证）；`C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2/mscorlib.dll`存在。1.3继续使用 `_deps_auto` 18/18，绝不把它与新1.4 overlay互相补齐。1.3的条件MBOptionScreen.v1.3.6缺失目前只记录为条件引用未启用，**不改用1.4.0 DLL伪装1.3版**；若真实构建显示缺类型/方法，停止并报告明确编译错误，另查合法1.3组件来源。

#### P9.3 SDK方案已定：官方8.0.425 Windows x64 ZIP，仓库内隔离

根因证据：旧失败 `tools/ModuleFrameworkApiTests/.generated/current/CoreOnly/obj/project.assets.json` 记SDK10.0.400和8.0.30下载要求；API/Native/组合的project生成器只指定net8.0，没有精确8.0.30。SDK10.0.400自带props对net8.0指定8.0.30，现有8.0.25自然不能满足。**这里不是把8.0.31拿去冒充8.0.30，而是选定自带配套引用/runtime的SDK8，迁前迁后始终用同一SDK重新建立基线。** 不修改目标框架或覆盖KnownFrameworkReference版本。

- 固定SDK **8.0.425**，配套runtime **8.0.31**；官方当前安全补丁版，本轮查询日期2026-09-17。来源：[微软8.0下载页](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)、[固定Windows x64 ZIP页面](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.425-windows-x64-binaries-zip)、[dotnet/core官方发布元数据](https://raw.githubusercontent.com/dotnet/core/main/release-notes/8.0/releases.json)。本轮只查元数据，未下载二进制。
- 下载URL：`https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.425/dotnet-sdk-8.0.425-win-x64.zip`。
- 官方元数据SHA-512：`f0b6f15bf6f1a0507205c0cb102ab99e1dee875c4682c8ed94665be1d580186a06b21455e83b3a01a0ff7f4cd887b67420f2e2fe09ed985534a4cea488ae1af9`。须同时核对版本、rid=win-x64、文件名和hash；源链接不通或hash不符就停止，不换第三方镜像、不关闭TLS。
- 获准后下载至P9.1的downloads目录，校验完整SHA-512；先列ZIP成员，拒绝绝对路径、`..`、链接/越界条目，再解压到此前不存在的 `local/dotnet/8.0.425`。不用EXE安装器；依赖源码中的license文件随ZIP保留，不上传这套SDK。
- 不修改根/父级global.json：本轮核实工作区与E盘根均无该文件。仅在子进程显式选此 `dotnet.exe`，PATH/DOTNET_ROOT仅进程内修改。`--version`必须是8.0.425；`--list-sdks`不得在这套目录混入其他SDK；核实BundledVersions声明的net8 targeting pack、win-x64 apphost、shared运行时实际存在，默认应与8.0.31相配；不再借系统10.0.400寻找8.0.30。
- 在下载/运行SDK前设置本地CLI_HOME与关闭遥测/首次证书/工作负载通知，NuGet cache保持既有工作区`.tmp/nuget-packages`。三个测试runner仍采用自己的离线NuGet.Config，不添加远端源凑PASS。net472使用本机已存在的reference assemblies；若它仍请求缺失包，记录包ID/版本，停止批准外的下载。

官方说明支持目录内非管理员安装；SDK解析不等于应用目标框架变更：[SDK安装说明](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script)、[SDK选择规则](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)。本包不需要dotnet-install脚本、全局安装或变更系统设置；只使用已校验ZIP。

#### P9.4 Native runner最小补丁及单独验收

精确文件 `tools/NativeModuleSubmissionTests/run.py`：

1. 在现有argparse中加 `--dotnet`；推荐默认 `DOTNET_EXE` 环境变量，否则 `dotnet`（通过 `shutil.which` 得到绝对路径）。显式参数可传绝对exe路径；解析后验证普通文件存在，找不到时在创建`.generated`前报清楚并退出，不回退旧G盘/静默换SDK。
2. `env`中的DOTNET_ROOT改为解析出的exe父目录；保留原本地CLI_HOME/NUGET_PACKAGES/禁证书/首次启动规则，并继承调用进程的遥测禁止设置。
3. 两处 `subprocess.run` 的首参数改用同一个解析路径；原run/build参数、150秒超时、输出、return code、CS0122负向探测均保持。原八项变异、enum重排和Host/源码输入一字不动；路径迁移留给B1。
4. README补 `--dotnet` 用法；语法用 `ast.parse`（不生成pyc），`--help`应在生成目录前退出；无效路径测试应明确失败且不创建输出。随后的正常/重排/8反例和内部可见性测试才证明runner修复没有削弱验证，不能仅“参数能解析”就放行。

#### P9.5 原流程的精确调用、产物与判定

以下是**获准前制定**的固定命令模板；实际执行结果见 P9.9，不以模板本身充当运行证据。用现有PowerShell7/编码helper在一个有界子进程执行，`$env:`仅影响该进程；不修改原一键脚本。运行前先确认所有路径已解析在预期根内，且新local排除已生效。

```powershell
$ErrorActionPreference = 'Stop'
$root = 'E:\AnimusForge-refactor-continuation-20260831'
$game = 'D:\steam\steamapps\common\Mount & Blade II Bannerlord'
$sdk = Join-Path $root 'local\dotnet\8.0.425'
$dotnet = Join-Path $sdk 'dotnet.exe'
$refs14 = Join-Path $root 'local\bannerlord-refs\1.4.7.117484'
$runtime = Join-Path $game 'Modules\AnimusForge\bin\Win64_Shipping_Client'
$harmony = Join-Path $game 'Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll'
$build = Join-Path $root '一键编译覆盖推送\build_single_module.ps1'
$env:DOTNET_ROOT = $sdk
$env:PATH = "$sdk;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $root '.tmp\dotnet-cli'
$env:NUGET_PACKAGES = Join-Path $root '.tmp\nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:PYTHONDONTWRITEBYTECODE = '1'
Set-Location -LiteralPath $root
$sdkVersion = & $dotnet --version
if ($LASTEXITCODE -ne 0 -or $sdkVersion.Trim() -ne '8.0.425') { throw 'SDK identity check failed' }
if ((Get-Command dotnet -CommandType Application).Source -ne $dotnet) { throw 'Build would use a different dotnet host' }
& $build -ProjectRoot $root -BannerlordRoot $game `
  -Bannerlord13ReferenceDir (Join-Path $root '_deps_auto') `
  -Bannerlord14ReferenceDir $refs14 -RuntimeDependencyDir $runtime `
  -HarmonyCorePath $harmony -Configuration Debug -Stage
& $build -ProjectRoot $root -BannerlordRoot $game `
  -Bannerlord13ReferenceDir (Join-Path $root '_deps_auto') `
  -Bannerlord14ReferenceDir $refs14 -RuntimeDependencyDir $runtime `
  -HarmonyCorePath $harmony -Configuration Release -Stage
```

脚本本身在预检/编译失败时throw；调用方设 `$ErrorActionPreference = 'Stop'`，逐步保留stdout/stderr和退出结果，Debug失败不继续Release。正式调用不带原盘点的TargetPlatformSdkPath/TargetPlatformDisplayName诊断覆盖；若遇Windows SDK访问拒绝，记录真实错误和所选SDK，处理权限边界而非篡改项目或静默绕过。

**需用户明确准许的6个重置根**（含其内部版本/Bootstrap子目录，Debug和Release各3个）：`bin/Debug/single_module_artifacts`、`obj/single_module/Debug`、`bin/Debug/single_module_stage/AnimusForge`、`bin/Release/single_module_artifacts`、`obj/single_module/Release`、`bin/Release/single_module_stage/AnimusForge`，均在当前工作区。已有内容先清单/hash，若含未识别数据或reparse point停止；不能顺手删除其父目录。Stage失败清理也只能在原脚本受限Stage根内。每次运行后将本次日志/元数据存到before或after证据目录；before产物hash/元数据先冻结，再让迁后构建重置同输出树。测试要消费当次构建，不用旧副本过关。

源守卫/地图按P6执行；构建后的聚焦测试在同一SDK进程环境下：

```powershell
python -B tools/ModuleFrameworkApiTests/run.py --dotnet $dotnet `
  --artifact-root (Join-Path $root 'bin\Debug\single_module_artifacts') `
  --artifact-root (Join-Path $root 'bin\Release\single_module_artifacts')
if ($LASTEXITCODE -ne 0) { throw 'API baseline failed' }
python -B tools/CampaignCompositionTests/run.py --dotnet $dotnet
if ($LASTEXITCODE -ne 0) { throw 'Composition baseline failed' }
python -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet
if ($LASTEXITCODE -ne 0) { throw 'Native baseline failed' }
python -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet --reorder-core-enums
if ($LASTEXITCODE -ne 0) { throw 'Native enum-reorder baseline failed' }
```

再逐一运行同一Native runner的 `--mutate ignore-cancel|text-success|drop-receipt|replace-confirmed|replay-id|skip-generation|skip-conversation|skip-revision`（这里竖线表示8个独立值，不是shell管道）。每个必须正常完成编译、由预期运行断言失败；捕获其日志与退出码，不因预期非零直接终止整组，也不能把任意非零都算PASS。API自带3项snapshot反例/外部拒绝探测、Composition自带5项反例不得skip。所有`.generated`保留当前输出，不清整个tools。

**成员核验**：在新SDK下使用 `dotnet msbuild AnimusForge.csproj -nologo -getItem:Compile`，分别附 `-p:BannerlordApi=1.3`/`1.4`获取源码成员；只求值不等于build PASS。B0a前后按规范化路径/Link比较完整集合；local任何文件不得进入生产编译。B1前后按P3五项映射归一后比较集合相等、无重复/遗漏，不能只比753。若求值环境失败，正式构建尚可但成员证据不完整，停在VERIFY。

**B0放行单**：SDK身份和配套pack、63文件manifest/hash及18项门禁、B0a源码成员不变、两项源码inverse与142地图双模式、Debug/Release各3构建+Stage、API真实产物元数据/组合/Native与所有预期反例全部满足，才能将B0标VERIFIED。任何未运行/未通过均不能触发五文件移动。

#### P9.6 B1引用修改再收紧：历史证据不能被路径迁移破坏

- `tools/NativeModuleSubmissionTests/source_boundary.py` 保留REVIEW键为历史路径，新增仅5项的 `current_path(old_path)` 物理读取映射（其余原样）。依赖hash、`:8-24`实际文件比对和`__main__`读取都使用映射；`git show baseline:old_path`及 `REVIEW['files'].get(old_path)` 不改。`ModuleFrameworkApiTests/source_boundary.py` 传给restore的仍是旧逻辑key，而其当前read改新路径。**不是重写旧source-review.json或刷新hash**。
- API runner的5个API输入建立显式集合，CoreOnly从SOURCES排除此集合；集合包含Contracts那1个和PublicApi那4个。SOURCES/变异查找键必须同用新物理路径，替换命中数断言仍有效。内部反向依赖检查不删除，不因目录换名把public类型编进CoreOnly。
- 五个被改的执行/守卫文件为 `ModuleFrameworkApiTests/{run.py,source_boundary.py}`、`NativeModuleSubmissionTests/{run.py,source_boundary.py}`、`CampaignCompositionTests/run.py`。两份README、3份architecture文档（代码图JSON/范围图MD/API指南MD）、原台账/HANDOFF随包更新；发现其他可执行旧路径读取就暂停扩清单，不做全局替换历史档案。
- 首包只建立 `src/AF.Contracts/PublicApi/V1`、`src/modules/AF.Module.PublicApi/{V1,Internal}` 必需目录。源文件必须直接迁移，不复制再同时保留旧编译输入；内容字节hash相同，旧namespace/visibility/enum/ABI不动。P3仍为唯一五行映射。
- 迁后完整复跑P9.5，地图5文件6锚点更新；无新提交时只声称working-tree定位通过，不能伪造recorded-revision。需提交时使用本批实际revision再跑记录模式；本地提交与推送权限分别处理，不含发布。

#### P9.7 一次批准的范围、完成定义和回退

推荐交给用户确认的范围为：**按R2执行B0a–B0e，再在B0全绿后执行P3五文件B1；允许上述本地SDK官方ZIP下载/解压、63DLL从D盘只读复制到local、.gitignore/local排除与Native runner修改、测试生成物和6个指定产物根重置、具名引用文档更新；不含业务拆分、全仓迁移、游戏写入、全局安装、数据清理、外仓同步、推送或自动化。** 用户确认这一范围后无需反复询问已列明的路径/技术选择；若只准B0就停在B0报告，不自动B1。

停止信号分清：缺下载授权→尚未授权，不是技术失败；下载/hash/来源不符→准备失败；成员/源码守卫不等价→修改失败；编译或行为断言异常→基线失败；仅LIVE/旧SAVE未运行→不影响无语义首包的离线结构验收，但仍不能称游戏验收完成。具体范围以最新用户授权为准，本计划不是授权替身。

回退：B0失败时保留local/输出及错误证据，不擅删；仅逆B0a/B0d的精确改动。B1失败则五文件按P3反向迁回，恢复本包引用补丁后重复源守卫；已经提交的切片用focused inverse commit，不reset。下载/SDK/参考副本需要清理时另列明确目录批准，不能“清环境重试”。已有六份dirty文档始终保护。

R2计划交付门槛：已给固定技术路线、真实来源与目标、写入白名单、参数、反例判定和停止/回退；不存在“让Sol自行找个目录/版本再说”的已知待设计项。**实际环境准备、正式验证、B1迁移仍是Sol获准后的工作，不通过写计划冒称已完成。** 全仓B2–B7仍按P2/P5逐owner补具名清单再迁，不能把B1详细化当全部文件已核实。

#### P9.8 R2文档检查回执（2026-09-17）

- 已完成只读验证：D盘resolver缺3项、63项来源并集全部存在且无重名目标、72个带HintPath的Include条目源路径存在；读取官方SDK固定URL/SHA-512。以上是文件/来源检查，不是MSBuild解析、下载校验或编译PASS。
- 1,056文件保护集前后聚合SHA-256仍为 `2cd37c578c6433c7442688f794addfcba083be9365bdc85f26affd5d7e935f01`；生产代码、项目/脚本/runner及既有规则文件未改。仍仅原六份文档dirty，暂存区为空；本轮写入只限原台账/HANDOFF。`local`、`src`、本次artifacts目录均未创建。
- 两段计划PowerShell经PowerShell7 AST解析PASS（未执行）；首个检查helper因Windows管道编码异常失败，改为纯ASCII驱动并从UTF-8文档读取后通过，没有运行计划代码。新增本地链接/显式锚点/围栏检查PASS，`git diff --check`通过（仅LF→CRLF提示）。142地图working-tree及两项API源码inverse再次PASS，未运行生成式测试runner。
- SDK ZIP下载/解压、63DLL复制、B0配置/runner补丁、build/restore/Stage、B1迁移、提交/推送/部署/LIVE/旧SAVE均NOT-RUN。下一步只需用户按P9.7明确执行范围，再由Sol从B0a顺序实施；不再沿旧P6的错误引用目录执行。

#### P9.9 Sol 执行记录（2026-09-17；B0 VERIFIED，B1 五文件结构切片 VERIFIED）

用户已明确批准按 P9.7 执行 B0a–B0e，并仅在 B0 全绿后执行 P3 的五文件 B1。起点复核仍为 `codex/af-main-refactor-continuation-20260831` / `d92c4b3e5c63ec1b16dca5b976c123eb56016cca`，暂存区为空，原六份 dirty 文档保留；五源与五目标、local/src/artifacts 和六个 Stage/产物重置根均已检查，目标不存在。首包只按 P9.1 白名单写入，先隔离 local，再准备 63 引用与固定 SDK，修 Native runner，执行 P9.5 基线。来源、hash、成员、构建或反例异常即停；不会为迁移放宽门禁、部署或推送。B1 尚未启动，LIVE/旧SAVE 仍独立未验收。

- B0a–B0d：`/local/` 与主项目 glob 排除、63 个同一 1.4.7 来源 DLL 的本地平铺副本/逐项 SHA-256 manifest、官方 SDK 8.0.425 ZIP 的 SHA-512 核对与安全解压、Native runner 的 `--dotnet` 路径适配完成。本地检查点 `e3a02cc5` **只提交四个 B0 配置/runner 文件**，未暂存原六份 dirty 文档；SDK/引用副本/证据均为 ignored 本地输入。初始与 B0a 后及 SDK8 正式求值的 1.3/1.4 Compile 集合均为 753、0 重复且完全相同，七个 EmbeddedResource/LogicalName 两线一致。
- B0e 已通过：原脚本 Debug/Release 各自完成 1.3+1.4+Bootstrap+本地 Stage；142 代码锚点的记录/工作树模式及 API/Native 两项源码逆变换 PASS；API 聚焦测试含 CoreOnly、3 个 snapshot 反例、外部 CS0122 拒绝及本次 4 DLL 的 1056 项元数据断言 PASS。六个输出根运行前不存在，Stage 复制的 PlayerExports 只留在本地忽略产物。证据在 `artifacts/workspace-structure-20260917/before/`；没有部署游戏或上传私密 Stage。
- **B0 阻断**：`python -B tools/CampaignCompositionTests/run.py --dotnet <固定SDK>` 的源码逆变换先 PASS，但当前组合项目编译报 `Api/V1/AfApi.cs(55,19): error CS0246: The type or namespace name 'AfDialogueClient' could not be found`。`tools/CampaignCompositionTests/run.py:17-21` 显式 SOURCES 仅列 AfApi/契约/快照投影，`AfApi.CreateDialogueClient:55-56` 已依赖 Native API 类型；这不是 B1 路径迁移造成，也不是行为反例成功。按 P9.7 停止：组合的 5 个反例和 Native 正常/枚举重排/8 反例均 NOT-RUN，B0 不标 VERIFIED，五个生产源文件未移动。现有 B0d 白名单只允许修改 Native runner/README；组合 runner/fixture 的基线修复需单独明确扩大授权，不能偷塞进 B1 或修改生产代码救测试。
- 用户随后单独批准将 B0 白名单仅扩大到 `tools/CampaignCompositionTests/HostStubs.cs`：补齐组合测试未调用的 `AfDialogueClient` / `CoreDialogueServices` 编译占位，不更改生产源码、组合断言或五个变异。先复跑组合正常路径及全部反例，再继续 Native 门禁；任一失败仍按 P9.7 停止，B1 不提前开始。
- **B0 放行**：追加的测试专用桩仅补组合编译闭包，组合正常路径 42 项及 5 个行为变异均 PASS；Native 原路径与 enum 重排各 41 项 PASS，8 个独立变异均编译成功且由预期运行断言拒绝，未把编译错误/超时算反例。当前机器 Python 默认为 cp936，原 Native 文本 fixture 为 UTF-8，按原 README 的 `-X utf8 -B` 启动后通过；README 已保留此启动参数。142 点地图记录/工作树与两项 inverse 复核通过。以上连同 B0a–B0e 的前述成员、来源、六 Stage、API DLL 元数据构成 **B0_VERIFIED**；证据位于 ignored `artifacts/workspace-structure-20260917/before/`。追加修复精确提交 `aab6a5de`，未暂存六份既有 dirty 文档。此时才放行 P3 五文件 B1，放行本身不预称迁移完成。
- **B1 五文件结构切片 VERIFIED**：严格按 P3 将一份纯契约移至 `src/AF.Contracts/PublicApi/V1/`、四份门面/投影移至 `src/modules/AF.Module.PublicApi/`，五份文件旧→新原始字节 SHA-256 均相等；只改 P9.6 的五个 runner/守卫、两个 README、三个当前 architecture 文件和本台账/HANDOFF。迁前/迁后 SDK8 正式求值的 1.3/1.4 Compile 成员按五项映射完全相同，各 753 项且无重复；两线七个 EmbeddedResource 的 Identity/LogicalName 相同。迁后原脚本 Debug/Release 各 1.3+1.4+Bootstrap+本地 Stage 成功；API CoreOnly、32 快照/119 API/3 快照变异/外部 CS0122、四实际 DLL 1056 元数据，组合 42 项/5 变异，Native 正常与 enum 重排各 41 项/8 个编译后断言反例，两个源码 inverse 均通过。142 点地图记录提交和工作树模式均通过；历史 review key/hash/hunks 与 `git show` 旧路径未改。原始产物 SHA-256 迁前后不同但大小相同，原始 DLL 字节相等不是本包门禁；源字节、Compile/资源成员及实际 DLL 公开元数据按上述门禁核验。日志/产物哈希在 ignored `artifacts/workspace-structure-20260917/{before,after}/`，含 PlayerExports 的 Stage 私密副本不上传。
- 本地源码/消费者检查点 `64eaa7a819fcb5746a9b7ceda1e93a5040a8f0d8`，地图绑定提交 `99360142`；原六份 dirty 文档不整份暂存，本执行记录与根 HANDOFF 仍留在原工作区差异中。未进入 B2–B7 或任何业务职责拆分，不推送、不部署、不碰游戏或旧存档；LIVE/旧SAVE 独立 NOT-RUN，不能把该结构切片当全部框架/整体 B1 深预算完成。

## 以下为 Sol 初盘历史；首包目标、当前状态与环境判断已由 P0–P9 取代

- `WORKSPACE-STRUCTURE-20260917` **BLOCKED（盘点/方案已写，迁移未开始）**。执行者：Codex；本机 Git 根 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`，起点 `d92c4b3e`。下方 G/F 盘与旧发布状态均为历史证据；本轮最新目标是原仓库整理标准下的模块化工作区，不续跑旧 Prompt/Memory/SDK 业务待办。
- 意图/边界：核实入口、调用者、状态 owner、消费者和构建/资源/测试路径；在**本台账**记录“原标准→现状→目标位置/批次→HOLD”、首个完整结构切片的旧→新映射和迁移前验证。预计只写本台账与根 `HANDOFF.md` 的新摘要，不改生产代码、脚本、资源、默认功能、游戏目录、存档、其他 worktree 或全局 Skill，不推送/部署。上一任务已有的 `AGENTS.md`、两份 Skill 引用及 `HANDOFF.md` 未提交说明改动必须保留，不算本轮成果。
- 责任/风险：AF 主体、Bannerlord/Native/Scene/Courier 适配、同 DLL 制作组模块及内部桥、版本化子 MOD 公共 API 分层盘点；存档类型/keys、程序集、三渠道、1.3/1.4、玩家资料一律不因目录计划改变。预先核实 `.tmp`/`_deps_auto` 等隐含依赖、资源 LogicalName、Stage/包消费者和源码守卫；没有构建基线与具体批量移动授权前不移动/删除/取消跟踪。
- 验证计划：Git/项目 glob 与依赖闭包只读检查、142 点代码地图、API 源码逆变换和可运行的聚焦测试；生产/资源迁移另须原单模块 1.3/1.4/Bootstrap Stage、源码成员/资源/哈希/受影响测试。若当前机器依赖缺失，记 `NOT-RUN` 并只交可独立核验的分类和方案。

## `WORKSPACE-STRUCTURE-20260917` 盘点结果与原标准对应

下表只列已核实的物理边界与**目标批次**，不是移动授权，也不是新总计划。复用[逐文件 owner matrix](animusforge-owner-matrix.md)作导航，但其中 `d4cb1467` 的历史判断须以本机 `d92c4b3e` 的实际调用/路径复核。`src/AF.*` 是同一实现 DLL 内的逻辑目录，不新建玩法 DLL；保留根 `AnimusForge.csproj`、`myaimod.sln` 和模块装载入口，直到各自消费者有验证后的迁移方案。

| 原标准要求 | 当前核实的 owner、入口/消费者及现状 | 目标位置 / 迁移批次 | HOLD / 未解决条件 |
| --- | --- | --- | --- |
| 唯一源码树、`src/` 按真实责任 | `AnimusForge/SubModule.xml:20-22` 只加载 Bootstrap；`AnimusForge.Bootstrap/BootstrapRuntime.cs` 选实现；`SubModule.cs:20` 装配、Harmony、Tick，`Refactor/Modules/CampaignComposition.cs` 注册行为，主 `AnimusForge.csproj` SDK glob 编译同 DLL。根有 333 个 C#；`Refactor/Modules` 同时含装配、内部桥、CoreDialogue，并非单一模块。 | 先完整迁移已核实的 `Api/` 公共 V1 分区（下表）；后续分别按 `AF.Foundation.Runtime`、`AF.GameAdapter.Bannerlord`、`AF.Persistence`、`modules/AF.Module.<Owner>`、`bridges/AF.Bridge.<A><B>` 映射**具名文件**，Bootstrap 单列；保留必要根入口。 | 原仓库清理门禁、逐文件消费者/Saveable/Harmony 审核、迁移前后双 API 线 Stage。不能整搬 `Refactor/` 或强制多 DLL。 |
| AF 主体和三渠道真实归属 | `MyBehavior.cs:58` 持有历史/Memory/Persona/`SyncData`（`17800`）；`ShoutBehavior.cs:46` 混 Scene+Native/Prompt/动作/任务，`SyncData:10851`；`CourierDeliveryBehavior.cs:39` 混运输/LLM/回执/保存，`SyncData:862`；`SubModule.cs:725-786` 消费三者 Tick。`Refactor/Runtime` 已有共享请求/记忆 owner，`Refactor/Adapters` 含旧而仍活跃的 facade。 | 后续按符号拆 `AF` 主体、Native/Scene/Courier adapter、Persistence；大类原位保留为过渡入口，责任提取与结构移动分批验收。 | 这些大文件/partial **未完成**整体分工，不得整文件放进 Memory 或按目录名删除 `Legacy*`。不续跑本轮范围外的业务算法/SDK 工作。 |
| 制作组模块与内部桥 | `PolicySystem/**` 77 C# 有独立政策 owner；`NobleGatheringBehavior.cs:135` 有自有存档；`AnimusForge.SiegeAftermathIntervention/**` 184 C# 为 GCCZ 规则；`SiegeAiInterventionBehavior.cs:51` 和 `CastleAftermath*Bridge.cs` 是 AF 侧运行适配；`Refactor/Modules/TeamModuleAdapters.cs` 三个 typed internal adapter，由目录装配消费。 | 后续分别规划 `src/modules/AF.Module.Policy`、Gathering、SiegeAftermath 与具名 `src/bridges/`/GameAdapter 薄接缝；已有 owner 目录先复用，不另造空模块。 | 制作组规则/数值/存档不在本轮迁移；GCCZ 外仓镜像需单独授权，且本轮禁止仓库外写入。 |
| 独立子 MOD 公共 API | `Api/V1/AfApi.cs:14` 提供公开查询及 Native client，`Api/V1/AfDialogueClient.cs:53` 投影内部 `Refactor/Modules/CoreDialogueServices` 的实际 Native 请求；`Api/Internal` 两文件映射内部状态。Scene/Courier 仍 NotSupported。 | **首个候选结构切片**：整个 5 文件 V1 public API 分区迁至 `src/AF.Contracts/Api/`，但仍编入 `AnimusForge.dll`，不改变 namespace、ABI 或能力。具体映射如下。 | 全量编译基线和受影响 runner 恢复前不移动；本切片只清一个真实责任分区，不算整个 `src/` 已整理。`AF.Contracts` 在此含同 DLL API facade/projection，不宣称纯 DTO 或独立程序集。 |
| `content/` 资源唯一 owner | `AnimusForge/ModuleData` 41、`GUI` 76、`CustomPrompts` 30 文件；主 csproj 显式嵌入 7 个 JSON 并固定 `LogicalName`；`AnimusForgeModulePaths.cs:30-43,120` 假定模块根/`ModuleData`；deploy 从根 `AnimusForge` 复制并核对规则文件。 | 按消费方分批映射至 `content/foundation|modules|bridges|profiles`，Stage 时仍产出相同 `Modules/AnimusForge/ModuleData|GUI|CustomPrompts`。 | 先区分静态默认值、安装自定义和用户可写内容；不得仅移动磁盘文件让 runtime 找不到资源。ONNX 模型独立决定许可及发布方式；当前 ZIP 明确排除。 |
| `tests/`、`tools/`、`scripts/` | 多数测试 runner 混在 `tools/`；`tools/PlayerExportsEditor` 是工具源码。`一键编译覆盖推送/build_single_module.ps1` 是唯一已支持的双版本+Bootstrap入口，`deploy_module.ps1:685,727-825` 固定内容/玩家资料路径，`package_mod.ps1:277,678-682` 固定 ZIP 边界。 | 后续按契约/模块/组合/持久化/兼容把 runner 源迁 `tests/`，工具源码留 `tools/`，既有一键脚本经独立授权和双版本验证后才进 `scripts/`。 | 本轮不改一键、覆盖、推送、包脚本；工具测试路径守卫须逐个更新，不能让旧树与新树同时编译。 |
| `docs/`、`references/`、`design/` | `docs/architecture`、`handoffs` 已部分成型；当前代码图 142 点，历史审计绑定旧源码路径。两套原版参考树共约 1.65 万 tracked 路径，根有设计/预览 PNG。 | 按原 `docs/architecture|modules|operations|compatibility|cases|handoffs|archive` 分类并链接，参考资料以版本/hash manifest 和来源许可裁定后再迁 `references/`/忽略的 `local/`；授权源素材入 `design/`，生成预览入 `artifacts/`。 | 参考源码/第三方许可未决、历史证据不可改写；不把四张 PNG 归档算首个模块包。 |
| `local/`、`artifacts/` 与用户资料 | 22,182 tracked 文件中 3,586 个仍命中 ignore；`_deps_auto` 提供 1.3 引用，`.tmp/build_check/1.4` 是构建候选，测试还读 `.tmp/nuget-packages`；`AnimusForge/PlayerExports` 约 3,139 个用户资料文件且 deploy 执行保留/合并/回写；日志/JSONL/压缩包可能含隐私。 | 分类后才设计 ignore、local 依赖提取及 artifact 输出；先证实 clean-clone、构建/Stage/包闭包，再按获批类别停止跟踪。 | **分类 HOLD**：玩家资料、参考树/许可、依赖、日志隐私、保护交接、缓存和工具分发各自保留；`git rm --cached` 也要单独批准、备份与核验，绝不批量删除或改写历史。 |

### 迁移前验证基线（本机，不借历史 PASS）

- `git rev-parse`：上述根/分支/HEAD；起始 dirty 仅 `AGENTS.md`、`.agents/skills/af-core-framework/SKILL.md`、`.claude/skills/animusforge-maintainer/references/{framework-coordination,repository-structure}.md`、`HANDOFF.md` 五个上一任务说明文件，均保留。
- 项目成员诊断：`dotnet msbuild AnimusForge.csproj -nologo -p:TargetPlatformSdkPath=<workspace> -p:TargetPlatformDisplayName=Windows -getItem:Compile` 在 1.3 默认和 `-p:BannerlordApi=1.4` 均返回 **753 Compile、0 重复、两线成员身份差 0**。属性覆盖仅绕过本机 Windows SDK 路径读取拒绝，是**静态求值诊断，不是原构建/Stage PASS**。`AnimusForge.csproj:109-158` 排除参考树/tools/Bootstrap/extension 默认项，再显式编入 72 个 extension 源；迁移后需重比成员身份而非只比数量。
- `_deps_auto/TaleWorlds.Library.dll` 内嵌 `v1.3.15.110062`，`.tmp/build_check/1.4/TaleWorlds.Library.dll` 内嵌 `v1.4.6.115628`；但默认 `F:/SteamLibrary/.../Mount & Blade II Bannerlord` 不存在，源模块 `AnimusForge/bin/Win64_Shipping_Client` 缺私有 runtime，暂存 1.4 目录也缺 `onnxruntime.dll`、`onnxruntime_providers_shared.dll`。官方脚本 `build_single_module.ps1:462-489` 会先验证引用/私有 DLL，后面 `:491-520` 重置项目内产物再编译；未在当前不完整闭包上运行该重置脚本。需用户提供合法现有游戏根与六个私有 runtime DLL 的**完整目录**及 Harmony 路径，或在项目既有支持方式下恢复，不下载任意 DLL。
- `python -B .agents/skills/af-core-framework/scripts/verify_code_map.py` 与 `--working-tree` 均 PASS（142 锚点，记录源码 `6e419f6d`）；`python -B tools/ModuleFrameworkApiTests/source_boundary.py` PASS。完整 `python -B tools/ModuleFrameworkApiTests/run.py --dotnet 'C:\Program Files\dotnet\dotnet.exe'` **FAIL/环境阻断**：本机只有 10.0.400 SDK、缺 net8.0 `Microsoft.NETCore.App.Ref (=8.0.30)`、`Microsoft.WindowsDesktop.App.Ref` 和 `Microsoft.AspNetCore.App.Ref`，离线 NU1100；未改测试或下载 ref pack。1.3/1.4/Bootstrap Stage、实际 DLL、Stage 资源/包、实机/旧档本轮均 `NOT-RUN`，历史六产物/17最终日志在本机缺失。

### 完整首包候选：公共 V1 API 分区（**未执行移动**）

| 旧路径 | 拟议新路径 | 当前责任 |
| --- | --- | --- |
| `Api/V1/AfApi.cs` | `src/AF.Contracts/Api/V1/AfApi.cs` | `AfApi` 版本/能力查询与 Native client 工厂；只通过内部 CoreDialogue 接线。 |
| `Api/V1/AfApiContracts.cs` | `src/AF.Contracts/Api/V1/AfApiContracts.cs` | V1 公开 ID/DTO，保持成员与数值。 |
| `Api/V1/AfDialogueClient.cs` | `src/AF.Contracts/Api/V1/AfDialogueClient.cs` | 外部请求/结果/取消投影，不改 Namespace/ABI。 |
| `Api/Internal/AfV1SnapshotProjection.cs` | `src/AF.Contracts/Api/Internal/AfV1SnapshotProjection.cs` | `ModuleFrameworkRuntime.CaptureSnapshot()` 的独立只读投影。 |
| `Api/Internal/AfV1DialogueProjection.cs` | `src/AF.Contracts/Api/Internal/AfV1DialogueProjection.cs` | 内外状态显式 enum 映射，不能退回数值 cast。 |

必要引用更新而非业务改造：`tools/ModuleFrameworkApiTests/{run.py,source_boundary.py}`、`tools/NativeModuleSubmissionTests/run.py`、`tools/CampaignCompositionTests/run.py` 的**当前工作树**源路径；`docs/architecture/af-framework-code-map.json` 的 5 个 API 锚点及当前代码范围图、`docs/architecture/af-public-api-guide-v1.md`、根 `HANDOFF.md` 的导航。历史 `git show 955a6be:Api/...`、旧审计 JSON/hash/历史 HANDOFF 路径**保留原义**，不要机械全局替换。主 csproj 默认 glob 应编入新位置一次；没有嵌入资源或运行时相对路径随这五文件迁移。验收需原 1.3/1.4 双实现 + Bootstrap Stage，753 编译成员旧→新一一映射、0 缺失/重复，文件内容标准化哈希一致、V1 API 元数据/源码守卫/Native 提交/组合测试通过，代码图 working-tree 和新提交记录模式通过；不把 Build 当实机/旧档证据。回滚用针对本批的逆向提交，不 reset。

**当前门禁：BLOCKED，非 DONE。** 依赖闭包和 net8 测试 ref pack 未恢复，且这 5 个 tracked 源文件及其消费者的成批移动/引用更新仍需展示本映射后获得明确批准。原仓库清理 gate 仍未完成；即使本首包获批并验证，也只算一个公共 API 结构切片，不完成 `content/tests/tools/scripts/docs/references/design/local/artifacts` 的全仓分层、实际主体职责提取或玩法验收。下一精确动作：确认合法现有依赖目录/测试 SDK，重建迁移前官方 Stage 与聚焦测试；其后就上述路径清单单独请求批量移动批准。

## 以下为此前发布及业务实施记录；不授权本轮推送或业务续跑

# 当前发布：已推送已验证候选（2026-09-16）

- 用户明确授权“推送到GITHUB”；本轮只发布和更新回执，不继续源码改动、部署或恢复自动化。
- 执行者：根代理。发布前fresh fetch，专用远端仍10defeb4，本地21 ahead/0 behind；已普通快进推送到origin/codex/af-main-refactor-continuation-20260831，ls-remote确认`6538cc360188b660e697b72bb6ff773b8a8660c9`。
- 代码/详细HANDOFF/审计已发布；此后只追加本发布说明。核对18生产源、6产物、17有效日志、142点地图与3份保护文件hash；167个变更路径无受排除文档、生成目录或DLL/压缩包；待推送历史也没有夹带排除文件。本轮未重跑构建，复核的是此前同源六Stage证据。
- 注意：origin/main已由437925b8更新为`0a641aab7bb3f802625e7a06a8667138aaf0c3d2`（fix: consume redirected ally call-to-war proposals）。本次不合并main，现有等价验收仍对固定437925b8，不冒称已包含或回归最新main修复。
- 整体阶段8依然未完成；本次发布成功不是全项目结项。下一代码工作须先评估新增main提交与现有主体接口的关系，再按最新详细HANDOFF推进。
- 仅更新根HANDOFF、当前详细HANDOFF、唯一台账及本地简明版；保护草稿保持、无生产源码/游戏/存档/默认开关变化。

## 以下为发布前候选与历史记录

# 当前整体收尾：并行联合包OFFLINE_VERIFIED，阶段8仍ACTIVE（2026-09-16）

- 生产/测试6e419f6d；四包提交79bf1288（summary run）、67fcb3ff（Native接口）、b0a20176（fingerprint）、6e419f6d（Courier准备/失效）。双Skill与main437925b8范围不变；主体与AF桥接，政策/宴会/GCCZ玩法不扩围。
- Native新API真正复用原owner回执/动作/必要记忆，去重/取消/容量/epoch/revision与显式enum投影落地；不是全部三渠道SDK完成。三代理互审修复API排队错目标/跨原UI回合、Courier同源失效卡Started和旧标签重放；当前源失效只由当前reservation走原Failure推进。
- 最终六Stage与4DLL1056元数据、actual1.4 Courier Host、Native41/8mutants/内部enum重排、Courier252+59/9mutants、summary95/源writer238/capture116、ports308/3mutants、history852、channel132/persona169、main身份146/36通过。只采用parallel-closeout-20260916审计明确的final及邻接证据；首次无final构建是互审修正前中间候选。
- 当前仍不满足整体DONE：共享Prompt混合线程/既有AsyncLocal下游、完整Courier retry/最终commit寿命、记录/字符硬预算、Scene/Courier公开SDK和全部内部双向服务、主体大类责任拆分/功能对照、当前候选LIVE/旧SAVE均继续必交。没有把行数或partial数量当进度。
- 最新交接docs/handoffs/2026-09-16-parallel-closeout-handoff.md，142点坐标及同候选审计JSON已同步；本地简明版.tmp/parallel-closeout-20260916/team-handoff.md。未推送/部署/存档操作/默认切换/自动化恢复，用户保护文件hash不变。

## 以下为本包实施记录及历史证据

# 当前整体收尾：记忆运行 owner 贯穿全链（2026-09-16）

- 用户要求完成整体收尾，继续CORE-CLOSEOUT-FULL，起点155f1b7a/生产51844800；本次唯一写入AF-REFACTOR，保护文档不动，不推送/部署/动存档/恢复自动化。
- M2明确缺口：共享_memorySummaryProcessing布尔值无法区分同generation重置前后的运行；旧finally的延迟清理可能清掉新运行。替换为独立运行owner/lease，旧lease不能释放新运行；scope显式贯穿规划、网络wave/重试、capture、逐结果接受、错误和完成通知。普通主线程dispatch、摘要文本/排序/限流/存档责任保持。
- 此为完整摘要运行生命周期责任，不把新增owner等同于全部M1/M2/M3和B1预算完成。继续保留实际首次深复制/终步成本、Prompt/LLM混合线程、内部双向服务和外部三渠道SDK及当前候选LIVE/SAVE为必交。
- 验证：先复现同代reset/replacement旧finally与迟到响应；实际三类摘要Host/业务/源writer/重试/plan和相关旧断言保持；精确source逆变换、六Stage/API/Host回放/保存身份、两份handoff与当前地图。退出只取消旧运行授权，不假装取消不支持的正在执行网络/已经完成写入。

- 记忆运行包当前离线结果：owner 47；旧36业务场景保留＋同代替换3例，固定155f1b7a旧36绿/新增3红、当前39全绿；真实capture/parser/terminal/writer联合原85＋新增10共95场景全绿。两个全链owner故障均被新用例检出。dispatcher37、sealing88、capture116、planning24、writer238保持；旧worker写入/错误吞没等7个受影响故障已重新接到真实新调用且有效失败，纯lease worker释放不再被当作游戏线程错误。固定main保存身份146键/36behavior保持。此前六Stage是本记忆源加旧邻接源码，三路合并后仍须重跑同候选构建/API与接口回归。

## 用户批准三路并行（2026-09-16）

- Prompt/线程包：Courier 双向实际准备先在原 owner phase 捕获输入，后台保留原规则/lore处理，再由原 owner phase 校验 session/participant/generation 并组装角色/资产/消息；共享规则/lore混合函数仍单列。只改批准的四处 Courier builder/caller 和新 partial。
- 记忆性能包：度量实际 DTO clone/raw fingerprint/最终校验；原生可变字段/List 使分片后删最终原子校验不安全。先验证 UTF-16 指纹分块写入的等价和性能，仍不把 O(chars) 改进声称硬预算完成。
- 内外接口包：实际 Native 入口完整接到版本化票据/结果/取消。Scene 现入口只等启动而非全部回复，Courier 含真实运输；二者须继续接真实回执，不能把目录能力改 Available 或另起缩水管线来冒充完成。用户要求三渠道不变。
- 三代理文件所有权隔离，根统一整合 main 等价、源码证据和六Stage。只本地源码；不自动推送、部署、默认切换或恢复自动化。

- 三路互审追加验收（未放行前）：Native API 排队请求原先只绑定档代，可在A结束/B开始后错投B；已加入入口会话epoch并独立复验拒绝，正在补默认UI回合revision插队守卫。外部V1 enum映射由数值cast改显式switch，以免内部枚举重排破坏公开契约。
- Courier新文字源检查有liveness风险：活的同会话文字变化后返回null，而既有Started仍true，tick不会重启。已交原owner修复并要求真实Start/失败或重试/下一tick证据；退休/替换不能释放新请求。首次联合六Stage/API1024是互审修复前候选，仅作中间证据，必须最终重建。

- Courier修复范围最窄扩展：Start→原队列→Begin→Prepare贯穿runtime-only reservation，弱引用session不改Saveable/新队列；同活会话仅本轮reservation仍current时由原Fail owner终结源失效并推进等待。两种Start/Begin/Prepare签名传递纳入精确审查，不能只reset共用bool而误伤较新启动。

## 以下为此前候选与历史记录

# 当前补充：Courier 失败回执不得伪装成已确认无副作用（2026-09-15）

- 全范围仍ACTIVE，29448d1b的Game退役已离线验证，接续c40671ea。后续Prompt追踪已确认My770行构造、AIConfig路由资格和KnowledgeLibrary内部live/同步网络混合，不能只把外层挂Task.Run或整体搬主线程。
- 本次先修最终回执复审发现的明确问题：Courier callback已进入后抛异常或返回null，原Invoke把它映射为RejectedByValidation+NoConfirmedEffect。不能证明没有副作用；应明确NonRetryableFailure+UnknownAfterStart，保持原errorCode和已经取得的真实result。入站缺回执转译也必须保留EffectState。
- 只改最终commit失败语义，不改信使送达/业务owner、不自动重试、不提交失败记忆。沿用原实际19检查并新增部分副作用/空回执/入站清理/诊断故障的结果分类反例；精确逆变换保留29448d1b及之前的旧source证据。完成后同候选六Stage/接口/回放，更新HANDOFF，不推送/部署。

- 当前生产路径已修正：只有进入commit后异常/空回执才归UnknownAfterStart/NonRetryableFailure；未开始准入/队列拒绝不变，真实成功回执原样返回。入站缺回执转译保留EffectState，纯诊断失败不抢占结果。新增34断言（原19+15）、固定29448d1b旧红和4有效故障反例通过；既有19/16/39与内部ports308+3反例及精确源码逆变换通过。最终六Stage通过，实际1.4 DLL也直接验证了新失败分类方法；最终API119/快照32/4DLL728元数据通过；实际1.4 DLL的失败分类→真实Host回执消费链也通过，未进入旧链重试。整体仍未结项。

## 以下为上包与历史记录

# 当前连续收尾：真实 GameEnd 与待办退役（2026-09-15）

- 全范围任务仍ACTIVE；继续上一包807bc5b9之后的I1/C1生命周期，不停止于人设消费者完成。新包先绑定实际Game身份，拒绝旧GameEnd误伤新Game；结束/替换先推进generation，再分别退役主体owner及静态订阅。
- 已证实SubModule.OnGameEnd只有地图按钮移除/base调用。Native与Courier有结果等待的主线程动作在清队列后仍只能靠deadline结束；拟复用现有队列并增加待办退役登记，不新建第二队列。未claim立即结算为失效，已claim保留真实回执；GameEnd永久停止旧owner新提交，读档/Mission重置只结束旧待办。
- 只处理AF主体和AF侧接线，不清用户存档/素材或修改制作组业务。保持目录Ready=adapter已装配的现有语义，不把GameEnd改成整个模块卸载；下一Game仍走原注册路径。
- 验证实际生命周期装配/失败隔离/旧结束回调/注册和reset竞态、三渠道守卫及相邻回归，最终六Stage和元数据。GameEnd仍不能替代全部preprocess/lore/B1/SDK/实机验收。

- 执行扩展到最终副作用队列：Native action 与 Courier 最终 commit 一并登记退役；Courier 引用旧代码只有读 expired、没有原子 claim，已用真实旧声明复现重复回调会二次 commit，改为一次 claim，deadline/退役只处理未 claim，原最终会话/入站清理 owner 未复制。
- 补充 MyBehavior 退役准入竞态：退出先推进 generation 但旧 singleton 还在清理，worker 可能持新 generation 在清队列后发布新待办。实际 dispatcher + 实际退役方法对照复现，现于清理第一步关闭 owner 准入，兼顾尚未懒加载 dispatcher 的路径。
- 当前离线证据：生命周期36/12有效反例，Native/Courier退役接线15，My竞态7/1反例，Courier最终commit19/3反例，Native action91/5反例、最终记忆184、待录历史111、历史852+27、主体队列37、三渠道132、人设169、Courier历史122/后处理39/owner phase16、内部ports308/3反例和原Campaign装配42/5反例。旧Game回调/Native退役/Courier重复提交各有旧红；不把编译错误、工具超时或fixture缺失计入行为反例。
- 最终同候选六Stage/API119+快照32+4DLL728元数据、实际Courier Host回放均通过；My源writer238项也通过。最终交接只采用final日志与产物hash，先前中间失败/工具超时单独保留不计行为反例。整体CORE-CLOSEOUT-FULL-20260915仍ACTIVE，未完成规则/lore/剩余角色资产消息主线程化、B1真实规模预算、外部三渠道SDK、内部双向服务和当前候选LIVE/SAVE。

## 以下为前一联合包与历史记录

# 当前连续收尾：三渠道人设消费与信使准入（2026-09-15）

- 用户要求全范围继续直到完成，任务CORE-CLOSEOUT-FULL-20260915保持ACTIVE；本包从4140bd04/生产043b62b4继续，唯一写入AF-REFACTOR，三份保护文件不动。已完成的单次GitHub推送不自动扩展为每批发布；不恢复自动化、不覆盖游戏/操作存档。
- 本包覆盖同一人设责任的全部消费者：Native等待、Courier回信/来信等待、Scene逐候选读取/失败回退与包更新。主线程读取人物/档案/状态、后台等待生成、主线程重验并接受；Native绑定原admission，Scene绑定原Mission/session/epoch，Courier绑定session/participant/generation。
- 连带收口信使入口Session/收信人/发信人/失败替代信的主线程准入，删除无调用的Native旧等待方法。保留Native失败中止、Courier失败降级、Scene只在两个字段都空时生成与缺字段事实回退的渠道语义，不另起LLM或队列。
- 通过实际新源码与原主线程dispatcher的物理线程/异步生成/owner与目标替换/空状态/冷却/失败/退场测试，保留精确全文件逆变换和main共同语义，最后联合双版本/Bootstrap、API、内部ports、身份验证。未修的preprocess/lore/升格同伴/B1/三渠道SDK不记DONE。

- 联查扩展C1：原Courier owner phase在动作已claim后仍把取消/超时当成未执行失败，独立实际方法16断言旧4失败/新全绿；改为未claim可退役、已claim等待真实结果（不伪造回滚），档代失效仍拒绝结果。既有39项回归有1项旧断言要求已执行仍抛取消，已明确按新回执契约调整为返回真实结果且副作用恰好一次；其余断言/反例不删弱。
- 完成人设等待deadline覆盖正在await的生成，而不仅仅覆盖生成后的状态轮询；Scene等待持续核对场景/候选，失效可退出，不取消其他渠道可能仍需要的共享生成。deadline是协作式，主线程调度延迟和真实HTTP取消仍单列，不冒充强制抢占。

- 本包联合离线验证完成：三消费者/信使准入169项、10有效反例、6全文件逆变换守卫；原消费者98项65失败且三声明与main一致。Courier执行回执16项/3反例通过（旧16项4失败），既有后处理39项及8反例、历史122项、原Hero生成125项、Native准备589项/主线程132项/历史852项、渠道132项、内部ports308项/3反例、API119/快照32/4DLL700元数据、实际Courier Host回放、六Stage与main存档身份146/36全部通过。
- 移除无消费者Native旧等待/两个live名称getter，原三消费者逻辑由真实新partial接线；Shout与Courier大类合计净减259行。deadline修复覆盖pending生成任务，但不取消共享生产请求；Scene无新固定超时，只在scope/candidate失效时退出。保留preprocess/lore/角色资产提示等其他live读、升格同伴、完整GameEnd/B1/内外SDK/实机门槛，整体任务不标DONE。

## 以下为已完成请求包与历史记录；当前执行以上方为准

# 当前任务：已推送后开始全范围主体收尾（2026-09-15）

- 用户明确授权“推送GITHUB，然后开始全范围收尾”。任务 `CORE-CLOSEOUT-FULL-20260915` ACTIVE；唯一写入G:/AFMOD/AF-REFACTOR，起点10defeb4/生产af754ab6。三份保护文件不动，不部署、不操作存档、不恢复自动化。
- 发布已完成：fresh fetch确认main437925b8不变、专用远端f03557fb；6 ahead/0 behind，排除保护文件和生成物并验证源码/产物/证据后，普通快进推送到origin/codex/af-main-refactor-continuation-20260831，ls-remote核对10defeb4976f3ffa096a77e847fba254308f6aba。此回执只证明上述已验证候选发布，后续新代码另行标明本地状态。
- 执行范围沿用main矩阵与14职责计划：主体完整功能/清理、三渠道和生命周期、记忆实际预算、内部双向服务/外部三渠道SDK、最终同候选验收与交接；政策/宴会/GCCZ业务与参考资料HOLD不扩围。完整责任包实现→回归→删除替代代码→联合检查，不凭单helper PASS放行整项。
- 首个真实跨渠道缺口：正常Hero人设自动生成/编辑器重生在await前后直接访问Hero/档案；Courier/Native可从worker调用，共享入口还先读Campaign。拟将事实/配置捕获和档案提交归原MyBehavior主线程队列，网络/解析在后台；拆出生成预约/重试owner，防止同代重置后的旧finally清掉新请求和重生回包覆盖玩家中途编辑。
- 先复现正常/单字段/重生/VoiceId保持/失败冷却/重复/编辑/换档/owner替换，再实现；维持原Prompt/辅助Gateway/公开签名与保存类型。正常Hero生成、消费者状态读取与升格同伴生成要分别标覆盖，不能把前者完成冒充整个人设/全部Courier准备完成。
- 验证：真实新源码+主后台线程/原dispatcher测试，main共同语义与旧缺陷复现、严格MyBehavior整文件逆变换、已有历史/渠道/内部ports/API/六Stage/身份；核实旧默认消费者未断，不拿测试替身冒充实机。

- 首条共享请求路径已OFFLINE_VERIFIED（全范围任务仍ACTIVE）：Hero正常自动生成/原外部入口/编辑器重生在主线程捕获事实和启动异步辅助Gateway，解析后主线程提交/UI；独立NpcPersonaGenerationOwner持有预约/冷却，清理后的旧lease不能覆盖或释放新请求。重生期间文本被编辑时保留新文本并报告失败，最新VoiceId保留。大MyBehavior净减192行，旧3状态字段及旧生成体被真实替代；不是只拆partial文件。
- 同固定main四个旧执行声明精确相同，旧114断言40失败，新125断言全通过；7有效行为故障、6逆变换守卫、原B1 15守卫与dispatcher37、history852/Native27、渠道132、Courier后处理39/历史122、内部ports308/3故障、V1/API119/快照32/四DLL680元数据、实际Courier Host回放、六Stage和main身份146/36通过。
- 未覆盖仍明确：Native/Courier外围同步状态轮询、Scene准备外围读取、升格同伴人设/技能、规则/lore、GameEnd完整释放、B1真实成本、内部双向服务及三渠道SDK、实机/旧档。原Prompt文本/解析器和存档接口保持；复用MyBehavior原MemorySummary命名队列，不新增队列，但不把每帧2callback冒充单job硬预算。

- 首包生产/测试043b62b4；105点地图、docs/handoffs/2026-09-15-full-closeout-persona-handoff.md与候选审计JSON已绑定。上一候选10defeb4已推送，新候选仅本地。

- 追加自审闭环：968ca283在扩展125项断言中发现2项排队清理假成功，修复为明确失败；最终043b62b4共125断言/7有效反例、六Stage通过，原114断言40失败仅作旧红证据。只使用带final后缀的最终候选构建/元数据日志。

## 以下为历史记录；当前全范围执行以上方为准

# 当前续点：Courier 双向历史捕获边界（2026-09-15）

- 用户继续主体收尾；任务 `COURIER-HISTORY-CAPTURE-20260915` OFFLINE_VERIFIED（仅历史子责任；整体收尾ACTIVE），起点f77fe5e4/生产73774a94，唯一写入G:/AFMOD/AF-REFACTOR；main比较基线仍437925b8。外部Native/Scene/Courier全部必交，但当前版本化提交SDK未完成。
- 实际发现：reply/inbound的Prepare在Task.Run内，两个Build...RequestOnMainThread仍直接执行live历史读取；其中还混有人设和同步preprocess/lore网络，不能把整个builder搬主线程。本切片只完整迁移双向历史捕获/检索责任，明确其余准备仍待办。
- 实施：persona阶段之后，用原Courier owner phase在主线程捕获交付事实和既有MyBehavior.CaptureHistoryContextWorkForHero快照；后台执行冻结历史检索，再在原owner phase核验session/participant/generation，request builder消费明确的prepared结果（已准备空历史也不重新读取）。删除两个旧历史读取块，不新增队列/复制检索算法/公共API。
- 验证：双向实际新helper+原owner phase的物理主/后台线程、旧新输入与空值/失效/替换/故障；既有HistorySnapshot、Courier后处理、ChannelCutover、内部ports、API/六Stage/存档身份。受影响整文件parity使用精确逆变换，不刷新hash豁免额外差异。
- 保留：人设/规则/lore/其余消息构造live读取、Courier运输与资产/事实提交时机不在此包改写；不声称整个Q1或SDK完成。三份保护文件不动，自动化PAUSED，不推送/部署/操作存档。

- 本候选结果：新helper+原owner phase双向122断言/4有效行为故障，整文件逆变换4守卫；既有历史852/Native27、渠道132、Courier后处理39、内部ports308/3故障通过。两个旧同步公开Capture消费者保持签名/默认参数和主线程同步契约；初次构建遗漏参数已修复，最终六Stage与4DLL648项元数据通过，actual Courier Host replay通过。
- 同固定main存档身份146键/36行为保持。日志位于.tmp/courier-history-20260915；只使用stage-debug-final.log/stage-release.log为最终候选构建证据，初始失败单独保留。旧同步入口可能阻塞、首次历史快照为随历史规模增长的主线程复制，均未冒充性能或整个SDK验收。

- 生产/测试提交af754ab6；详细HANDOFF为docs/handoffs/2026-09-15-courier-history-capture-handoff.md，100点地图/审计JSON绑定同源码；只读API与整体收尾状态未冒充完成。

## 以下为历史记录；当前实施以上方为准

# 当前任务：对照 main 的主体收尾与双层接口（2026-09-15）

- 用户授权开始收尾：仅复现/拆净AF主体，政策/宴会/GCCZ等玩法不重构；内部契约稳定，外部子MOD明确要求Native/Scene/Courier三渠道都开放。任务 `CORE-CLOSEOUT-MAIN-20260915` ACTIVE，唯一写入G:/AFMOD/AF-REFACTOR，起点f03557fb/生产f07cb2a2。
- fresh fetch：origin/main固定437925b856fae76b4e9ee207e96ba048f35d5a67；重构远端f03557fb与本地0/0。按此main主体功能建立缺口/保留/迁移/证据表，不拿旧测试基线代替main，也不复制main已知缺陷来凑相等。
- 本切片优先C1共用请求生命周期：InteractionRequestCoordinator与main相同，直接Cancel/Dispose有旧取消回调打断新请求、停机不能遍历其余渠道、在途token提前释放的风险。先旧红复现，再将CTS所有权/取消与完成清理拆到内部lease；协调器保留原公开构造/Execute/Cancel/Dispose签名及渠道/session规则，不新建第二套调度器。
- 通过真实共享facade接线验证三渠道；契约和普通/失败/档代/重复/取消语义对照main。外部三渠道已纳入必交，但当前Api.V1只读仍是未完成状态，不在闭环前虚报Supported，不新增缩水LLM/动作/记忆链。
- Courier的background prepare仍含persona/history/preprocess/live读；这是单列待修缺口，不能整段搬主线程造成网络阻塞。本轮不冒称此缺口、B1深复制/预算或Campaign/Mission全部完成。
- 验证计划：main旧红/当前绿与共同基线、故障注入、既有pipeline/装配/API/存档身份/六Stage，稳定公开签名与只读/制作组契约；更新main功能矩阵/符号迁移与HANDOFF。三份保护文件保持，自动化PAUSED，不推送/部署/改默认，不删除仍有兼容/存档责任的类型。

- 首个C1基础切片已OFFLINE_VERIFIED（整体收尾仍ACTIVE）：对固定main运行30项共同用例通过，5类缺陷旧红；新51项通过、3类有效反例被行为拒绝。原CTS字典/直接释放已替换为独立内部lease，只有请求结束且取消回调结束才释放；取消异常隔离、已取消不启动生成，旧public签名保持。
- 实际验证：旧main编译消费者不重编译、换新核心程序集后Native/Scene/Courier调用通过；现有pipeline40/提交边界69/回执39/async18/匿名prompt13/Native失败4、Courier后处理39、内部ports308+3故障、API119/256并发/32快照/3故障、4DLL584元数据、最终源码六Stage及main身份146/36通过。NuGet漏洞数据离线获取有NU1900警告，不声称已完成包漏洞审计。
- 主体范围与三渠道外部必交矩阵已写docs/phase8/af-core-main-closeout-matrix-20260915.md；只读API没有被改成假Supported，政策/宴会/GCCZ业务未动，外部三渠道SDK、Courier线程准备、完整Campaign/Mission和B1预算/深复制仍未完成。测试与本轮修复不等于整个main主体已完全验收。

- 本轮生产73774a94；详细入口docs/handoffs/2026-09-15-main-closeout-lifetime-handoff.md，96点地图与审计JSON绑定该候选。仅本地，整体收尾仍ACTIVE，未推送/部署。

## 以下为历史记录；当前收尾以上方为准

# 当前任务：推送已验证重构交付（2026-09-15）

- 用户明确授权“推送到GITHUB”。任务 `GITHUB-DELIVERY-20260915` PUBLISHED；起点1345b0bc，生产f07cb2a2；目标仅origin/codex/af-main-refactor-continuation-20260831（klfwdf/AnimusForge），不触碰main/legacy远端、不强推/改历史。
- fresh fetch基线af618912，14 ahead/0 behind；已核实快进关系、60条变更路径、无新构建/临时产物，3份保护文件未改变且待推送历史不触及。本地专用Native HANDOFF不在分支树或整个祖先历史中。
- 同候选既有测试/六Stage证据复用，91点地图与差异空白检查通过，无源码改变不重跑构建。仅发送已提交代码/文档；自动化PAUSED、不部署游戏。已正常快进推送af618912→1345b0bc，ls-remote核对远端完整SHA=1345b0bce8c2f73de6a6dfe8a8d87330280de681；本次再提交当前交付说明，不改变生产源码。

## 以下为历史记录；当前交付以上方为准

# 当前任务：框架内外边界收口与对照链修复（2026-09-15）

- 用户要求“继续直到完美”；继续实际拆分与验证，不承诺零BUG，不自动推送/部署/恢复自动化。任务 `FRAMEWORK-SNAPSHOT-BOUNDARY-20260915` OFFLINE_VERIFIED；起点06a457c3，生产955a6be3，唯一写入G:/AFMOD/AF-REFACTOR，保留三份保护文件。
- 先处理上一轮完整port测试阻断：确认既有Memory source_parity可严格恢复到90201155，接入旧owner比较前；保留完整文件相等与证据哈希门禁，不能略过未审查差异或只刷新hash。
- 实际拆分：ModuleFrameworkRuntime不再引用Api.V1，持有内部生命周期状态并捕获不带游戏/活目录引用的只读目录快照；Api.V1侧独立投影为既有public DTO。唯一真实Directory/注册入口保留；所有旧枚举、原因码、顺序、Stopped不评估gate、公开表面保持。
- 生命周期核对：读档generation与Mission结束清理仍归原owner，注册不等于读档可用；本轮不假造统一GameEnd清理。公共投影拆分只处理模块目录快照作用域，不冒称Campaign/Mission生命周期完成。新增中间快照仅在显式API查询分配，小表有界；不引入Tick/轮询/反射。
- 验证：旧失败/新完整port回归和有效故障；源隔离、快照不可变/并发/旧新public输出、装配回归、六Stage与实际DLL元数据、存档身份、地图/HANDOFF。明确实机和B1原深复制/预算未完。

- 结果：新增纯内部ModuleFrameworkSnapshot/ModuleBindingSnapshot与内部生命周期枚举；Runtime删除Api.V1依赖/公开DTO构造/映射，净减19行；AfV1SnapshotProjection在API侧独立投影。Capture在原装配锁内，投影在锁外，仅按显式查询分配有界小表，无新任务/注册器/游戏引用。
- 历史port完整链已恢复：接入原B1严格逆变换，4个owner整文件+SubModule历史对照、13签名/31调用点、308断言和3有效故障全部通过，未放宽hash或跳过缺失方法。预期失败曾触发测试进程挂起/EXE占用，改测试Main受控非零退出并只清理核实路径下的失败测试进程；重跑通过，失败日志保留。
- 本候选：无API引用的CoreOnly真实编译、32快照边界/128并发、119公开API/256并发、3快照故障、42装配/5故障、15记忆逆变换守卫、六Stage、4DLL元数据584、SyncData146/行为36均通过。公开V1语义/存档/默认和制作组业务未改。
- 工程师差异审查通过：唯一Directory/原状态锁保持，跨停止/重载快照不可变，不在投影时二次求gate；玩家视角只做源码推演与可见API反馈对照，未进行游戏内实测。完整Campaign/Mission生命周期、主体其他职责迁移、B1深复制/预算、实机/旧档/live Economy/AFEF仍未完成，不标项目“完美”。

- 最终生产/测试f07cb2a2；新详细HANDOFF为docs/handoffs/2026-09-15-snapshot-boundary-handoff.md，91点地图与审计JSON绑定同源码。仅本地，未推送/部署。

## 以下为历史记录；当前实施以上方为准

# 当前任务：框架装配职责真实拆分（2026-09-15）

- 用户明确要求“编排好了吗，那开始拆分”。任务 `FRAMEWORK-COMPOSITION-EXTRACTION-20260915` OFFLINE_VERIFIED；起点812b34b0，生产基线61d57892；唯一写入G:/AFMOD/AF-REFACTOR。fresh fetch远端af618912，本地8 ahead/0 behind；不融合/推送。
- 先实现I1装配切片：SubModule原36个CampaignBehavior与4个模型包装注册移到专门装配owner，现有ModuleFrameworkRuntime作为唯一委托入口；制作组typed目录注册从runtime生命周期状态提取。只搬移装配，不搬移领域规则/存档类型，不新增注册器/队列或无消费者接口。
- 保持原顺序、每次Campaign回调新建实例、最后一个非AF模型作为inner/默认模型fallback、逐模型失败继续与行为注册异常传播；非Campaign/no-op。模型注册成功不等于读档完成，目录Ready语义不改，旧Campaign/Mission清理路径不伪造。
- 验证：固定旧源码抽取对照、当前真实装配方法+引擎stub执行、顺序/隔离/失败/重复调用/故障反例；既有API/并发/拒绝访问、六项Stage、存档身份与代码坐标。保留未覆盖的完整生命周期/三渠道业务/B1深复制工作。
- 修改范围：SubModule、Refactor/Modules装配类，相关源码级测试及文档。自动化PAUSED，不部署/操作存档/改默认/制作组玩法；两份用户草稿和指定本地Native简明版不动。

- 本切片结果：CampaignComposition实际承接36个行为、CampaignModelComposition承接4个包装模型、TeamModuleRegistration承接3组typed目录声明；SubModule净减148行，ModuleFrameworkRuntime净减25行。旧实现已从原位置移除，无第二套清单/注册器，公开接口/Saveable身份未变。
- 验证：装配42项+5类有效故障反例，原/新整文件逆变换与4个模型方法/注册顺序对照；API119+并发256+外部访问拒绝、4DLL元数据556、Debug/Release×1.3/1.4/Bootstrap六Stage、SyncData146/Behavior36保持。首次Debug因误移除仍被UI使用的PolicyEffects using失败，已恢复并重跑成功，失败日志保留。
- 已知阻断：历史TeamModulePortParityTests完整入口仍因61d57892就已缺失的ProcessMemorySummaryQueueAsync源码定位失败，未通过/未豁免；独立13签名/308真实port断言与组合后的历史SubModule逆变换通过。不把部分检查写成全仓合格。
- 生产/测试955a6be3；详细入口docs/handoffs/2026-09-15-composition-extraction-handoff.md已记录创建释放表与验证，代码地图86锚点绑定本候选；完整Campaign/Mission生命周期、公共投影进一步分离、三渠道业务拆分和B1深复制仍待办。本轮切片完成不等于阶段8或整个框架DONE。

## 以下为历史记录；当前实施以上方为准

# 当前优先级：先做整体框架编排蓝图（2026-09-15）

- 用户最新要求“先进行框架的编排”。任务 `FRAMEWORK-COMPOSITION-BLUEPRINT-20260915` COMPLETE（仅设计/文档，生产编排实现未完成）；本轮暂停继续深复制与细部业务拆分，先厘清装配根、作用域、模块依赖、启动/停止及对话执行编排。不是把B1验收跳过，也不等于已经实现完整Host。
- 源码基线61d57892，当前HEAD16af548b；唯一写入G:/AFMOD/AF-REFACTOR。范围为新增编排蓝图与现有计划/总HANDOFF的优先级链接，不改变C#、接口签名、游戏默认或存档，不创建空模块/第二注册器/第二套队列。
- 实际参照：SubModule的初始化/停止调用、ModuleFrameworkRuntime目录装配、TeamModuleServices三个typed桥、LegacyInteractionPipelineComposition与InteractionRequestCoordinator、MemorySummaryDispatcher及Host。记录已实现/待实现，避免把目录Ready解释成Campaign可接单。
- 验证：源码坐标/相对链接、编排与原职责计划一致、无生产diff、3个保护文件hash；文档轮不重跑无关构建。自动化仍PAUSED，未授权推送/部署或制作组玩法变化。

- 产出：`docs/architecture/af-core-composition-blueprint-20260915.md` 已区分装配Composition/对话Workflow、已有/目标、四层生命周期与单一owner、启动/停止、内部/public端口与后续实施出口。根HANDOFF/原P0–P6及14类职责清单均链接新优先级，未建立竞争台账或修改代码。
- 核查：当前C#仍61d57892；蓝图源码坐标/链接和3个保护文件hash检查；不重跑无关构建。后续先实例创建/释放表和现有装配入口演进，不把蓝图当可发布实现；仅本地提交。

## 以下为历史执行记录；当前优先级以上方为准

# 当前任务：M1/M2 捕获与接受调度职责提取（2026-09-15）

- 用户授权按职责计划开始实施，接口稳定、细致拆分。本轮任务 `B1-DISPATCH-OWNER-20260915` OFFLINE_VERIFIED（本切片交付完成，整体B1仍VERIFY）；唯一写入G:/AFMOD/AF-REFACTOR，分支codex/af-framework-skill-delivery-20260911；起点b7c90201，前生产9617f96a，fresh fetch远端af618912，本地3 ahead/0 behind，不融合/推送。
- 真实前置责任：MyBehavior.MemorySummaryMainThread目前持有捕获/接受共用队列、CAS待办状态、每tick额度/耗时和异常完成。先把它们提取为独立runtime owner + 窄internal host契约，MyBehavior仅留引擎身份/线程/设置/诊断适配和既有调用入口；迁移全部读到旧预算字段的规划调用，不新增第二套队列或兼容死字段。
- 原行为保持：同步与排队共用2操作/实际执行耗时预算，FIFO/档代和owner拒绝、reset退役未开始任务、部分完成异常准确抛回，不伪造网络取消/事务回滚。公开V1/制作组ports、存档DTO/键、Prompt/动作规则不变；纯runtime不引用游戏程序集。
- 范围：Refactor Contracts/Runtime新调度owner，原MemorySummaryMainThread适配与MemorySummaryPlanning预算读取；相关实际helper/captured/business/planning/writer/terminal/sealing测试接入新真实组件，源码守卫/地图/交接。验证旧新相同行为、真实故障控制、同候选六项Stage/API/存档身份。
- 此包是M1/M2的线程接受基础提取，不冒称首次整图capture/copy已分段，也不宣称全部14包完成。深复制/完整writer/原子尾步仍待下一包；B1未合格不进B2。自动化PAUSED，不部署、不操作存档、不改制作组业务。两份用户草稿与指定本地Native简明版保留。

- 当前结果：实际队列/claim-retire/预算/异常完成owner已移出，Host从156行降为57行；规划2处耗时读取改为新owner，未保留旧队列/计数器。原32项共同调度对照绿、当前37项绿、7个有效故障控制；captured116/business36/planning24/writers238/sealing88/terminal85/commit51和六Stage已通过，API/身份及最终材料收口中。
- 契约说明已写 `docs/architecture/af-memory-dispatch-contract.md`。本包只完成线程接受基础责任；首次capture/copy与深来源/完整writer仍待做，不能标M1/M2整体DONE。

- 最终候选61d57892：API119+并发256、4DLL元数据532、SyncData146/behaviors36保持；81点地图记录/工作树通过，214份冻结材料与六产物hash在新验收JSON。统一入口 `docs/handoffs/2026-09-15-memory-dispatch-owner-handoff.md`，契约在 `docs/architecture/af-memory-dispatch-contract.md`。本轮仅本地提交，未推送/部署，自动化仍PAUSED。

## 以下为历史任务；当前实施以上方为准

# 当前任务：修复内层同数量变动，并细化收尾前职责拆分计划（2026-09-15）

- 用户已明确授权修复本轮已复现的覆盖问题并制定后续模块化计划。任务 `B1-INNER-STRUCTURE-20260915` OFFLINE_VERIFIED；本轮修复与规划交付COMPLETE（整体B1仍VERIFY）；单代理，唯一写入 G:/AFMOD/AF-REFACTOR，分支 codex/af-framework-skill-delivery-20260911，起点 af618912 / 生产4d6994bc。自动化仍PAUSED，不推送、部署、操作存档、切默认或开展未经本轮审查的大范围搬迁。
- 旧行为/根因：DailyMemoryDraftEntryNormalization 在跨窗口时只看内层列表引用/count，64→64替换、删补、换位漏失效，最终发布旧_lineResult。上一检查真实抽取封存调用已复现2个对照绿、3种同数量变化红；不是实机症状归因。
- 意图：在现有内层游标上绑定实际List结构版本（含lines/trigger bind的相邻边界），O(1)校验，变化走既有失效重封；保留同步规则、单权威owner和实际预算，不重写净化规则、不引入无消费者接口。预计改MemorySealing、sealing runner/harness与精确源守卫/地图；按影响面验旧红/新绿/故障反例、相邻回归和原六项Stage。
- 规划：沿原P0–P6/B1–B3细化高内聚owner/typed端口/调用者迁移/旧符号删除/最终验收，不把partial、空接口或行数减少当拆分完成。补充可审查的职责包及迁移表模板，区分本轮实际修复与未来实施，不改制作组玩法或public范围决定。
- 兼容/保护：无存档字段/类型、Prompt/API/玩法/默认/原构建脚本变更；两份用户草稿与指定本地Native简明版不改不暂存。代码保持英文，说明/提示词可中文；每阶段在同候选证据和回滚点齐套后才放行，不承诺零Bug。

- 本轮修复结果：内层line/trigger的List结构版本探针已接入，移除两份post-Done无用line发布状态；88/0，旧4d同88例80/8，三个有效故障控制与captured116/business36/terminal85、13项精确源守卫通过。原六项Stage已通过；首次构建选到系统runtime-only dotnet，临时PATH切已有G盘SDK后成功，原失败日志保留，不改脚本。
- 规划结果：收尾前职责拆分设计已写入 `docs/phase8/af-core-responsibility-decomposition-plan-20260915.md`，沿原P0–P6而非另起阶段；当前仍B1未整批合格。新计划不等于这些职责已迁移，自动化继续PAUSED，本轮不推送。

- 最终候选：9617f96a；本轮API119/并发256、4DLL元数据532、146 SyncData键/36行为保持，六DLL/marker/Stage比对通过；78点导航绑定当前修复。统一交接见 `docs/handoffs/2026-09-15-inner-structure-fix-and-modularization-handoff.md`，原计划第18节链接新的职责拆分设计。无后台任务在继续实施，自动化保持PAUSED；代码/文档均仅本地提交。

## 以下为历史记录；当前授权及状态以上方为准

# 当前自动实施：B1 深 line/trigger 预算（2026-09-14）

- 本切片离线联验完成，整体B1继续VERIFY；单代理；写入本 Codex worktree（detached HEAD `4d6994bc`），检查点`eb6389f4`，前生产`86805518`。指定远端仍`origin/codex/af-main-refactor-continuation-20260831`。按第16节接续，不重做owner记录额度/typed raw/排序/共享窗口/索引，不进入B2。
- 意图：把单draft内1024行净化与weekly trigger bind改为共享metadata计费；draft身份仍一次expensive。抽出`BindDailyMemoryDraftWeeklyTrigger`/`SanitizeDailyMemoryDraftLine`供同步Sanitize与续跑共用。未完成draft的line/trigger列表保持私有，列表引用/count变化失效重封。
- 并发/语义：line/bind可跨窗口提前可见；trigger列表`SanitizeWeeklyMemoryMaterialTriggers`仍一次原子。不是整draft事务。同步oracle仍是40b92e67原`SanitizeDailyMemoryDrafts`。
- 预计路径：MyBehavior.cs helpers、MemorySealing entry续跑、sealing harness/runner/parity/审查表、代码图与交接。无Prompt/玩法/存档字段/API/默认或原构建脚本变化。
- 验证：当前76/0；旧40b同76例61/15；`unbudgeted-line-normalize`与`ignore-line-source` BUILD_PASS后断言红；12项源守卫；代码图77锚点记录提交与工作树通过。本轮未重跑captured/六Stage/API/存档身份；LIVE/SAVE=NOT_RUN。
- 剩余限制：trigger列表sanitize、首次capture/复制、全owner/raw/最终绑定、Apply仍未硬切分。不用删除数据或只改数字宣告B1完成，不自动推送/部署或操作存档。

- 结果：生产/测试`4d6994bc`；1×1024行9窗、max_lines=127；owner 257/65记录额度不变。下一步直接处理首次capture/copy。自动化保持PAUSED。

## 以下为历史暂停与实施记录；最新续点以上方为准

# 当前状态：用户暂停自动化，整体审查与 GitHub 交接（2026-09-14）

- **生产开发 PAUSED；阶段 8 / B1 未整批验收，不是 DONE。** 最新用户要求关闭自动化、说明整体进度与拆分、上传代码和详细 HANDOFF、保留本地简明版。本入口覆盖下方历史 ACTIVE / 自动继续安排。
- 调度已通过应用工具把 `af-7-8` 改为 PAUSED，并读回确认；旧 `af` 也为 PAUSED。没有恢复、另建自动任务或继续生产改动。
- 交付任务 `PAUSE-DELIVERY-20260914`：COMPLETE（仅暂停/审查/文档/GitHub交付；生产重构仍PAUSED且B1未合格）；执行者为本任务单代理，唯一写入区 G:/AFMOD/AF-REFACTOR。起始 HEAD `8f0e3ab8`，最后已离线验证生产/测试 `86805518`；指定远端 `origin/codex/af-main-refactor-continuation-20260831`，fresh fetch `3f00fefa`，本地 ahead 17 / behind 0。
- 意图范围：核对原始 AF 与当前大类家族/真实接口/剩余工作；更新根 HANDOFF、原 P0–P6 计划最新暂停入口、详细交接及审计 JSON；本地简明版放 `.tmp/` 不上传。既有两个用户草稿和指定 Native 本地简明版不改不暂存。
- 风险与验证：无 C#/Prompt/玩法/存档/默认/构建脚本变更；复用同源码86805518的已执行证据，重新核对源码、74点地图、冻结日志与产物 hash、文档坐标/链接和 outgoing 历史。推送前再fetch检查祖先关系/排除文件，普通快进，远端ref核验；若分叉停止，不融合或强推。
- 本轮不部署、不读写真实存档、不删旧实现、不改制作组业务；B1深来源/原子尾步、B2 Courier/完整三渠道、B3生命周期及public选择/最终实机门槛仍保留。后续实施须用户明确恢复。

- 本轮文档成果：新整体暂停 HANDOFF、原计划第 17 节、根当前入口、整体审计 JSON；本地主体简明版 `.tmp/AF主体简明HANDOFF-20260914.md` 不提交。源码/测试仍86805518，结构复核52个Refactor文件、20个owner额外partial、三大家族115983行；B1未整批合格，不能宣称拆完或只剩实机。
- 发布门禁：74点地图双模式、当前源码和既有六项产物/183份冻结材料hash、3个保护文件、文档链接/坐标与outgoing排除检查；这些是本轮重新核查，不冒称全量业务或游戏重跑。Git引用核验后普通推送，实际结果由工具及本地push-receipt记录；生产开发保持PAUSED。

- 发布结果：已普通推送 `3f00fefa → dcc17ee70832e2c63725bf08b0a284f9a94429d3`，19提交/41差异文件，git ls-remote核实一致。本条为随后的文档回执，不更改生产86805518；全局重构不标DONE，后续开发仍须用户明确恢复。

## 以下为历史实施记录；最新暂停状态以上方为准

# 当前自动实施：B1 owner草稿净化记录预算（2026-09-14）

- 本切片离线联验完成，整体B1继续VERIFY；单代理、唯一写入G:/AFMOD/AF-REFACTOR；当前生产/测试86805518，前生产40b92e67，fresh fetch远端3f00fefa未变。按第16节接续，不重做raw摘要/排序/共享窗口，不进入B2。
- 意图：把封存末尾单owner整个草稿列表净化改为按实际draft逐条授予预算，并复用稳定排序组件。原同步Sanitize入口保留；单entry净化体原样提取（主线程原地、后台clone、先占key再判断empty、标签/AFEF/marker规则不改）。
- 并发/语义：仅规范化元数据可分记录提前可见；源列表的删除/去重/排序结果完成后才发布。持续检查owner列表引用/结构，发布前校验每条key/日期以及空winner是否长出新lines，拒绝过时删除；失效重走原owner封存与索引。保留原引用、别名副作用顺序、同日稳定顺序，不能把整owner改成假事务。
- 预计路径：MyBehavior.cs的Sanitize与单entry helper、MemorySealing状态与真实caller、captured/sealing/terminal等源提取适配、源码精确inverse/地图与交接。无Prompt/玩法/存档字段/API/默认或原构建脚本变化。
- 验证：真实40b旧实现大owner预算反例；主线程与后台净化原行为oracle、empty-first去重/别名/大小序、追加/替换/同slot/修改key/新lines/同步排空/异常和Campaign累计授予；保留既有60/116等语义，对应反例/相邻与最终六Stage/API/存档身份。
- 剩余限制：一个draft内的lines/trigger文本净化仍原子；key/empty guard、单字符串/全owner绑定和Apply也未硬切分。不用删除数据或只改数字宣告B1完成，不自动推送/部署或操作存档。

- 结果：75/0、旧40b同75例62/13、27有效故障反例、12项精确源守卫与相邻回归/最终六项Stage/API/存档身份通过。257与65记录owner由一次全净化变每窗口最多8；单draft1024行仍原子，深line/初捕获/全owner绑定/Apply继续待做，不进入B2。

## 以下为上一已验证切片（历史）

# 当前自动实施：B1 完整raw来源摘要成本（2026-09-14）

- 本切片离线验证完成，整体B1继续VERIFY；单代理，唯一写入G:/AFMOD/AF-REFACTOR，当前生产/测试40b92e67，前生产8bcde78b；fresh fetch远端3f00fefa未变。按计划第16节继续，不重做排序，不进入B2。
- 意图：完整raw来源hash由JSON序列化改为显式字段/列表有界buffer编码，去掉反射装箱/属性名和JSON转义的重复成本；保留所有字段、列表顺序/null元素、null/empty/absent区别。不用不完整writer epoch替代原数据校验，不改变Prompt或权威提交。
- 预计路径：MyBehavior.MemorySummaryInput及私有DTO编码边界、Refactor/Runtime纯摘要组件，captured/terminal/sealing直接测试适配、原字段反例与严格inverse/地图/交接。原通用ComputeMemorySummaryFingerprint（计划/编辑器/上下文）继续原JSON契约；只有瞬时来源指纹格式改变，存档/API/模型字段身份不变。
- 风险与验收：字段遗漏、长度/类型分帧冲突、Unicode、state presence、源/owner/generation/重试/接受要有正反例；原8bc真实执行成本对照，不以常量或砍字段伪造提速。未来DTO新字段由反射字段覆盖测试阻止漏编入；摘要仍完整原子O(N)，不当作深记录硬预算。
- 验证：新旧同数据/全字段变更/明确故障反例、实际capture→execute→final check及相邻commit/封存/规划/UI/history/native、现有六项Stage/API/持久化身份。只本地提交，不推送/部署/碰存档/改原一键脚本或制作组玩法。

- 结果：116/0、旧8bc同套112/4、35有效故障反例、294递归字段修改、9向量/5守卫、相邻回归、11项精确inverse以及最终六项Stage/API/存档身份通过。1000记录×12摘要分配约降96–98%；仍为完整原子O(N)，不进入B2。下一步继续初捕获/净化/owner绑定/Apply预算，不重复本轮typed raw摘要优化。

## 以下为上一已验证切片（历史）

# 当前自动实施：B1 队列原子排序（2026-09-14）

- 本切片离线联验完成，整体B1仍VERIFY；单代理，唯一写入 G:/AFMOD/AF-REFACTOR；当前生产/测试8bcde78b，前生产9158132c；fresh fetch远端3f00fefa，无新变化。
- 意图：将Daily/Major封存尾部的稳定排序提取到有真实消费者的可续跑纯运行时组件，复用Campaign预算；同步调用、先pending再去重、原地净化、引用身份、文化排序及同键稳定次序保持。正常净化仍主线程原子执行，允许净化元数据先于排序发布，不启动第二条总结链。
- 路径：MyBehavior.cs/MemorySealing、Refactor/Runtime排序组件、直接sealing/business等测试适配及精确inverse/地图/交接。存档DTO不搬迁，Prompt/玩法/public/默认/原构建脚本不改。
- 风险与验收：不得发布跨tick过期列表/元数据/culture；排序每次实际比较/移动计费、同步可排空；旧915真实执行红例、变异与原40场景、相邻回归和六项Stage/API/存档身份。数组分配/净化/键捕获/最终标量绑定仍原子，不用本切片宣称B1硬预算完成。真实游戏/存档未运行，不部署/推送。
- 验证完成：封存60/0，旧915的46/14，20有效故障反例；相邻24/36/109/238/85/51/54/23、UI/history/native/channel、10项strict inverse与最终六项Stage/API/存档身份通过。规划extractor曾缺2个真实helper（编译失败非红例），已补齐并复跑。下一续点是剩余初捕获/raw/净化/绑定/Apply原子成本，不进入B2；本轮详见最终排序HANDOFF。

## 以下为上一已验证切片（历史）

# 当前自动实施：B1 Campaign维护共享预算（2026-09-14）

- 本轮共享Campaign窗口与延迟启动修复已完成影响面离线验证，整体B1保持VERIFY；本轮生产/测试9158132c，当前主代理单独实施，起点2fc32b6d、前生产73a6977c，检查点01e24b9e。启动fetch远端3f00fefa，无新协作覆盖。
- 目的：把OnCampaignTick内主维护与deferred维护接到同一有限时间窗口，封存的metadata/expensive授予跨多次调用累计；抽出真实独立预算组件，保持显式同步/无限预算语义。EngineTick摘要回调仍是独立既有窗口，不冒称全游戏帧预算。
- 关联正确性：窗口耗尽时已完成封存的summary启动意图须留到下一窗口，不能同日丢失；绑定save generation，退场/异常不泄漏旧窗口。保留正常、空/终止任务、重复调用、异常/耗时/同步/读档等对照。
- 预计路径：MyBehavior.cs、MemorySealing/新的预算接缝与Refactor运行时组件、直接business/sealing tests、精确source inverse/定位图及交接。不得改Prompt、provider/存档身份、制作组玩法、默认/public入口或原一键脚本。
- 验证：先用原73a6977c真实Campaign维护段证明重复授予/重复deadline和迟到启动缺口，再实现并复测真实链与故障反例；保留已有30/23等语义用例；相邻回归、六项Stage、API/持久化身份，按最终源码绑定。源码范围/已验/未验见[本轮交接](handoffs/2026-09-14-b1-campaign-budget-handoff.md)。
- 自动化继续ACTIVE；下一续点为剩余深来源与原子尾步，不重做本轮已共享的Campaign窗口。40场景、14反例、旧版35/5和相邻回归已通过；最终六项Stage通过，接口/身份检查及最终交接落盘。未推送/部署/操作真实存档；用户草稿原hash保持。

---

# 每小时自动执行入口（2026-09-14）

用户已授权完善计划并恢复自动推进。现有 `af-7-8` 已经应用工具更新并读回确认 **ACTIVE，每小时一次**；沿用当前任务和原调度，不另建任务。当前入口为[根HANDOFF](../HANDOFF.md) → [原计划第16节](phase8/af-core-precloseout-plan-20260913.md)。

- 本次配置完成：只改计划/当前入口/自动化提示词，没有新增生产修改；配置起点55af6d3e、意图检查点e70ef0cc，生产仍73a6977c。
- 后续自动实施范围：从B1深来源/原子预算与真实主体职责提取继续，整批验收后才进B2/B3；不重做已完成9项集成和素材索引。不把本次“配置完成”当作生产阶段完成。
- 双Skill、单代理、同DLL内外接口/制作组业务边界保持；不自动推送、部署、操作真实存档、切默认、融合分叉或广泛删旧。
- 完成获准可做工作或仅余外部阻塞时，写技术/制作组两份HANDOFF并通过应用工具暂停。未来用户暂停/停止指令立即优先。
- 已验证：计划链接/续点、受保护草稿hash、源码未变、工具ACTIVE回执及提示词/原频率/目标任务读回一致。下方旧暂停/本机手动配置状态保留为历史，不覆盖当前自动执行入口。

---

# 当前实施入口（2026-09-14：B1 集成与主体职责提取）

**用户已明确恢复主体重构。本机手动执行，自动化保持暂停。** 唯一当前状态为 [根 HANDOFF](../HANDOFF.md) → [本轮交接](handoffs/2026-09-14-b1-index-owner-integration-handoff.md)，本段替代下面旧审查/暂停入口，不改变历史证据。

- 基线 `3f00fefa`，生产 WIP `c21523f8`；工作区 `G:/AFMOD/AF-REFACTOR`，本地分支 `codex/af-framework-skill-delivery-20260911`。远端 fresh fetch 0/0。
- 执行者：当前主代理，单代理实施。现有 B1 / P1 本轮范围 VERIFY：生产73a6977c已闭合9个原未审符号的精确inverse/证据并提取素材索引独立运行时职责，保持原 AF 的键/重复记录/副作用和存档语义。
- 范围：主体源文件、直接运行时组件、相关测试/代码图与交接；政策/宴会/GCCZ 业务、public 写 API、默认入口、构建覆盖脚本不改。不部署、碰真实存档、推送或恢复自动化。
- 验证：保留精确原行为/当前 WIP 对照和有效故障反例；相邻主业务/历史/Native、严格 inverse 与代码地图，最终同候选 Debug/Release×1.3/1.4/Bootstrap 项目内 Stage。未验证层不标 DONE。
- 原始 AF 功能起点 `d4cb1467`，B1 局部迁移参照 `62abfdb3` 与 `c21523f8`；批准修复与应保留语义分开。两份用户草稿和指定本地专用 Native 简明版 hash 保持。
- 状态：本轮素材索引职责提取与 WIP 精确集成已完成影响面离线联验；54 声明/2 删除/组件锁和 8 门禁测试通过，素材23/7反例、封存30/8反例、相邻回归、六项Stage、API/存档身份通过。B1整体保持VERIFY（深来源预算/实机等未完），阶段8未DONE；源码/证据/回滚在本轮最终HANDOFF固化，不进入B2。

---

# 当前审查入口（2026-09-13，状态核对与详细交接）

- 当前任务：核对原始 AF → 现有拆分、实际阶段、功能复现/缺口，编写详细 HANDOFF 和既有计划的收口修订，并按用户授权普通推送专门重构分支。
- 起点 HEAD `007dbeee`；生产现场 `c21523f8` 为暂停 WIP，最后完整离线联验生产 `62abfdb3`。本次审查/文档 ACTIVE，不代表开发或自动化恢复。
- 范围：只改交接/计划/审计记录；不改生产、测试实现、配置、默认入口或游戏文件。两份用户草稿、本地专用 Native 简明版不改、不暂存。
- 验证：Git/源码坐标与原始基线对照、聚焦渠道回归、原地图/当前地图与严格 inverse 门禁、文档链接和限定 diff、推送祖先/远端 ref 核验。下方阶段标题为历史，不代表当前全项目已完成。
- 执行状态：审查与文档已完成；当前入口为[详细 HANDOFF](handoffs/2026-09-13-af-stage-architecture-parity-detailed-handoff.md)，后续沿用[原计划第 15 节](phase8/af-core-precloseout-plan-20260913.md)。本轮渠道 132/0；旧图 53 点 PASS，工作树图/严格 inverse 确认未集成 WIP。生产未改，自动化 `af-7-8` 保持 PAUSED；GitHub 交付以远端核验回执为准。

---

# AnimusForge 重构执行清单

> **2026-09-13 当前状态入口：** [根 HANDOFF](../HANDOFF.md) → 当前收尾前计划及执行证据（当前同一B1来源/预算/writer/接受联合重构，未整批合格，不进B2）。下方旧“当前/最新/未开始/未推送”段落均保留为当时记录，不能覆盖新用户授权或当前 Git 状态；本轮不把历史门禁、旧失败或未完成项改写成 DONE。

> 本文件是 AF 重构的公共进度台账。它记录目标、阶段、当前状态、验证证据和交接信息；不替代 `.claude/skills/animusforge-maintainer/` 中的长期工作规范。

## 最新指令：暂停自动化并全面检测（2026-09-08）

- 用户明确要求关闭自动化；`af-7-8` 与旧 `af` 均已确认 PAUSED。下方自动接续段落为历史记录，不构成继续自动运行的授权。
- 检测锁定源码 `35524b04`，未修生产/正式测试、未推送或部署。确认 4 个 P1 与 2 个 P2 功能问题，另有 Bridge/目录不一致及未验证风险；构建通过不能将阶段八升级为 DONE。
- 完整报告：`G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-08-full-refactor-audit-35524b04.md`。后续修复按报告给用户确认，不由已暂停的自动化继续修改。

## 检测收尾交接（2026-09-08）

- 用户要求做结尾工作；仅归档检测与交接，不修生产、不恢复自动化、不推送或部署。源码验收仍绑定 `35524b04`，4 个 P1 与 2 个 P2 功能问题尚未修复。
- HANDOFF：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-08-audit-closeout-handoff.md`；制作组短文：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-08-audit-closeout-team-brief.md`。
- 本地证据 ZIP 已通过完整性和逐文件 SHA-256 校验；索引见 `G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-08-audit-evidence-manifest.json`。证据包不是可安装 MOD，未进入远端。

## 自动接续重启（2026-09-08）

- 用户已批准“设置自动化开改”；复用当前任务 heartbeat `af-7-8`，每30分钟，不创建重复自动化。唯一代码工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 已 fetch 并安全快进到共享重构分支 `aefa02ad15758222b87e4e240a85c52eb3f913d9`。两份2026-09-06 integrated-phase8-handoff/team-brief本地草稿原样保留、不暂存；不改其他工作区、游戏或真实存档。
- 任务 `P0-CUTOVER-20260908` VERIFY（代码/离线验证完成，实机未测）：先修Scene/Courier外层在Host终态失败后重新请求的缺口。owner为Conversation/Courier调用边界。计划路径：ShoutBehavior.cs、CourierDeliveryBehavior.cs、定向外层回归工具及本计划/HANDOFF；不改存档key/type、玩法数值、Bridge开关、默认选择或官方构建脚本。
- 验证：先对生产外层控制流作故障注入红测，修后回放成功/失败/空回复/回退/取消/异常，补既有Host契约与官方1.3/1.4/Bootstrap项目内Stage。真实LIVE/SAVE仍未在本轮执行。
- 当前实际Native已恢复完整旧入口；Scene/Courier接入不等于三渠道等价完成。13 wired与历史PASS不可提升为全领域实机通过。具体计划及20领域清单见 `docs/phase8/refactor-execution-plan-20260908.md`。
- 回滚基线 `aefa02ad`；本地意图提交后执行，后续按定向逆提交回滚，不reset/rebase/force-push。推送、部署、新默认切换及广泛删旧另待明确方案批准。
### P0-CUTOVER-20260908 首轮结果

- 意图提交 `bdeeadd8`，修复只改Scene/Courier现有外层控制流。Scene以Host接管状态替代空回复判断，终态失败break至现有收尾；Courier保留送达前预生成，失败先seal既有PostprocessConsumed/清残留文本再推进返程，保护已排队legacy完成和已完成回信。
- 从真实生产连续block抽取编译的44案例：基线 `aefa02ad` 19 PASS/25 FAIL，修后44 PASS/0 FAIL；提取器5 tests通过。Host/队列/网络依赖为stub，不冒充完整游戏状态机。
- Debug/Release × 1.3/1.4/Bootstrap六项官方构建各0 warning/0 error；Interaction 40+69+39+4通过，生产Configured/Detached/Courier Host回放通过；Bridge16/13/3、20自测、入口10自测通过。持久化校验确认远端已有3处导航行号过时，仅校正行号后142键/168绑定通过，无存档key/type变化。
- Interaction runner有NuGet漏洞元数据获取NU1900警告，测试正常完成；不修改源配置或关闭审计掩盖警告。
- 独立源码审查无本轮阻断；日志 `.tmp/cutover-20260908/`、`.tmp/channel-cutover-boundary/`。完整命令/哈希/未验证项见 `docs/handoffs/2026-09-08-cutover-terminal-safety-handoff.md`。
- 下一精确任务 `P0-SCENE-PARITY` TODO：从完整Scene现有前处理/消息/动态PostprocessRules到detached重新捕获链做差异回放，优先复用完整上下文，不重复计算或偷偷切Native默认。自动化保持ACTIVE；没有推送或部署。

### P0-SCENE-MAIN-PARITY-20260908 VERIFY（主请求交付离线通过）

- 基线 `92ad625a`；fetch后远端仍为aefa02ad，本地ahead3，无远端新提交。两份旧草稿不变。
- 初查确认默认Scene先调用BuildStrictSceneMessagesForNpc消费当前AFEF并构造完整role消息，detached随后重新捕获，主请求不再使用该完整messages；信任、当前事实、场景标签及历史顺序会丢失。后处理另有真实PostprocessRules/资格/归一化/relay时机缺口，不能混称本轮全部等价。
- 本轮意图仅闭环主请求保真：冻结已准备的messages并沿现有Scene端口交给Gateway；保留public工厂ABI与无prepared的opt-in行为，不复制另一套Prompt逻辑，不改Postprocess/Action/Memory/default/存档或GCCZ。后处理所需旧捕获暂留，不宣称已消除重复前处理。
- owner为Conversation.Scene/Prompt adapter。拟修改ShoutBehavior.cs及定向source-linked/外层回归与文档；验证同一角色/顺序/文本/事实/当前输入/5000token完整进入main请求、不可变性、无跨请求重用及非Scene路径不变；原44外层故障回归、相关Host契约与官方六项构建。
- 先做新旧实际factory/callsite反例，再修复并清理不再使用的默认主消息重组选择。本轮实机NOT_RUN，不推送/部署/切Native默认。

### Scene 主请求保真结果与下一项

- 意图提交 `8c30424e`；生产仅ShoutBehavior 21行差异：默认每轮将已经完整准备的messages冻结为PromptPackage传入同一Scene ports。原public二参数factory保留，委托private factory；无prepared时保持旧组合行为。main不再丢弃原AFEF/信任/role消息，也不再额外拼接snapshot输入。
- 实际CreateChatMessage是匿名{role,content}，LegacyPromptPackageAdapter只支持字典，不能拿它转换该真实请求；本轮复用已有LegacyConfiguredChatGateway.BuildPromptPackage，不新增反射/消息序列化实现、不改全局CreateChatMessage。
- 扩展原外层harness实际执行production factory/callsite/匿名converter：基线92ad625a 46 PASS/6 FAIL（原44全通过），修后52 PASS/0 FAIL；抽取7 tests通过。六项官方Debug/Release双API/Bootstrap各0warning/0error；Interaction与Configured/Detached/OptIn生产回放、142键/168绑定、Bridge16/13/3通过。NU1900仍为本机NuGet元数据网络警告，不隐藏。
- 验证仅证明已准备main消息交付保真；未执行BuildStrict场景构造、真实API或LIVE/SAVE。postprocess保持原delegate，复capture/正文规则冒充tag_rules/逐领域归一化/relay/firstTurn及NPC回复参数缺口仍在，不能标Scene全部等价或删旧。
- 下一精确任务 `P0-SCENE-POST-PREP` TODO：在原Queue调用时机下把TryRunSceneUnifiedActionPostprocess原实现分离成prepare/network/normalize，先保持旧入口可重放；禁止直接复用缺少Scene relay/summon/guide的Courier builder或提前以空回复生成post prompt。详见新HANDOFF。Native诊断CompareMainMessages对匿名消息的遗漏纳入P1，不依赖字典fixture的PASS直接迁移。
- 日志 `.tmp/scene-main-20260908/` 与 `.tmp/channel-cutover-boundary/prompt-*`，独立审查无本轮新增阻断。完整证据见 `docs/handoffs/2026-09-08-scene-main-prompt-fidelity-handoff.md`。自动化继续；未推送、未部署、未操作存档。

### SCENE-POSTPROCESS-MILESTONE-20260908 VERIFY（Scene 定向离线通过，全局门禁仍有失败）

- 用户明确授权 Scene 默认改为仅生成正文，再由完整后处理统一执行动作/接力和原听众记忆 owner；不切 Native、不改开关、不部署游戏。原基线 `d40808b3`、意图 `17151d6b`。
- 完成同类型 partial 的完整 prepare/network/complete 拆分并接回默认流程；保留原动态规则/资产/债务/候选/normalizer，取消早期 Host commit、重复玩家历史与漏旁听 owner。主线程 prepare/dispatch、后台只传网络字符串，原 public opt-in ABI 不变。
- 同里程碑修复正文标签播放旁路、relay 与 speech 互等、generation/session/epoch 跨档发布、五处会话清理晚回调、request deadline 及旧 gate/waiter 干扰新请求；移除重复原实现与失效提交屏蔽条件。没有借此删仍有调用的 facade。
- 验证：Channel 132 / extraction 14，原方法差分 71 + guard 2 / mutation 5 / extraction 8，Queue 37 / mutation 7，Gate 6（原版三种竞态实际红测）；Interaction 40+69+39+4，生产 DLL 七套回放通过，Duel 双 API 35；官方 Debug/Release 的 1.3/1.4/Bootstrap 六项 0 warning / 0 error，构建前后输入指纹一致。
- 集成基线含其他作者 `4a239d95` / `cec3877a` 的遭遇安全修复，未覆盖或归为本轮成果。其移除 InteractionComponentSafePatch 可选 gate 后，runtime-game-adapter 清单仍声明 gated wired，当前 Bridge validator FAIL；20 个自检中的 1 个仓库基准 error 同因。未恢复安全 gate 或降低检查掩盖。下一项先核对 mandatory safety 与 optional bridge 的真实边界。
- 入口清单已登记新 partial；LIVE/SAVE 不升级。保留两份既有草稿；未推送、未部署、未操作存档、未跨工作区写入。真实游戏/旧档/live Economy/AFEF/TTS NOT_RUN，不把本里程碑或测试数量当阶段八 DONE。
- 完整 HANDOFF 与制作组简报：`docs/handoffs/2026-09-08-scene-postprocess-milestone-handoff.md`、`docs/handoffs/2026-09-08-scene-postprocess-team-brief.md`。后续继续前段 capture、Native 完整 Prompt/流式/主动开场 parity，以及 BattleSpeech 异步回退重入验证；自动化按完整模块接续。

## GitHub 融合交接推送（2026-09-06）

用户明确授权“融合然后推送”。已完成本地 `38c72484` 与共享远端 `8f1fa8db` 的正常合并，代码提交 `fb01c03c`；三个终端冲突按功能融合，保留API引导/设置与本地功能修复。融合后六项构建、终端/周报/Duel/Gateway/Host及相关契约回归通过。新交接为 `docs/handoffs/2026-09-06-merged-refactor-handoff.md`，制作组文案同目录 `2026-09-06-merged-refactor-team-brief.md`；普通推送目标仍为 `refactor/prepare-af-restructure`，不覆盖main、不force push。

两份原handoff/简报的本地未提交占位草稿保持原样；本次不部署游戏、不操作存档、不切默认入口、不恢复自动化。阶段八整体仍未标DONE；最终发布位置见GitHub交接记录。

## 当前整体收尾任务（2026-09-06）

用户要求不再逐小批次交付，统一完成阶段 8 的功能对照、接入、清理和回归。基线 `97515f3f`；本轮保持同一工作区/分支，内部可使用回滚 checkpoint，但不以每个 checkpoint 作为交付终点。

- 整体范围：旧终端剩余功能与入口、三渠道实际默认路径/替代覆盖、Bridge fallback 隔离、存档与领域验收目录、项目内 Debug/Release 双 API/Bootstrap 和现有相关回归。
- 先确认实际调用链，再清除已替代实现；不按 Legacy 命名删活跃 owner、不改变存档 identity、不通过重命名或弱化测试虚报完成。
- 使用并行子任务分别核对主入口、终端与验收边界，统一集成/构建，避免多个任务同时改同一文件或共享 Stage。主任务负责最终验证与提交。
- 认可用户报告的制作组既有实测基线；新增修改仍按实际证据区分源码/回放/编译与实机。此前整体修复时没有对新 DLL 的部署、真实存档操作或推送授权；现用户仅追加 GitHub 交接推送授权，仍不覆盖游戏、不恢复自动化。
- 状态 `VERIFY / 整体迁移未完成`：已统一修复周报正文/主线程与读档边界、终端外部交接/战争fallback、RAG开关隔离及部署/入口清单漏检，删除29个无入口私有方法和被替代弹窗；六项构建与相关回归通过。实际默认三渠道迁移、Scene detached规则等价及ModuleHost接入仍有实现缺口，不能宣告阶段8 DONE。集中交接：`docs/handoffs/2026-09-06-integrated-phase8-handoff.md`。

## 当前本机切片（2026-09-06 阶段 8 功能对照修复）

- 基线 `220b1dd5`；工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 用户授权继续阶段 8、对照旧代码恢复功能并清除确认失效的实现。本切片先处理终端查询/臣属选择、百科与退出、战争归档，以及部署/持久化验收缺口。
- 替代路径未完成功能对照之前不删除；仍承担三渠道默认玩法、存档迁移或双 API 兼容的代码不得仅因名为 legacy 就删除。后续默认切换和清理必须有对应 LIVE/SAVE 证据。
- 计划复用现有契约、补最小回归，构建 1.3/1.4/Bootstrap；不修改官方构建脚本、不部署、不写真实存档、不推送、不恢复自动化。
- 第二批标签字典替代与旧菜单清理 `VERIFY`：基线 `b8757240`，意图 `6cbc73e0`；统一分页/搜索/完整来源详情、刷新反馈和当前快照导出；六项构建及生产回放通过，详见同一功能对照文档。未触及实际游戏或发布。
- 本批状态 `VERIFY`：部署门禁、终端查询/臣属替代和已失效路径清理、即时和平归档及 WarStats 契约清单已实现；Debug/Release 六项构建与定向回放通过。用户确认制作组已完成既有验收；本批新增改动仍区分 offline 与 LIVE/SAVE。功能对照与清理证据见 `docs/phase8/functional-parity-closeout-20260906.md`。阶段 7/8 均未宣告 DONE。

## 当前任务（2026-09-03 Bridge 接线收尾接续）

本轮离线责任认领/入口复核计划已单独记录于
`docs/phase8/offline-owner-bridge-closeout-plan-20260904.md`；它是 `PROVISIONAL_AUTHORIZED`
准备态，不会把正式目录晋级为 `ASSIGNED/COMPLETE`，也不会解除 LIVE/SAVE、默认切换或发布门禁。

> **2026-09-04 较早工作树校正（已被下方后续复核覆盖）：**本机实际工作区为
> `F:\\AnimusForge-main`，该次记录的 HEAD 为 `34b3f35811130e26b60a5407451d169de3667dbb`。
> `PersistenceIdentityAudit --json --quiet` 在该次记录的 HEAD 上实际返回 `PASS`（sync 99、behavior 35、
> module `AnimusForge`）；“缺 89 个基线 blob / FAIL”仅适用于此前另一 partial worktree 的历史记录。
> 当时的 Debug unified Stage 已按授权部署到 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`，
> 三份 DLL 与项目 Stage 哈希一致，`installedMatchesStage=true`、`gameRunning=false`。本机仍未启动游戏，
> LIVE/SAVE、默认切换、facade 清理和发布仍未完成。详见 `docs/handoffs/2026-09-04-live-host-prep-and-current-state.md`。

> **后续复核覆盖：**随后仅为离线验证重新生成了 Debug/Release Stage，未再次部署；游戏目录仍是较早
> 的 Debug Stage，因此当前 `installedMatchesStage=false`。实机开始前以
> `docs/handoffs/2026-09-04-live-host-prep-and-current-state.md` 的 04:20 覆盖记录为准。

- `LIVE-HOST-PREP-20260904` VERIFY（较早部署快照，已被后续复核覆盖）：当时 Stage 已构建并完成 scoped Debug 部署；部署只更新统一
  `Modules\AnimusForge`，保留 `CustomPrompts`/`Logs`/`PlayerExports`，不启动游戏。下一步是制作组在
  隔离存档完成 1.3/1.4 Campaign/Mission 的 LIVE/SAVE 和 rollback evidence；没有这些证据不得升级
  阶段 7 或执行阶段 8 的删除、默认切换、Release 发布。

- `OFFLINE-GAP-20260903` VERIFY：已完成 Bridge caller/body validator、Bridge 隔离运行时配置测试、Phase 8 入口候选 inventory、PersistenceIdentityAudit 性能/进度契约和 ModelCatalog 稳定错误码/双语映射；不启动游戏、不读写真实存档、不部署、不切换默认入口、不删除 facade、不修改发布结构、程序集身份、SubModule.xml、SyncData key/type 或构建脚本。owner 为 Foundation/Bridge runtime、Phase8/Tools 与 ModelCatalog adapter/既有 UI owners。阶段 7 保持 `VERIFY`，阶段 8 保持 `BLOCKED`，本次本地提交已 push。实现提交：`552d8b9`、`f1a17f7`、`4feac3c`、`4a8e929`、`8f12298`、`ab6ce72`；意图记录提交：`01e7bc1`。

- `BRIDGE-CONFIG-20260903` VERIFY（本轮离线收尾已完成；历史记录中的普通 push 授权未在当前工作树执行）：承接 checkpoint `13e21560` 的 Bridge 运行时接线，已完成显式依赖 Production replay、全量离线回归、文档同步和最终安全审查；当前为 `10 wired / 6 declared-only`。owner 为 Foundation/Bridge runtime 与各已接线领域；本轮不启动实机、不读取或写入真实存档、不部署、不切默认入口、不删除 facade、不修改一键编译/覆盖脚本，也不把 offline/compiled 证据提升为 LIVE/SAVE。已审阅路径包括 `Refactor/Runtime/FeatureBridgeRuntime.cs`、已接线 adapters/behaviors、`AnimusForge/ModuleData/FeatureBridges.json`、`docs/phase8/bridge-binding-manifest.json`、`tools/BridgeBindingContractTests/` 及相关总纲/阶段8/handoff；验证包含 Bridge validator/unit、Production/compiled suites、双 API Debug/Release/Bootstrap Stage、git diff/凭据/产物审查。阶段 7 总体仍 `VERIFY`，阶段 8 执行仍 `BLOCKED`。

- `PUBLISH-20260902` DONE（仅指自动化关闭与GitHub制作组交接，不代表阶段7/8 DONE）：用户明确要求关闭自动化并把当前重构分支、HANDOFF和制作组简报推送GitHub。`af-7-8`与旧`af`均为PAUSED；收尾编写前工作树clean，本地HEAD `19e5d6b1`，`origin/refactor/prepare-af-restructure`为`9566bf3b`，ahead 19 / behind 0。最终发布只增加交接文档，通过普通fast-forward push同步到同一远端分支，完成后要求本地HEAD与远端分支完全一致；禁止force push、部署、覆盖游戏、切default或把离线证据提升为LIVE/SAVE。本轮发布总交接为`docs/handoffs/2026-09-02-github-publish-and-team-handoff.md`，制作组短文为`docs/handoffs/2026-09-02-stage7-stage8-team-brief.md`。

- `LOCAL-7-C3` VERIFY（2026-09-02，LiveHostReadinessAudit explicit-root portability 已完成）：`--game-root` 改为显式必填，删除 F 盘默认路径，保留 repo-derived `--project-root`，新增纯 fixture/CLI 契约；C3 测试 4/4、Python 编译与 `git diff --check` PASS。工具仍只读，不启动游戏、不部署、不读取存档，真实 LIVE/SAVE 仍 NOT-RUN。

- `PERSISTENCE-OFFLINE-20260902` VERIFY（2026-09-02，Persistence/Profile/Identity scanner/catalog 收尾）：排除 `.tmp`、artifacts、缓存和依赖输出，支持跨 partial 文件解析唯一字符串常量，catalog 同步 44 个 flattened key；Persistence/Profile/Config `95 literal / 121 typed / 8 types / 3 profiles / 44 flattened` PASS，Identity `sync=99 / behavior=35 / module=AnimusForge / bootstrap=1` PASS。不改生产 SyncData/key/type、程序集或部署流程，真实旧档仍 NOT-RUN。

- `DEBUG-DEPLOY-20260902` VERIFY（2026-09-02，用户授权的 Debug 测试编译与统一模块部署）：来源为本地 `F:\AnimusForge-main` 的 `refactor/prepare-af-restructure`、HEAD `109835cd18fee09ebd591fa254f0af1aa913acb4`，不是 `main`；统一脚本构建 1.3/1.4/Bootstrap 均 0 warning / 0 error，并事务替换 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`。部署时 Bootstrap/1.3/1.4 哈希为 `BF57E46CF3C095FB3205DBA4A7428339A1C574BC30B3B8DE882822E4ACC2AAE9` / `5F66A4932AB1948BBB71D38C80C6AADC63AD3F5F508004B1F2469FB13544E970` / `D28931E9129E3E6F441BC5297466BA99FC886BD9DD15A5C3484B7EFCF598D16C`，部署当时项目 Stage 与安装完全一致，合并 PlayerExports 4753 个。随后重建当前 Debug Stage 的实现哈希为 1.3 `BB157A03F97F606158203E3A68F53AEC7687F6BFD5850728760446285CFC2ABE`、1.4 `F43DFD482596BA58501A48723225CF6999E3C2143B0E7029B4363410ED6A5376`；安装目录仍为部署时实现哈希，只有 Bootstrap 仍相同。readiness 的 `installedMatchesStage` 只比较 Bootstrap，实机前须重新部署当前 Stage。未启动游戏、未读写存档，Release/默认切换/最终发布仍 BLOCKED。

- `RELEASE-OFFLINE-20260902` VERIFY（2026-09-02，Release Stage/Production Duel/ZIP 离线闭环）：统一脚本重建 1.3/1.4/Bootstrap Stage 均 0 warning / 0 error；Production Duel Release replay `35/35 PASS`、1.3/1.4 parity PASS；ZIP `F:\AnimusForge-main\.tmp\packages\release-final-20260902\AnimusForge_v1.3.7.2_20260902_100952_233.zip` 共 4919 entries，SHA-256 `1215A88666E6FCCD949BE413C75719B2C96BCA061546FCAD86DB9AB0F805ACE5`，Bootstrap-only XML/双实现 marker/hash/ONNX 与旧模块排除均通过。未安装 Release、未启动游戏、未读写存档，真实 Release/LIVE/SAVE/默认切换/最终发布仍 BLOCKED。

- `BRIDGE-CONFIG-20260902` VERIFY（2026-09-02历史快照，功能 Bridge 配置与安全接线）：新增 `docs/phase8/bridge-binding-manifest.json` 与 `tools/BridgeBindingContractTests/`，逐路径/逐 symbol 对齐 canonical 16 组 Bridge，当日快照为 `3 wired / 13 declared-only`。仅在当时已审阅的 `AfGcczShoutBridge.IsActive`、`WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved`、`SceneActionsIntegrationBoundary.InitializeRuntime` 三个一次性/事件边界接入 `FeatureBridgeRuntime` Gate；禁用/失败保留原版或各自 owner fallback，不新增 Tick 全量扫描、存档字段或 live 对象跨边界。该快照不代表当前接线或 LIVE/SAVE。

- `BRIDGE-CONFIG-20260903` VERIFY（2026-09-03，Bridge 配置与安全接线离线收尾）：在既有边界上接入 `conversation-gateway`、`conversation-action`、`action-memory`、`action-economy`、`policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、`memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration` 共 10 组 source-bound Gate；`bootstrap-host`、`host-runtime`、`runtime-game-adapter`、`persistence-domain-owners`、`scene-duel`、`tools-content-release` 保持 `declared-only`。同时修正配置缺失/损坏时的 fail-closed 诊断、拒绝非规范大小写 ID，保持 NoOp/native/owner fallback、无 Tick 扫描、无新增 save key/type 和无 live 对象跨边界。Bridge validator `16 bindings / 10 wired / 6 declared-only / configEnabled=10`、相关纯测试与双 API Debug/Release/Bootstrap Stage 均 PASS；真实 Campaign/Mission、LIVE/SAVE、默认切换和发布仍 NOT-RUN/BLOCKED。

- `LOCAL-7-C2` VERIFY（2026-09-02，ShoutNetwork SSE replay dependency closure 已完成）：基线 `8bf0c1e4`，意图checkpoint `28ad96f2`，实现 `ae49e3c8`，远端仍为 `9566bf3b`。红契约证明 `ShoutNetworkSseReplayTests.csproj` 缺shared Import且仍硬编码 `F:\SteamLibrary` / `Modules\**`复制；现已完整删除local copy target，只导入既有 `BannerlordReplayDependencies.targets`。新增source-only五consumer契约，要求net8.0、exact shared Import并拒绝project-local target、机器绝对盘符、Modules/Workshop递归和AnimusForge implementation复制；README登记第五consumer与安全复现路径。未改`Program.cs`、shared helper、生产C#、Stage、官方脚本或游戏。source contract 5/5、原helper 9/9、缺property fail-closed、Shout Debug/Release runner六类SSE业务断言均PASS；两份78项依赖manifest hash一致，绑定M2 fresh Debug 1.4 Stage `D806B988...` / MVID `337e6131-fc69-4f31-a86b-ced3e1a65acd`，终审P0=0/P1=0。Release只证明Release runner加载同一Debug AF Stage；helper不会拒绝所有未选中stale DLL，故未来机器必须先审计并可逆隔离旧runner bin/obj，本轮迁移前两目录均不存在。证据在`.tmp/validation/shout-sse-dependency-c2-final-20260902-031458`。这是tool-only离线闭环，不产生LIVE/SAVE证据，阶段7仍VERIFY、阶段8执行仍BLOCKED。

- `LOCAL-7-M2` VERIFY（2026-09-02，Duel exact detached dispatch provenance 已实现并完成离线/compiled 验证）：基线 `3522dc3e`，意图 checkpoint `17f617a5`，实现 `b93f93df`。新增 internal `IRequestBoundActionPlanExecutor` 与 data-only `DetachedDuelDispatchContext`，在 stale gate 和 commit reservation 后、任何 Economy/legacy 副作用前重新计算并校验 canonical request/action fingerprint，Queue 唯一 DuelId，再把同一 context 精确转移到 meeting pending、arena/local、wilderness 与 conversation-exit delayed host。显式状态为 Rejected、Queued、Started、UnknownAfterStart；它们终结本次Interaction commit，Queued/Started/Unknown均non-retryable，但底层owner仍可沿同一DuelId记录后续session/outcome，绝不回填原commit。Native/Scene 启用 exact owner；Courier 没有真实 `PrepareDuel` owner，防御性 exact Rejected；`Duel+Mood` companion 保留，但任何可能已发生的 Mood effect 只报 Unknown，不伪造 NoConfirmedEffect。queue 先于 Economy/gameplay，Economy失败或异常释放 queued Duel；host 只在 holder publication 后标记 accepted，delayed consumer 必须同时满足 Queued+HostAccepted；load 清理 pending trigger/runtime/UI/menu/queue 并把 active receipt 转 Unknown，不重放 Mission、stake、death、Economy 或 Memory。实际结算的三条路径必须先成功写入同一 result receipt，才能继续 Memory/renown/stake/death；outcome owner为64 active/512 retained，host另以4096 process-lifetime exact-ID seen tombstone阻止rollover复活并在满容量时fail-closed。public `IActionPlanExecutor`、原 executor 构造器、Duel public ABI、默认入口、`_duelCooldowns` key/type、Saveable ID、Fourberie optional seam 与 M1 legacy-unbound路径均未改。Duel Dispatch 16/16、Duel Outcome 18/18、Interaction/Host/receipt/Economy/Persistence/Profile/Migration/Identity、Production Duel Debug/Release 35/35 与六项 Stage 均 PASS，独立终审 P0=0/P1=0；证据在 `.tmp/validation/duel-dispatch-m2-final-20260902-021935`。真实 Campaign/Mission、旧档、live death/stake/Economy/AFEF/Fourberie 与默认切换仍 NOT-RUN，因此阶段7保持 VERIFY、阶段8执行保持 BLOCKED。

- `LOCAL-7-M1` VERIFY（2026-09-01，Duel actual-session typed owner/outcome/readback 已实现并完成离线/compiled 验证）：基线 `9955658b`，意图 checkpoint `fc3cd722`，实现 `16f3cbef`，远端仍为 `9566bf3b`。新增 process-local、bounded、NOT-RECOVERABLE 的 `DuelOutcomeOwner`；meeting、arena/local、wilderness 三条实际开始/终态路径均接入 typed owner，成功绑定的 session 在 Memory、renown、stake、death、UI 前先锁定 `ResultIdentity`，再以 Confirmed/Partial/AttemptedUnconfirmed/Unknown 分量收尾；reserve失败只保留legacy玩法并fail-closed为无typed readback。stake/debt/after-lines 现在只接受同一回复中的精确 `[ACTION:DUEL]`，一次绑定实际 `DuelId`，失败或终态清理，禁止跨 Duel 泄漏。detached legacy Duel callback 不再伪报玩法成功，而返回 terminal `UnknownAfterStart` / `duel.outcome_pending`，保留已确认 Economy subset 且禁止 fallback/replay。legacy路径继续使用明确的 `Domain / legacy-unbound` 合成 provenance，不能冒充某个 ActionPlan request 的因果证明；exact detached binding已由后续M2 `b93f93df`独立补齐，未修改该兼容语义。legacy public void ABI、`_duelCooldowns : Dictionary<string,float>`、Fourberie optional guard、默认入口和所有存档 identity 均未改；Duel contract 16/16、Interaction/Host/receipt、fresh 1.3/1.4 production replay 32×Debug/Release、Persistence/Profile/Migration/Identity 与 Debug/Release 六 Stage 均 PASS。真实 Campaign/Mission、旧档、live death/stake/Economy/AFEF/Fourberie 与默认切换仍 NOT-RUN；阶段7保持 VERIFY，阶段8执行保持 BLOCKED。

- `LOCAL-8-A` VERIFY（2026-09-01，阶段8非破坏性完整领域准备已实现）：基线/已推送远端 `9566bf3b`，意图checkpoint `9a088f2f`，实现链 `b1c5a81a`→`1e341c43`→`f4a02018`→`6b1d16f1`→`8bdd9363`。`full-domain-readiness-catalog.json`以canonical 20个**验收责任桶**（不是20个物理DLL）记录英文ID、role owner、代表性真实入口、Prompt/ActionPlan适用性、存档/fallback/default/current evidence、blocking gates和canonical 16组Bridge；早期8-ID design catalog与Pending entry type保留。20个maintainer均为`ROLE_PLACEHOLDER`、entry coverage均`REPRESENTATIVE`，团队确认`ASSIGNED`且补齐`COMPLETE`前real readiness必定BLOCKED。16 Bridge区分13组`PAIR`与3组`CROSS_CUT`，证据必须显式`bridgeIds`，并在OFFLINE、LIVE 1.3/1.4、SAVE 1.3/1.4覆盖对应case。`cleanup-candidates.json`逐真实symbol登记12 KEEP/3 HOLD/3 REVIEW_REMOVAL；工具验证symbol存在、同文件按candidate ID独立，audit/replacement/rollback evidence都必须绑定candidate+owner domain，candidate/global/inventory checkpoint一致且严格早于HEAD。本轮零删除。红基线证明旧49-record fixture只覆盖8-ID仍可绿；最终62个纯fixture测试、Bridge10/6、Composition18/24、ModuleCatalog8/3/16/8与all-missing full-20 BLOCKED均PASS。未改生产C#、key/type、默认入口、GCCZ/NEW-10/游戏，六Stage按纯工具切片规则N/A；真实Campaign/Mission、旧档、live Economy/AFEF/Notoriety及发布仍NOT-RUN，阶段7保持VERIFY、阶段8执行保持BLOCKED。

- `LOCAL-7-L` VERIFY（2026-09-01，Notoriety exact line/session outcome代码与离线验证完成）：基线 `68dce8e9`，意图 checkpoint `cddc7628`，实现 `80729cb9`。审计确认旧 owner 的 read路径可提前roll、active只按Hero且不入档、void line/finalize吞错、finalize先删active再写aggregate。现只为拥有 H recoveryId/payloadHash/part + memory session identity 的 detached line建立 `AFNR1` exact owner：duplicate line在任何roll前命中；read roll只冻结到active，首个实际line owner commit再把known-state与witness同存既有 `_af_player_notoriety_state_v1` JSON；session finalize冻结绝对 sessions/bonus/day target，readback后Applied。不同exact session先收尾旧session，迟到旧finalize不能消费新session；legacy/exact混用将L receipt终止为Unknown并回旧语义；零line prompt roll不再伪造完成session。loaded Open→Unknown且保留line tombstone，Confirmed只重放绝对data target，绝不重roll/重finalize。legacy void ABI、默认route、H/I/K wire与95 literal key/type均不变。AFNR1 contract 14/14、Interaction 40/69/39、Memory/Courier/Economy、fresh Production OptIn/三Host、Profile 95/121/42/40、Migration10、Identity99/35与Debug/Release六Stage均PASS；真实Campaign、MBRandom、save/load/crash、旧档与default仍NOT-RUN，故阶段7仍VERIFY。

- `LOCAL-7-K` VERIFY（2026-09-01 自动接续，weekly exact-intent/outcome owner 代码/离线验证完成）：基线 `da15241f`，意图 checkpoint `7cdf6435`，实现 `765b2386`。首版只接受 **Economy-only、whole ActionPlan、owner full Applied + full count + ConfirmedEffect + exact actual fingerprint + memory HistoryWritten**；canonical sidecar projection不调用可注入 gameplay planner，actual planner 每次 commit恰好调用一次。独立 `AFWM1` data-only ledger绑定 request/trace/channel/session/subject/runtime/save/Courier direction/turn/action/candidate/payload hashes，64 pending / 512 terminal；有效 journal的 durable identity probe先于 live payload重建，防止已 Applied/Confirmed 请求因债务消失或 foothold变化绕过 Duplicate/Conflict。只有 Confirmed可做 data-only Daily trigger attach；Prepared load转 Unknown，partial/unknown/rejected/mixed/legacy/不支持估值一律不发布。新增 symbolic `_af_weeklyActionOutcomeReceipts_v1 : Dictionary<string,string>`，不改 95 个 literal key、H seed/hash/wire、Courier `AFCI1`、public ABI或默认入口；坏 journal原样保留并禁用该 sidecar，人工修复前不提供K的跨重启防重。focused、8项 production/compiled回放、Profile/Migration/Identity 与 Debug/Release 六 Stage PASS，独立终审 P0=0/P1=0；证据在 `.tmp/validation/weekly-outcome-k-final-20260901-163221`。compiled/fixture不等于真实 Campaign/save/live Economy/AFEF，故阶段 7仍 VERIFY、default cutover仍 BLOCKED。

- `LOCAL-7-J` VERIFY（2026-09-01 自动接续，memory auxiliary recovery 边界代码/离线验证完成）：基线 `d2f37a8a`，意图 checkpoint `3436d739`，实现 `84e92f80`。已确认 legacy live 链为 `AppendExternalDialogueHistory → AppendDialogueHistory → AppendDialogueHistoryById → AppendDailyMemoryLineById`，而 detached facade 直接进入 H owner。H 的 Daily writer 现只发布 Daily marker/projection，不再读取、附着或删除无 request/outcome 身份的 pending weekly candidate，也不在 tick/load/`ExistingPending` 重放非幂等 notoriety。只有 brand-new `Began` 在同一次调用内完成全部 core receipt，且 user/assistant 的 Daily marker 精确匹配 recovery ID + payload hash + part 时，才各执行一次 current-runtime `NoteConversationLineForExternal` best-effort；异常与 core Completed 隔离，结果仍为 `attempted_unconfirmed / NOT-RECOVERABLE`。legacy live `Attach→Save→Note` 未改；H schema/seed/hash/wire、I `AFCI1`、SyncData key/type 均未改。focused/production/Profile/Identity 与 Debug/Release 六 Stage PASS，production 断言是 compiled-DLL reflection/IL 结构守卫，不是 live notoriety mutation/fault/save-load 证明；日志在 `.tmp/validation/memory-aux-boundary-20260901-141201-final`。既有 P1 仍在：legacy weekly candidate 早于 action outcome、无 turn/request 绑定且可能跨轮；detached 尚无 weekly owner。故阶段 7/default cutover 继续 BLOCKED。

- `LOCAL-7-I` VERIFY（2026-09-01 自动接续，Courier inbound completion 代码/离线验证完成）：基线 `0e276ce1`，意图 checkpoint `b5395164`，实现 `de3220b7`。Courier 在 memory owner 开始前把 `AFCI1` receipt 存入既有 `_af_courier_sessions_v1` 的 session JSON；receipt 绑定 opaque recovery ID、owner payload hash、session/sender/current-player/party 和冻结 visible letter，full-wire checksum 与 32,768 字符上限 fail-closed。MyBehavior 只公开 internal recovery identity/status seam，不调用 Courier；Courier tick 轮转且每 tick 最多处理一条，只有 owner payload-matched Completed/Applied/Duplicate 才补三字段并推进原状态机。Missing/Disabled/Quarantined/PayloadMismatch、坏 wire、pre-owner/无 receipt commit 均终止该 inbound session并释放等待暂停，不重放 ActionPlan/Economy/postprocess，也不复用 `PostprocessConsumed`。focused/production/profile/identity 与 Debug/Release 六 Stage PASS，独立只读终审 P0=0/P1=0；日志在 `.tmp/validation/courier-inbound-completion-20260901-124752`。默认 inbound 仍是 legacy、真实 Campaign/save/load/AFEF 仍 NOT-RUN，故阶段 7 不标 DONE。

- `LOCAL-7-H` VERIFY（2026-09-01 自动接续，memory-only 持久恢复代码/离线验证完成）：基线 `a8001b87`，意图 checkpoint `6f8d8cc0`，实现提交 `f6e5e694`；首次 fetch 曾因 `schannel: failed to receive handshake, SSL/TLS connection failed` 失败，收尾重试成功，远端仍为 `fc8c344e`、本地 ahead 25。MyBehavior 新增唯一 symbolic `_af_interactionMemoryRecovery_v1 : Dictionary<string,string>`，64 pending / 512 completed / 64 quarantine；opaque id、full-wire checksum、payload hash、process nonce、最多六个 Daily/Recent 步骤、单步骤五次失败后隔离、跨日 sealed draft、Scene provenance、non-Hero retarget/destroy 均 fail-closed。payload/tick 不含 ActionPlan/postprocess/executor/afterCommit，cache 不能绕过 ledger；旧六参/四 void ABI、95 literal/121 typed、99 identity/35 behavior、程序集拓扑与默认入口不变。本轮所列 focused suites 全 PASS；Debug/Release 六项 Stage 0 warning/error，独立只读复核未报 P0。真实 Campaign/AFEF/旧档仍 NOT-RUN；Courier inbound session completion 是独立 `afterCommit` P1，详见 `docs/handoffs/2026-09-01-memory-only-recovery.md`。

- `LOCAL-7-G` VERIFY（2026-09-01 自动接续，structured unknown 代码/离线验证完成）：基线 `c2a2be96`，意图 checkpoint `899effbb`，实现提交 `d765270a`；fetch 后远端仍 `fc8c344e`。尾增 `UnknownAfterStart=6` 和 additive effect receipt，保留旧 enum 0–5、outcome interface、executor 六参构造器与 `InteractionCommitResult` 唯一公开四参构造器。Port 将 callback throw/null/非法回执归为 fact-free unknown；Hero/Party/Merchant 以显式 mutation observation 贯穿物品/RP、固定资产、装备恢复队列/rollback 吞错路径，停止后续 action、保留此前已确认 count/facts。Executor/Committer/Host/cache/duplicate/in-progress 全部终态不重放；count0 不伪报 `ActionsExecuted`，dispatcher 不能用 fake success 覆盖 owner 回执，callback 未启动仍可安全 fallback。Port unknown 8、Economy-aware unknown 3/receipt 4、Host 69（三渠道）、request receipt 39、Production Economy/owner/三 Host/OptIn、95-key/121-binding profile、99-sync/35-behavior identity 与 Debug/Release 六项 Stage 全部 PASS；独立终审无 P0/P1。真实 live mutator fault、Campaign/Mission、AFEF、旧档仍 NOT-RUN；不含补偿、memory-only/afterCommit recovery 或 durable tombstone。完整证据见 `docs/handoffs/2026-09-01-unknown-action-effects.md`。

- `LOCAL-7-F` VERIFY（2026-09-01 自动接续，known-partial 代码/离线验证完成）：基线 `67603f18`，意图 checkpoint `7186048d`，实现提交 `8f22d737`；fetch 后远端仍 `fc8c344e`。Hero/Party/Merchant owner 用尾增 enum `PartiallyApplied` 返回短计数和真实 facts；旧 `Applied + short count` 在 port 兼容归一化。Additive outcome interface 不破坏旧 receipt/构造器；executor 对 known partial 或 Economy 后 legacy reject/throw 返回 `NonRetryableFailure`，只保留 Economy owner count/facts。Committer 只写 outcome facts，返回 `ActionsExecuted=true`；memory 失败仍终态，duplicate 不再执行 Economy/memory，Host 不 fallback/afterCommit。红测复现 facts/count 丢失；Economy-aware partial 4 + partial receipt 4、Port partial normalization 4 + enum ABI、Host 51（三渠道）、Production partial 2、相关 production/Interaction/Persistence 和 Debug/Release 六项 Stage 全部 PASS。真实 live Economy/AFEF/save 仍 NOT-RUN；`UnknownAfterStart`、durable memory-only/afterCommit recovery 未闭合。完整证据见 `docs/handoffs/2026-09-01-partial-economy-outcomes.md`。

- `LOCAL-7-E` VERIFY（2026-09-01 自动接续，opt-in owner 代码/离线验证完成）：基线 `3d9778d2`，意图 checkpoint `bbe35aa8`，实现提交 `b2542fdd`；fetch 后远端仍 `fc8c344e`。已证明 economy-only 在旧 executor 中直接 `Executed`、不调 Courier owner；现于任何 Economy Replay 前调用可选 channel gate。Courier gate 重解析 active session/recipient，验证 channel/session/subject、outbound、delivery、terminal/consumed；mixed 仅 prevalidate，economy-only 先置既有 JSON 字段 `PostprocessConsumed`。不新增 key/type/field，不从 raw 推导 visible reply，保留旧六参构造器二进制签名。Gate 五类顺序/失败 contract、production session fixture 18 assertions、Production Economy/Courier/Configured、Interaction 40+48+4+38、Economy port、95-key/121-binding profile 和 99-sync/35-behavior identity 均 PASS；Debug/Release 的 1.3/1.4/Bootstrap 全部 0 warning / 0 error。当前 detached Courier 仍无 production caller/default cutover；真实 save/load、live asset/AFEF NOT-RUN，因此只标 VERIFY。完整证据见 `docs/handoffs/2026-09-01-courier-economy-reservation.md`。

- 最新本机owner实现提交：Memory runtime receipt `5d3dc5f0`、Courier reservation `b2542fdd`、known partial Economy `8f22d737`、unknown effect `d765270a`、durable memory-only recovery `f6e5e694`、Courier inbound durable completion `de3220b7`、memory auxiliary boundary `84e92f80`、weekly exact owner `765b2386`、Notoriety exact owner `80729cb9`、Duel actual-session outcome owner `16f3cbef`、Duel exact detached dispatch provenance `b93f93df`；测试依赖闭环含第五个Shout SSE consumer到 `ae49e3c8`，阶段8完整领域门禁到 `8bdd9363`。`LOCAL-8-A`只完成准备态；`LOCAL-7-C3` 与 Persistence/Profile/Identity 离线收尾已完成。真实测试人员可并行按20领域包采集Duel及其他领域的LIVE/SAVE证据；真实Host/旧档/live Economy/AFEF前仍不得删除facade、切默认或发布。

- `LOCAL-7-D` VERIFY（2026-09-01 自动接续，batch runtime 代码/离线验证完成）：基线 `9b8cb509`，意图 checkpoint `3fe3f656`；fetch 后远端仍 `fc8c344e`。已复现旧生产 DLL 在缺 Campaign 时虚假 Applied；现由 MyBehavior 原写入实现返回 daily/recent 原始 owner 状态确认，batch facade 只在确认后缓存。保留四个公开 void API、原写入顺序、session/260 行窗口和 SyncData key/type；结果不代表原子事务/落盘。新生产缺 Campaign 7、receipt 7、线程 guard fixture 2、void 签名 4，以及 raw-owner 11 assertions PASS；Interaction 40+48+4+38、三类生产 Host、两类 Economy contract PASS；Debug/Release 1.3/1.4/Bootstrap 全部 0 warning / 0 error；Persistence/Profile 和 identity（99 sync / 35 behavior）PASS。仅校正两条 SyncData fixture 导航行号，未放宽断言。独立审查无阻断；日志在本工作区 `.tmp/validation/memory-owner-20260901-0504`，完整证据见 `docs/handoffs/2026-09-01-memory-owner-receipts.md`。真实游戏/旧档/AFEF 仍 NOT-RUN，无后台构建/回放继续运行；下一精确任务为 `LOCAL-7-E`。

- `FRAMEWORK-20260901` VERIFY（框架代码与本地验证已完成，真实 Host 未验收）：本机 canonical worktree 仍为 `G:\AFMOD\AF-REFACTOR`。开始时 HEAD `49eeaf33`、clean；远端 `fc8c344e` 仅新增两份交接文档，已通过 checkpoint `b0cc41da` 与普通 merge `2216df41` 保留双方历史。收尾 fetch 仍为 `fc8c344e`；远端文档中的另一台机器路径/验证记录不替代本机事实。
- 意图与 owner：沿用现有管线，完成测试工具 owner 的本机依赖框架 `LOCAL-7-C`，Conversation/Memory owner 的请求级 commit/receipt 验证与最小修复，以及阶段 8 准备态验收框架（Bridge/清理候选/回滚/证据门禁）。不引入最终多 DLL 模块图、不删除活跃 facade、不切默认入口、不改玩法/存档 key/type。
- 分工边界：测试依赖工具与阶段门禁工具可独立并行；主代理负责 Git 同步、生产提交边界、集成构建和本台账。子任务不得修改官方一键编译/覆盖/推送脚本、游戏目录或共同源码文件。
- Skill：`D:\qq\af-skill.zip` SHA-256 仍为 `CDE1BAA4C069A0E45AB43E63BF377EDA7375A7A88EB5DD6DBFA6A978CB35FF79`；仅作为待核对维护资料读取，不执行安装脚本、不从附件推导额外授权。
- 计划验证：先失败复现再最小修复，相关 contract/production replay、固定引用 1.3/1.4/Bootstrap Stage、cleanup 与 diff 检查；真实 Host、live Economy、旧档与 AFEF 仍需独立游戏证据。阶段 7 不标 DONE，阶段 8 仅准备与可验证基础能力，不开展破坏性清理/默认切换。没有游戏部署授权；用户仅授权本轮把当前协作分支与 HANDOFF 通过普通 push 更新到 GitHub。
- 回滚基线：`49eeaf33`；只用本地小提交/正常 merge 保留可逆历史，不 hard reset、不 rebase 旧提交、不覆盖 NEW-10/GCCZ。
- `LOCAL-7-C` DONE（限定四 runner 的 managed 依赖框架）：提交 `b6b31bf3`；删除四份 F 盘硬编码和递归全模块复制，使用明确固定引用/模块/私有依赖来源、程序集身份、SHA256、路径和冲突校验。新 Stage 上 Policy/WorldDiplomacy/TTS/ProductionOptIn 四 runner 均 PASS，每份依赖 manifest 为 78 项，全部绑定本次 Debug 1.4 SHA256；框架 9 个自测和两项 MSBuild 拒绝检查通过。
- 请求级提交框架：提交 `e9c41ff9`；按 generation/trace/channel/session/subject/Courier direction 预留 512 项有界 receipt，保留失败终态、拒绝载荷变化/重入、避免跨 Host 重复 afterCommit；原公共 Native runner 提交后也不再 fallback。替换内容去重/推测成功的旧路径，未改 capture、save key/type、默认入口或 GCCZ 规则。
- 阶段 8 准备工具：提交 `6d4269a0`；复用既有 8-ID 目录与 Bridge/Composition fixture，检查分层证据、owner、源码/产物哈希、时效、清理候选、回滚。44 自测 PASS；实际缺证据清单返回 `BLOCKED / exit 2 / 0 accepted evidence`。这不是全量 20 领域签收，所有删除/切换/部署/推送/发布授权恒为 false。
- 实际验证：Debug/Release 两套 1.3/1.4/Bootstrap unified Stage 全部 0 warning / 0 error；原 Interaction 40 + Host 48 + Native callback 4 + request receipt 38 cases PASS；生产 1.4 的 12 个提交后故障与 6 个重建 committer 用例 PASS；其他本轮生产/Economy/Gateway 回放、Persistence/Profile 与 identity 审计通过。日志在 `G:\AFMOD\.build-cache\af-framework-20260901`，详见 `docs/handoffs/2026-09-01-framework-continuation.md`。
- 当前明确未完成：`LOCAL-7-D/E/F/G/H/I/J/K/L` 均为代码与离线证据 VERIFY，live AFEF/旧档/Campaign/Economy/default 尚未验收。H 只修 core Daily/Recent memory，I 只修 Courier inbound session completion，J 隔离无证据 auxiliary recovery，K 只覆盖 Economy-only whole-plan exact outcome且只做 Confirmed data attach，L 只覆盖具备 H recovery/session identity 的 detached Notoriety line/session；均不承担 gameplay compensation，K不为 mixed/legacy/partial/unknown补写成功，L也不把 legacy line、marker或aggregate值提升为exact成功。H→I 中间存档仍无法还原旧 visible reply。真实 Host证据到位前不切默认、不删除 facade。
- 真实 Campaign/Mission、旧存档、live Economy、AFEF 与默认切换仍 NOT-RUN/VERIFY；阶段 8 仅准备工具通过、执行门禁仍 BLOCKED。以上为 2026-08-31 快照；2026-09-02 已另按用户明确授权完成一次 Debug 双版本编译与统一模块测试部署，详情见当前任务的 `DEBUG-DEPLOY-20260902` 条目；仍未启动游戏、未读写真实存档，也没有在后台继续运行构建或回放。

## 当前状态（2026-08-31 本机接续）

- 文档任务 `PROGRAM-20260831` DONE（仅指总纲编写完成，不是 AF 全量重构完成）：按用户要求新增 `docs/animusforge-complete-refactor-program-20260831.md`，覆盖 20 领域、P0–P4 接续、owner/集成/测试分工、分层验收、发布清单与回滚边界；本轮仅改两份文档。已 fetch 确认远端 `182da1db`，编写前本机 HEAD `d8c81b5e`、ahead 4、clean；两项独立只读审查和成文复核完成，领域编号/远端 SHA/引用路径/Markdown fence/`git diff --check` 均通过。未重新运行构建/回放/游戏验证，未修改生产代码、Skill、默认入口或部署状态，未推送；文中测试结果明确引用上轮证据。当前生产接续任务仍为 `LOCAL-7-C`，`LOCAL-7-A/B` 仍 VERIFY。

- 本机 canonical worktree：`F:\AnimusForge-main`；分支 `refactor/prepare-af-restructure`。
- 已 fetch 的远端基线：`182da1db4db4199cf65783f911f3cb6d46b18970`，`origin/refactor/prepare-af-restructure`；`a096c1b1` 仅作历史比较点。下面的 F 盘机器记录保留为历史，不代表本机部署或最新远端。
- `G:\AFMOD\NEW-10` 保持 `0006d45b`，`G:\AFMOD\GCCZ` 保持 `3849f6f`；接手时两者工作区干净。其他机器是否有未提交或正在进行的工作未知；本轮只在独立本地分支工作，不推送。
- 当前状态：`LOCAL-7-A` VERIFY（核心基线通过，扩展回归有 4 个环境阻塞）；`LOCAL-7-B` VERIFY（源码、双版本构建与生产 DLL 回放通过，真实 Host 尚未验收）。本线程无继续执行中的写入；阶段 7 总体验收仍未完成。
- 已完成纵切片 `LOCAL-7-B` 的代码部分：Conversation Host 提交边界；源码提交 `b24fdf4b`。沿用现有 Gateway/owner/facade，没有切换默认三渠道。
- 计划路径：本台账、`AGENTS.md` 的本地回滚/边界说明；若复现提交边界缺口，限 `Refactor/Adapters`、`Refactor/Runtime`、现有 focused runner 和 owner 文档。不改存档 key/type、程序集身份、GCCZ 规则或构建/覆盖脚本。
- 本机 SDK 为 `8.0.422`；游戏根为 `E:\steam\steamapps\common\Mount & Blade II Bannerlord`。真实 Campaign/Mission 未初始化、未获游戏部署授权；live Economy/旧存档/AFEF 验收保持 NOT-RUN。仅用纯测试和 production-DLL replay 证明相应边界。
- 回滚：源代码基线 `182da1db`；编码前建立本地 checkpoint，后续按小提交反向回滚，不改写历史、不覆盖 NEW-10、游戏、ONNX 或玩家数据。
- `LOCAL-7-A` 基线进展：checkpoint `8020112e`；Debug 1.3/1.4/Bootstrap Stage 各 0 warning / 0 error，InteractionPipeline 40 cases、Economy port/executor、Configured Gateway、PersistenceChunk、Economy owner/state fixture 和三渠道 production configured host PASS。Persistence/Profile/Config 首次 FAIL：`_patienceStates_v1` 两个 ref 的 fixture 行号为 37172/37181，当前源码为 37010/37019；121 条绑定中仅这两处行号不同，未发现 key/type/ref/source 变化。只校正导航行号，保留完整严格校验。
- `LOCAL-7-B` 原意图（已实现，VERIFY）：owner 为 Conversation host lifecycle。复现并修复 `DetachedInteractionHost.ExecuteAsync` 在 commit 已开始后因 memory failure、afterCommit/dispatch exception 或缺失返回值再调用旧 fallback 的路径；新增已有 InteractionPipeline runner 内的故障注入回归，并检查三渠道 production-DLL 路径。先写失败测试再改 host，不改变经济玩法、save、标签或默认入口。真实 Host/AFEF 仍 NOT-RUN。
- `LOCAL-7-A` 核心基线 PASS：校正 fixture 后 Persistence/Profile/Config PASS（95 keys / 121 bindings / 8 types）；Production configured host、Economy-aware commit、Hero/Party/Merchant owner factory replay PASS。readiness 为 PASS，但 `gameRunning=false`、`installedMatchesStage=false`（该字段只比较 Bootstrap），不代表游戏已验收。固定引用：1.3 `v1.3.15.110062`，1.4 `v1.4.6.115628`；本机游戏 `v1.4.7.117484`。
- `LOCAL-7-B` 红绿测试：旧 source-linked host 的 memory/commit/dispatch/late-callback/cancel 用例失败；旧 staged 1.4 DLL 实测 `NativeConversation/memory_throw` 错走 fallback 并返回 Succeeded（`production-configured-boundary-red.log`）。修复源码后原 40-case suite + 新 48-case matrix PASS；重建 Debug 1.3/1.4/Bootstrap 各 0 warning / 0 error，新 staged 1.4 的三渠道 12 个提交后故障回放 PASS。保护范围为一次 `ExecuteAsync` 的提交回调，不承诺跨请求或跨存档的经济事务 exactly-once。
- 扩展回归执行失败（环境加载，不能记为 PASS）：PolicyGateway / WorldDiplomacyGateway 缺 `MCMv5, Version=5.12.3.0`；TtsGateway / ProductionOptInEntry 缺 `TaleWorlds.CampaignSystem`。四个 runner 的 `.csproj` 仍从 F 盘复制依赖；本轮未改这些工具或官方脚本。其余已执行的 Gateway/生产回放/十项 Python 审计结果见本机 handoff。
- 下一精确任务 `LOCAL-7-C` TODO：在测试工具 owner 范围内审查这四个 runner 的依赖复制，改为显式本机路径/固定版本引用并验证闭包，重跑失败项；不修改官方一键构建流程、不把游戏依赖打入客户端 Stage。随后审查跨请求经济 receipt/重复记忆提交和真实 Host/旧档证据。
- 完整命令、构建哈希、清理说明与回滚：`docs/handoffs/2026-08-31-local-refactor-commit-boundary.md`；本机日志 `G:\AFMOD\.build-cache\af-refactor-20260831`。本轮只修改 AF 基础提交边界与测试/文档，未改 GCCZ 核心/桥接，不需复制 AF 主体到 GCCZ；未推送、未部署、未安装全局 Skill。

## 历史状态（另一台机器，2026-08-30）

- 项目：Mount & Blade II: Bannerlord AnimusForge mod
- canonical worktree：`F:\AF测试重构`
- 当前分支：`refactor/prepare-af-restructure`（原重构仓库分支；本地项目目录为 `F:\AF测试重构`）
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`
- 基线父提交：`96a1c60f1877813a9fb3440ddad068d6e92afa1e`（policy 功能基线）
- 当前工作 HEAD：`b1ce1d2b`（`test: align live host readiness deployment state`；本地已提交，当前领先 origin 3 个提交，推送仍因 GitHub HTTPS 443 连接重置失败）
- 当前阶段：阶段 7 ACTIVE，进入真实 Host 验收准备；阶段 4/5/6 契约、生产回放和 Economy owner/state fixture 保持通过（阶段 1 清理 HOLD；阶段 3 设计已完成；阶段 0 基线详细记录按用户决定跳过）
- 当前任务：在真实初始化 Campaign/Mission Host 可用时，验证 live Economy、三渠道主线程 commit、confirmed facts、旧存档和 AFEF；纯 contract、生产 1.4 回放和状态 fixture 已完成
- 当前负责人：Codex 重构会话
- 物理程序集策略：暂不拆分为多个玩法 DLL；先在单一 `AnimusForge.dll` 内完成逻辑模块化
- 旧存档目标：必须兼容；至少保持现有程序集身份、序列化类型和 SyncData key，必要变更必须提供迁移与证据
- 游戏基线策略：保留可复现的测试记录，但不要求现在由用户立刻完成全量手测；优先记录关键功能和重构前后对比结果
- BannerlordRoot：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord`
- 已安装模块目录：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`
- 主要游戏内测试版本：Bannerlord `v1.4.8.119303`（本机当前安装）
- 1.4 构建引用要求：按 `1.4.x` API 线管理；每个开发者可以使用自己的合法 1.4.x 安装，但构建记录必须写明精确 `BuildInfo`，共享验收使用固定代表性 overlay
- 最后更新：2026-08-30（live host readiness、Git 与本地化审查）
- 状态：IN PROGRESS
- 最近验证：LiveHostReadiness 审计 PASS（project stage、安装模块、Bootstrap-only、1.3/1.4 stage 均存在且匹配）；此前 Economy-aware executor、owner/state fixture、Production 1.4 commit、World Diplomacy intent-boundary、三渠道 host 和 Gateway 回放均 PASS；1.3/1.4/Bootstrap Debug unified stage 均 `0 warning / 0 error`。真实 Campaign/Mission 仍未验收。
- 依赖记录：实际外部模块路径已解析；当前机器游戏 BuildInfo 为 `v1.4.8.119303`，可复现 1.4 overlay 为 `v1.4.6.115628`。
- 阶段 1 阻塞：用户已决定先保持仓库现状；1.3.x/1.4.x 游戏源码参考仓库保留在 tracked reference plane，其他未决对象也不做清理，许可证/第三方 provenance 继续作为待确认项。

## 重要工作区事实

- 工作区在准备开始时并非完全干净：`AnimusForge/SubModule.xml` 有用户已有修改（版本从 `v1.3.7` 变为 `v1.3.7.2`，且文件末尾换行发生变化）。本次准备不回滚、不覆盖该修改。
- 项目当前采用一个 `Modules/AnimusForge` 模块、Bootstrap 加载单一版本实现的发布契约。
- 当前主实现项目是 `AnimusForge.csproj`，Bootstrap 项目是 `AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj`。

## 阶段总览

### 阶段 0：准备与基线 — IN PROGRESS

- [x] 创建本地准备分支
- [x] 安装项目级 `animusforge-maintainer` skill（未提交）
- [x] 创建基线报告
- [x] 创建本公共重构台账
- [x] 审阅 skill、基线报告和本台账
- [x] 完成当前仓库只读结构盘点
- [x] 确认代表性存档和游戏内基线方案
- [x] 形成第一版功能—owner—依赖—风险重构地图（见 `docs/animusforge-refactor-map.md`）
- [x] 完成第一版逐文件 owner matrix（见 `docs/animusforge-owner-matrix.md`）
- [x] 记录可运行的 1.3.x、1.4.x、Bootstrap 构建结果（Debug unified stage；1.4 使用 v1.4.6.115628 overlay）

### 阶段 1：仓库边界与可重复性 — IN PROGRESS

- [x] 盘点源码、内容、测试、工具、脚本、文档、引用、依赖和产物平面（见 `docs/animusforge-repository-boundary-audit.md`）
- [x] 确认 `.gitignore` 与用户数据/生成产物边界（历史 tracked 生成物仍需后续分批处置）
- [x] 固化构建、stage、package、deploy 的现状说明（见 `docs/animusforge-repository-boundary-audit.md` 与 `README_BUILD.md`）
- [x] 建立初版仓库边界与分发决策表（见 `docs/animusforge-repository-boundary-decision-table.md`；法律/许可证确认仍未完成）
- [ ] 确认许可证、分发和第三方文件处理原则

### 阶段 2：模块目录与所有权地图 — DONE（设计完成；生产迁移未开始）

- [x] 建立现有功能 → 当前入口/文件 → 目标 owner 映射（首条根 AF 基础 LLM 对话只读切片见 `docs/animusforge-phase2-root-llm-owner-slice.md`）
- [x] 建立 `SubModule.cs` 注册/调度分组清单（只读；未改变注册顺序或运行行为）
- [x] 设计 Host/Composition registry DTO 与独立 contribution groups（只读；报告见 `docs/animusforge-phase2-registry-dto-design.md`；未接入运行时）
- [x] 建立纯 validator 输入/输出 fixture（见 `docs/animusforge-phase2-registry-validator-fixtures.md`；未实现 validator，运行频率 0）
- [x] 建立阶段 2 影响面、候选 Bridge、模块非目标与回滚入口地图（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；仅首轮高层设计）
- [x] 首轮标注存档、Prompt、标签、Harmony、Tick、UI、主线程和版本影响（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；逐文件细化已在 Conversation/Memory/Action 切片完成）
- [x] 首轮标注跨模块行为和候选 Bridge（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；contract test 已有独立 fixture/runner）
- [x] 为每个目标模块建立非目标和回滚入口模板（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；具体切片填写留待生产迁移）
- [x] 建立首轮 Conversation/Memory/Action contract 边界逐文件影响表与纯 contract test matrix（见 `docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；仅设计，测试 NOT-RUN）
- [x] 将 contract matrix 映射到真实方法/调用点并建立独立纯 fixture 目录（见 `docs/animusforge-phase2-conversation-memory-action-method-map.md` 与 `docs/fixtures/phase2-conversation-memory-action/`；YAML parser NOT-RUN）
- [x] 细化 Settlement/Siege 与 Policy/Diplomacy 候选 Bridge contract（见 `docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`；仅设计，未实现）
- [x] 为两组 Bridge fixture 建立纯 contract 验证矩阵/runner（见 `tools/BridgeFixtureContractTests/`；不接入生产 `.csproj`）

### 阶段 3：Contracts 与基础运行时 — DONE（设计完成；生产实现未开始）

- [x] 定义模块身份、能力、事件、DTO、契约版本（见 `docs/animusforge-phase3-af-contracts-design.md`；未创建生产项目）
- [x] 设计模块 manifest、profile、依赖和健康状态（见 `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`；未实现 Foundation/Registry）
- [x] 整理 Foundation、主线程调度、后台任务、诊断和 SafeMode（见 `docs/animusforge-phase3-foundation-runtime-contracts.md`；未创建生产项目）
- [x] 整理 GameAdapter 与 1.3/1.4 API 边界（见 `docs/animusforge-phase3-game-adapter-api-boundary.md`；未修改生产 helper）
- [x] 为上述 catalog/contract 建立纯 metadata runner（见 `tools/ModuleCatalogContractTests/`、`tools/AFContractsContractTests/`、`tools/FoundationRuntimeContractTests/`；不接入生产 `.csproj`）
- [x] 设计 no-op module、dependency-missing、optional-provider、SafeMode 和 failure-isolation 纯组合矩阵（见 `docs/animusforge-phase3-composition-matrix.md` 与 `docs/fixtures/phase3-composition-matrix/`；18 cases、24 invariants）
- [x] 建立 GameAdapter API boundary 纯 fixture/runner（见 `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/`；14 cases）
- [x] 进行阶段 3 最终设计审查并确认进入阶段 4（见 `docs/animusforge-phase3-final-review.md`；PASS WITH LIMITATIONS）
### 阶段 4：Persistence / Profile / Config — IN PROGRESS

- [x] 完成 95 个字面量 `SyncData` key、主要 JSON 根和 PlayerExports 分类的首轮目录；符号 key/chunk/字典类型仍待补齐
- [x] 建立持久化 namespace 与迁移目录（9 个逻辑 namespace、schema/lifecycle/owner 和 legacy-first 幂等策略 fixture；运行时迁移尚未接入）
- [ ] 保留现有程序集/类型/key 兼容性
- [x] 建立配置快照、模块开关和 profile 解析边界（仅建立不可变契约；暂不接管 DuelSettings/MCM）

### 阶段 5：Conversation 统一交互管线 — IN PROGRESS

- [x] 建立三条旧入口的首轮 detached snapshot/history facade（不替换旧调用点）
- [ ] 统一场景喊话、自由对话、信使的快照/资格/Prompt/历史结构
- [ ] 统一后处理标签、动作执行入口和 AFEF 事实写入
- [ ] 保留旧入口作为 facade
- [ ] 验证三渠道规则和记忆一致性

### 阶段 6：Memory / Prompt / Action — IN PROGRESS

- [ ] 提取 Memory 与事实服务（当前切片 active：统一 detached Memory/AFEF commit facade）
- [ ] 提取 Prompt/Rule 与前后处理规则
- [ ] 建立统一动作解析、授权、当前状态验证、主线程执行和结果记录
- [ ] 先迁移低风险动作垂直切片（当前已完成协议解析/白名单切片，真实 Economy/Reward/Debt 验收待进行）

### 阶段 7：领域模块渐进迁移 — ACTIVE

- [ ] 接入共享 Configured Chat Gateway（领域切片已逐步接入；真实游戏内回放与默认路径切换仍待验证）
- [ ] 接入 Policy / WorldDiplomacy / Economy / Courier / Duel / WorldMap / Siege 等领域专用 provider，同时保留各自 JSON、重试、预算和降级语义
- [ ] 接入 Knowledge/RAG、周报、主动 NPC、辅助分类器和 TTS 的统一 capability/diagnostic 边界

建议顺序（以实际依赖盘点为准）：

1. Economy / Trade / Debt / Reward
2. Policy
3. Courier
4. Duel
5. WorldMap
6. Scene
7. Diplomacy
8. Siege / Battle
9. Knowledge / UI

每个领域都必须保持旧入口可用，完成调用方、存档、双版本、渠道、profile 和组合验证后才删除旧实现。

### 阶段 8：Bridge、旧结构清理与最终验收 — TODO

- [ ] 仅为确有跨模块所有权的行为建立 Bridge
- [ ] 验证 A、B、A+B、A+B+Bridge、Bridge 故障矩阵
- [ ] 清理 God Object、重复注册、旧 facade 和临时代码
- [ ] 验证 1.3、1.4、Bootstrap、stage、package、存档和游戏内场景
- [ ] 记录所有 NOT-RUN 与剩余风险

## 目标逻辑模块（第一版，非最终物理 DLL 方案）

- `AF.Contracts`
- `AF.Foundation.Runtime`
- `AF.GameAdapter`
- `AF.Persistence`
- `AF.Profile` / `AF.Config`
- `AF.Module.Conversation`
- `AF.Module.Memory`
- `AF.Module.Prompt`
- `AF.Module.Action`
- `AF.Module.Policy`
- `AF.Module.Economy`
- `AF.Module.Courier`
- `AF.Module.Duel`
- `AF.Module.WorldMap`
- `AF.Module.Scene`
- `AF.Module.Diplomacy`
- `AF.Module.Siege`
- `AF.Module.Knowledge`
- `AF.Module.UI`
- `AF.Bridge.*`

第一阶段优先建立逻辑边界和公共契约，不为了目录图强行拆成许多 DLL。发布契约仍是一个 Bootstrap 加载一个版本化 `AnimusForge.dll` 实现。

## 每个重构切片的必填记录

- owner：Foundation / GameAdapter / 单一 Module / 联合 Bridge
- 改动文件与公共契约
- 影响的渠道、profile、Bannerlord API 线、Harmony/Tick/UI
- 存档 namespace、key/type 和用户数据影响
- 运行频率、缓存、队列上限、主线程边界
- 验证命令和实际结果
- 回滚 commit 或旧 facade
- 下一步和阻塞项

## 状态规则

- `TODO`：尚未开始
- `IN PROGRESS`：正在处理
- `VERIFY`：实现完成但验收未完成
- `DONE`：验收证据完整
- `BLOCKED`：有明确阻塞原因
- `NOT-RUN`：检查未运行，必须写原因；不能当作通过

## 变更意图记录

| 时间 | 任务 | 范围 | 风险 | 验证 | 状态 |
|---|---|---|---|---|---|
| 2026-09-02 | PUBLISH-20260902：关闭自动化并GitHub交接 | PAUSE `af-7-8`；新增总HANDOFF，更新制作组简报/总纲/台账；普通push当前分支到`origin/refactor/prepare-af-restructure` | 只允许fast-forward；不force、不部署、不切default；发布动作DONE不等于阶段7/8 DONE | push前fetch/clean/ahead19 behind0；文档/fence/link/diff；push后HEAD==remote且ahead/behind 0/0 | DONE |
| 2026-09-02 | LOCAL-7-C2：ShoutNetwork SSE replay dependency closure | 基线 `8bf0c1e4`，checkpoint `28ad96f2`，实现 `ae49e3c8`；五consumer source contract；ShoutNetwork只导入既有ReplayDependencies targets；仅改runner/tool文档与测试 | 拒绝F盘硬编码、Modules/Workshop递归扫描、implementation复制与模糊同名覆盖；不改Program业务断言、生产C#、Stage/官方脚本、游戏；future stale output须先隔离 | 红测shared Import缺失；source 5/5、helper 9/9、missing-property gate、Shout Debug/Release业务回放、78项manifest/hash/identity与cleanup/diff PASS；LIVE/SAVE N/A | VERIFY |
| 2026-09-02 | LOCAL-7-C3：LiveHostReadinessAudit explicit-root portability | `tools/LiveHostReadinessAudit/` source/README/纯fixture CLI 测试；`--game-root` 显式必填，repo-derived project root 保留 | 删除 F 盘默认选择；工具只读，不启动游戏、不部署、不读取存档；不把 readiness 提升为 LIVE/SAVE | C3 `4/4 PASS`；Python compile、旧路径扫描、`git diff --check` PASS；真实 Campaign/Mission/LIVE/SAVE NOT-RUN | VERIFY |
| 2026-09-02 | Persistence/Profile/Identity scanner/catalog 离线收尾 | `tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`、`tools/PersistenceIdentityAudit.py`、phase4 persistence catalog | 排除生成物/缓存，跨 partial 常量解析；不改生产 SyncData/key/type、程序集、CampaignBehavior 注册或部署 | Profile/Config `95/121/8/3/44 PASS`；Identity `99/35/AnimusForge/Bootstrap-only PASS`；真实旧档/SAVE NOT-RUN | VERIFY |
| 2026-09-02 | 用户授权 Debug 双版本编译与统一模块测试部署 | 本地 `refactor/prepare-af-restructure` HEAD `109835cd`；`build_single_module.ps1 -Configuration Debug -Deploy`；目标 `F:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord\\Modules\\AnimusForge` | 仅 scoped Debug 测试安装；保留 Logs/PlayerExports/ONNX；不启动游戏、不切 default、不把安装视为 LIVE/SAVE 或 Release；后续 Stage 重建后实现 DLL 需重新部署 | 部署时 1.3/1.4/Bootstrap `0 warning / 0 error`、事务部署 exit 0、三 DLL hash 一致、readiness PASS、gameRunning=0、PlayerExports 4753 合并；当前安装实现 DLL 相对最新 Stage 已过时 | VERIFY |
| 2026-09-02 | Release Stage/Production Duel/ZIP 离线闭环 | `build_single_module.ps1 -Configuration Release -Stage`、Production Duel Release replay、Release ZIP/package validator | 仅项目内 Stage/ZIP；不安装 Release、不启动游戏、不读写存档、不把离线工件当发布许可 | 双 API/Bootstrap 0 warning / 0 error；Duel 35/35；ZIP 4919 entries/hash `1215A886...ACE5`、Bootstrap-only/marker/ONNX/旧模块校验 PASS | VERIFY |
| 2026-09-02 | BRIDGE-CONFIG：16 组 Bridge 绑定与安全 Gate（历史快照） | `docs/phase8/bridge-binding-manifest.json`、`tools/BridgeBindingContractTests/`、`Refactor/Contracts/FeatureBridgeContracts.cs`、`Refactor/Runtime/FeatureBridgeRuntime.cs`；当日仅接入 `AfGcczShoutBridge`、`WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved`、`SceneActionsIntegrationBoundary.InitializeRuntime` 三个既有入口 | 历史状态 `3 wired / 13 declared-only`；禁用/失败保留原版或 owner fallback；不新增 Tick 扫描、save key/type、网络或 live 对象跨边界，不改终端 UI | 历史 Bridge binding `16/3/13 PASS`，Debug 双 API/Bootstrap Stage `0 warning / 0 error`；LIVE/SAVE、默认切换、Release/最终发布仍 NOT-RUN/BLOCKED | VERIFY |
| 2026-09-03 | BRIDGE-CONFIG：Bridge 接线安全收尾 | `Refactor/Runtime/FeatureBridgeRuntime.cs`、已接线 adapters/behaviors、`AnimusForge/ModuleData/FeatureBridges.json`、`docs/phase8/bridge-binding-manifest.json`、`tools/BridgeBindingContractTests/` 与总纲/handoff 文档；10 组 source-bound Gate，6 组保留 declared-only | 配置缺失使用内建审阅默认值，配置损坏/未知/非规范大小写 ID fail-closed；保持 NoOp/native/owner fallback、无 Tick 扫描、无新增 save key/type、无 live 对象跨边界；不改终端 UI | Bridge validator `16/10/6`、Python 单测 `15/15`、PhaseEightReadiness `62/62`、BridgeFixture `10/6`、Composition `18/24`、ModuleCatalog `8/3/16/8`、Foundation `6/8/16`、GameAdapter `14`、Persistence/Profile `95/121/44`、LiveHostReadiness PASS；Interaction/Duel/Economy/Gateway/Knowledge/Production suites 与双 API Debug/Release/Bootstrap Stage 均 PASS；真实 Campaign/Mission、LIVE/SAVE、默认切换和发布仍 NOT-RUN/BLOCKED | VERIFY |
| 2026-09-03 | OFFLINE-GAP-20260903：离线缺口全量修复（当前切片） | Bridge validator 真实方法体/顺序负例；纯 net8 Bridge runtime isolation runner；Phase8 入口候选生成器与 catalog `entryPaths`；PersistenceIdentityAudit 单快照/batch/progress/quiet；ModelCatalog stable error code/参数/中英文 formatter 与 UI 映射；新增契约/回放测试 | 仅工作区内可逆离线变更；不启动游戏、不读写真实存档、不部署、不切换默认、不删 facade、不改模块发布结构/程序集/SubModule/SyncData key/type/构建脚本；临时 Stage 产物留在忽略目录 | Bridge 20/20、隔离 9 场景、Phase8 68/68、Persistence 5/5、ModelCatalog replay；真实审计对 partial-clone 缺 89 个 blob fail-closed；all-missing readiness 保持 BLOCKED/exit 2；Debug/Release 双 API/Bootstrap Stage 均 0 warning / 0 error；提交 `01e7bc1`→`552d8b9`→`f1a17f7`→`4feac3c`→`4a8e929`→`8f12298`→`cebac17`→`aad83c3`→`ab6ce72` | VERIFY |
| 2026-09-02 | LOCAL-7-M2：Duel exact detached dispatch provenance | 基线 `3522dc3e`，checkpoint `17f617a5`，实现 `b93f93df`；internal request-bound executor、pre-effect owner Queue、显式 dispatch context、Native/Scene delayed holder、Courier reject、exact request readback；仅改 AF-REFACTOR | 不用 ambient/subject-latest；Queue 必须先于 Economy；同 context 显式转移到 actual start；Queued/Started/Unknown non-retryable；不改 public executor/Prepare/commit constructor ABI、default/save/Fourberie/M1 legacy-unbound | 红测requestId断链；Dispatch16/16、Outcome18/18、focused/三渠道fresh production、Persistence/Profile/Migration/Identity、Production Duel Debug/Release各35与Debug/Release六Stage PASS；实机/旧档/live Economy/AFEF NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-M1：Duel actual-session typed owner/outcome/readback | 基线 `9955658b`，checkpoint `fc3cd722`，实现 `16f3cbef`；pure bounded receipt owner、三 actual-start/terminal seam、ResultIdentity-first、component effects、exact DuelId artifact binding、additive readback | process-local/NOT-RECOVERABLE；legacy dispatch返回`UnknownAfterStart / duel.outcome_pending`而非玩法成功；当前live provenance仅`Domain / legacy-unbound`，M2补exact request绑定；不重放Mission/death/Economy，不改public void ABI、cooldown key/type、Fourberie/default/save identity | 红测旧void/aggregate/hero-only gap；Duel 16/16、Interaction/Host/receipt、fresh production Duel Debug/Release各32、Persistence/Profile/Migration/Identity与Debug/Release六Stage PASS；真实Campaign/Mission/旧档/live death/stake/Economy/AFEF/Fourberie NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-K：weekly exact-intent/outcome owner | Economy-only candidate/actual fingerprints、`InteractionResultCommitter`终态 gate、独立 `AFWM1` ledger、MyBehavior Confirmed-only Daily attach、additive symbolic SyncData与 focused/production fixture；基线 `da15241f`，checkpoint `7cdf6435`，实现 `765b2386` | 只接受 whole-plan owner full Applied/count/effect+memory；canonical sidecar不调用injected planner；有效 journal的durable identity在live payload前防重；Prepared load→Unknown，坏 journal保留并禁用且不再提供K防重；不存raw/action/callback，不改H/I wire/public ABI/default | red CS2001→weekly/economy/Interaction40+Host69+receipt39/Memory/Courier/Economy Port及8 production/compiled回放 PASS；Profile 95/121/42 symbolic/40 flattened、Migration10/corrupt2、Identity99/35 PASS；Debug/Release六Stage 0 warning/error；终审P0/P1=0；live Campaign/save/Economy/AFEF NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-J：memory auxiliary recovery boundary | `MyBehavior.MemoryRecovery.cs` 与 ProductionOptIn compiled-DLL guard；基线 `d2f37a8a`，checkpoint `3436d739`，实现 `84e92f80` | H 仅修 Daily/Recent；不读/附着/删除无 exact identity 的 weekly candidate；Notoriety仅 brand-new同步完成+exact marker后各part一次 attempted-unconfirmed，异常不污染core；legacy/H/I key/hash/wire不变 | Memory/Interaction69/receipt39/Courier/Economy/Production Host/OptIn/Profile/Identity PASS；Debug/Release六Stage 0 warning/error；独立审查本diff P0/P1=0；weekly exact-intent/detached owner NOT-IMPLEMENTED，live Notoriety/save NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-I：Courier inbound durable session completion | Courier-owned batch memory wrapper、`AFCI1` receipt、MyBehavior recovery identity/status query、load/delivery gate、one-per-tick scheduler；基线 `0e276ce1`，checkpoint `b5395164`，实现 `de3220b7` | receipt 先于 memory owner；绑定 owner payload hash防同 CommitId 冲突；invalid/quarantine/pre-owner/no-receipt均abort并解锁；不重放Action/Economy，不复用`PostprocessConsumed`；默认 caller不切 | contract含arm-before-owner/inner throw/5 outcomes/32k Unicode；production receipt/load/Applied恢复/fail-closed/tick one；Interaction69/receipt39、Memory、三Host、Economy、95/121 profile、99/35 identity、Debug/Release六Stage PASS；live NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-H：memory-only persistent recovery | MyBehavior additive journal/marker owner、逐组件 idempotent repair、SyncData、focused/persistence/production replay；基线 `a8001b87`，checkpoint `6f8d8cc0`，实现 `f6e5e694` | payload 无 ActionPlan/afterCommit/raw postprocess；opaque id+nonce+checksum、frozen provenance、64 pending/512 tombstone/64 quarantine、单步骤五次失败后隔离；坏 schema/hash/marker/超限 fail-closed；每 tick 最多一组件；旧 public ABI/default/identity不变 | 最多六步/12 fault、restart/corrupt/long Courier/nonhero、Production 3498、Interaction/Host/Economy、39-flat/95-key/121-binding、99-sync/35-behavior、Debug/Release六Stage PASS；live AFEF/旧档 NOT-RUN；Courier afterCommit另列 I | VERIFY |
| 2026-09-01 | LOCAL-7-G：UnknownAfterStart effect state / terminal receipt | Economy status/effect contracts、Hero/Party/Merchant replay-aware mutation observation、port、executor、committer、Host/cache 与 focused/production replay；checkpoint `899effbb`，实现 `d765270a` | post-callback malformed receipt 不可信且清空 count/facts；unknown action 不造 fact、不 fallback/afterCommit/重放；旧 helper ABI/save identity不变；无补偿、durable recovery、默认切换 | Port unknown 8、Executor unknown 3/receipt 4、Host 69×三渠道、receipt 39、Production Economy/owner/Host/OptIn PASS；Debug/Release 六项 Stage、profile/identity PASS；独立终审无 P0/P1；live NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-F：known partial Economy outcome/facts/terminal receipt | Economy contract、Hero/Party/Merchant owners、port、executor、committer 与 focused/production replay；checkpoint `7186048d`，实现 `8f22d737` | 仅 owner count/facts；不推断 legacy；memory fail 不重放 action；enum尾增/接口additive；无 save key/type；UnknownAfterStart 和跨重启 durable recovery未解决 | partial direct/commit/duplicate/memory fail、mixed reject/throw、Host 51、Port normalization/ABI、Production partial 2 PASS；双配置六项 Stage、profile/identity PASS；live NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-E：Courier Economy owner 前置 gate 与 economy-only 持久消费 | `LegacyNativeActionPlanExecutor`、`CourierDeliveryBehavior`、Economy-aware/Production replay 与 owner 文档；checkpoint `bbe35aa8`，实现 `b2542fdd` | 复用 `_af_courier_sessions_v1` 内既有 `PostprocessConsumed`，无新 key/type/field；reservation 后失败不可自动重试；mixed 后半仍可 partial；opt-in 无默认生产调用者 | gate ordering/fault 5、production Courier session 18 assertions、Production Economy/Courier/Configured、Interaction 和 Economy port PASS；Debug/Release 六项 Stage 0 warning/error；profile/identity PASS；实机/save NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-D：Memory owner runtime 回执 | `MyBehavior.cs`、`LegacyInteractionSnapshotAdapters.cs`、ProductionOptIn replay、owner/receipt 文档、SyncData 行号 fixture；先记录意图并 checkpoint `3fe3f656`，再红测与最小替换 | 无新 save key/type、无新管线或默认切换；公开 void 兼容保留；Failed 可能已有部分写入，禁止推断安全重试/回滚；真实游戏仍未授权部署 | production 缺 owner/线程 fixture/原始读回 PASS；Interaction、三 Host、两 Economy runner PASS；Debug/Release 六项 Stage 0 warning/error；95-key/121-binding profile 和 99-sync/35-behavior identity PASS；cleanup/diff 和独立审查通过；实机/旧档 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 7 Policy EventAndRebellion Gateway 取消传播（当前切片） | `PolicySystem/Npc/PolicyLlmClient.cs`、`Refactor/Adapters/LegacyPolicyLlmGateway.cs`、`tools/PolicyGatewayReplayTests/`；为旧 EventAndRebellion 重试入口增加可选 `CancellationToken` 并从共享 Gateway 贯穿到 HTTP/backoff，新增本地可控 provider 回放 | 保持既有 route/profile/JSON、thinking/兼容降级、重试、stale、Policy/王国创建主线程边界和存档 key/type；凭据仅留在旧 profile 发送边界；不切换三渠道，不改构建/部署脚本 | `dotnet run --project tools/PolicyGatewayReplayTests/PolicyGatewayReplayTests.csproj`：`PASS policyGatewayReplay callerCancellation=1 retryDelayCancellation=1 timeoutIsolation=1 credentialBoundary=1`；Debug/Release 1.3/1.4/Bootstrap unified stage 均 `0 warning / 0 error`；真实 Policy provider、旧存档和游戏内回放 `NOT-RUN`；未部署 | VERIFY |
| 2026-08-30 | 修正 Policy Gateway 重构后的契约测试断言（当前切片） | `tools/PolicyEffectModule.ContractTests/Program.cs`；将“必须在调用方显式出现旧三次重试调用”的静态断言改为同时接受共享 `LegacyPolicyLlmGateway.GenerateAsync` 路径，并继续要求旧路径具备有界重试 | 仅修正测试对已登记 Gateway 重构的表达，不放宽生产重试/失败语义，不改变存档、配置、默认入口或构建脚本 | 1.4 pinned reference 下完整 Policy contract runner：`PASS assertions=9031 modules=18 syntheticDescriptors=64 activeContributions=100`；Policy Gateway replay 已通过；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 6 Memory/AFEF receipt 失败可重试边界（当前切片） | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`tools/InteractionPipelineContractTests/`；将 detached memory commit 的 receipt 登记从 legacy history/AFEF 写入前移到成功写入后，避免写入异常后重试被错误抑制 | 保持旧 `MyBehavior` history/AFEF owner、SyncData key/type、user/assistant 语义和主线程边界；receipt 仍为有界进程内运行时数据，不进入存档；不改变默认三渠道路径 | InteractionPipeline contract `40 cases PASS`；Policy Gateway replay PASS；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实旧存档和游戏内 memory 回放 `NOT-RUN`；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 辅助规则路由接入共享 Gateway（当前切片） | `AIConfigHandler.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将辅助规则路由的固定 system/user prompt 经共享 Gateway 发送，并保留 auxiliary thinking 控制、400 plain retry、配置化 token/temperature 和规则 owner 的解析/重试 | API key 只在发送边界；响应正文不进入共享 DTO；不改变规则资格、提及实体发布、格式重试、fallback 或三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实辅助路由 HTTP和游戏内规则回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 AI 错误分析与 XihaiAction 辅助分类器接入共享 Gateway（当前切片） | `AiErrorAnalysisInquiry.cs`、`extensions/AnimusForge.XihaiAction/src/Runtime/AfV130ConfiguredGatewayTransport.cs`、`extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；错误分析和 SceneActions/Consent/BattleSpeech 分类器通过共享 Gateway 发送，保留原反射 transport fallback、闭集解析和生命周期 | API key 只在发送边界 resolver 闭包；分类器仍由原 single-flight/battle-speech flight 控制；缺少辅助配置回退原 transport；不改变动作白名单、consent、存档或默认三渠道 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；SceneActions Core `88 passed / 0 failed`；Static Verifier `13 passed / 0 failed`；真实 HTTP、取消和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 周报 Event/WeeklyReport 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将周报批量/单组/完整周报/第 0 周短摘要的冻结 system/user prompt 通过共享 Gateway 发送，保留周报 owner 的批量、解析、重试、降级、限速和主线程写入 | 仅在显式周报调用点启用；EventAndRebellion route 的 URL/model/key、thinking/plain retry 和配置化 token/temperature 仍由旧配置 owner 解析；响应正文不进入共享 DTO；不切换三渠道或周报默认调度 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实周报 HTTP、旧存档和游戏内周报回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Memory 压缩/重大履历/Memory Overview 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将三个后台非流式摘要入口的最终 system/user prompt 复制为 immutable `PromptPackage`，通过 Auxiliary route Gateway 发送，保留 owner 的资格、重试、标签/JSON 解析、过期检查和主线程提交 | API key 只在发送边界；force-thinking-disabled、配置化 token/temperature、失败降级和原有内存/AFEF 存储语义保持；后台不携带 Hero/live 对象；不切换三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Memory HTTP、旧存档和游戏内摘要回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 主动 NPC LLM owner-only 审查（当前切片） | `ProactiveNpcRequestBehavior.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、`MyBehavior.cs`；核对主动 NPC 的候选扫描/需求判定/开场 prompt 与三渠道、Memory owner 的边界，确认不存在独立 HTTP/LLM transport | 主动 NPC 只负责低频增量候选扫描和状态机；开场生成复用 Native/Scene/Courier facade，摘要复用 Memory owner；不新增平行请求、规则或存档字段；默认路径不变 | 静态调用图核对完成；无独立 transport 可迁移；真实主动 NPC、三渠道和旧存档回放仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Persona/升格同伴人设与技能接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将 NPC Persona、升格同伴 Persona、升格同伴技能的最终 system/user prompt 通过 Auxiliary Gateway 发送，保留 JSON 解析、fallback、stale 和主线程存储 | API key 只在发送边界；保留 Auxiliary URL/model、thinking/plain retry、配置化 token/temperature、原有资格与失败语义；Gateway 不携带 Hero/live 对象；不改变 Persona/技能存档结构或三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Persona HTTP、旧存档和游戏内生成回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 叛乱王国命名 EventAndRebellion 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将叛乱建国命名的冻结 system/user prompt 通过 EventAndRebellion Gateway 发送，保留 60 秒超时、三次重试、格式/重名校验和王国创建主线程边界 | API key 只在发送边界；保留专用 URL/model、thinking/plain retry、配置化 token/temperature、命名失败中止和原有重试/限流语义；不改变王国/存档结构或默认三渠道路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实命名 HTTP、旧存档和游戏内叛乱回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 外部 Auxiliary API facade / PlayerNotoriety 摘要接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`PlayerNotorietyBehavior.cs`；将 `CallAuxiliaryApiTextForExternal` 及其 PlayerNotoriety 摘要调用转到统一 Auxiliary Gateway，保留非阻塞/失败弹窗、force-thinking-disabled、原摘要解析和过期语义 | API key 只在发送边界；不改变外部 facade 的返回契约、PlayerNotoriety 存储或三渠道默认路径；Gateway 不携带 Hero/live 对象 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 PlayerNotoriety HTTP、旧存档和游戏内摘要回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 AIConfigHandler ACTION 后处理与 Auxiliary Simple Dialogue 接入共享 Configured Chat Gateway（当前切片） | `AIConfigHandler.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；统一动作后处理和简单辅助对话的标准请求、鉴权、超时、thinking/plain retry 与 assistant extraction，保留原调用方的阻塞重试、协议解析和 fallback | API key 只在发送边界；动作标签闭集、响应格式校验、历史/AFEF、领域资格和默认三渠道保持；响应正文不进入共享 DTO；不改变 ActionPostprocess/Auxiliary 配置来源 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实动作后处理/辅助对话 HTTP、旧存档和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 外部 Auxiliary API facade / PlayerNotoriety 摘要接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`PlayerNotorietyBehavior.cs`；将 `CallAuxiliaryApiTextForExternal` 及其 PlayerNotoriety 摘要调用转到统一 Auxiliary Gateway，保留非阻塞/失败弹窗、force-thinking-disabled、原摘要解析和过期语义 | API key 只在发送边界；不改变外部 facade 的返回契约、PlayerNotoriety 存储或三渠道默认路径；Gateway 不携带 Hero/live 对象 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 PlayerNotoriety HTTP、旧存档和游戏内摘要回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 叛乱王国命名 EventAndRebellion 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将叛乱建国命名的冻结 system/user prompt 通过 EventAndRebellion Gateway 发送，保留 60 秒超时、三次重试、格式/重名校验和王国创建主线程边界 | API key 只在发送边界；保留专用 URL/model、thinking/plain retry、配置化 token/temperature、命名失败中止和原有重试/限流语义；不改变王国/存档结构或默认三渠道路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实命名 HTTP、旧存档和游戏内叛乱回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Persona/升格同伴人设与技能接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将 NPC Persona、升格同伴 Persona、升格同伴技能的最终 system/user prompt 通过 Auxiliary Gateway 发送，保留 JSON 解析、fallback、stale 和主线程存储 | API key 只在发送边界；保留 Auxiliary URL/model、thinking/plain retry、配置化 token/temperature、原有资格与失败语义；Gateway 不携带 Hero/live 对象；不改变 Persona/技能存档结构或三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Persona HTTP、旧存档和游戏内生成回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 AI 错误分析接入共享 Gateway（当前切片） | `AiErrorAnalysisInquiry.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将现有辅助 API 的固定 system/user prompt 复制为 immutable `PromptPackage`，通过共享 Gateway 发送，保留错误分析结果展示、超时和失败回调 | API key 仅在发送边界 resolver 闭包内；不改变错误详情脱敏、辅助 API 配置、60 秒用户可见超时语义或非阻塞展示；响应正文不进入共享 DTO/日志 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实错误分析 HTTP 和游戏内弹窗 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Knowledge/RAG 短句生成接入共享 Gateway（当前切片） | `KnowledgeLibraryBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将 RAG 专用短句的既有 system/user prompt 复制为 immutable `PromptPackage`，经共享 OpenAI-compatible Gateway 发送，再由知识 owner 继续解析和确定性降级 | API key 仅在发送边界 resolver 闭包内；不改变知识文件、存档 key/type、Prompt 内容、解析/fallback、UI 阻塞时序或默认三渠道；该 UI 操作仍为单次同步调用，不进入 Tick | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；此前 7 个阶段 Python runner、InteractionPipeline `40 cases PASS`、GiveAssetTagCodec `80557 assertions PASS`；真实 RAG HTTP、知识文件写入和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Policy 实际调用方接入共享 Gateway（当前切片） | `Refactor/Adapters/LegacyPolicyLlmGateway.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs`、`PolicySystem/Core/CustomPolicyBehavior.Generation.cs`、`KingdomStrategicProfileBehavior.cs`；将 NPC ruler draft/effect/repair、玩家政策 main/postprocess/repair、王国战略建国卡的既有调用方接入统一 `ILlmGateway` 契约，保留原 profile/JSON/重试/兼容降级与测试 override | Gateway 只复制字符串 role/content，凭据留在旧 domain client；不改变 Policy 资格、存档 key/type、同步字段、主线程提交或默认三渠道；后台不携带 live 游戏对象；不引入新的玩法动作 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Policy HTTP、旧存档和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Town Ambient 接入共享 Configured Chat Gateway（当前切片） | `Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`TownAmbientAiClient.cs`、`docs/animusforge-phase7-domain-gateway-boundary.md`；保留 Town Ambient 的开关、预算、缓存、多人 JSON 解析和纯文本降级，将标准 OpenAI-compatible HTTP/鉴权/超时/取消/assistant extraction 统一到 Gateway | 凭据只由入口在发送边界解析，不进入 contract/snapshot/log/save；不改变默认三渠道、TTS、场景动作或 Town Ambient 开关；gateway 仅处理字符串 Prompt，不解析/执行游戏对象 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline 仍 `39 cases PASS`；真实 Town Ambient HTTP/游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 World Diplomacy 接入共享 Gateway contract（当前切片） | `Refactor/Contracts/LlmContracts.cs`、`Refactor/Adapters/LegacyWorldDiplomacyLlmGateway.cs`、`WorldDiplomacyBehavior.cs`；排队发送点先冻结 JArray 为 PromptPackage，再经 Gateway adapter 调用既有 WorldDiplomacy provider/client，保留领域重试、thinking fallback、stale、token/cache/truncation metadata | 领域 client 仍是 route/credential/retry authority；共享 DTO 不携带 key/response body/live 对象；当前 adapter 的取消受旧 client 边界限制；默认外交行为和存档结构不变 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline `40 cases PASS`；真实 World Diplomacy HTTP、读档和游戏回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Policy Gateway adapter 与 Persona Gateway 接入（当前切片） | `Refactor/Adapters/LegacyPolicyLlmGateway.cs`、`ShoutUtils.cs`；为 NPC Policy/事件叛乱保留旧 profile/JSON/重试 authority，并将无名 NPC Persona 的 ShoutNetwork 调用通过统一 Legacy Gateway；Persona JSON/存储仍由旧 owner 负责 | 不改变 Policy/Persona 现有资格、存储、配置或默认入口；Policy adapter 尚为 opt-in contract，Persona 仅切换既有 ShoutNetwork 传输包装；API key 不进入公共 DTO | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline `40 cases PASS`；真实 Policy/Persona HTTP 和游戏回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Dedicated TTS 接入 Gateway contract（当前切片） | `Refactor/Contracts/TtsContracts.cs`、`Refactor/Adapters/LegacyVolcTtsGateway.cs`、`TtsEngine.cs`；将火山 V1 请求 payload、鉴权 header、响应 code/base64 音频解码纳入 Gateway，保留 TtsEngine 的开关、队列、音频解析、播放和失败回调 | Token 仅作为发送边界参数；不进入 TTS contract、存档或普通日志；保留旧 V1 header 映射和 code=3000 语义；真实语音服务和游戏内播放仍未验证 | InteractionPipeline `40 cases PASS`（含 TTS bytes detach）；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 直接 LLM transport 盘点（当前切片） | `MyBehavior.cs`、`PolicySystem/Npc/PolicyLlmClient.cs`、`WorldDiplomacyLlmClient.cs`、`TownAmbientAiClient.cs`、`DuelSettings.cs`、`ModOnboardingBehavior.cs`、`ShoutNetwork.cs`；逐项标记共享 Gateway 已覆盖、legacy 主链路 facade、配置/向导连通性验证和无调用 dead path | 不删除旧私有入口；不切换三渠道；不把 API key/响应正文带入公共 contract；仅对确认存在且不改变流式/配置验证语义的运行时入口安排后续切片 | 静态扫描完成：Policy/World/Town/TTS/AIConfigHandler 新辅助路径已有 Gateway adapter；Scene/Native/Courier 主链路保留 legacy facade；`CallUniversalApiDetailed` 无生产调用者；DuelSettings/ModOnboarding 为用户主动配置验证；真实 HTTP、旧存档和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 ShoutNetwork 流式 Gateway 契约（当前切片） | `Refactor/Contracts/LlmContracts.cs`、`Refactor/Contracts/LegacyShoutNetworkGateway.cs`；为既有 ShoutNetwork SSE 主回复增加 `ILlmStreamingGateway` opt-in 契约，分离增量回调与最终结果，保留旧动态名称过滤、重试、stale 和取消处理 | 仅支持 `MainReply`；`Postprocess` 明确拒绝；不改变默认 Scene/Native/Courier 调用点，不重复提交 onDelta/onComplete，不携带 live 对象、凭据或响应正文进入公共 contract | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline `40 cases PASS`；7 个 Python runner、XihaiAction Core `88 passed / 0 failed`、GiveAssetTagCodec `80557 assertions PASS`；`ConfiguredChatGatewayReplayTests` 本地 HTTP 回放 PASS（success/thinking retry/5xx/cancellation）；真实 ShoutNetwork SSE、取消/stale 时序、旧存档和游戏内回放仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 全量现有 LLM 重构启动（阶段 4 → 阶段 6/7） | `F:\AF测试重构` 独立工作树；全量现有 LLM 调用、三渠道、Memory/AFEF、Prompt/Action、领域适配、TTS/辅助模型；先做 Persistence/Profile/Config 和公共契约，最终一次性切换默认路径 | 保持单一 `AnimusForge` 发布模块、Bootstrap、程序集/序列化类型/SyncData key、旧入口 facade、三渠道、主线程和 1.3/1.4；不新增玩法、不删除用户/参考/生成物、不立即拆物理 DLL | 首批验证台账一致性、key/type/JSON/fixture；后续必须通过契约、组合、1.3/1.4/Bootstrap、stage/package、旧存档和游戏内分渠道/领域验收 | ACTIVE |
| 2026-08-30 | 阶段 6 Memory/AFEF 统一 batch commit facade（当前切片） | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Runtime/InteractionResultCommitter.cs`、`Refactor/Runtime/MemoryCommitReceiptCache.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`tools/InteractionPipelineContractTests/`、`docs/animusforge-phase6-memory-afef-commit-boundary.md`；以一次性 user/assistant/facts commit 接入旧 MyBehavior，保留旧 Append fallback | 不改变 MyBehavior 的既有存储格式、SyncData key/type、AFEF 文本协议、默认三渠道入口或 Courier 时序；receipt 仅内存有界缓存；动作拒绝/stale/cancel 不写 confirmed AFEF；后台不持有 live 对象 | InteractionPipeline `40 cases PASS`；统一 `build_single_module.ps1 -Stage` 的 1.3/1.4/Bootstrap 均 `0 warning / 0 error`；unified stage 成功且未部署；真实三渠道、旧存档、网络和游戏内写入仍 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 6 Action 协议平衡解析与 Economy/Reward/Debt 有限白名单（当前切片） | `Refactor/Adapters/LegacyActionTagParser.cs`、`Refactor/Adapters/LegacyActionTagCatalog.cs`、`Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、`tools/InteractionPipelineContractTests/`、`docs/animusforge-phase6-action-protocol-and-economy-boundary.md`；修复嵌套/冒号资产 token，并将 detached executor 从 ACTION:* 收敛到既有有限协议目录 | 不改变 RewardSystemBehavior 及领域 owner 的主线程资格/资产/债务校验；未授权协议拒绝；保留 GCCZ 数字动作、旧 A/AD/ADP/ATT/ATP 协议；不改变默认入口、存档、SyncData、程序集或发布结构 | InteractionPipeline `40 cases PASS`；GiveAssetTagCodec `80557 assertions PASS`（20,000 fuzz/25,000 pressure tags）；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；未部署；真实三渠道 Economy/Reward/Debt、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 4 Persistence/Profile/Config 首轮目录与纯 runner | `docs/animusforge-phase4-persistence-profile-config-catalog.md`、`docs/fixtures/phase4-persistence-profile-config/`、`tools/PersistenceProfileConfigContractTests/`；95 个字面量 SyncData key、17 个 owner 文件、40 个符号 SyncData 来源、PlayerExports 分类、9 个 persistence namespace、3 个 profile 和 5 个配置快照案例 | 只读生产源码；不改变 key/type、存档程序集、配置运行时、生产 C#、项目/脚本、SubModule 或游戏目录 | `python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`（普通与 `--json`）PASS：`literalKeys=95 sourceFiles=17 symbolicSources=40 symbolicFamilies=4 profiles=3 cases=5 credentialFieldsExcluded=5 namespaces=9 migrationIdempotent=True unknownDataPreserved=True`；`git diff --check` PASS；生产/构建禁止路径 NONE；chunk/字典字段、真实存档和游戏验证 NOT-RUN | VERIFY |
| 2026-08-30 | AF Contracts 第一版生产边界 | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Contracts/LlmContracts.cs`、`docs/animusforge-phase4-llm-contract-boundary.md`；定义三渠道快照、Prompt/Action/Result/Trace 和 LLM Gateway 输入输出，不接入旧 Behavior | 新类型不得携带 TaleWorlds live 对象、API key、可变全局配置或私有模块类型；保持单一程序集、旧 facade、主线程复核、1.3/1.4 common surface | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；纯 Persistence runner PASS；静态检查无禁止路径；接入、旧存档、游戏内和网络验证 NOT-RUN | VERIFY |
| 2026-08-30 | Legacy ShoutNetwork LLM Gateway adapter | `Refactor/Contracts/LegacyShoutNetworkGateway.cs`；将不可变 Prompt contract 适配到现有 `ShoutNetwork`，保留旧重试、generation、DuelSettings 配置和错误文本；不替换调用点 | 过渡期 provider snapshot 仍由旧 DuelSettings 实际解析；不得把新 endpoint/model 宣称已生效；不得携带 live 游戏对象或凭据进入 DTO | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；网络真实调用、三渠道接入、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Shared InteractionPipeline contract/runtime seam | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Contracts/InteractionPipeline.cs`、`docs/animusforge-interaction-pipeline-boundary.md`；定义规则选择、Prompt 组装、LLM 生成、可见文本规范化、ActionPlan 输出的共享顺序；不执行动作、不写存档、不接旧渠道 | 后台只接收不可变 envelope/provider snapshot；动作和 AFEF 必须由主线程 facade 后续处理；旧三渠道、旧 tag/parser、存档 key/type、1.3/1.4 不得改变 | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；纯 Persistence runner PASS；真实 provider、三渠道接入、Action 执行、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | InteractionPipeline fake contract tests | `tools/InteractionPipelineContractTests/`；以 fake selector/composer/gateway/normalizer/postprocessor 验证顺序、无资格跳过、stale/cancel 映射、不可变 snapshot、三阶段 RAW/FINAL、后处理隔离、ActionPlan 提交和 Native facade；不引用 Bannerlord 或生产程序集 | 测试不能证明真实 Harmony/HTTP/主线程/存档行为；不得把 fake 通过当作三渠道接入完成 | `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；真实 provider、三渠道接入、Action 执行、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | RuntimeConfigSnapshot 第一版 | `Refactor/Contracts/ProfileConfigContracts.cs`、`docs/animusforge-phase4-config-snapshot-boundary.md`；定义 profile、模块开关、provider 元数据和请求 generation 的不可变配置快照；暂不接管 DuelSettings/MCM | 快照不含 API key、凭据或 live 游戏对象；reload 只影响未来请求；需要存档/Harmony/CampaignBehavior 的模块仍非 runtime-toggle-safe；不改变现有配置读取和存档 | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；snapshot smoke PASS（由 InteractionPipeline runner 的 `configReloadIsolation=true` 覆盖）；真实 MCM reload、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道旧入口 detached adapter 首轮 | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`docs/animusforge-phase5-legacy-facade-adapters.md`；从旧 ShoutBehavior/MyBehavior/CourierDeliveryBehavior 公共 facade 捕获不可变 snapshot/history，先不替换生产调用点 | 只在主线程捕获 live 游戏对象并复制字符串/ID；后台不得持有 Hero/Agent/Campaign/Session；不改变旧 Prompt、Action、AFEF、SyncData、配置 authority 或 courier 时序；适配层异常必须返回空/降级快照而不影响旧链路 | `git diff --check` PASS；InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；Persistence/Profile/Config runner PASS：`literalKeys=95 sourceFiles=17 symbolicSources=40 symbolicFamilies=4 profiles=3 cases=5 credentialFieldsExcluded=5`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS，含 `versions/1.3` 与 `versions/1.4`；三渠道真实接管、网络、存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 统一交互请求协调器 | `Refactor/Runtime/InteractionRequestCoordinator.cs`；在公共管线外层统一 provider 解析、模块开关、同会话替换取消、外部取消和读档 generation/stale 复核；不接入旧调用点 | 只接收不可变 `InteractionEnvelope`/`RuntimeConfigSnapshot`；不得持有 live 游戏对象、凭据或可变配置；请求结束后必须释放 CTS；不得把取消、stale 或配置缺失变成动作执行；旧三渠道和旧 gateway 行为保持不变 | `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实网络、旧存档、三渠道接管和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 旧规则/Prompt/Action 共享组合根 | `Refactor/Adapters/LegacyInteractionPipelineComposition.cs`、`docs/animusforge-phase5-interaction-request-coordinator.md`；以显式 ports 连接旧规则选择、Prompt 合成、后处理上下文、标签解析和可见文本规范化到兼容单阶段或三阶段 `IInteractionPipeline`，不复制规则、不接入调用点 | ports 不得携带 live 游戏对象或私有模块状态；具体渠道必须继续保留自己的资格和主线程复核；组合根不可单独宣称三渠道已切换 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap/unified stage PASS；三渠道真实接管、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三阶段 LLM 管线 | `Refactor/Contracts/FullInteractionPipeline.cs` 及相关 Contracts；在兼容单阶段管线旁增加前处理规则选择、主回复生成、可见文本 FINAL、后处理 Prompt/标签 ActionPlan 的共享顺序；不接入旧调用点 | 后处理失败不能吞掉已生成的可见主回复；RAW/FINAL 与 trace 必须分离；动作仍只能由主线程 facade 复核执行；取消/stale 不得写入历史或执行动作 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实网络、三渠道、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 主线程 Action/Memory 提交边界 | `Refactor/Runtime/InteractionResultCommitter.cs`；对完成的 `InteractionResult` 在主线程复核并执行 ActionPlan，分离可见回复历史写入与成功 AFEF 写入；不接入旧调用点 | 不接受 stale/cancel/失败结果；不执行空 ActionPlan；executor 失败不能伪造事实；memory 写入顺序固定为 user → assistant，且不携带 live 对象；旧三渠道保持不变 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实 Action executor、旧存档、三渠道和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native Conversation facade 旁路垂直切片 | `Refactor/Adapters/LegacyNativeConversationFacade.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`；串联 Native snapshot capture、三阶段 coordinator 和主线程 commit，旧规则/Prompt/Action 通过 ports 注入；不改 `ShoutBehavior` 现有调用点 | facade 不保存 live 游戏对象；capture/commit 必须在主线程；默认不切换旧路径；generation 二次校验必须先于动作执行；旧历史可能已有 pending user input 时由调用方关闭重复写入 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实 Native 接入、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native Conversation 显式 opt-in 宿主接线边界 | `ShoutBehavior.cs` 新增生产侧 facade 创建/配置快照入口；`LegacyChannelInteractionFacade` 提供三渠道共用生命周期；只提供主线程 capture 与显式 ports 注入，不替换现有 `SubmitNativeConversationTextInternalAsync` 默认路径 | 不在宿主层复制规则或持有 live 对象；不得把凭据写入 `RuntimeConfigSnapshot`；调用方必须负责主线程 Commit、旧 pending user input 去重和失败 fallback；未注入 ports 时不可启动新管线 | InteractionPipeline runner PASS（16 cases）；Persistence/Profile/Config runner PASS（95 literal keys、9 namespaces）；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署；真实 Native 端到端和游戏内验收 NOT-RUN | VERIFY |
| 2026-08-30 | Legacy Gateway 主回复/动作后处理分阶段路由 | AIConfigHandler.cs、Refactor/Contracts/LegacyShoutNetworkGateway.cs；主回复保持 ShoutNetwork，Postprocess 走现有非交互动作后处理 API；不改变默认旧入口 | 不弹阻塞重试窗口；凭据仍由旧 authority 读取；后处理底层旧 API 为同步 HTTP，取消时由 coordinator 丢弃结果但不能中断底层请求；真实三渠道仍未切换 | InteractionPipeline runner PASS；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署；真实网络/取消端到端 NOT-RUN | VERIFY |
| 2026-08-30 | Detached rule selector 与三渠道 lifecycle facade | Refactor/Adapters/LegacyDetachedRuleSelector.cs、Refactor/Adapters/LegacyChannelInteractionFacade.cs、MyBehaviorMemoryFacade；统一请求生命周期，前处理只读取 immutable snapshot 字符串/ID，memory 不跨异步边界持有 Hero | 规则检索仍由旧辅助 API authority 执行；真实 rule/prompt/action 全量 ports、三渠道默认接入和真实游戏验证未完成；Hero 解析只允许交互边界，不能进 tick 热路径 | InteractionPipeline runner PASS（17 cases）；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署 | VERIFY |
| 2026-08-30 | Legacy PromptPackage adapter | Refactor/Adapters/LegacyPromptPackageAdapter.cs、LegacyShoutNetworkGateway.cs；在旧 role/content 消息与不可变 PromptPackage 间做边界复制，供三渠道共用 | 空消息丢弃、非法 role 归一为 user；不携带 live 对象；仅完成消息形状适配，Native/场景/信使完整 Prompt 组装仍未迁移 | InteractionPipeline runner PASS（18 cases）；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署 | VERIFY |
| 2026-08-30 | Detached PromptPackage 与 ActionTag ports | Refactor/Adapters/LegacyPromptPackageAdapter.cs、LegacyActionTagParser.cs；统一旧 role/content 消息复制和受 allowlist 约束的 ACTION 解析，供三渠道复用 | 只产生不可变 PromptPackage/ActionPlan；不执行动作、不写 AFEF；完整 Prompt 组装和各领域 Action executor 仍待接入 | InteractionPipeline runner PASS（19 cases）；Persistence/Profile/Config PASS；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署 | VERIFY |
| 2026-08-30 | 共享 detached Prompt composer | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Adapters/LegacyDetachedPromptComposer.cs`、`ShoutBehavior.cs`、InteractionPipeline fixture；冻结交互边界生成的 system/prefix/suffix 字符串块，按场景喊话权威消息顺序生成共享 `PromptPackage`，并提供 Native opt-in overload | 不携带 live 游戏对象、凭据或可变配置；不重建提示词、不把 ACTION 标签注入主链路；旧 Native/Scene/Courier 默认路径不切换；仅在交互边界复制，后台无扫描/轮询 | `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` PASS：`cases=19`；`git diff --check` PASS；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；stage 成功且未部署；真实 rule/prompt/action ports、网络、旧存档和游戏内三渠道验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached Prompt sections capture 扩展 | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`；让 SceneShout/Courier 与 Native 使用同一 `DetachedPromptSections` envelope 输入，保留各自目标/送达时序 | 只复制交互边界生成的字符串块；不解析/持有 live 游戏对象，不改默认入口、Prompt 文本、ACTION、AFEF、SyncData 或 courier 时序；当前输入由渠道明确标记是否已写入历史，避免重复 | InteractionPipeline 19 cases PASS；Persistence/Profile/Config PASS；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；stage 成功且未部署；真实三渠道、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Detached action protocol port | `Refactor/Adapters/LegacyActionTagParser.cs`、InteractionPipeline contract fixture；在 detached 边界覆盖既有 `ACTION/A/AD/ADP/ASS/GUI/ATT/ATP/RELAY/FOL/STP/END` 标签族，输出不可变 `ActionPlan` | 仅接受显式 allowlist；参数化模板只允许有限具体实例；不解析 AFEF/CONTENT，不执行动作、不写 AFEF；默认三渠道链路保持旧 parser/执行器 | InteractionPipeline 19 cases PASS；`ACTION` 旧拆分兼容，协议族 allowlist/参数化模板/拒绝路径通过；真实动作执行、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Detached postprocess Prompt port（当前切片） | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Adapters/LegacyDetachedPostprocessPromptComposer.cs`、InteractionPipeline fixture；冻结后处理 system/tag rules、history/AFEF、runtime target facts 和 latest visible reply，按既有三段式顺序生成后处理 PromptPackage | 后处理 sections 必须由渠道 owner 使用现有规则/事实 helper 生成；composer 不猜规则、不读取 live 对象、不把 raw reply 当可见文本；默认三渠道链路保持旧后处理 | InteractionPipeline 21 cases PASS，包含后处理 composer 顺序和 raw/visible 隔离；真实网络、动作执行、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached lifecycle 组合入口 | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`ShoutBehavior.cs`；Native 支持主线程 Prompt sections provider，SceneShout/Courier 暴露共用 `LegacyChannelInteractionFacade` 工厂 | provider/capture 只在交互边界运行；不携带 live 对象、不改变默认入口、Prompt、ACTION、AFEF、SyncData 或 Courier 时序；未注入真实 ports 时不可启动新管线 | InteractionPipeline 21 cases PASS；Persistence/Profile/Config PASS；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；stage 成功且未部署；真实三渠道、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Atomic main/postprocess detached sections bundle | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`ShoutBehavior.cs`、InteractionPipeline fixture；Native provider 一次返回主 Prompt 与后处理 Prompt sections，避免两阶段快照错配 | bundle 只含不可变字符串 sections；provider 只在 capture 边界执行；不携带 live 对象、不改默认入口、规则文本、ACTION、AFEF、SyncData 或 Courier 时序 | InteractionPipeline 22 cases PASS，含 atomic bundle contract；统一 1.3/1.4/Bootstrap stage PASS；真实 Native/网络/旧存档/游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native detached sections 实际组装与 parity 旁路 | `Refactor/Adapters/LegacyNativePromptParity.cs`、`ShoutBehavior.cs`、`docs/fixtures/phase5-native-prompt-parity/native-message-order.json`、`docs/animusforge-phase5-native-prompt-parity.md`；以 Native 现有最终 role/content 和后处理 system/user 字符串为权威，生成 detached main/postprocess sections，记录哈希 parity 并汇合 atomic bundle | 只复制最终字符串/稳定消息；不复制规则文本、不把 ACTION 标签放入主链路、不携带 live 对象/凭据；parity 显式开启且异常 fail-open 到旧 Native；默认入口、旧历史、AFEF、SyncData、TTS 不变 | InteractionPipeline runner PASS：25 cases（含 Native main/postprocess parity 与 atomic bundle）；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；Stage 成功且未部署；真实 Native 网络、detached facade 发送、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native detached provider 实际旁路接入 | 在不改变默认入口的前提下，将 parity 已确认的 Native sections 接到真实 detached provider/coordinator/action executor；建立失败回退、stale/cancel、主线程执行和 old-vs-detached 网络请求对照 | 真实 rule/prompt/action ports 尚未完成前，不得切换默认 Native；不得让 live 对象跨异步边界；不得改变现有后处理领域资格和执行顺序 | 已完成 opt-in runner/provider 边界与 parity 旁路；真实 provider/游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native opt-in coordinator/fallback runner | `Refactor/Adapters/LegacyNativeConversationOptInRunner.cs`、`ShoutBehavior.CreateNativeConversationOptInRunnerForExternal`；将 detached Generate、主线程 commit 回调、基础设施失败回退和 stale/cancel 隔离闭合为显式 Native runner | runner 不持有或解析 live 对象；commit 必须由渠道宿主在主线程回调；stale/cancel 不重试旧路径；默认 Native 入口不切换 | InteractionPipeline runner PASS：28 cases（含 runner success、legacy fallback、cancel isolation）；统一 1.3/1.4/Bootstrap Stage PASS；真实 Native provider、网络、动作执行和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native ActionPlan 主线程执行器接入（当前切片） | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`ShoutBehavior.CreateNativeConversationActionPlanExecutorForExternal`、InteractionPipeline fixture；将 detached ActionPlan 做严格 raw/tag 一致性校验后，复用现有 Native `ApplyNativeConversationGameActionsCore` 执行，并保持主线程目标复核 | 执行器只在宿主主线程创建/调用并闭包当前 live 目标；不接受 raw 中未进入 ActionPlan 的动作标签；stale/cancel/目标失效不得执行或写 AFEF；不改变默认 Native、旧标签、SyncData、程序集或 Courier 时序 | InteractionPipeline 31 cases PASS；Persistence/Profile/Config 及 5 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 0 warning/0 error；未部署；真实 Native 网络、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | SceneShout detached 记忆快照对齐（当前切片） | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`；在场景交互边界复制当前 Hero 或非 Hero memory namespace 的历史与稳定 memory id，供共享 detached envelope 使用 | 仅在 capture 边界解析 Agent/Character/内存 key；后台不持有 live 对象；保留默认 SceneShout、AFEF、SyncData 和场景时序；无法解析时降级为空历史，不伪造对象或事实 | InteractionPipeline 32 cases PASS；Persistence/Profile/Config 及 5 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 0 warning/0 error；未部署；真实 SceneShout、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached commit 编排（当前切片） | `Refactor/Runtime/DetachedInteractionHost.cs`、Native host adapter；统一 capture → coordinator → 主线程 commit → Memory/Action 的生命周期，不替换旧入口 | host 只接收渠道提供的 capture、主线程 dispatch、ActionPlan executor 和 memory facade；取消/stale/验证拒绝不得重试或写 AFEF；保留 Courier 送达/返回时序和三渠道旧 fallback | InteractionPipeline 32 cases PASS（含 detached host）；1.3/1.4/Bootstrap unified stage 各 0 warning/0 error；未部署；真实三渠道、旧存档、网络和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | SceneShout 单目标 detached Prompt/Action ports（当前切片） | `ShoutBehavior.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`docs/animusforge-phase5-scene-shout-ports.md`；从现有单 NPC 场景喊话组装点捕获不可变 main/postprocess sections，并提供显式 facade 与主线程 commit 接口 | 只在交互边界解析 Agent/Character/Memory；默认 SceneShout 不切换；ActionPlan 仍须由调用方提供 allowlist 和主线程执行器；不复制规则、不让 live 对象跨异步边界、不改变 AFEF/SyncData/Courier 时序 | InteractionPipeline、Persistence/Profile/Config、阶段 2/3 runners；1.3/1.4/Bootstrap 与 unified stage；真实 SceneShout 网络、动作、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Courier reply/inbound detached Prompt ports（当前切片） | `CourierDeliveryBehavior.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`docs/animusforge-phase5-courier-ports.md`；复用现有 Courier reply/inbound message builder 的最终 role/content 顺序，复制为 immutable PromptPackage/history，并保留 Courier session 送达/返回状态机 | 只在交互边界解析 session/Hero 并复制字符串；默认 Courier 不切换；ActionPlan 仍须由渠道 owner 在主线程复核/执行；不复制 Courier 状态机、不改变 SyncData/存档/送达时序 | InteractionPipeline、Persistence/Profile/Config、阶段 2/3 runners；1.3/1.4/Bootstrap 与 unified stage；真实 Courier 网络、动作、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | SceneShout/Courier detached ActionPlan 主线程执行适配 | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`；以稳定 session/Agent identity 在 commit 边界重新解析目标，复用既有场景动作与 Courier 领域执行入口，避免重复写历史 | 执行器只在主线程调用；stale/cancel/目标失效或渠道/subject 不匹配时拒绝，不执行动作、不写 AFEF；Courier 送达/返回状态机保持原样，默认三渠道入口不切换；ActionPlan raw/tag 必须严格一致且受 allowlist 约束 | InteractionPipeline `32 cases PASS`；Persistence/Profile/Config PASS；6 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`，stage 未部署；真实网络、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached baseline 资格与 opt-in host 闭合 | `ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、旧 ports/composer/gateway；为无玩法规则命中的普通 LLM 对话提供仅生成文本的基线 RuleSelection，补全 SceneShout/Courier 显式 host/config/ports 入口 | baseline 不授权 ActionPlan，动作仍由明确 allowlist 和主线程 executor 控制；不重建 Prompt、不替换默认入口、不改变 SyncData/AFEF/送达时序；真实 HTTP/游戏内运行前保持 opt-in | InteractionPipeline 32 cases PASS；Persistence/Profile/Config PASS；6 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`，stage 未部署；真实网络、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | Detached host commit/历史边界契约测试（当前切片） | `tools/InteractionPipelineContractTests/Program.cs`；验证成功 commit 回调只发生在 dispatch 内、`appendPlayerInput=false` 不写入 NPC seed、stale/rejected commit 不触发回调或旧 fallback | 纯 runner 只能证明 host 生命周期契约，不能替代真实 Courier/SceneShout/Native HTTP、主线程、旧存档或游戏内验收；默认三渠道入口保持不变 | InteractionPipeline runner `36 cases PASS`；`git diff --check` PASS；真实网络、旧存档、游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | RuntimeConfigSnapshot 原子存储与 reload 边界（当前切片） | `Refactor/Runtime/RuntimeConfigSnapshotStore.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`AIConfigHandler.cs` 及纯契约测试；以不可变快照替换 detached 请求读取时的可变配置引用，并保留旧 DuelSettings/MCM 作为来源 | reload 只替换未来 capture 使用的快照；进行中的请求继续持有旧快照；快照不含凭据/live 对象；不切换默认入口、不改变存档、SyncData、程序集或构建流程 | InteractionPipeline `40 cases PASS`（含 atomic reload/failure isolation）；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`；未部署；真实 MCM reload、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-29 | 阶段 2 根 AF 基础 LLM 对话 Owner 映射 | `docs/animusforge-phase2-root-llm-owner-slice.md`；只读核对 Host、Conversation、Prompt/Rule、LLM Gateway、Memory/Persistence、UI adapter 边界 | 保持注册顺序、旧入口、存档 key/type、三渠道和单一程序集；不移动源码 | 已核对真实入口/方法/调用关系；未改生产代码 | IN PROGRESS |
| 2026-08-29 | SubModule 注册/调度分组只读清单 | `docs/animusforge-phase2-submodule-registration-catalog.md`；记录生命周期、Harmony、Model、CampaignBehavior、Mission adapter、ApplicationTick/EngineTick 顺序；不修改源码 | 注册顺序、失败隔离、主线程和 Tick 热路径是组合根风险 | 真实入口与顺序已抽取；运行频率 0；未改生产代码；清单验证通过 | DONE |
| 2026-08-29 | registry DTO 只读设计 | `docs/animusforge-phase2-registry-dto-design.md`；只定义 Host/Composition 元数据和 contribution groups，不接入运行时 | 不持有 Behavior 实例、TaleWorlds 对象、delegate 或 raw dictionary；保持旧 facade、注册顺序、失败隔离、Tick、存档和三渠道 | 设计文档完成；运行频率 0；未编译、未运行、未改变生产行为 | DONE |
| 2026-08-30 | registry validator 输入/输出 fixture | `docs/animusforge-phase2-registry-validator-fixtures.md`；有效快照、无效输入、依赖/顺序/owner/profile/线程/失败隔离输出样例；不实现 validator | 仅文档设计；不持有运行时对象；不改变 SubModule、程序集身份、SyncData key/type、三渠道或 1.3/1.4 构建策略 | 文档已写入；fixture 频率 0；`git diff --check` PASS；工作区边界检查无生产/脚本/配置路径；未编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 2 影响面、候选 Bridge 与回滚地图 | `docs/animusforge-phase2-impact-bridge-rollback-map.md`；首轮覆盖 Save、Prompt/Rule/Tag、Harmony、Tick、UI、线程、API、用户数据、候选 Bridge、非目标与回滚模板 | 只读设计；不移动源码、不接入 registry、不改变旧 facade、三渠道、存档、程序集或发布结构 | 文档已写入；频率 0；`git diff --check` PASS；工作区边界检查无生产/脚本/配置路径；逐文件 contract matrix、实现和实机验证未运行 | DONE |
| 2026-08-29 | Conversation/Memory/Action contract matrix | `docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；三条 contract 边界、逐文件影响、三渠道一致性、有效/无效 fixture 和纯测试矩阵；不实现 DTO/测试 | 保持旧 facade、三渠道、AFEF、存档 key/type、主线程和 1.3/1.4 contract；不移动生产 C# | 设计文档完成；测试 NOT-RUN；方法级映射与 fixture runner 已另行完成；未编译、部署或游戏验证 | DONE |
| 2026-08-29 | Conversation/Memory/Action 方法级映射与纯 fixture 目录 | `docs/animusforge-phase2-conversation-memory-action-method-map.md`、`docs/fixtures/phase2-conversation-memory-action/`；基于真实源码方法行号建立 contract 对应、有效/无效输入和预期输出 | 只读材料；fixture 不在 `.csproj` 中，不引用 TaleWorlds，不改变旧 facade、存档、三渠道或线程边界 | `git diff --check` PASS；生产/脚本/配置路径无变化；YAML 自动解析 NOT-RUN（当前环境无 YAML parser）；未编译/部署/游戏测试 | DONE |
| 2026-08-29 | Settlement/Siege 与 Policy/Diplomacy 候选 Bridge contract | `docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`、`docs/fixtures/phase2-settlement-policy-bridges/`；定义两个候选 Bridge、现有可复用边界、A/B/A+B/A+B+Bridge/Bridge failure 组合和回滚 | 不新增平行动作/通知链；不改变 Policy save/receipt、Settlement/Mission 主线程、旧 facade、程序集、SyncData key/type 或 1.3/1.4 策略 | 文档与 3 个 JSON fixture 已写入；PowerShell `ConvertFrom-Json` 全部 PASS；`git diff --check` PASS；生产/脚本/配置路径无变化；未实现 Bridge/runner，未编译/部署/游戏测试 | DONE |
| 2026-08-29 | Settlement/Siege 与 Policy/Diplomacy Bridge fixture runner | `tools/BridgeFixtureContractTests/validate_bridge_fixtures.py`、`tools/BridgeFixtureContractTests/README.md`；独立标准库 runner，验证 10 个 A/B/A+B/A+B+Bridge/Bridge failure 案例和 6 项不变量 | 不引用 Bannerlord/生产程序集，不调用网络/存档，不接入生产 `.csproj`，不执行 Bridge | 普通输出与 `--json` 输出均 PASS；`bridgeFixtureCases=10`、`invariants=6`；未编译生产 C#、未部署、未游戏测试 | DONE |
| 2026-08-29 | 阶段 3 module manifest/profile/dependency/health catalog 与 runner | `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/fixtures/phase3-module-catalog/`、`tools/ModuleCatalogContractTests/`；设计 8 个逻辑 module/bridge、3 个 profile、依赖/能力/生命周期/health 规则和 16 个无效场景 | 设计-only；不创建 Foundation/Registry，不绑定 entry type，不改变程序集、SubModule、SyncData、存档、构建或发布结构 | runner 普通输出和 `--json` 均 PASS；modules=8、profiles=3、invalidCases=16、healthStates=8；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-29 | 阶段 3 AF.Contracts capability/event/DTO/version 设计与 runner | `docs/animusforge-phase3-af-contracts-design.md`、`docs/fixtures/phase3-af-contracts/`、`tools/AFContractsContractTests/`；设计 9 个 contract、3 个 typed event、6 个 capability 和 18 个无效场景 | 设计-only；不创建 `AF.Contracts` 生产项目，不暴露 live Bannerlord 类型，不改变程序集、SyncData、存档、三渠道或 API 线策略 | 普通输出与 `--json` 均 PASS；contracts=9、events=3、capabilities=6、invalidCases=18；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 Foundation runtime contract 与 runner | `docs/animusforge-phase3-foundation-runtime-contracts.md`、`docs/fixtures/phase3-foundation-runtime/`、`tools/FoundationRuntimeContractTests/`；设计 dispatch、background snapshot/cancellation、diagnostics/trace、SafeMode/lifecycle/health contract 和 16 个无效场景 | 设计-only；不创建 Foundation 生产项目，不接入 SubModule/Tick，不持有 delegate/live object，不改变程序集、存档、SyncData 或 fallback | 普通输出与 `--json` 均 PASS；contracts=6、healthStates=8、invalidCases=16；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 重构台账一致性审查与修正 | `docs/animusforge-refactoring-and-repository-reorganization-plan.md`、`docs/handoffs/2026-08-30-refactor-preparation.md`；修正阶段 3 条目误放阶段 2、阶段状态归属和陈旧验证记录 | 仅文档修正；不改变生产代码、程序集、存档、SyncData、构建/部署流程 | 4 个独立 runner 全部 PASS；`git diff --check` PASS；禁止生产/脚本/配置路径无变化 | DONE |
| 2026-08-30 | 阶段 3 纯组合矩阵与 runner | `docs/animusforge-phase3-composition-matrix.md`、`docs/fixtures/phase3-composition-matrix/`、`tools/CompositionMatrixContractTests/`；覆盖 no-op、required/optional provider、版本不兼容、SafeMode、stale、部分启动失败、Bridge failure、toggle 冲突和 health 边界 | 设计-only；不实现 Module Host、不接入生产 `.csproj`、不改变 SubModule、存档、程序集、Tick 或 fallback | 普通输出与 `--json` 均 PASS；cases=18、invariants=24；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 GameAdapter 1.3/1.4 API boundary 与 runner | `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/`；设计 helper/capability、版本差异、missing member、Bootstrap marker、反射缓存、主线程和 unified package 边界 | 设计-only；不修改现有 helper、条件编译、构建/部署脚本、SubModule、程序集、存档或发布结构 | 普通输出与 `--json` 均 PASS；cases=14、apiLines=2、helpers=7；`git diff --check` PASS；未重新构建/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 最终设计审查 | `docs/animusforge-phase3-final-review.md`；核对阶段 3 checklist、5 份阶段文档、6 个 runner、14 个 JSON fixture、阶段归属、未验证项和禁止路径 | 结论仅覆盖设计/fixture；不代表生产 Foundation/Contracts/GameAdapter、旧存档、双版本运行时或实机验收完成 | 6 个 runner 全部 PASS；14 个 JSON fixture `ConvertFrom-Json` PASS；`git diff --check` PASS；禁止生产/构建/配置路径无变化；审查结论 PASS WITH LIMITATIONS | DONE |
| 2026-08-30 | 准备材料提交与推送 | 当前全部准备文档、fixture 和独立 runner 已暂存并创建本地提交；目标为 `origin/refactor/prepare-af-restructure` | 仅提交已确认的 docs/fixture/runner；无生产 C#、项目、脚本、配置或游戏目录变化 | 本地 commit 已创建；两次 `git push` 均因 GitHub 443 网络连接失败未完成；远端未更新，需网络恢复后重试 | VERIFY |
| 2026-08-29 | 用户决定先保持仓库现状 | 所有参考源码、生成物、用户数据、第三方依赖、工具发行物和归档保持原路径；不删除、不移动、不取消跟踪、不改 `.gitignore` | 暂不处理不会解决许可证/provenance 缺口，但避免误删用户/参考资料 | 用户明确选择 HOLD；未执行清理、移动或去跟踪 | HOLD |
| 2026-08-29 | 参考仓库保留边界确认 | `原版游戏本体代码1.3.x/`、`原版游戏本体代码1.4.5/` 作为用户确认的 tracked 游戏源码参考平面保留；不进入 AF 客户端 ZIP | 参考树与生产源码边界必须清晰；公开分发许可证仍未确认 | 已读取两套参考仓库目录和 tracked 数量；未执行删除/移动/去跟踪 | IN PROGRESS |
| 2026-08-29 | 阶段 1 初版仓库边界与分发决策表 | `docs/animusforge-repository-boundary-decision-table.md`；只建立保守处置分类，不执行删除/移动/去跟踪 | 缺少许可证/第三方清单；用户导出、参考源码、依赖 overlay、ONNX、工具发行物和归档不能默认发布 | 决策表已建立；许可证与 provenance 仍未确认，阶段 1 保持 IN PROGRESS | IN PROGRESS |
| 2026-08-29 | 阶段 1 仓库边界与可重复性审计 | `docs/animusforge-repository-boundary-audit.md`、只读扫描与现有 build/stage/package/deploy 说明；不清理文件、不改脚本 | 17,039 个 `.cs` 中 16,365 个位于原版 1.3/1.4.5 参考树；3,568 个 tracked 文件同时被 ignore 规则命中；许可证/第三方分发政策缺失 | 已完成分类统计、`.gitignore`/tracked-ignored 核对、构建流程读取；许可证/第三方原则、历史 tracked 生成物处置和实际存档/游戏基线仍未完成 | IN PROGRESS |
| 2026-08-30 | 第一版重构地图完成 | `docs/animusforge-refactor-map.md`：运行链、owner、持久化、交互、风险、顺序 | 不移动源码；目标仍为单一 `AnimusForge.dll`、旧存档兼容 | 3 个只读审计结果合并；构建仍被依赖闭包阻塞 | VERIFY |
| 2026-08-30 | 依赖闭包与 unified stage 构建验证 | 无生产 C#、脚本、程序集身份、SyncData key 或游戏目录变更 | 1.3 v1.3.15.110062、1.4 v1.4.6.115628、Bootstrap 均 0 警告/0 错误；stage 成功；实际安装游戏当前为 v1.4.8.119303，未冒充同版本验证 | 仍需旧存档、游戏内与精确 v1.4.8 overlay 验收 | VERIFY |
| 2026-08-30 | ShoutNetwork SSE 流式 Gateway opt-in 回放验证 | `Refactor/Contracts/LlmContracts.cs`、`Refactor/Contracts/LegacyShoutNetworkGateway.cs`、`ShoutNetwork.cs`、`tools/ShoutNetworkSseReplayTests/`；为保留的旧 SSE 传输建立可控 provider 回放，验证增量/最终文本、取消/stale、thinking retry 与 ACTION 隔离 | 仅 Debug transport override；默认 Scene/Native/Courier 三渠道不切换；不改变 API key、历史、AFEF、SyncData、程序集或部署流程 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；7 个 Python runner、Configured Gateway、InteractionPipeline `40 cases`、XihaiAction Core、GiveAssetTagCodec 全部 PASS；SSE 回放 `success=1 thinkingPlainRetry=1 cancellation=1 stale=1 deltaFinalParity=1 actionIsolation=1`；真实游戏内 SSE、旧存档和三渠道运行时回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | Native/SceneShout/Courier 生产 opt-in capture/factory 回放 | `tools/ProductionOptInEntryReplayTests/`；直接加载 project-local 1.4 实现，调用三渠道生产 opt-in capture 与 detached ports factory，验证无活动游戏会话时 fail-closed、channel identity、Courier session identity、玩家输入冻结和 ports 非空 | 仅验证生产公开 opt-in entry 的 capture/factory，不启动真实 LLM、不执行动作、不写存档；默认三渠道、API key、历史、AFEF、SyncData、程序集与部署流程不变 | `productionOptInEntryReplay native=1 scene=1 courier=1 identity=1 failClosed=1 ports=1 noDefaultCutover=1`；未部署；已初始化游戏 host 的真实生成/主线程 commit、旧存档和游戏内回放仍 NOT-RUN | VERIFY |
| 2026-08-30 | World Diplomacy Gateway 调用方取消传播修正（当前切片） | `WorldDiplomacyLlmClient.cs`、`Refactor/Adapters/LegacyWorldDiplomacyLlmGateway.cs`；将 shared Gateway 的 cancellation token 贯穿领域请求、thinking plain retry 和 retry delay，调用方取消不再伪装成 timeout/retryable failure | 旧 API 签名保留可选 token 兼容；route、payload、thinking fallback、token/cache metadata、stale generation、存档和默认外交行为不变；只在请求边界取消，不进入 Tick | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；`WorldDiplomacyGatewayReplayTests` caller cancellation、retry-delay cancellation、timeout isolation、credential boundary PASS；真实 provider、旧存档和游戏内外交回放仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | Dedicated TTS Gateway V1 本地 HTTP 回放 | `tools/TtsGatewayReplayTests/`；直接加载 project-local 1.4 实现，调用 `LegacyVolcTtsGateway`，验证 V1 payload、header 映射、code=3000/base64 成功、provider 错误、非法音频和 caller cancellation | token 只在 header 发送边界；payload 使用 legacy literal token，不泄露 credential；不改变 TtsEngine 队列、播放、Rhubarb、VoiceMapping、存档或默认路径 | `ttsGatewayReplay success=1 headers=1 credentialBoundary=1 providerError=1 invalidAudio=1 cancellation=1 malformedExtra=1`；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`；真实火山服务、游戏内播放和旧存档仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | Native/SceneShout/Courier 已初始化 host 真实生成与 commit（下一项） | 复用现有 opt-in host、detached ports、可控 provider 和主线程 commit seam，验证三渠道完整链路：规则命中、Prompt、LLM RAW/FINAL、ActionPlan、主线程复核/执行、历史与 AFEF | 仅 opt-in 验证；默认三渠道入口不切换；取消/stale/失败必须 fail-closed；不改变 SyncData、存档、程序集、构建/部署流程 | 尚未运行；需要已初始化游戏 host 或等价可控 host fixture | IN PROGRESS |
| 2026-08-30 | 三渠道旧主回复 transport 收口到 LegacyShoutNetworkGateway | `Refactor/Contracts/LegacyShoutNetworkGateway.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`；将剩余 Scene/Native/Courier 非流式与流式主回复调用统一经过 Gateway 兼容边界，原样保留 token 统计、prompt retry、thinking、取消、SSE 回调和错误文本语义 | Gateway 仍以 `ShoutNetwork` 作为 legacy provider 实现；未伪造 detached capture、规则、ActionPlan 或存档迁移；不改变三渠道业务动作、历史/AFEF、SyncData、程序集或默认时序 | 外部直接调用点清零（仅 Gateway 内部保留 legacy transport）；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`；Configured/Interaction/ProductionOptIn/SSE/TTS/WorldDiplomacy replay 全部 PASS；未部署 | VERIFY |
| 2026-08-30 | Primary legacy transport 可控回放与取消 seam | `ShoutNetwork.cs`、`tools/PrimaryLlmGatewayReplayTests/`；为非流式 primary send point 增加仅 Debug 的 scoped provider override，验证生产 Stage 程序集经 `LegacyShoutNetworkGateway` 的 thinking→plain retry、最终文本、credential body boundary 和 caller cancellation | 仅 Debug 测试 seam；发布路径仍使用 `DuelSettings.GlobalClient`；不改变 payload/解析/重试/错误文本、默认三渠道、存档或配置来源 | `primaryLlmGatewayReplay success=1 thinkingPlainRetry=1 credentialBoundary=1 cancellation=1`；未部署 | VERIFY |
| 2026-08-30 | 生产程序集 detached host 集成回放 | `tools/ProductionDetachedHostReplayTests/`；直接加载 project-local 1.4 Stage 的生产程序集，以动态端口接入真实 `LegacyChannelInteractionFacade`、`FullInteractionPipeline`、`DetachedInteractionHost` 和 `InteractionResultCommitter`，验证 main/postprocess/visible/commit 边界 | 仅使用 fixture Gateway 和内存代理，不解析/持有 live Bannerlord 对象，不写真实存档；不改变默认三渠道入口、动作执行或发布结构 | `productionDetachedHostReplay capture=1 main=1 postprocess=1 visibleFinal=1 commit=1 memoryBoundary=1 fallbackIsolation=1`；未部署 | VERIFY |

| 2026-08-30 | 阶段 7 Knowledge/RAG Gateway owner 边界与可控回放（本轮） | `KnowledgeLibraryBehavior.cs`、`Refactor/Adapters/LegacyKnowledgeRagGateway.cs`、`tools/KnowledgeRagGatewayReplayTests/`；将 RAG 短句生成的 provider 配置、主回复阶段约束、取消和凭据发送边界收口到领域 Gateway，并以本地可控 provider 回放验证 | 保留现有 RAG prompt、最大 token 限制、禁用 thinking、解析/确定性 fallback 和知识数据写入时序；不改变 SyncData/key/type、默认三渠道、历史/AFEF、构建/部署脚本或程序集身份；API key 只在发送边界 | `knowledgeRagGatewayReplay success=1 empty=1 providerFailure=1 cancellation=1 nonMainExclusion=1 credentialBoundary=1`；Debug/Release 1.3/1.4/Bootstrap 均 `0 warning / 0 error`；真实知识库、旧存档和游戏内回放仍 NOT-RUN；未部署 | VERIFY |

- 最新 handoff：`docs/handoffs/2026-08-30-refactor-preparation.md`
- 下一位接手者先读取：`CLAUDE.md`、`.claude/skills/animusforge-maintainer/SKILL.md`、本文件、baseline 和最新 handoff。


| 2026-08-30 | 阶段 7 Courier inbound/reply 生产 opt-in host 回放（本轮） | `CourierDeliveryBehavior.cs`、`tools/ProductionCourierHostReplayTests/`；通过生产 Courier capture、detached ports、facade 和 host 接入可控 Gateway，验证 reply 的主/后处理与 inbound 的 seed/history 边界 | 仅 opt-in/等价 host fixture；保留信使送达/返回状态机、旧 facade、user/assistant/AFEF 语义和失败回退；不切换默认入口、不改变 SyncData/key/type、构建/部署脚本或程序集身份 | `productionCourierHostReplay courierPorts=1 replyMain=1 replyPostprocess=1 replyCommit=1 inboundMain=1 inboundCommit=1 inboundNoUserSeed=1 cancellationBoundary=1 fallbackIsolation=1`；真实 Bannerlord host、旧存档和游戏内回放仍 NOT-RUN；未部署 | VERIFY |

| 2026-08-30 | 阶段 7 NPC Policy generation job 取消传播与读档清理 | `PolicySystem/Npc/NpcRulerPolicyBehavior.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Persistence.cs`；为 generation job 建立运行时 CTS，贯穿 draft/effect/repair Gateway 请求，读档/新游戏取消旧 job，取消结果不进入 pending commit | 保持 Policy route/profile/JSON、重试、stale/version、存档 key/type、主线程提交和默认行为；CTS 不序列化；不切换三渠道、不改构建/部署脚本 | 代码与现有 Policy Gateway/InteractionPipeline 回归已通过；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Policy provider、旧存档和游戏内回放 NOT-RUN；未部署、未提交、未推送 | VERIFY |

| 2026-08-30 | 阶段 7 auxiliary/event LLM 入口 owner 审查（已完成静态部分） | 首批只读审查 `MyBehavior.CallUniversalApiDetailed`、`DuelSettings`/`ModOnboardingBehavior` 验证入口及其他 auxiliary/event transport；确认共享 Gateway 覆盖、owner、凭据、取消、stale、fallback 和线程边界 | 不修改生产 C#、构建/覆盖/推送脚本、默认三渠道、SyncData/key/type、程序集身份或游戏目录；若发现缺口，先登记最小后续切片再改代码 | 计划：静态调用图、owner/credential 检查、必要的纯回放；生产游戏/真实 provider/旧存档仍需单独验收 | ACTIVE |

| 2026-08-30 | 阶段 7 DuelSettings 聊天连接测试 POST Gateway 收口（本轮） | `DuelSettings.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`tools/ConfiguredChatValidationReplayTests/`；将四条 MCM 聊天连接测试的 HTTP 发送收口到共享配置 Gateway 原始验证交换边界 | 保留各设置 owner 的 prompt、模型/温度/thinking 控制、后处理 JSON 校验、错误提示和 UI；凭据仅在发送边界；GET models、默认三渠道、存档、构建/部署和程序集不变 | `configuredChatValidationReplay success=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 MCM 与旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |

| 2026-08-30 | 阶段 7 ModOnboarding GET models/模型获取 Gateway 边界（本轮） | `ModOnboardingBehavior.cs`、`DuelSettings.cs`、`Refactor/Adapters/LegacyModelCatalogGateway.cs`、`tools/ModelCatalogGatewayReplayTests/`；将 onboarding Base URL 探测、带凭据模型列表请求和 MCM 模型刷新收口到模型目录 adapter | GET `/models` 保持独立于聊天 Gateway；业务 owner 继续负责 HTTP 状态策略、模型解析、排序/UI 和 `_baseUrlValidationVersion`/`_modelFetchVersion` stale 时序；凭据只在发送边界，不进公共 DTO、存档或日志 | `modelCatalogGatewayReplay probeNoCredential=1 fetchCredentialBoundary=1 httpFailure=1 cancellation=1 invalidConfig=1`；相关 Configured/Validation/InteractionPipeline 回归通过；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 onboarding、旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |

| 2026-08-30 | 阶段 7 ModOnboarding 聊天验证 POST Gateway 收口（本轮） | `ModOnboardingBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将组合验证 `ValidateApiTargetAsync` 和 MCM 单目标验证接入共享配置聊天验证 exchange，保留 provider-specific payload、响应解析、HTTP 状态/错误提示以及 `_apiValidationVersion`/CTS/stale/UI pending 时序 | 不混入 GET `/models` 协议；不切换 Native/SceneShout/Courier 默认路径，不改变 SyncData/key/type、存档、程序集、构建/部署脚本；凭据仅在发送边界 | `ConfiguredChatValidationReplay success=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；Configured/ModelCatalog/InteractionPipeline 回归通过；1.4 direct 与 Debug/Release unified stage 的 1.3/1.4/Bootstrap 均 `0 warning / 0 error`；真实 provider、游戏内 onboarding、旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 7 ModOnboarding provider-specific 验证失败语义（本轮） | `ModOnboardingBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`tools/ProductionValidationProviderReplayTests/`、`tools/ConfiguredChatValidationReplayTests/`；验证共享 validation exchange 原样保留 OpenAI/Anthropic provider 转换、响应解析与 YJ/Gemini thinking 控制，并修复已准备 JSON 的二次转换风险 | 保留 provider payload、thinking 控制、原始响应解析、HTTP 状态/错误提示、取消/stale/UI 时序；已准备 JSON 使用 raw validation 入口；不改变默认三渠道、存档、SyncData/key/type、程序集、构建/部署脚本或游戏目录 | `productionValidationProviderReplay openAi=1 anthropic=1 yjGeminiThinking=1 credentialBoundary=1`；`configuredChatValidationReplay success=1 preparedJsonPreserved=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；ModelCatalog/InteractionPipeline 回归通过；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 onboarding、旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 7 XihaiAction auxiliary classifier transport owner 审查（本轮） | `extensions/AnimusForge.XihaiAction/src/Runtime/AfV130AuxiliaryTextClassifier.cs`、`AfClassifierTransport.cs`、`AfV130ConfiguredGatewayTransport.cs`、`AfV130CallApiTransport`、`tools/XihaiClassifierTransportReplayTests/`；确认 Gateway owner、凭据解析、single-flight、caller/lifetime cancellation、可选依赖 fallback，并修复 Dispose 与已取得 `SemaphoreSlim` 的竞态 | 保留 classifier closed-set 协议、输出限制、可选扩展降级、战术安全规则、默认三渠道、ActionPlan、存档、程序集、构建/部署脚本和游戏目录；不改变发布身份 | `xihaiClassifierTransportReplay shortCircuit=1 closedSet=1 ordinarySingleFlight=1 consentLimit=1 battleSpeechLimits=1 lifetimeCancellation=1`；XihaiAction Core `88 passed / 0 failed`；StaticVerifier `13 passed / 0 failed`；主模块 Debug/Release unified stage 仍 `0 warning / 0 error`；扩展 runtime 独立编译因缺少 .NET Framework 4.7.2 Developer Pack `NOT-RUN`；真实扩展加载、provider、旧存档和游戏内分类器仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 SyncData key/type/owner/chunk 兼容审计（本轮） | `docs/fixtures/phase4-persistence-profile-config/persistence-catalog.json`、`tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`、`tools/PersistenceChunkReplayTests/`；对账 95 个字面量 key、13 个 chunked string 基础 key、38 个 flattened dictionary 基础 key、40 个符号来源，并验证真实 `CampaignSaveChunkHelper` 的 UTF-8 分块/恢复/损坏隔离 | 只做 fixture/validator/test 工具；不改变现有程序集身份、CampaignBehavior 类型、SyncData key/type、存档 owner、迁移运行时、构建/部署脚本或游戏目录；legacy inline fallback 和未知数据策略保持原样 | `persistenceProfileConfig literalKeys=95 symbolicSources=40 chunkedStringKeys=13 flattenedDictionaryKeys=38 chunkMaxBytes=12000 ... PASS`；`persistenceChunkReplay smallInline=1 utf8Boundary=1 missingChunk=1 oversizeCount=1 legacyFallback=1 dictionaryRoundTrip=1 corruptDictionary=1 safeSyncIsolation=1`；真实旧存档加载/typed runtime 仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 typed SyncData ref 绑定与 legacy save fixture（本轮） | `docs/fixtures/phase4-persistence-profile-config/syncdata-binding-catalog.json`、`tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`、`tools/PersistenceChunkReplayTests/`；为 95 个 exact key 对账 121 次 `ref` 绑定、8 类 C# 类型，验证同一 key 的 save/load 类型一致和真实 chunk helper 回放 | 只新增审计 fixture/validator/test 工具；不替换 `SyncData` 调用、不改 key/type、程序集身份、CampaignBehavior 注册、构建/部署脚本或游戏目录 | `persistenceProfileConfig ... typedBindings=121 typedBindingKeys=95 typedBindingTypes=8 PASS`；`persistenceChunkReplay ... PASS`；真实旧存档加载、SaveSystem typed binding、SafeMode 运行时仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 legacy-first SafeMode/缺失字段纯迁移 fixture（本轮） | `docs/fixtures/phase4-persistence-profile-config/legacy-first-safe-mode-migration-cases.json`、`tools/PersistenceMigrationContractTests.py`；验证 scalar/list/dictionary/TroopRoster/chunked storage 的旧表示、缺失 key、类型不一致、未知字段、失败不发布和幂等策略 | 纯 fixture only；不接入真实 SaveSystem，不修改生产 SyncData/key/type、程序集身份、CampaignBehavior、构建/部署脚本或游戏目录 | `persistenceMigrationContract cases=6 unknownRetention=1 missingOptional=1 typeMismatchRollback=1 chunkFailureClosed=1 idempotent=1 legacyFirst=1 PASS`；真实旧存档/SaveSystem/SafeMode runtime 仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 存档 owner/type/程序集身份基线对账（本轮） | `tools/PersistenceIdentityAudit.py`；对比当前工作树与基线 `d4cb1467376c6e923f4295dcefc7878c11dbc7c1` 的 `SyncData` owner 类型、CampaignBehavior 类型名、`AnimusForge` 程序集名、SubModule/Bootstrap 注册边界 | 只读 Git/source/assembly audit；不回滚用户改动、不修改生产 C#、SyncData/key/type、构建/部署脚本或游戏目录 | `persistenceIdentity sync=99 behavior=35 module=AnimusForge bootstrap=1 PASS`；`syncAdded=[] syncRemoved=[] behaviorAdded=[] behaviorRemoved=[]`；Debug stage 实现程序集名均为 `AnimusForge`，Bootstrap 为 `AnimusForge.Bootstrap`；真实旧存档仍 NOT-RUN；未部署、未提交、未推送（本轮待提交） | VERIFY |
| 2026-08-30 | 阶段 8 本轮最终审查与回原重构分支推送（当前切片） | 已完成 staged 文件、凭据/私有路径、程序集/模块身份、SyncData key/type、构建/测试证据审查，并创建本轮提交；待将当前工作树无强制推送到 `origin/refactor/prepare-af-restructure`，本地项目目录为 `F:\AF测试重构` | 不 force push、不部署游戏目录、不修改构建/覆盖/推送脚本，不改变默认三渠道切换；真实旧存档/游戏 host/XihaiAction runtime build 的 NOT-RUN 风险保留在提交记录 | 本轮全量相关回归、Debug/Release unified stage、staged diff 审查均完成；剩余为 remote fast-forward 验证 | COMMITTED |
| 2026-08-30 | 阶段 7 configured-chat 流式 Gateway 与 Universal 遗留路径收敛（本轮） | `Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`MyBehavior.cs`、`tools/ConfiguredChatGatewayReplayTests/`、`tools/ConfiguredChatValidationReplayTests/`、`tools/KnowledgeRagGatewayReplayTests/`；新增 adapter-local generation diagnostics 与通用 SSE 流式解析，保留 thinking plain retry、取消、错误状态、响应采样和凭据发送边界；`CallUniversalApiDetailed` 改由 shared Gateway 承接 | 不切换默认 Scene/Native/Courier；不改变 Prompt/Action/Memory/AFEF、SyncData/key/type、程序集身份、三版本发布结构或构建/部署脚本；ResponseBody/RequestBody 仅留在 legacy caller/adapter 诊断边界 | `configuredGatewayReplay success=1 streaming=1 thinkingPlainRetry=1 retryable5xx=1 cancellation=1 credentialBoundary=1`；Configured validation、Knowledge/RAG、Primary Gateway replay PASS；1.4 direct、1.3/1.4/Bootstrap Debug unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 host、旧存档仍 `NOT-RUN`；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 生产三渠道等价可控 Host provider/commit/fallback 验证（本轮） | `tools/ProductionConfiguredHostReplayTests/`；直接加载 project-local 1.4 implementation，使用生产 `LegacyConfiguredChatGateway`、`LegacyChannelInteractionFacade` 和 `DetachedInteractionHost`，以 loopback provider 验证 Native/SceneShout/Courier 的 main/postprocess、commit/history、credential、provider failure fallback 和 cancellation | 等价可控 Host fixture，不代表真实 Bannerlord campaign/mission host；不切换默认三渠道、不执行真实游戏动作、不改变 SyncData/key/type、程序集或部署流程 | `productionConfiguredHostReplay native=1 scene=1 courier=1 mainPostprocess=1 commitHistory=1 credentialBoundary=1 providerFallback=1 cancellationBoundary=1`；1.4 production stage 已加载；真实游戏 Host、live Agent/Hero、旧存档和游戏内 ActionPlan 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt 主线程 replay port contract（本轮） | `Refactor/Adapters/LegacyEconomyRewardDebtMainThreadPort.cs`、`tools/EconomyRewardDebtPortContractTests/`；建立主线程、目标快照、capability 排除、领域异常和 applied count fail-closed 边界，实际变更仍回调现有 domain owner | 仅完成 contract boundary，未接入 `RewardSystemBehavior`、未解析 live Hero/item/debt/settlement，不改变默认三渠道、SyncData/key/type、程序集或部署流程 | `economyRewardDebtPort valid=1 mainThread=1 staleTarget=1 capabilityFailClosed=1 nonEconomyExclusion=1 noApplicable=1 exceptionIsolation=1 countValidation=1 PASS`；1.4 production compile PASS；真实经济动作、旧存档和游戏内验收仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt Hero→玩家生产 owner adapter（本轮） | `RewardSystemBehavior.EconomyReplay.cs`、`Refactor/Adapters/LegacyEconomyRewardDebtMainThreadPort.cs`、`Refactor/Contracts/EconomyRewardDebtContracts.cs`、`Refactor/Adapters/LegacyEconomyRewardDebtAdapter.cs`、`tools/EconomyRewardDebtPortContractTests/`、`tools/ProductionEconomyOwnerReplayTests/`；复用现有 Hero 金币/物品/RP/债务/固定资产 owner，增加主线程与当前目标复核，补齐单参数 GIVE_GOLD、债务期限/备注 | Hero→玩家生产接线；非 Hero/商人/部队仍 fail-closed；不切换默认三渠道、不改变 SyncData/key/type、程序集、存档或部署流程 | `economyRewardDebtPort valid=1 mainThread=1 staleTarget=1 capabilityFailClosed=1 nonEconomyExclusion=1 noApplicable=1 exceptionIsolation=1 countValidation=1 debtMetadata=1 singleArgumentGold=1 PASS`；`productionEconomyOwnerReplay factoryFailClosed=1 productionType=1 noCampaignMutation=1 PASS`；双版本/Bootstrap Debug unified stage `0 warning / 0 error`；真实游戏内经济动作、旧存档和 AFEF 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt PartyBase→玩家 owner adapter（本轮） | `RewardSystemBehavior.EconomyPartyReplay.cs`、`tools/ProductionEconomyOwnerReplayTests/`；新增 PartyBase capture owner factory，复用既有部队金币/物品/RP 物品转移方法，执行前复核 stable subject、active party、主线程和实际数量 | 仅支持 PartyBase→玩家金币/普通物品/RP 物品；DebtCreate、DebtResolve、SettlementTransfer、商人路径仍拒绝；不切换默认三渠道、不改变 SyncData/key/type、程序集或部署流程 | `productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 productionType=1 noCampaignMutation=1 PASS`；Economy port contract `... debtMetadata=1 singleArgumentGold=1 PASS`；1.3/1.4/Bootstrap Debug unified stage `0 warning / 0 error`；真实 PartyBase、游戏内库存、ActionPlan、旧存档和 AFEF 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt Merchant owner adapter（本轮） | `RewardSystemBehavior.EconomyMerchantReplay.cs`、`tools/ProductionEconomyOwnerReplayTests/`；增加 CharacterObject/Settlement owner factory，复用现有商人金币/物品/RP 物品和市场债务方法，执行前复核当前定居点、商人资格、主线程和实际结果 | 支持商人→玩家金币/普通物品/RP 物品/市场债务创建与解除；SettlementTransfer 仍拒绝；不切换默认三渠道、不改变 SyncData/key/type、程序集或部署流程 | `productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 merchantFactoryFailClosed=1 productionType=1 noCampaignMutation=1 PASS`；Economy port contract PASS；1.3/1.4/Bootstrap Debug unified stage `0 warning / 0 error`；真实商人/Settlement、市场库存债务、游戏内 ActionPlan、旧存档和 AFEF 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 7 三渠道 Economy-aware ActionPlan commit 接入（当前切片） | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`Refactor/Runtime/InteractionResultCommitter.cs`、三渠道 owner factory 与 Economy replay contract；在不改变旧入口的前提下，把 Hero/Party/Merchant owner port 接入 detached ActionPlan commit，过滤已确认的 Economy tags，合并 owner confirmed facts | 只在主线程执行；拒绝 stale、缺 capability、部分/零应用和 raw plan 篡改；保留旧 action authority、三渠道 facade、存档/程序集/SyncData key/type、构建和部署流程；不切换默认三渠道 | 计划：新增纯 composite/receipt fixture，生产 1.4 回放覆盖 Native/Scene/Courier 的 Economy route 与 non-economy delegation；真实游戏 host、live inventory/debt、旧存档和 AFEF 仍 NOT-RUN；未部署 | VERIFY |

| 2026-08-30 | 阶段 7 真实初始化 Campaign/Mission Host 与游戏内 Economy 验收（下一切片） | 使用已完成的 owner/state fixture 作为前置，接入或验证真实初始化 Campaign/Mission host；覆盖 Hero/Party/Merchant live inventory、market/debt、三渠道主线程 commit、confirmed facts、旧存档与 AFEF | 保留默认三渠道与旧 action authority；不改变程序集、模块 ID、SyncData key/type、存档类型、构建/覆盖/推送脚本或游戏目录；无真实 host 时不得宣称已完成游戏内验收 | Economy owner/state fixture、生产 1.4 executor、三渠道 production host、World Diplomacy intent-boundary smoke、1.3/1.4/Bootstrap stage 构建已 PASS；真实 live 对象、旧存档、AFEF 仍 NOT-RUN；Host readiness 已通过，只读审计确认游戏未运行且未部署 | ACTIVE |
| 2026-08-30 | 阶段 7 Economy owner/state fixture（本轮） | docs/fixtures/phase7-economy-aware-commit/economy-owner-state-cases.json、tools/EconomyOwnerStateFixtureContractTests.py；固化 Hero、Party、Merchant、Courier-Hero、inactive/stale/missing-settlement/unknown-owner 边界 | 仅为可审计纯 fixture，不创建或修改 Bannerlord 对象；字符串/ID-only；不改变默认三渠道、程序集、模块 ID、SyncData key/type、存档或部署流程 | economyOwnerStateFixture cases=7 eligible=4 rejected=3 stringOnly=1 hero=1 party=1 merchant=1 courierHero=1 failClosed=1 PASS；真实 live host、旧存档、AFEF 仍 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 7 Live Host readiness 只读审计（本轮） | tools/LiveHostReadinessAudit/live_host_readiness_audit.py、README；检查游戏可执行文件、project-local stage、Bootstrap/1.3/1.4 实现、安装模块、SubModule Bootstrap 加载、游戏进程和标准存档目录 | 只读；不启动游戏、不部署、不读取存档内容、不修改游戏目录；installedMatchesStage=false 仅表示未部署 | liveHostReadiness gameRoot=1 exe=1 stage=1 bootstrap=1 implementation13=1 implementation14=1 installedModule=1 gameRunning=0 saveDirs=1 noDeployment=1 PASS；真实 Campaign/Mission、live Economy、旧存档和 AFEF 仍 NOT-RUN | VERIFY |

## 本地化状态（2026-08-30）

- 现有发布资源已有部分中英双语：`AnimusForge/ModuleData/Languages/sceneactions_strings.xml` 与 `CNs/sceneactions_strings-zh-CN.xml` 各 124 条；`sets_hostile_meeting_strings.xml` 与对应中文文件各 6 条；中文 GCCZ 说明书另有 7 条。
- 本轮审查发现 `Refactor/` 没有建立独立的本地化资源/错误码解析边界，`Refactor/Adapters/LegacyModelCatalogGateway.cs` 仍有 3 条中文硬编码诊断文案；因此不能宣称“重构代码已完成中英本地化”。
- 下一项本地化任务：先把对用户可见的 Gateway/配置错误改为稳定 error code + 英文/简体中文资源映射，后台只传 code 和参数，主线程 UI 再解析；不得把 API key、原始响应或凭据放入资源/日志。

| 2026-08-30 | live host 重启探测与本地化审查（本轮） | `tools/LiveHostReadinessAudit/`、安装模块日志、`AnimusForge/ModuleData/Languages/`、`Refactor/Adapters/LegacyModelCatalogGateway.cs`；核对当前部署匹配、游戏进程和中英文资源覆盖 | 只记录证据，不把主菜单/进程存在当作 Campaign/Mission 验收；本轮不改变程序集、模块 ID、SyncData key/type、存档类型或生产本地化调用 | 审计 PASS：`installedMatchesStage=true`、项目/安装 stage 齐全；重启后的 Bannerlord 进程已退出且没有产生新的 Bootstrap 日志，因此真实 live Economy、旧存档、AFEF 仍 `NOT-RUN`；资源覆盖为 SceneActions 124/124、hostile meeting 6/6，重构层仍有 3 条中文硬编码诊断，完整双语本地化 `NOT-DONE` | VERIFY |

| 2026-08-31 | 阶段 7 离线闭环与真实 Host 验收包（当前切片） | owner：Conversation/Memory/Action × Economy；先运行并补齐已有 Economy-aware executor、三渠道 production replay、Memory/AFEF receipt、Persistence identity/migration 和 Gateway 回归，随后准备 Native/SceneShout/Courier 与 Hero/Party/Merchant live 验收清单；不新建平行 pipeline | 只修改阶段 7 相关最小生产/测试/文档路径；保留用户已有 `.claude/settings.local.json`、`RuleBehaviorPrompts.json`、`DuelSettings.cs`、`NobleGatheringBehavior.cs`；不修改构建/覆盖/推送脚本，不部署、不提交、不推送，不切换默认三渠道，不改变程序集/模块/SyncData key/type/存档类型；纯验证不进入 Tick 热路径，回放按请求运行，receipt 保持有界进程内缓存 | 运行现有 contract/replay 与 `git diff --check`；如发现缺口只补最小 fixture/runner；必要时运行现有 unified stage，仅写 project-local 输出；真实 Campaign/Mission、live Economy、真实旧存档和安装目录加载明确记录 `NOT-RUN/BLOCKED` | ACTIVE |
| 2026-09-01 | 阶段 7 Notoriety exact line/session outcome owner（`LOCAL-7-L`） | `PlayerNotorietyBehavior.ConversationOutcomes.cs`、`Refactor/Runtime/NotorietyConversationOutcomeReceipt.cs`、focused contract 与 production opt-in replay；为具备 H recovery/payload/part 和 opaque memory-session identity 的 detached line 建立 `AFNR1` witness，duplicate probe 位于 active/RNG 前，aggregate 与 receipt 同存既有 Notoriety JSON，finalize 冻结绝对 target 并 readback | 保留 legacy public void ABI、默认入口、H/I/K wire、95 literal key/type 与程序集身份；不从 Daily marker、日志或 aggregate 反推成功；loaded Open 转 Unknown 且不重 roll/finalize；不部署、不切 default、不删除 facade | checkpoint `cddc7628`、实现 `80729cb9`；AFNR1 14/14、Interaction 40/69/39、Memory/Courier/Economy、fresh production hosts、Profile 95/121/42/40、Migration 10、Identity 99/35、Debug/Release 六 Stage 全 PASS。独立子代理终审因 usage limit 未执行；真实 Campaign/MBRandom/save-load/crash/旧档/AFEF/default 均 `NOT-RUN` | VERIFY |
| 2026-09-01 | 阶段 8 完整20领域准备态门禁（`LOCAL-8-A`） | `tools/PhaseEightReadiness/`、`docs/phase8/full-domain-readiness-catalog.json`、`cleanup-candidates.json`；保留早期8-ID设计目录，同时把canonical20责任领域、canonical16 Bridge与逐symbol清理/回滚纳入只读证据工具 | 20领域不是20个物理DLL；role/entry未认领保持BLOCKED；PAIR/CROSS_CUT分离；KEEP/HOLD/REVIEW均不授权删除；不改生产C#/default/key/type/玩法/游戏 | checkpoint `9a088f2f`、最终工具提交 `8bdd9363`；PhaseEightReadiness 62 tests、Bridge10/6、Composition18/24、ModuleCatalog8/3/16/8 PASS；all-missing=`BLOCKED/exit2/full20/auth false`；真实Campaign/Mission、旧档、live Economy/AFEF、default/release `NOT-RUN` | VERIFY |
| 2026-09-07 | FirstChance 48k 根因调查交接（当前切片） | `docs/handoffs/2026-09-07-firstchance-root-cause-handoff.md`；记录日志 `count=48220`、NullReference 堆栈、外部 `RelationshipSeaStopPreparation` 工作树路径、Policy history snapshot 异常及下一项定位任务 | 只更新交接/台账；不修改生产 C#、构建/覆盖/推送脚本、默认三渠道、程序集/模块身份、SyncData key/type、存档类型或游戏目录；工作区既有未提交修改全部保留 | 日志已解压审阅；当前仓库无 `RelationshipSeaStopPreparation` 类，故不能宣称根因已修复；真实 Campaign/Mission、旧存档、AFEF 和 FirstChance 修复验证 `NOT-RUN` | ACTIVE |
## 主体 J13e2 Taunt 有限离线收口（2026-09-25）

状态：**`J13e2_OFFLINE_VERIFIED / J13_ACTIVE`**，仅表示本包离线门禁闭合；下一包依[原计划](plans/j13-domain-owners-plan.md)为 e3 Encounter，再依序 e4 Settlement/Inspection、e5 Exercise，不提前 J14。意图 `802beba4`；生产/行为切片 `e62e2a82`（和平 Mission 上下文）、`9ec814eb`（延迟犯罪/信任账本）、`5873d594`（冲突状态/恢复），MCM/聚合接线 `bee366a8`。一基坐标、覆盖及未覆盖责任见[范围图](architecture/af-framework-code-scope.md)与[代码地图](architecture/af-framework-code-map.json)；坐标/字符串契约**不单独构成行为证据**。

- **真实 owner、生产消费者、保存身份**：`src/modules/AF.Module.Taunt/ScenePeaceConflictContextOwner.cs:7–67` 根据 Mission、LocationEncounter/当前 settlement、Campaign location、MapEvent/Encounter 战斗、Siege handler、team AI 类型、MissionMode、围城与和平 location allowlist 判定资格；物理入口额外看 MCM，口头挑衅仍可走原 `TryStartSceneTauntFight`。`SceneTauntBehavior.cs:3056–3093,5365–5382,5449–5591,5593–5689,7450–7464` 的原 Mission host 采集真实 TW 事实，在普通、物理/原生 alley 与 carryover 初始化前消费同一 owner；缺失或不匹配的 encounter settlement 现 fail-closed。`src/modules/AF.Module.Taunt/SceneTauntPenaltyLedgerOwner.cs:9–114` 持有延迟犯罪池与每次击倒 +1.3 的信任小数余额，负责 case-insensitive ID、保存投影/恢复、封顶预留/失败回滚、清除和累计；`SceneTauntBehavior.cs:83–97,131–264,792–859,1027–1078,1130–1170,1270–1325` 保留原 `IDataStore` 键、Campaign 事件、原生 `ChangeCrimeRatingAction` 与信任写入。`src/modules/AF.Module.Taunt/SceneTauntConflictLifecycleOwner.cs:5–47` 独占 active/armed/occurred 状态及普通、carryover、升级、外部 SETS 标记、结束/武装败北保留转换；`SceneTauntBehavior.cs:2357–2405,5495–5520,7490–7510,8156–8170,9769–9828` 原 Mission host 只在转移成功后执行 FightHandler、武器/队伍、惩罚与清理。原 Campaign 类型与保存键 `_sceneTauntDeferredCrimeByFaction_v1`、`_sceneTauntCriminalTrustRewardTenthBySettlement_v1`、armed carryover 等不改；`_crimeRefillReserveByFaction` 的 deprecated 保存壳未借本包删除。
- **Patch/损伤边界（仅离线核对）**：`CampaignComposition.cs:38` 原行为注册一次、`SceneTauntBehavior.cs:119–128,269–286` 原事件/每 Mission 一次挂载保留，独立 Duel Mission 排除。`StartupPatchComposition.cs:333–369` 仍逐个注册五个原 patch：`SceneTauntWieldBlockPatch` 对 `Agent.TryToWieldWeaponInSlot/TryToWieldWeaponInHand/WieldInitialWeapons` 的前缀只在无武器冲突阻拦；`SceneTauntMissionDifficultyPatch` 对 `SandboxMissionDifficultyModel.GetDamageMultiplierOfCombatDifficulty` 的后缀仅本机制改完整战斗倍率；`SceneTauntNativeConversationBlockPatch` 对 `MissionConversationLogic.StartConversation(Agent,bool,bool)` 与 `MissionAlleyHandler.CheckAndTriggerConversationWithRivalThug/StartCommonAreaBattle` 的前缀仅冲突升级阻拦；`SceneTauntLeaveMissionBlockPatch` 对 `BasicLeaveMissionLogic/MissionFightHandler.OnEndMissionRequest` 的前缀仅本机制不可离场时阻拦；`SceneTauntFightAutoEndDelayPatch` 对 `MissionFightHandler.OnMissionTick` 的前缀仅本机制延迟原自动结束。非本机制/异常保留原版继续路径；未新增 `PatchAll`/事件订阅。`OnAgentHit/OnScoreHit/OnAgentRemoved` 在 host `:4000–4130`，owned-settlement 被动攻击 `:3015–3053,3900–3974` 先通过和平资格，`ClearRuntimeState:9791–9828` 调用原队伍还原并清空状态；关闭不是把伤害归零，已激活冲突的收尾仍走原路径。原生事件顺序/真实敌对队伍恢复仍须游戏内测。
- **可复核离线证据**：`SceneTauntContextContractTests` **22/22** 和 penalty **16/16**、lifecycle **14/14**，覆盖和平正例、battle/siege/arena/training/mismatch 反例、物理 MCM off/口头独立、重复进入/升级/结束重入、犯罪封顶/部分扣除/失败回滚/清池、信任小数；`PhaseEightParityReplayTests` 对当前 Debug 1.4 生产 DLL SHA256 `4C8FDC5AF6BD57AFDBB380B639FB4340792FA08237A0F2515C40EB2434DAE0A9` 反射回放三个实际 owner，`J13E2DomainOwnerContractReplay` 另核对生产入口、五 patch、保存/惩罚/恢复接线，完整 Phase8 通过。原脚本的四个仓内重建目录绝对路径/内容/无重解析点重核后，不带 `-Stage/-Deploy` 完成 Debug/Release × Bannerlord `v1.3.15.110062`/`v1.4.7.117484` + Bootstrap 六构建，均 0 警告/错误；四实现 SHA256 为 Debug 1.3 `7906F20FA15255FB805A60BAAE3E0962420E367C29D6982A0904CA795152896A`、Debug 1.4 如上、Release 1.3 `22136CE58FCD0F444BAD9BAEDD2E68000CF3FE639598938A20C3F14A7CB2C516`、Release 1.4 `80CB23DB4E19BADE7A2ECDBAB2AAF37A02BAF923230F811AF39CB52CE4D62235`。V1 **119**/四 DLL metadata **1080**、PersistenceIdentity **142/36**、迁移 fixture **10**、source inventory **7**、代码地图 **647** 锚点 recorded/working-tree 通过。首轮新命名空间的 `MathF` 1.3 编译失败，改成 `Math.Max/Min` 后六构建通过，失败未隐匿。
- **频率/未验**：资格仅在原冲突候选/攻击/carryover 待激活路径计算，不给普通 Tick 新增全场扫描；生命周期为 O(1) 标量转换；犯罪池仅非空且离开场景可提交时按 pending faction 遍历，保存时投影，无新反射、轮询或锁。生产 DLL 反射仅在测试进程；源码接线不证明 Harmony 已在游戏命中。真实 1.3/1.4 游戏内 Mission/Harmony、原生伤害与队伍敌对顺序、实际 MCM 点击、旧档加载、原生犯罪/信任副作用和帧性能均 **NOT-RUN**，不得冒充实机验收。未 push、Stage、部署、打包、写游戏/外仓或改自动化；`.dotnet-cli-home/` 保留。
## 主体 J13e3 Encounter 开工意图（2026-09-25）

状态：`J13e3_ACTIVE / J13_ACTIVE`，基线 `c27a4408`；仅按[原计划](plans/j13-domain-owners-plan.md)推进目标解析、释放授权/超时、pending 返回与会面生命周期，之后 e4、e5，不提前 J14。已复核[军团成员目标案例](army_member_custom_meeting_target_case.md)与[1.3/1.4 差异](bannerlord_1_3_to_1_4_5_compatibility_diff.md)：`EncounterConversationTargetResolver.cs:11–160` 优先显式会话参数，`LordEncounterBehavior.cs:4833–4914,5261–5301` 保留仍合法的已选军团成员，菜单各入口反复消费；`LordEncounterBehavior.cs:76–99,285–325,7253–7375` 持有释放 request/授权/延迟执行，含 encounter/party/Mission/save 代际与 120 秒上限。原 Campaign `CampaignComposition.cs:35`、tick `ApplicationTickComposition.cs:73,114`、三个会话 patch `StartupPatchComposition.cs:276,293,301` 均保持；不得新增重复注册或因本包改默认入口。先修复现有 `EncounterLifecycleBoundaryTests` 仅接受旧机 SDK 路径、在本仓不可运行的受控工具入口，跑源方法提取基线；再按责任逐切片迁真正 owner、补目标/释放/失效回调/聚合证据。`.dotnet-cli-home/` 保留；旧档、原版事件顺序、游戏内会面/1.3/1.4/性能均未由离线 fixture 证明，无 push、Stage、部署、打包、游戏/外仓写入或自动化。
