# AnimusForge Codex Instructions

## Local refactor continuation boundary

- For the 2026-08-31 continuation, use `G:\AFMOD\AF-REFACTOR` on `codex/af-main-refactor-continuation-20260831`; see the current execution ledger before editing. Historical NEW-087 and F-drive labels below do not select this machine's worktree.
- Preserve existing NEW-10, GCCZ, other worktrees and authors' changes. Writes outside the task workspace require explicit approval for the exact change; keep proposals local otherwise.
- Create local checkpoint/intent commits before substantial changes and commit each verified slice. Roll back with focused inverse commits, never hard reset or rewritten history.
- Do not push, deploy to the game, install global skills, or change the default interaction entry points without explicit authorization. Keep contract/replay evidence separate from live-game acceptance.

本仓库是 Mount & Blade II: Bannerlord 的 AnimusForge mod，当前目标分支/目录是 `animusforge-1.3.x`。

## 必须遵循
- 所有代码编写必须优先顾虑性能：识别并避免热路径中的全量扫描、重复计算、重复反射、无效分配、无意义锁竞争和空转轮询；新增功能必须说明其运行频率与缓存/分批策略，且不得以牺牲既有规则或功能为代价进行优化。
- 更频繁地考虑使用 `grill-me` skill 来压力测试设计，但普通明确修 bug/加功能时不要为了提问而阻塞实现。
- 不要擅自修改项目的一键编译/覆盖流程；本项目已经有自己的构建和覆盖方式。
- 不要回滚用户已有改动。

## 单模块双实现输出案例

当任务涉及以下内容时，先阅读并套用 `docs/bannerlord_dual_module_output.md`：

- 修改 `一键编译覆盖推送` 下的一键编译、覆盖、推送或打包脚本。
- 修改 1.3.x / 1.4.x 双实现构建流程（当前源码差异基线为 1.4.5）。
- 调整输出到 Bannerlord `Modules` 目录的模块文件夹。
- 修改 `SubModule.xml` 的模块 `Id`、`Name` 或 DLL 加载方式。

该案例的核心经验：

- 本项目使用一套源码，分别编译 1.3.x / 1.4.x 实现，但只发布一个 `Modules/AnimusForge` 模块。
- `SubModule.xml` 只加载 `AnimusForge.Bootstrap.dll`，Bootstrap 根据游戏版本只加载一个实现。
- 两个实现保持程序集名 `AnimusForge`，分别存放在 `versions/1.3/AnimusForge.dll` 和 `versions/1.4/AnimusForge.dll`，严禁同时加载。
- 模块 `Id`、`Name` 和输出文件夹统一为 `AnimusForge`，打包只生成一个 ZIP。
- 不要恢复 `AnimusForge_1_3_x` / `AnimusForge_1_4_5` 双模块输出，也不要覆盖游戏或 TaleWorlds 原版 DLL。

## 百科按钮注入案例

当任务涉及以下内容时，先阅读并套用 `docs/encyclopedia_button_injection_case.md`：

- 在百科页增加按钮。
- 在 Hero/NPC 百科页注入 UI。
- 按 MCM 开关显示或隐藏百科按钮。
- 点击百科按钮后打开 AnimusForge 自定义编辑器、弹窗或菜单。
- 处理 Gauntlet 按钮看得见但点不动的问题。
- 处理文本输入时百科页面响应 Backspace、切页、前进/后退等按键的问题。

该案例的核心经验：

- 用 `GauntletMovie.Load` 获取真实 `IGauntletMovie.RootWidget`，自动生成 prefab patch 只作为 fallback。
- `ButtonWidget` 要接收事件，文字子控件不要抢事件。
- 真实 root 可能不是自动生成类，需要缓存 root 和 datasource 的关系。
- 自定义文本弹窗打开时，要阻止原版 `SandBox.GauntletUI.Encyclopedia.EncyclopediaData.OnTick()` 继续处理百科快捷键。

如果用户说“调用百科按钮案例”“参考之前百科按钮案例”“像编辑个性与背景按钮那样加按钮”，默认就是指 `docs/encyclopedia_button_injection_case.md`。

## 指令标签输出案例

当任务涉及以下内容时，先阅读并套用 `docs/directive_tag_output_case.md`：

