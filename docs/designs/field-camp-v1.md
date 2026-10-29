# 大地图扎营与营中检阅 v1 设计（2026-10-30 定稿）

来源：与用户逐条确认的设计问答（Q1–Q55）。本文件只记录已定决定和调查事实，尚未实现。

## 1. 与旧检阅的关系

- 旧入口（终端 `troop_inspection`，`UI/Terminal/AnimusForgeTerminalBehavior.cs:780`、`TroopInspection/TroopInspectionBehavior.cs:226`）保留并存，营地版本稳定后再决定是否删除。
- 扎营期间禁用旧检阅，提示先拔营。旧 `TroopInspection/` 只加这一处互斥判断。
- 营中不做操练指挥，操练留在旧检阅。
- 新代码放在 `src/AF.GameAdapter.Bannerlord/FieldCamp/`。

## 2. 大地图扎营

### 2.1 入口与条件
- 入口：大地图快捷键（MCM 可改）+ AF 终端新增一项。
- 只有玩家能扎营，AI 不扎营。
- 允许：玩家独立行动，或玩家是军团长（整个军团停下）。
- 禁止：定居点附近、海上、围城中。

### 2.2 流程
- 搭营 1 小时、拔营 1 小时。
- 扎营后弹出营地菜单（GameMenu）：巡视营地 / 休整等待 / 拔营。进场景是可选项，休整收益不依赖进场景。
- 现有停队实现为 `MobileParty.SetMoveModeHold()`（如 `TroopInspectionBehavior.cs:1840`），仓库此前没有任何扎营、等待菜单代码。

### 2.3 休整收益
- 伤员恢复速度 ×2。
- 士气每小时 +1，扎营恢复上限 70；更高士气只能靠营中互动。
- 军团扎营时收益覆盖整个军团；营中互动结算只作用于玩家主队。
- 不碰食物、经济。

### 2.4 军团
- 凝聚力照常下降，不改原版模型（这就是久驻的代价）。
- 扎营期间离开军团的部队：拆掉它的营地模型，不再享受收益，下次进营不再生成它。不锁定成员。

### 2.5 遇敌
- 不做岗哨预警或营地防御加成，按原版遭遇处理。
- 遭遇触发时自动拔营，不扣拔营时间，未处理问题按“置之不理”结算；战斗用原版场景。

### 2.6 大地图营地模型
- 每支部队各放一个独立实体，大小按这支部队自己的兵力分 1 / 3 / 5 顶帐篷。不放篝火（没有地图比例的篝火预制体）。
- 帐篷：可汗 `map_icons_khuzait_tent_a/b`，巴丹尼亚 `map_icons_battania_tent_a/b`，其他文化 `map_icon_siege_camp_tent`（`Native/Prefabs/map_icon_parts.xml`、`props_siege_mapicons.xml:120`，仅在 1.4.8 核实）。不用 `map_icon_siege_camp_1..5`，避免看起来像围城。
- 取 Scene：`((SandBox.MapScene)Campaign.Current.MapSceneWrapper).Scene`，1.3/1.4 写法相同。
- 用 `GameEntity.Instantiate(scene, prefab, MatrixFrame)` 建独立实体，不挂到队伍 `StrategicEntity`（视觉重置会重建）。1.3 的帧重载多一个 `scriptInclusingTag` 参数，调用时不传。不复用 `AddNewEntityToMapScene`（不返回实体，无法移除）。
- 位置：扎营那一刻在部队位置附近偏移，检查是可通行陆地，否则用部队原位置。只算一次，部队本身不散开。
- 重建：`MapScreen.OnFinalize` 会 `ClearAll()`，读档会新建 Scene。在 campaign tick 中，仅扎营状态下比较“当前地图 Scene 与缓存是否同一个”“实体是否仍有效”，失效则按存档状态重建。不加 Harmony patch。
- MCM 开关，默认开启。放置失败时退回只显示“扎营中”，写日志，扎营照常可用。

### 2.7 存档
- 扎营状态、营地位置、开始时间、未处理问题、兵种记忆写入 AF 存档。读档后仍在营中。
- 属于存档契约：第 1 步落地前先给用户看字段清单。

## 3. 营地场景

