## 收紧重生经历与压缩记忆（2026-10-16）

- 手动重生的输入统一限制到当前日及前9个游戏日；过期、未知负日期或未来日期的压缩记忆/历史均不进入，游戏日0合法。最新一块压缩记忆过期时不退回更旧块，只省略该来源。
- 最多8条重要行动，原稳定ActionKind筛选；日常动向/军团集结加入/普通围城进退排除，战斗、被俘释放、亲属死亡、婚育及领地变化保留，其他显式IsMajor事件仍可进入。原近期行动清理/投影复用，存档不变。
- 从原owner最近24条历史中选择最多8条，优先AFEF，其余仅player/npc且最多4条；日期与MemoryId均校验，按原顺序呈现。来源预算为行动2000字符、压缩摘要800字符、历史序列化项共2400字符且单条正文400字符，外层JSON/来源字段另计。
- 中英规则要求按长期影响调整、保留原核心性格，摘要不是其内声明的独立事实证明；普通寒暄/行军/饮食/短暂情绪不足以改变长期人设。程序只做稳定类型/日期/来源/篇幅筛选，重要承诺和关系变化的语义判断仍由实际生成模型完成，不宣称已程序化提取。
- 完整回放573项，专项CN/EN各100项；追加真实host到gateway的旧/未来压缩记忆排除，以及日期边界、日常行动排除、显式扩展重大事件、AFEF优先和普通发言上限。记忆存储/原清理渲染与引擎/provider为fixture，不能替代真实存档或模型验收。

下方16/24条与2000字符压缩摘要等为首次接入历史，当前以本节为准；运行命令仍沿用下方入口。

## 手动重生的最近经历（2026-10-16）

当前回放执行生产 `MyBehavior.PersonaGeneration.CaptureRecentPersonaExperiences`、完整生成适配器、dispatcher、`PersonaRegenerationExperienceContext`、真实Prompt reader/composer及JSON parser。引擎、provider、记忆存储读取与原行动渲染/清理边界使用确定性替身；不能替代真实存档或模型验收。

- 手动重生读取目标人物最近16条近期行动（生产沿用10日窗口）、最新一块压缩记忆与最近24条对话/AFEF。保留日期、speaker/kind和原文；按来源限制请求节选，不改写存档数据。空记忆仍可重生，其他人物资料不进入，自动首次生成不读取这部分输入。
- 最新压缩记忆可能较旧，固定提示词要求尊重日期、区分台词与已确认事实、本人经历与他人行动。旧个性、世界书及用户生成要求仍走原接线；不新增输出语言锁定。
- 全回放518项；专项CN/EN各45项。专项使用各版本实际JSON作为稳定运行文件，执行同一生产reader与最终请求组装。实际引擎、真实provider和玩家旧档未验。

```powershell
python -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py
python -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py --experience-language zh-CN
python -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py --experience-language en
```

下方为原回放历史，实际当前覆盖以本节及本次运行输出为准。

# Hero 人设生成生命周期回归

## 2026-10-03 当前生成优化回归（替代下方旧 Prompt 保留结论）

```powershell
python -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py
python -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/verify_lore_and_settings.py
```

当前 runner 直接编译生产生成器、预约 owner、dispatcher、文本规则，提取当前 `PersonaEditorController.RunHeroPersonaRerollAsync` 放入带实际 port 形状的 fixture；不再以旧 MyBehavior UI 投影冒充当前 UI。engine、网络、Hero 和 profile 是替身，不证明实机。新增阻塞检索回放验证人物名独立查询、捕获后的 MCM 文案不变、正文可选及查询/映射失败回退、开始 HTTP 前的 owner/generation/lease/target/编辑保护；原正常补全、重生 VoiceId、冷却和提交保护继续运行。

`verify_lore_and_settings.py` 编译生产单条选择/正文方法、真实索引/召回/重排算法、版本条件与文本映射方法，以及真实分层 JSON 读写方法。复用既有 Lore game-leaf fixture 和 fake ONNX/reranker ports；磁盘往返仅在全新仓内 artifacts，验证默认/自定义替换/显式空值重读、其他提示词删除覆盖的原语义和损坏原件保护。故障反例 `--mutate empty-resets-default|take-last|ignore-variant-conditions` 必须编译后因行为断言失败，不接受编译失败作为负控成功。