- 新增 LLM 后处理动作标签。
- 排查“前处理选中了话题，但后处理标签规则没有注入”的问题。
- 排查“后处理输出了标签，但游戏机制没有实际触发”的问题。
- 修改 `RuleBehaviorPrompts.json`、`ActionPostprocessPrompts.json` 或相关标签解析代码。
- 参考 `Duel`、`IBarter, Bestow, or Exchange Assets`、`Debts and credit`、`hero_join_party`、`Change Settlement Ownership` 这些成功案例。

该案例的核心经验：

- AnimusForge 聊天链路是前处理、主链路、后处理三段式。
- 新机制不能只给主链路加正文规则，还必须把同一话题的 `PostprocessRules` 注入后处理 `{tag_rules}`。
- 标签格式必须同时出现在提示词规则和 C# 解析/执行入口里。
- 成功日志应能追到三段：话题命中、后处理标签输出、机制执行结果。

如果用户说“调用指令标签案例”“参考后处理标签案例”“像 Duel/交易/债务/英雄入队/领地转移那样做标签”，默认就是指 `docs/directive_tag_output_case.md`。

## 场景 Agent 命令移动案例

当任务涉及以下内容时，先阅读并套用 `docs/scene_agent_command_movement_case.md`：

- 场景内 NPC 带路、传唤、跟随、停止跟随。
- 村庄、城市、城堡 mission 场景中的 NPC 命令移动。
- 参考话题 `Lead the way, summon, and follow` 或规则项 `scene_mechanism_actions`。
- 排查“移动命令触发了，但 NPC 实际没移动/移动错/后续会话找不到对象”的问题。

该案例的核心经验：

- 场景内 NPC 命令移动要以 `Agent` / `AgentIndex` / `LocationCharacter` 为目标节点。
- 不要用裸坐标作为主目标节点；坐标只能用于距离判断、站位、门口等待点、stuck 兜底或返回原位。
- 这套案例适用于村庄、城市、城堡等场景内行为，不适用于大地图移动，也不包括原版 meeting 会面专用链路。
- 后处理标签应从【带路与传唤NPC清单】选人物，再由 C# 解析到当前场景 Agent 或 location/proxy。

如果用户说“调用场景 Agent 移动案例”“参考带路传唤跟随案例”“不要用坐标节点，要用 agent 节点”，默认就是指 `docs/scene_agent_command_movement_case.md`。

## 军团成员自定义会面目标案例

当任务涉及以下内容时，先阅读并套用 `docs/army_member_custom_meeting_target_case.md`：

- 大地图接触军团后，原版先进入军团成员选择界面。
- 排查“选择军团其他成员后，自定义会面仍导向军团长”的问题。
- 修改 `ConversationManager.OpenMapConversation`、`ConversationManager.SetupAndStartMapConversation`、`Conversation.Start` 相关 encounter/conversation patch。
- 修改 `LordEncounterBehavior` 的 `_targetHero`、`EnsureEncounterTargetHero(...)`、`IsEligibleCustomLordEncounterTarget(...)` 或自定义会面菜单目标解析。
- 排查军团成员会面、原版 meeting 选择目标、AnimusForge 自定义会面菜单目标不一致的问题。

该案例的核心经验：

- `PlayerEncounter.EncounteredParty.LeaderHero` 在军团遭遇中通常会指向军团长，不能代表玩家在成员选择界面里选中的附属队伍首领。
- conversation 入口 patch 要优先从原版传参解析选中 Hero，再把遭遇首领作为最后兜底。
- 合法性判断要允许当前遭遇军团里的附属队伍首领。
- `EnsureEncounterTargetHero(...)` 不能无条件用遭遇首领覆盖 `_targetHero`；只要当前 `_targetHero` 仍然合法，就必须保留。

如果用户说“调用军团成员自定义会面目标案例”“选军团成员却会面军团长”“自定义会面目标串到军团长”，默认就是指 `docs/army_member_custom_meeting_target_case.md`。

## 场景伤害上下文防误触案例

当任务涉及以下内容时，先阅读并套用 `docs/scene_damage_context_guard_case.md`：

- 修改或新增 `OnAgentHit`、`OnScoreHit`、`OnAgentRemoved`。
- 修改伤害倍率、伤害归零、伤害放大、死亡压制、死亡延迟、昏迷转死亡。
- 修改场景内 `Agent.SetTeam`、`Team.SetIsEnemyOf`、`Mission.PlayerTeam` 或 `MissionFightHandler` 队伍缓存。
- 增加和平场景攻击、挑衅、吵架升级、NPC 反击、犯罪、忠诚度、安全度、关系惩罚。
- 处理“关闭后回归原版逻辑”、藏身点/竞技场/攻城守城/训练场伤害异常。
- 任何未来可能影响场景伤害、击倒、死亡或战斗敌对关系的新逻辑。