### 3.1 Mission
- 新写和平 mission（参照 `MissionState.OpenNew("AnimusForge_WildernessDuel", ...)`，`DuelBehavior.cs:3200`），不沿用旧检阅的 dummy 敌方队伍 + MapEvent + 战斗 mission 做法（`TroopInspectionBehavior.cs:1827-1858`）。
- 场景按地形选原版野战地图（`battle_terrain_*`，同 `SceneModel.GetBattleSceneForMapPatch`）；不用藏身处场景。
- 士兵、站位、物件全部由 AF 生成。`Mission.SpawnTroop` 1.3 多一个 bool 参数，走 `BannerlordApiCompat.SpawnInspectionTroop`（`BannerlordApiCompat.cs:291`）。注意 1.4 过早使用旧出生点可能掉进虚空（兼容差异文档 `:68`）。
- 新场景类型要纳入场景伤害防误触 allowlist 判断（`docs/scene_damage_context_guard_case.md`）。

### 3.2 人数
- Agent 总上限默认 150（MCM 可调），按兵种比例抽样。
- 营帐、篝火数量按真实兵力计算，帐篷最多 40 顶。
- 伤员、哨兵占 Agent 名额。

### 3.3 物件布置
- 帐篷按文化：玩家主队按家族文化；军团其他领主按各自文化。可汗、阿塞莱、巴丹尼亚用各自帐篷，其他文化统一中性帐篷。
- 候选预制体（仅 1.4.8 核实，1.3 未实测）：
  - 帐篷 `khuzait_tent_a/b`、`tent_aserai_a..d`、`tents_pict_a..e`、`battania_street_tent_a`
  - 篝火 `campfire`、`burning_campfire`、`burning_campfire_big`、`outdoor_fire_small`
  - 旗帜 `flagpole_a`、`flag_pole_big`、`generic_flag_vertical_a_front`
  - 补给 `crate_a..c`、`bd_barrel_a`、`bd_sack_heap_a`、`bd_cart_a`
  - 栅栏 `fence_a`、`fence_old_a1`；武器架 `weapon_rack_a` 等；床 `bed_convolute_a`
  - `cargo_tent` 只有 1.4，`siege_barricade_*` 带 `DestructableComponent`，单独摆放前要验证
- 岗哨：栅栏/障碍 + 火堆或旗杆 + 哨兵 Agent。岗位数按兵力计算，有上限。
- 伤兵帐：帐篷 + 床 + 地铺，伤员在旁坐卧，医生同伴站旁边。
- 补给区：木箱、木桶、粮袋、货车，数量按存粮缩放，军需官站旁边。
- 每类物件一张候选名单，初始化时逐个尝试，失败换下一个，全部失败则跳过该类并写日志。

### 3.4 碰撞
- 帐篷、栅栏开物理；篝火、木箱等小物件不开。
- 营地按网格布置，留宽通道，士兵移动目标点只放在通道上；卡住时沿用场景 Agent 移动案例的卡死兜底。
- 不在运行时改 navmesh（原版帐篷没有可挂接的 navmesh 面组）。

### 3.5 人物
- 士兵平时过营地生活：围篝火、站岗、在帐篷边闲逛。
- 集合 / 解散只通过喊话触发，后处理标签执行，不设快捷键；复用 `docs/scene_agent_command_movement_case.md` 的 Agent 目标节点做法。集合时一次性计算各兵种方阵位置，解散后各回原位。
- 同伴按原版队伍职务站位（军需官在补给区、医生在伤兵帐、侦察兵在岗哨）。
- 俘虏关在俘虏栏，可交谈，屠戮标签（`TroopInspectionPrisonerSlaughterRuntime.cs`）沿用。
- 军团其他领主站在各自营帐旁，用现有 Hero 对话交谈。

### 3.6 时间与退出
- 每次巡视离开时按固定时长扣大地图时间（默认 1 小时，MCM 可调）。
- 按 TAB 回到营地菜单。

## 4. 交谈与问题

### 4.1 渠道
- 复用场景喊话链路，新增“营地”场景类型。场景喊话已支持普通士兵 Agent（`IsHero=false`，`Prompt/SceneAgentIdentityPromptCaptureAdapter.cs:1531-1592`）。
- 遵循 `docs/free_conversation_scene_shout_alignment.md` 的三渠道对齐规则。

### 4.2 兵种记忆
- 新记忆键：玩家主队 + 兵种（`CharacterObject`）。现有非 Hero 记忆只有野外部队的 `af_nonhero:`（`docs/wilderness_nonhero_party_memory_case.md`）。
- 三渠道（信使、自由对话、场景喊话，含旧检阅和战场喊话）对玩家主队普通士兵都读写这份记忆。
- 每个兵种保留最近 20 条，超出部分压缩成摘要。只限玩家主队。

