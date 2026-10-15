# GCCZ 村庄治理与和平领地入口：AF 接线说明（2026-10-15）

## 2026-10-15 复核与游戏集成更新

本节取代下文首次交付的“未部署／防跨访问刷新待做”状态。AF 产品 `16b93932` 已合入游戏当前阅兵、独立 TTS/GPT-SoVITS（融合 `ece939b5`），双版本构建和部署核验通过：14 个文件更新、其余 4735 个不变。实机仍待玩家测试，未推送。

- 可复用 `VillageDispositionEconomy.DestroyRemainingResources` 在纯毁坏后销毁未取得的四成，保持六成收益，不允许同次访问追加取得；独立提交 `b4b0691`，新增 3 条回归，当前核心 2658 条 PASS。
- AF-only：GCCZ MCM 使用实例草稿与 live SaveTriggered，避免 clone/preset setter 写穿原配置；Undo 后不遮蔽终端新值。主提示词去掉动作标签表，文化自然询问与后处理分开。迁殖接上独立每日食物生产；显式激活的 GCCZ 村庄可 TAB 离开，不受附近屠村目标卡住；菜单优先真实阅兵 EntryId。原 SETS 未胜战斗继续阻拦。
- 用户明确将退出重进刷新资源保留为彩蛋，不加 10 天限制；援助仍按 7/21 天。此项不再是待修问题。敌中藏匿仍未完成。
- 详细唯一台账：宿主 `docs/animusforge-refactoring-and-repository-reorganization-plan.md#gccz-review-game-20261015`；证据 `artifacts/gccz-review-game-20261015/`。MCM/游戏/旧档/真实 AI 不以替身回归代替。

### 首次实现记录（以下为历史边界）


当前融合宿主是 `F:/AF/AF-GCCZ-TOWN-20261007`，分支 `codex/sets-village-gptsovits-test-20261014`，不是历史 G 盘。实际进度只维护宿主 `docs/animusforge-refactoring-and-repository-reorganization-plan.md#gccz-village-governance-20261015`。完整数值见 `../plans/gccz-friendly-village-design-20261014.md`。

## 可复用核心

`Village/` 下 9 个同步文件：VillageAftermathAuthorityKind、VillageAftermathEntryPolicy、VillageDispositionActionCatalog、VillageDispositionDialogueSession、VillageDispositionEconomy、VillageDispositionPromptProfile、VillageDispositionTutorial、VillageGovernanceRules、GcczVillageCastleTutorial。保持命名空间、旧枚举值和已存字段身份。新增友方 visitor、有治理权判定、协助值上限 6、7/21 日援助里程碑、确认阶段／TAB 决策、新标签和双语教程；旧敌中资源数值保持。

友方有 23 个玩家行为；认可游说和核对文化方案是两个内部回执。文化问题回执之后，必须同一英雄的下一轮明确确认才可执行，不能在询问那一轮直接改造。

## AF 实际 owner 与调用点

- `VillageAftermathBehavior` 负责会话、主／后处理、权限、持久化和离场；旧政策执行器已移除，保留旧文化存档键。
- `Governance`：真实交付、每类冷却、不同种类组合、每轮独立到期、原配置协助值。
- `Culture`：英雄／方案／回复绑定、两层选择 UI、10000 金钱、立即迁殖或 84/42 日融合、完成通知和领主记忆。
- `Growth`：会话启动时查实际当前原版／mod model，安装一次 postfix；native 招募概率 × 援助 × 迁殖，生产／正向户数增量只在迁殖期乘 0.2。每日刷新援助缓存，无 Tick 扫描所有村庄。
- `Economy`：沿既有预算及原生 personnel/prisoner/inventory 界面结算；友方政治按最大档补差；实收奴隶才减户。`Massacre` 使用入场目标快照和 0.75 秒调度，复用城镇寻路／战斗帮助器。
- `SettlementEntryTroopSelectionBehavior`：友方／敌中都消费最多 12 名真实健康随行者（含英雄），禁用 GCCZ 中重新触发 SETS 抵抗；只有玩家正常离场请求能默认掠夺，不在通用销毁回调生成奖励。
- `FriendlyEntry`：城镇／城堡和平来源单独标识，跳过 native siege aftermath，清理旧战败领主身份；菜单按敌对／阅兵锚点排序。原 SETS 先结算军事奖励链不变。
- 和平城堡的主提示词、人物身份、即时反应不能仍声明“刚攻下、本城守军战败”。AF 新增 `SiegeCastleRuntimePromptProfile.Build(facts, catalog, peacefulEntry)` 保留旧重载，新增两项 CN/EN 资源并接入实际 reader；这是宿主新本地化层的差异，不整文件覆盖独立仓的旧 Castle/Town 版本。
- MCM 独立 GCCZ 入口只投影原 DuelSettings owner，初始值 0–5；城镇／村庄／城堡教程依序显示。全部设置仍保存到模组 Configs，不改其他 MCM ID。

## 覆盖与未完成

独立当前工作区核心 2655 项通过（包括用户已有的未提交 Council 测试），原 Program.cs SHA256 保持 `3001CA03BF710974E413E89FADBA333A2620B9FD88B0AE592B5CA4024CF37807`。AF 双 API/Bootstrap/双 Coup 门禁通过；25 项源码资源接线；两份实际 DLL 的 CN/EN 和平／战后城堡提示词各 8 项回放。原分块持久化回放通过，不代表新增 native 存档或实机已验。

仍未实现：敌中藏匿；跨访问预算与原版恢复的严格关联。当前预算只防同次访问重复领取，不得宣称退出重进已防刷。友方本就不提供藏匿。文化中止当前不退款（口径仍待确认）；5 件物资当前只接受非食物非动物贸易品。菜单位置、MCM 取消/保存、真实 AI 两步确认、场景演员、推进天数与旧档必须实机验收。

没有部署、打包或推送。独立仓其他 Council/UI 未提交文件和原 Program.cs 不纳入本次提交。详细构建日志/产物哈希与来源提交见宿主唯一台账和 `artifacts/gccz-village-governance-20261015/receipt.json`。