该案例的核心经验：

- 不能只凭 `Settlement.CurrentSettlement`、`MobileParty.MainParty.CurrentSettlement` 或 `settlement.OwnerClan == Clan.PlayerClan` 判断和平场景。
- 和平定居点机制必须使用 allowlist，并排除 `PlayerEncounter.Battle`、`MapEvent.PlayerMapEvent`、`CampaignSiegeStateHandler`、`Siege/SallyOut/FieldBattle`、`Deployment/Stealth/Duel`、竞技场、训练场和正在被围攻的定居点。
- “关闭后回归原版”应当退出本模组处理，而不是把伤害改成 0。
- 队伍敌对转换、忠诚度惩罚、死亡延迟等副作用只能在严格命中的机制场景内初始化；后续维护可以依赖已初始化状态。

如果用户说“调用场景伤害案例”“按伤害上下文防误触检查”“兼容攻城/竞技场/藏身点/原版战斗”“回归原版伤害逻辑”，默认就是指 `docs/scene_damage_context_guard_case.md`。

## 信使 / 自由对话 / 场景喊话三渠道对齐案例

当任务涉及以下内容时，先阅读并套用 `docs/free_conversation_scene_shout_alignment.md`：

- 修改信使、自由对话、场景喊话任一 LLM 交流链路。
- 修改前处理、主链路 prompt、后处理标签、动作执行入口、对话历史或每日记忆写入。
- 处理“某一渠道知道/记录/触发了，但另外两个渠道不知道/没记录/没触发”的问题。
- 修改信使回信、自由对话回复、场景喊话回复的历史写入、AFEF 事实写入或 NPC 可见输出。
- 调整给予、展示、债务、交易、领地、部队、世界地图命令、英雄入队、决斗等机制在三渠道中的触发。

该案例的核心经验：

- 信使、自由对话、场景喊话必须共享同一套话题选择、规则资格、历史结构、记忆注入、后处理标签和执行入口。
- 场景喊话当前是最完整实现；信使和自由对话应适配到这套结构，而不是各自发明请求体或历史格式。
- 三渠道都必须写入并读取同一套 AnimusForge 记忆结构，NPC 发言用 assistant 语义，玩家发言用 user 语义，已发生事实必须用 AFEF。
- 出现“信使知道但自由对话/场景喊话不知道”“自由对话能触发但其他渠道不能触发”“同一历史在三渠道格式不同”等现象，默认按三渠道不同步 bug 处理。

如果用户说“对齐三渠道”“信使/自由对话/场景喊话要一致”“按三渠道对齐规则检查”，默认就是指 `docs/free_conversation_scene_shout_alignment.md`。

## Bannerlord 1.3 / 1.4.x compatibility diff

When a task touches Bannerlord API usage, campaign behaviors, mission behaviors, encounter flow, party/roster logic, settlement models, Gauntlet UI, Harmony patches, dual-implementation build scripts, deployment, or packaging, first read and apply:

- `docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md`

Core rules:

- One source tree must keep both `BannerlordApi=1.3` and `BannerlordApi=1.4` builds passing.
- Prefer existing compatibility helpers before adding new `#if BANNERLORD_1_4_OR_GREATER` branches.
- Use `#if BANNERLORD_1_4_OR_GREATER` only for compile-time API/signature differences.
- After compatibility-sensitive edits, verify both builds.

## Local NEW-087 AF/GCCZ Fusion Overlay

- This workspace is the local AF 0.8.7 + GCCZ fused test tree.
- Keep reusable GCCZ rules and profiles in `AnimusForge.SiegeAftermathIntervention`; AF host files should remain thin guarded adapters.
- Mirror reusable GCCZ source/rule changes back to `G:\AFMOD\GCCZ` when they are not already represented there.
- Do not edit `G:\AFMOD\YM0.8.7` directly; it is the requested AF 0.8.7 source staging path but was empty when this workspace was created.
- Before covering the unified Bannerlord module, build both `BannerlordApi=1.3` and `BannerlordApi=1.4` plus Bootstrap, or use the repository one-click single-module scripts.