### 4.3 问题生成
- 扎营那一刻由 C# 判定一次，挑最严重的 1–3 件。
- 开场白用双语本地化模板，玩家搭话后才走 LLM（前处理、主链路、后处理）。
- 问题类型：

| 类型 | 条件 |
|---|---|
| 欠饷 | 最近有发不出工资的记录，或金币低于一周工资 |
| 缺粮 | 剩余食物少于 3 天 |
| 伤员多 | 伤员占比 > 25% |
| 连败 | 最近 7 天输过战斗 |
| 久未休整 | 连续行军超过 N 天没有进城或扎营 |
| 士气低落 | 士气 < 30 |
| 兵种不睦 | 两个文化的兵种都占较大比例（阈值实测再调） |
| 同伴不和 | 两个同伴关系为负，或个性冲突 |
| 请战或请赏 | 部队状态良好时的正面请求 |

- 同伴也会提问题，同伴之间的不和算问题类型。

### 4.4 结算
- LLM 后处理输出结算标签（形如 `[CAMP_RESOLVE:问题ID:鼓舞|应允|驳回|整肃]`），C# 校验后当场执行。不做承诺制。按 `docs/directive_tag_output_case.md` 同时注入前处理话题与后处理 `{tag_rules}`。
- 不新增军纪数值，效果全部落在现有数值上：

| 处理 | 效果 |
|---|---|
| 鼓舞 | 士气 +3 到 +8；部分问题要花钱或物资，钱不够不能选 |
| 应允 | 当场满足请求，士气 +5，付出相应代价 |
| 驳回 | 士气 -2 到 -5，不花钱 |
| 整肃 | 全军士气 -3，提问题的兵种再 -3；可开除闹事者（每次最多 1% 兵力）；来自同伴则关系 -5 到 -10；同一次扎营同类问题不再出现 |

- 一次扎营营中互动带来的士气提升总共不超过 +20。
- 仓库没有士气修改封装，只有直接写 `party.RecentEventsMorale`（`SiegeAiInterventionBehavior.cs:2671`、`:5380`），需新增一个小封装。

### 4.5 延续
- 未处理的问题留到下次进营；拔营时按“置之不理”结算（士气小幅下降）。重复进营不重新生成。

## 5. 性能与运行频率

- 大地图：扎营状态与休整收益只挂 `HourlyTick`；问题判定只在扎营时做一次；营地模型重建检测在 tick 中只做两次引用比较，且仅在扎营状态下。
- 营地场景：物件在 mission 初始化时一次性布置；士兵 AI 每 0.5 秒一批、每批最多 20 人；列队位置只在集合时计算一次。
- 兵种记忆：只在对话写入时更新，按键直接查。
- LLM：开场模板不调用；每轮对话、每次集合 / 解散喊话各走一次三段式链路。

## 6. 交付顺序

每步完成后本地提交；不主动编译或部署，除非用户要求。

1. 扎营菜单、休整、遇敌处理、大地图营地模型、存档字段（先给用户看清单）、MCM
2. 营地 mission：场景选择、营地布置、士兵生成与营地生活、集合与解散
3. 营中交谈：接入场景喊话，新增兵种记忆键并三渠道对齐
4. 问题系统：判定、开场模板、后处理结算标签、四种处理方式
5. 同伴职务站位、同伴问题、军团领主、俘虏栏

## 7. 默认数值（全部 MCM 可调）

搭营 1h、拔营 1h、伤员恢复 ×2、士气 +1/h 至 70、营中士气提升上限 +20、整肃开除每次最多 1%、Agent 上限 150、帐篷上限 40、巡视扣时 1h、每兵种记忆 20 条、每次扎营问题 1–3 件。

## 8. 风险与未确认

- 1.3 只保证编译通过，不做实机测试。所有预制体名称只在 1.4.8 安装中核实，1.3 未实测（本机 `Native` 为 1.4.8，`package_info.txt` 却写 1.3.4）。
- 进出定居点或 mission 时 `MapScreen` 是否被销毁未确认，靠 2.6 的重建检测兜底。
- 帐篷开物理后的士兵寻路效果未实测。
- 旧检阅的终端本地化键：`AF_TERMINAL_UI_Root_TroopInspection(_Hint)`（`af_ui_strings.xml:2062`），检阅提示键 `AF_EX_UI_Inspection*`（`afexercise_ui_strings.xml`）。新功能的字符串需按 `af-bilingual-localization` 同时补中英。
