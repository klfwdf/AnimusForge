# 外部接力：政策效果与外交内战全英文显示

状态：已被用户随后要求的[三个独立 C/D/E 包](2026-10-10-localization-cde-plan.md)
替代。下文保留调查证据，不再作为整包写权限；Policy归C，Diplomacy/CivilWar归D，资产归E。

阶段：待接力。工作区 F:/AnimusForge-main，分支以实际 Git 为准。
调查基线 `e1d625e1bb8c77cb3e90ef0751df2170c2e09e3a`；并发 HEAD 会推进。
本文件由主线按用户最新目标整理，历史报告仅作证据，不是独立授权来源。

## 可直接交给 Agent 的任务

请完成 AnimusForge 政策、外交、内战剩余玩家可见中文的中英本地化。
最终目标是 N 网全英文发行，CN/EN 两个 ZIP 名分别带 `_CN` / `_EN`，
包内原文件名不变，中文原版资产完整保留。当前仅负责下述源码显示切片，
不要执行打包、产品构建、Stage、部署或 push，不加入 C# 语言探测。

先读取 AGENTS.md、`.claude/skills/animusforge-maintainer/SKILL.md` 与
`docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md`，再核实 Git 与真实调用链。
每轮标明调查/修改/验证/总结；修改前说明文件。修改用 apply_patch。
focused commit 只包含自己路径；共享工作树提交使用精确路径 `git commit --only`，
不要提交他人的 staged 内容。不得 hard reset、checkout 覆盖、清理他人临时文件。

## 独占可写范围

1. `PolicySystem/Core/CustomPolicyBehavior.Management.cs`、
   `CustomPolicyBehavior.Effects.cs`、相关 UI/History 显示生产者，
   `PolicySystem/UI/PolicyUiTexts.cs`；新增小型显示目录可放相邻目录。
2. `src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior*.cs`、
   `Profiles/KingdomStrategicProfileBehavior*.cs`、
   `Application/WorldDiplomacyAnalysisApplication.cs` 及其显示消费者，
   既有 `DiplomacyUiText` 等目录。不要改变外交规则或 LLM 执行协议。
3. `src/modules/AF.Module.Kingdom/CivilWar/` 的独立显示投影：
   `CivilWarUiText.cs`、Actions/Panel/DialogueTags 必要接线；规则与原事实保留。
4. 三对资源：`content/modules/PolicySystem/ModuleData/Languages/afpol_ui_strings.xml`、
   `content/modules/AF.Module.Diplomacy/ModuleData/Languages/afdip_ui_strings.xml`、
   `content/modules/AF.Module.Kingdom/ModuleData/Languages/afkingdom_ui_strings.xml`
   及各自 `CNs/*-zh-CN.xml`。
5. 对应政策/外交/内战专项测试，可以补中英显示断言并保留行为断言。
   交付文档用 `docs/handoffs/2026-10-10-localization-external-policy-diplomacy-delivery.md`。

跨出以上范围发现生产者/消费者：先给主线精确路径和所需窄接线，
不要擅自碰 Economy 的 `RewardSystemBehavior.cs`。必要接线由主线协作。

## 禁区与在途任务

- Memory 模块、Memory适配器与 MyBehavior.cs：现有 C 审查子智能体在做。
- WorldMap 模块及资源：现有 B 审查子智能体在做。
- MCM/Terminal/Courier themes、UI/Errors、af_ui_strings：主线持有。
- package_mod/package_localization、打包脚本与打包测试：打包切片持有。
- LanguageData、content-map、主台账/HANDOFF：主线持有；复用已注册资源即可。
- Prompt、世界书中文或英文原件、协议标签、存档键、程序集身份、构建脚本。
- 不改任何全仓 source_parity/hash 基线来绕过不相关红灯；登记具体阻断。

## 已确认的缺口与真实入口

行号按上述基线，修改前重新定位符号，不能盲按旧行数打补丁。

### 政策

- `PolicySystem/Core/CustomPolicyBehavior.Management.cs:2278`
  `BuildLocalPolicyEffectText` 返回“无模块效果”“未知国家”“发布地效果”，
  以及“原因”中文拼接。`EffectText` 是实际历史面板字段，不是仅内部日志。