旧 source parity、旧 review hashes 与历史逆变换保留不刷新。它们绑定历史“原 Prompt 不变”假设，且此前已被无关 WorldBulletin 依赖漂移阻塞；本轮获准的提示词变化以新行为回归为准，不把历史校验写成通过。默认 SDK 为仓内 `local/dotnet/8.0.425`。本轮不证明真实 MCM 点击/游戏重启、存档加载、LLM 文风质量或实机性能。

验证正常 Hero 自动补全、原公开外部入口和编辑器重生请求的捕获/生成/提交。升格同伴生成及 Native/Courier 状态轮询不在本包冒充完成。

## 运行

```powershell
G:/Python310/python.exe -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py
G:/Python310/python.exe -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py --original
G:/Python310/python.exe -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/test_source_parity.py
# 七个变体必须编译成功，再因行为断言失败而非零退出：
# worker_capture, worker_commit, ignore_edit, stale_lease, release_new, drop_voice, false_queued_success
G:/Python310/python.exe -X utf8 -B tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py --mutate worker_commit
```

- 当前：125 checks / 0 failures，26 个生成/外部/UI 场景及 12 项独立预约/冷却检查。
- `--original`：114 checks / 40 failures。执行10defeb4的四个真实旧生成/外部/UI声明；已逐声明核对它们与固定main437925b8完全相同。新增预约owner的12项仅用于当前代码，不虚报旧代码也执行了它们。
- 旧失败包括后台读写/配置捕获、owner/目标替换仍写入、清理后旧回包写入、重生覆盖中途编辑、UI错误成功提示。40是失败断言数，不是40个不同Bug。
- 当前编译整个生产 `MyBehavior.PersonaGeneration.cs`、整个 `NpcPersonaGenerationOwner`、原 `MemorySummaryDispatcher` 和真实重生UI方法。独立物理主线程消费队列，强制异步响应；引擎、人物注册表、存储、parser和网络用替身，不能证明实机。
- 原 Prompt 文本完整保留，正常补缺失字段、重生保留最新VoiceId、5分钟失败冷却、显式重试绕过冷却但不绕过busy均验证；中途编辑保护与清理失效是有意修复，不复制main缺陷。
- 精确逆变换恢复整个MyBehavior到10defeb4，6个守卫拒绝额外源变更/漏reset/状态变更/依赖漂移，并核对原Prompt块。B1入口仅先撤销这些精确人设变更，仍保留其完整旧文件对照与15项守卫，不刷新B1生产哈希。
- `.generated/` 为忽略产物。测试沿用本机SDK G:/AFMOD/.dotnet-sdk/dotnet.exe，不把机器路径放入生产。

## 边界

生产复用MyBehavior已有的budgeted主线程dispatcher（保留历史MemorySummary名称），没有第二条队列。事实/设置在主线程捕获并启动已有异步辅助Gateway，不同步等待网络；返回后再次调度主线程写档案/UI。独立owner只持有请求预约与冷却，不存Hero/档案。档案Saveable类型和原读写/导入接口不改变。

本包未证明：大存档事实捕获/FindHeroById主线程成本上界、完整GameEnd/所有队列释放、真实HTTP取消、消费者外围所有live读取、升格同伴技能/人设流程、实际游戏/旧档、三渠道版本化提交SDK。既有状态接口仍是同步game-facing调用，不可因异步生成入口已安全就推断其worker读取也已安全。

追加排队清理回归：在capture执行前或accept执行前清理同代队列，必须不保存、不显示重生成功。初版968ca283在扩展125项中有2项失败；修复后125/0，新增false_queued_success反例编译后拒绝。该队列清理不会触发真实存档操作，测试仅重置fixture/实际dispatcher。


## 2026-10-01 历史入口维护

当前 source proof 绑定批准候选 `f6e2ead7` 的具名 owner 消费者及现有生产依赖审查 hash；不再以整个旧 MyBehavior/Shout/Courier 根文本阻塞迁移。历史逆向/快照只保留为历史材料，本轮未重验旧整类逆向。当前真实生产方法的运行/有效负控仍独立执行。`--run-root` 只创建新仓内输出，runner 安全改动以 `runnerSafetyChanges` 精确 before/after 记录，不刷新原生产 hash。
