# AnimusForge Codex Instructions

本仓库是 Mount & Blade II: Bannerlord 的 AnimusForge mod，当前目标分支/目录是 `animusforge-1.3.x`。

## 必须遵循
- 更频繁地考虑使用 `grill-me` skill 来压力测试设计，但普通明确修 bug/加功能时不要为了提问而阻塞实现。
- 不要擅自修改项目的一键编译/覆盖流程；本项目已经有自己的构建和覆盖方式。
- 不要回滚用户已有改动。

## 双版本输出案例

当任务涉及以下内容时，先阅读并套用 `docs/bannerlord_dual_module_output.md`：

- 修改 `一键编译覆盖推送` 下的一键编译、覆盖、推送或打包脚本。
- 修改 1.3.x / 1.4.5 双版本构建流程。
- 调整输出到 Bannerlord `Modules` 目录的模块文件夹。
- 修改 `SubModule.xml` 的模块 `Id`、`Name` 或 DLL 加载方式。

该案例的核心经验：

- 本项目使用一套源码，同时输出 `AnimusForge_1_3_x` 和 `AnimusForge_1_4_5` 两个完整 mod 文件夹。
- 两个输出模块的 `SubModule.xml` 必须使用不同 `Id`，但 DLL 文件名都保持 `AnimusForge.dll`。
- 不要把 1.3.x 和 1.4.5 混放进同一个 `Modules/AnimusForge` 文件夹。
- 不要覆盖游戏或 TaleWorlds 原版 DLL。

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

## Bannerlord 1.3 / 1.4.5 compatibility diff

When a task touches Bannerlord API usage, campaign behaviors, mission behaviors, encounter flow, party/roster logic, settlement models, Gauntlet UI, Harmony patches, dual-version build scripts, deployment, or packaging, first read and apply:

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
- Before covering Bannerlord modules, build both `BannerlordApi=1.3` and `BannerlordApi=1.4` or use the repository one-click dual-version scripts.