- 追踪 `BuildPlayerVisibleEffectLinesForExternal`：玩家国家、地方政策、NPC 公告
  效果正文仍中文。需要统一可见效果行的双语输出，数值、正负号、目标、周期不变。
- `tests/bridges/Policy/PolicyEffectModule.ContractTests/Program.cs:4581/4599/4619`
  原测试要求中文并禁 ASCII，属于旧文案契约。改为验证中英两种显示，
  保留模块ID/范围/参数/数值和可见效果准确性；不能把旧断言当永久白名单。
- 两条剩余时间测试仍锁中文，而生产已英文 fallback；核实并修语义断言。
- `IndependenceReason` / `EndReason` 等字段兼有共享事实用途，显示要投影，
  不能把数据库/存档内容直接改成英文，不能丢原因信息。

### 外交

- `src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyAnalysisApplication.cs:30`
  `execution.Notify(document.MechanicalResult)` 直接显示中文存档事实；多个生产点
  在 :73/:270/:326/:389。增加独立显示投影，保留 `MechanicalResult` 存储原语义。
- `Agenda/VoteDealBehavior.cs`、`.Agenda.cs` 的内政面板约129行旧中文还未处理；
  此数是待核查代码行，不是确认129键。追踪通知、Inquiry、VM才计入。
- `Profiles/KingdomStrategicProfileBehavior.cs:774/822-823/881` 的生成错误
  与 `.DevUi.cs:785-810` 中文前缀比较耦合，生产者/消费者必须成对处理。

### 内战

- `KingdomCivilWarOwner.Actions.cs:379/381` 的 `ConcessionPending`、`Conceded`
  将原始 reason / `ctx.Notes` 填入英文通知，仍夹中文。
  独立结构化显示投影保留完整效果细节，不靠“已完成”泛化句吞掉原信息。
- `KingdomCivilWarOwner.cs` 的 TryApplyTag 返回原始中文 `result.Message`，
  `DialogueTags.cs` 的 RecruitBlockReason 同样未收口。
- 实际消费者 `src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs:16210-16213`
  同一 civilWarStatus 同时进入 giverFacts/receiverFacts 后上屏；必须分别保留
  原事实与显示值。需要主线在 Economy 边界接线，请发窄补丁设计，不直接改该文件。
- 原未知旧存档回执可以保留真实原文作为明确旧数据兼容限制；不可猜测翻译，
  但要报告该限制，不能宣称所有旧档英文纯净。

## 实施约束

Bannerlord `TextObject("{=ID}English fallback")`；中文只进中文资源。
变量、换行、格式、数字、模块ID与规则匹配值保留。固定语义复用已有ID。
在显示打开/结果发布边界解析，不在 Tick 新增扫描或反射。
原共享事实用来存档/AFEF/Prompt，保留其内容；可见固定文案走独立投影。
不要通过删信息、吞掉动态尾巴、隐藏错误或把所有失败替换为泛化句来清零中文。
运行时内嵌中文全量外置是总目标，当前 UI 切片余下内部字符串须明确登记，
不能为纯英文目的单改协议标签或逃逸成 Unicode 编码。

## 验证与交付

- 资源契约：`python tests/content/J15ContentContractTests/LocalizationResourceContractTests.py`。
- 真实本地化解析：运行现有 `LocalizationRuntimeTests.ps1`，用
  `.tmp/build_check/1.3`、`1.4` 的引用，仅验证相关 ResourceName，不是产品构建。
- `git diff --check`，确认中英键集合、变量、英文fallback、无字面 `\\n` 缺陷。
- 新的显示断言覆盖正常/失败/动态变量、未知旧回执及共享事实原值不变。
  若专项需要编译，暂标 NOT-RUN；可以 prepare-only，不能报实际执行通过。
- 不得在独占包完成后宣称全仓英文清零、可发布。交付报告列每个focused SHA、
  精确键数、遗漏、已跑/未跑、跨包接线与回滚 `git revert <SHA>`。

证据源：[B原交付](2026-10-10-localization-wave2-b-delivery.md)、
[C原交付](2026-10-10-localization-wave2-c-delivery.md)。两者是历史交付数据，
其中“测试锁中文所以保留”与“显示中文尾巴不是缺陷”的结论不适用于最新全英文目标。
