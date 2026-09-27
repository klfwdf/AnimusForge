# J15 content / profile 执行计划

> **当前请求与执行入口（2026-09-27）**：本轮只重构计划及交接，不继续产品实施。当前产品停点仍为 `F3_OFFLINE_VERIFIED`，依据 `f4280eed`；没有因计划完善而重新授权 F4 搬迁、F5 游戏部署或用户数据写入。后续从[范围决定](#j15-scope-decisions)、[F4 工作清单](#j15-f4-execution)、[F5 验收清单](#j15-f5-execution)执行；需要用户输入的事项集中在[授权与停点](#j15-approvals)。详细实际证据仍只在[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-executable-plan-20260927)。
>
> **已完成基础，不重做**：107 项内容映射及 30 份 Prompt 默认、主要用户数据读写/迁移、单向 Stage/部署/ZIP 已有分片证据；F3 Debug/Release Stage/ZIP 各 123 项仅是当时工程候选。模型与仓内 PlayerExports 已完成的迁移/退役不再当待办重跑。F4 仍缺 GUI 33 项、TPAC/设计源、剩余资料及模型首次初始化/来源闭合；F5 同候选完整验收未做。123/107 是基线计数，不是以后必须维持的数量，更不是完整包证明。
>
> **阅读顺序**：第 9 节是当前施工规格，第 1–8 节保留早期设计及证据背景；与第 9 节冲突的旧复制、合并、回写、7 项资源计数及旧停点不得重新执行。F4/F5 是 J15 内部批次，不扩展成新 UI 开发、J16/J17 或全仓重构。
>
> **以下为历史切片摘要，不是当前操作单**。2026-09-26：`J15c_DEFAULTS_OFFLINE_VERIFIED / J15_PARTIAL_HOLD`。用户要求直接按计划推进，产品 `9db8fa8b` 完成 30 份默认 Prompt 原样归位及安装覆盖接缝，测试修复 `a3fa77af`；映射 107，旧根 HOLD 40。完整投影/覆盖反例/Bridge/Policy/六构建/四 DLL 资源与 API/当前 DLL Phase8 已验，详细证据及额外重复清理被自动审查拒绝的边界见主台账。该时刻后续由第 9 节及主台账当前条目取代。
> 最新补证 `2b61f1c5`：PlayerExports 独立合成保护 45 断言、四个行为变异拒绝通过；ONNX 五文件匹配固定上游版本。没有真实备份/迁移或分发决定，不解除 G0.3/G0.4，也没有重跑被拒的整体 runner 清理。证据见主台账当前条目“数据保护合成契约与模型来源补证”。
> J15a/b 历史完成状态：`J15a/b_OFFLINE_VERIFIED / J15_PARTIAL_HOLD`。J15a 七项 EmbeddedResource 在 `ade4f629` 完成；J15b 其余 69 个已确认静态 JSON/GUI/XML/语言文件在产品/测试提交 `04056ce7` 完成；J15c 默认源已按上方续接完成，完整 J15d 尚未关闭。
> 规划基线：`fc445335b364859c1d5b87c122f51d7a46d1df57`；计划意图提交：`0d9bb898`。J14 产品基线仍为 `e58f3558cddfe473f396bb7f11470b05e30afce9`，已有限离线完成。
> 本文细化[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md)的 J15，不新建进度账本；J15a/b 的实际证据已回写主台账，[HANDOFF](../../HANDOFF.md)只保留当前摘要。文中 J15c/d 的拟议文件、命令和验收仍是后续规格，不是已实现或已通过。

J15a 实际结果：七项文件按 owner R100 迁入 `content/modules`，显式 `content/content-map.json` 与窄 `content_layout.ps1` 接通 csproj、Stage/Deploy 源投影和必要工具；合成契约、既有定向回归、不带 Stage/Deploy 的双配置六构建及四实现 DLL 七资源逐字节审计通过。没有真实 Stage、Deploy、Package、游戏/外仓/玩家数据写入。该切片当时停在 J15a，现已由下方 J15b 结果接续；它仍不能单独解释为整个 J15 完成。

J15b 实际结果：69 个已确认静态文件按 15 个 module/foundation owner 逐项 R100 归位，映射扩为 76 项；真实投影逐文件 SHA、格式与语言/mbproj/SubModule/movie/sprite/brush/writer 引用、11 个 overlay alias、Prompt/Xihai Core/inventory、当前 DLL Phase8 及不带 Stage/Deploy 的六构建通过。旧 ModuleData/GUI 精确保留 41 个 HOLD；完整 Xihai StaticVerifier 的三个外部 TPAC/bridge 失败已记录，未冒充通过。没有 Stage、Deploy、Package、游戏/外仓/玩家数据写入。按用户要求停在 J15b，后续必须从 J15c 重新核实 dirty 和授权。

## 1. 原始 J15a 启动指令（历史，不再直接执行）

以下代码块保留规划时输入，不能覆盖本文顶部的实际 J15a 完成状态，也不能作为 J15b/c/d 的新授权。

```text
执行 E:/AnimusForge-refactor-continuation-20260831/docs/plans/j15-content-profile-plan.md。
先核实实际 Git 根、分支、HEAD 和 dirty，再读 AGENTS.md、HANDOFF.md 当前段、两套仓库 Skill 和主台账当前条目。J14 已有限离线完成，不重开无关产品责任。
本轮先做 J15 G0：核实逐文件资源 owner、读写者、旧路径→content 源路径→不变运行路径、EmbeddedResource、脚本和测试消费者，形成精确迁移白名单与授权清单；可以编写仓内清单、合成契约测试和本地意图提交。
不要只凭“执行 J15”便批量搬资源或修改一键构建/部署行为。按计划第 6 节，在首次迁移前一次性向我确认具名文件迁移、必要的最小资源接线补丁、构建清理及是否允许项目内 Stage；获准后按 J15a→b→c→d 连续完成，不为每个机械步骤重新提问。
不 push、不部署到游戏、不打包发布、不写外仓/真实玩家存档、不恢复自动化、不安装工具、不开始 J16/J17；保留 .dotnet-cli-home/ 和其他作者改动。
不复制第二棵可编辑资源树，不修改游戏内资源名/路径、Prompt 正文/标签/默认值、用户配置覆盖语义或 J14 public API。PlayerExports、CustomPrompts、来源不明素材和 ONNX 按各自 gate 处理，不能借 HOLD 宣称全包完成。
每个完整、已验证切片独立本地提交；失败先诊断，不刷新 hash 或删断言凑 PASS。结束或中断时更新同一主台账、受影响范围图/代码地图及简短 HANDOFF，写明下一条具体动作和 NOT-RUN。
```

本次没有自动创建新任务，也没有选择或切换模型。J15a/b 已完成；后续新对话应从 J15c 重新核实实际 Git/dirty、读取主台账当前条目并取得对应范围授权，不能再按上方历史块从 G0/J15a 重做，也不能将本文当作已经批准 J15c/d。

## 2. 目标、非目标与完成含义

**目标**：已确认的静态默认资源在仓库中只有一个可编辑源，按真实 owner 归到 `content/`；既有编译、资源读取、项目内组装和后续单模块交付能消费同一源。运行时仍看到原来的 `ModuleData/`、`GUI/`、`CustomPrompts/` 等路径，7 项嵌入资源的 `LogicalName` 不变。

**非目标**：玩法/Prompt 文案调整、全局 profile 系统或 ModuleHost、public API 扩展、Policy/GCCZ 业务重写、C# 源码大搬家、原版 DLL/Bootstrap 重构、工具目录整理、全仓清理、用户数据删除、改变一键入口及安装/发布策略。`SubModule.xml` 保留当前位置、版本和唯一 Bootstrap 加载；J16/J17 不提前实施。

profile 分三类，不能混为一谈：

- 真正运行配置：如 `FeatureBridges.json`，保留现有消费者和失败/默认语义。
- 玩家导入资料包：`PlayerExports` 内 kingdom/persona/voice 等，受 G0.3 保护，不等于静态安装 profile。
- `docs/fixtures/phase3-module-catalog/module-catalog.json` 等设计/测试 profile：不是已上线 runtime profile，不搬进产品冒充功能。

**有限退出**：已批准的静态批次可记 `J15a/b_OFFLINE_VERIFIED` 或 `J15_STATIC_OFFLINE_VERIFIED`。仍待批准/未迁移的原 J15 范围明确记 `J15_PARTIAL_HOLD`，不能只建索引就标整个 J15 完成。仅当每类原范围都有已验迁移或用户明确批准的范围调整、且 d 的必要门禁全部通过，才记 `J15_OFFLINE_VERIFIED`；这仍不等于实机、旧档或发布完成。

## 3. 已核实基线与风险

工作区：`E:/AnimusForge-refactor-continuation-20260831`；分支：`codex/af-main-refactor-continuation-20260831`。规划开始时 tracked clean，只有原未跟踪 `.dotnet-cli-home/`。新对话重新读取，不按历史盘符切工作树。

规划时 `git ls-files` 元数据计数：`AnimusForge/ModuleData` 41、`GUI` 76、`CustomPrompts` 30、`ONNX` 5、`PlayerExports` 3139、`AssetSources` 6、`AssetPackages` 1，另有 `SubModule.xml`、`VoiceMapping.json` 各 1。**这些是 tracked 数，不是安全迁移白名单。** 对 `AnimusForge/` 的 untracked/ignored 查询本次均为空；不外推到游戏安装、副本或下一次运行。

| 已读证据（基于规划基线，一基行号） | 对 J15 的约束 |
| --- | --- |
| `AnimusForge.csproj:81–107,539–546`：7 项 `EmbeddedResource`、5 项 GCCZ `WithCulture=false`、直接部署 fail-closed | 只调整已迁资源的 Include；资源名、culture、程序集、引用验证不变，不能打开直接 MSBuild 部署 |
| `AnimusForgeModulePaths.cs:12–43,92–136`：模块根探测、`GetModuleDataFilePath` | 不让运行时改读仓库 `content/`；目录归位不是 runtime 路径迁移 |
| `src/modules/AF.Module.Prompt/Configuration/PromptConfigurationLoader.cs:25–33,55–211` | 磁盘/内嵌读取与 fallback 不一律相同，见第 5 节 |
| `GcczLocalizedResourceLoader.cs:13–55` | 磁盘→内嵌→fail-safe 的原顺序不变，5 项 GCCZ 默认数据不改业务 |
| `CourierLetterInputPopup.cs:83`、`AnimusForgeApiOnboardingPopup.cs:122`；`AnimusForgeCourierUiSprites.cs:230`、`AnimusForgePlayerNotorietyUiSprites.cs:202` | movie 名、sprite category/name、`GUI/SpriteParts` 相对路径都是实际消费者约束 |
| `DuelSettings.cs:4962–4967,5013–5032`；`VoiceMapper.cs:388–415` | CustomPrompts 是真实可写配置；VoiceMapping 外部 JSON 当前仅供手动导入/导出，不能改成自动加载 |
| `OnnxEmbeddingEngine.cs:472–536`、`OnnxCrossEncoderReranker.cs:556–611` | ONNX 路径/候选顺序不变；大模型来源、外部 data 文件及 tokenizer 必须一起分类 |
| `Refactor/Runtime/FeatureBridgeRuntime.cs:14–36` | FeatureBridges 为启动期有界读取，不能新增每帧 profile 扫描/重载 |
| `一键编译覆盖推送/deploy_module.ps1:238–249,720–735,786–828` | 现有脚本直接消费旧资源树；仅改 csproj 或搬文件会导致组装缺资源，必须有获准的最小接线 |
| 同脚本 `:406–473,983` | PlayerExports 按更新时间优先、同时间按 source priority 合并，再非删除回写源；不是“安装目录无条件获胜”，也不能回写到 curated 源 |
| `一键编译覆盖推送/build_single_module.ps1:281–315,487–490` | 无 Stage 也有递归清理；必须单独核对并批准精确生成目录 |
| `一键编译覆盖推送/package_mod.ps1:717–786,823–836` | 包装消费完成的模块，排除 ONNX/Logs；源 SubModule 仅供版本，J15 不应改包结构/版本策略 |
| `tools/package_policy_system_source_overlay.py:116–134`；`tools/repository_source_inventory.py:47–62,74–83` | 另有固定旧路径消费者；inventory 当前还没有 `content/` 分类，必须随实际迁移更新而非消除 HOLD |

**敏感项**：ModuleData 内已有异常 HTML、`UnnamedNpcProfiles.json` 及 5 个分文件；不能仅凭目录名判静态。GUI 中有 `.tmp.png` 和旧 `ss_*` 图，不能按名称删除或声称版权已确认。`PlayerExports` 虽被 `.gitignore` 忽略，3139 个已跟踪文件仍受 Git 管理；忽略规则不是去跟踪/删除授权。

规划只读核验了代码地图 recorded/working-tree 两模式，各 755 锚点通过，`sourceRevision=e58f3558…`。没有重新构建、跑产品行为、Stage、访问 provider 或游戏；地图 PASS 仅证明定位。

## 4. 仓库源布局与唯一映射

### 4.1 源与运行目标分开

拟采用：`content/modules/<实际-owner>/…`、`content/bridges/<实际-owner>/…`、`content/foundation/<实际-owner>/…`、`content/profiles/<已确认-default-id>/…`。只有有实际文件的目录才创建；不为凑架构空建目录。文件下可保留 `ModuleData/GUI/CustomPrompts` 子层，便于对照运行路径。

| 资源组 | 首选内容 owner 与处理 |
| --- | --- |
| Preprocess / RuleBehavior / ActionPostprocess JSON | `AF.Module.Prompt`；共享文件只有一个 owner，不能按每个标签复制给各业务模块 |
| ProactiveNpcRequest / RpItemIntroduction JSON | 分别核对 `AF.Module.Social` / `AF.Module.Economy` 的业务归属；Prompt loader 仍是读取者，不据此吞并所有业务默认内容 |
| 5 个 `GcczTown*.zh-CN.json` | `AnimusForge.SiegeAftermathIntervention` 内容 owner；AF host loader 仍是薄适配。仅仓内路径调整，外仓同步另获精确授权 |
| GUI prefab/brush/sprite | 按 Conversation、UI、Onboarding、Weekly、Social、Economy、Diplomacy、WorldEvents、WarStats、PolicySystem 等真实消费者逐项归属；共享 sprite 索引单归 UI，不能拆坏 category/atlas |
| Languages、Items/action XML、SceneActions settings/mbproj | 按 WarStats、Settlement、SceneActions/Xihai、Economy/GCCZ 实际引用分组；保留 XML ID、文件基名与相对引用，不能因扩展名相同全归一个 owner |
| FeatureBridges / TownAmbientDialogue | 分别核实现有 Bridge 配置 owner / TownAmbient 业务 owner；无对应独立模块时记录现有 host，不创建假模块 |
| CustomPrompts 的默认副本 | 只有经确认的 curated 默认项可迁；Memory、Persona、Weekly、Kingdom、Diplomacy、PolicySystem 等分属真实业务。runtime 可写实例不进静态替换目录 |
| PlayerExports、UnnamedNpcProfiles、VoiceMapping | G0.3 数据保护；只先列路径/读写者/数量，不批量输出内容、拷贝或去跟踪 |
| ONNX、AssetPackages、AssetSources、PNG | 模型/素材来源与分发 gate 单列；ONNX 归 Knowledge 的逻辑关系可记录，物理迁移不先行；设计源不假装运行资源 |

上表后半部分是执行路由，不是逐文件完成证据。G0 必须把每个拟迁文件落实为确切源/目标、owner、消费者、批准状态；未知文件明确 HOLD。

### 4.2 单一内容映射，不造新的运行时框架

G0 先在主台账关联本地完整清单；产品实施时拟新增 `content/content-map.json`，只承担**构建期映射**，不是运行时 module manifest。最小条目：`owner`、`source`（仓库相对文件）、`target`（模块相对文件）、可选 `logicalName`。分类/HOLD/旧新 hash/授权与证据放本地清单和台账，不塞进游戏配置，不记录玩家文本或凭据。

- 每项一个文件，无递归 wildcard；验证源与目标大小写不敏感冲突、重复 LogicalName、缺源、绝对路径、`..`、UNC/盘符/ADS、父/子 reparse 以及 target 越界，**在任何输出写入前全部失败关闭**。
- 只有获准静态文件进复制映射；禁止把 `bin/`、Logs、玩家存档、未知 PlayerExports、模型 provenance HOLD 变成普通静态条目。
- 同一源可同时作为磁盘文件和 EmbeddedResource，但只能有一个可编辑源。csproj 显式 Include 与映射逐项交叉校验；不再自动生成一套可编辑 JSON，不在每次游戏启动扫描 manifest。
- 迁后旧 tracked 静态文件退出；不留“兼容副本”或 junction。若旧路径存在未知本地文件，不覆盖/删除：阻止该批迁移并请求决策。
- 允许同一份源投影到**明确的生成目录**，那是 hash 可验的输出，不是第二资源源树。不能在普通无 Stage 构建时偷偷回填 `AnimusForge/ModuleData`。

### 4.3 最小脚本接缝（需批准）

默认保留全部一键入口、命令参数、Build→Stage→Deploy 分工及事务行为。必要改动只限：

1. `AnimusForge.csproj` 的 7 项资源 Include 随迁移更新，原 LogicalName/WithCulture 全保留。
2. `deploy_module.ps1` 的 source 校验、两处资源组装和 source hash 查询改用同一映射；未迁/受保护输入仍按原策略。必须先映射默认值、后按原规则保留安装用户配置，不能让静态复制覆盖最后的用户版本。
3. 若无可直接复用函数，新增同目录 `content_layout.ps1`，只放“校验/解析/投影到指定输出”的窄 helper，由 Stage 与 Deploy 共用。测试可加载这个无顶层副作用的 helper，**不得 dot-source 整个 deploy 脚本当测试**。
4. CustomPrompts 获准迁移时，同步调整 `Merge-InstalledCustomPromptsIntoStaging` 的默认源读取（包括 Policy 子目录补齐），但保留实际用户覆盖及旧格式迁移语义。不触碰 PlayerExports 回写，除非 c 获得独立具名授权。
5. `tools/package_policy_system_source_overlay.py` 保持 overlay 交付的原运行目标名，仅让已迁 runtime asset 的源定位消费新映射；验证真实 `build_file_set()`，不是只修字符串或跳过缺失文件。

`build_single_module.ps1`、`package_mod.ps1`、BAT/一键入口、Bootstrap 默认**不改**：现有调用链应继续使用同一 deploy 接缝和完成模块。若实读发现另一个不可避免消费者，先给出具名最小扩展，不迁整个脚本树、不另建第二条发布管线。不获准脚本接线就停在 G0/测试，不先搬走资源把仓库留坏。

## 5. 必须保持的行为与性能

- 原样迁移以 SHA256 字节一致为准，含编码/BOM、JSON 字段/默认值、XML ID、movie 名、纹理/sprite 名、mbproj 内部相对路径；禁止顺手格式化/压缩图像/改 prompt。
- Preprocess：旧 schema 可使用内嵌新版；**缺文件/非法 JSON 原本报错，不是全部自动 fallback**。RP introduction：磁盘缺失/损坏可转内嵌，两边不可用保留失败；GCCZ：保留 disk→embedded→fail-safe。分别做反例，不统一成新“万能 loader”。
- 一套源码、两实现同名 `AnimusForge`、Bootstrap 唯一选择、单模块布局不变。内部制作组接口和外部 V1 API 不变，存档 type/key/Behavior 身份不变。
- CustomPrompts 缺省生成、旧格式转换/隔离、fingerprint/reload 不变；PlayerExports 合并不删除、更新时间/同时间优先级、一次 legacy 迁移与源回写不变。要改变这些，另开数据迁移设计与授权。
- 无新增 Tick/请求热路径 I/O、hash、全树扫描或反射。映射校验/复制每次组装一次，工作量按实际文件/字节统计；大模型 hash 只在获准基线/验证时流式计算，不进入游戏帧。runtime loader 频率、已有缓存与懒加载保持。

## 6. 分阶段执行与批准点

### G0：冻结基线、精确清单和一次性批准

1. 核实 Git/dirty、维护 Skill 的 repository-structure/persistence/validation/ledger 参考、框架协调及双版本/单模块案例。保护其他作者改动；仅本包文件暂存。
2. 只读枚举上述资源根的 tracked/untracked/ignored、大小、来源状态和真实读写者。安全静态文件才计算字节 hash；不把玩家文本/API key、异常 HTML 正文写入报告或提交。核实实际 consumer，包括 csproj、loader、脚本、工具、测试与 XML/mbproj 引用。
3. 形成具名迁移表：旧路径→新路径→不变运行路径、owner/消费者、SHA、LogicalName、writer、分类/授权、需要修改的文件。对每类 G0.3/G0.4 写“阻挡什么、解除需什么”，不能拿旧历史 HOLD 一票否决无关静态项，也不能凭新目录绕过相关 HOLD。
4. 先建有用的红例/基线：缺源/重复 target/越界/reparse 被拒、原资源 hash/LogicalName 集合、原 loader fallback、用户覆盖保持。复用现有测试；新映射缺口集中一个有界测试入口，不每个 JSON 建 harness。
5. 一次向用户提交以下**分别明确**的批准请求：具名静态迁移白名单；第 4.3 节精确脚本接线；四个生成目录清理；是否允许项目内 Stage。数据备份/移动、外仓同步另列，不混入普通构建许可。G0 完成后记 `J15_G0_READY`，未获准部分保持 WAITING/HOLD。

四个原无 Stage 构建清理目录（实际根重新解析，检查目录内容、祖先/后代 reparse；不能只验目录本身）：

- `E:/AnimusForge-refactor-continuation-20260831/bin/Debug/single_module_artifacts`
- `E:/AnimusForge-refactor-continuation-20260831/bin/Release/single_module_artifacts`
- `E:/AnimusForge-refactor-continuation-20260831/obj/single_module/Debug`
- `E:/AnimusForge-refactor-continuation-20260831/obj/single_module/Release`

Stage 若获准，必须再从实际脚本解析它的精确输出/清理路径并批准，不能由以上四项推导。原 Stage 会复制 PlayerExports（`一键编译覆盖推送/deploy_module.ps1:732`），故要确认该复制范围及敏感数据处理；没有许可时只做 synthetic 投影验收，不谎报真实 Stage。

### J15a：七项 EmbeddedResource 与最小内容接线

首片精确资源：`PreprocessPrompts.json`、`RpItemIntroductionPrompts.json`、`GcczTownPrompt.zh-CN.json`、`GcczTownEntryPresentation.zh-CN.json`、`GcczTownActionPresentation.zh-CN.json`、`GcczTownHiddenResidents.zh-CN.json`、`GcczTownManual.zh-CN.json`，当前均在 `AnimusForge/ModuleData/`。

- 迁移前建立本地意图提交；获准后将上述已确认静态源按 owner 原样归位，接入显式资源映射、csproj 及实际组装消费者。先做一个完整纵向批次，不留下“编译能过、Stage 缺文件”的半截状态。
- 扩充已有 Prompt Configuration 测试；为 GCCZ fallback 复用/补一个相关资源测试。新增映射测试拟放 `tests/content/`，测试产物仅在 `artifacts/j15-content/` 下具名运行目录，自动清理也须纳入授权，不清整个 artifacts。
- 双 API evaluated EmbeddedResource 清单恰好 7 项、无重复/文化旁程序集漂移；四实际实现 DLL 的 ManifestResource 名称和字节 hash 等于迁前资源；Compile 集合未多收测试源码。
- Stage 与 Deploy 两条资源构造分支共用 helper，合成临时模块投影布局/hash 相同；真实 Stage 依授权验证。关键反例和 Debug 双版本+Bootstrap 通过后记 a 的有限离线结果，进入 b。

### J15b：其余已确认静态 JSON / GUI / XML / 语言

**实际状态（2026-09-26）**：本节已按产品/测试提交 `04056ce7` 完成并有限离线验证；具体 69 项 owner/target、41 个 HOLD、候选 SHA、通过项和完整 Xihai verifier 的三项未通过见主台账当前条目。下列条目保留为实施规格与复核边界，不是下一步待执行清单。

- 按 G0 清单按 owner 分几个完整批次，补齐 RuleBehavior/ActionPostprocess/Proactive、GUI 及游戏 XML/语言等资源；禁止递归移动整个 ModuleData/GUI 作为快捷方式。
- 每批沿“源→映射→组装目标→loader/LoadMovie/XML/sprite”核对；GUI 共享索引与其引用图片必须同时可达。mbproj 中若含引用设计源，保留相对可达性；无法保证就先 HOLD 该小组而非改运行资源名。
- 同步所有受影响测试/overlay 的**源定位**。生产 loader 不应因源码物理位置改变；测试中“安装路径断言”仍断言旧路径，不能全仓字符串替换。
- 异常 HTML、来源不明图、动态 persona 文件不混入静态映射。更新 inventory 对 `content/` 的精确分类及反例，保留未解除的各 HOLD，不用“所有 content 都安全”掩盖风险。
- 字节/引用/投影/相关 loader 正反例通过，即结束 b；不要因还剩根文件或可以抽 helper 无限细分。

### J15c：profile、可写默认项、模型与素材边界

- FeatureBridges 等实际静态默认配置按原 owner 接线；不把设计 fixture 的 single-player/safe-mode/developer profile 做成新运行功能。
- CustomPrompts：区分仓内经确认的默认与用户修改，逐文件批准后只移动默认源；测试安装用户同名修改、未知文件、缺失默认、Policy 旧/新布局、重复组装。保护现有内容优先级，不改正文。
- PlayerExports / UnnamedNpcProfiles / VoiceMapping：先列 writer、选择路径、备份责任、允许的备份位置与恢复 hash 验证方法。没有具名批准不复制、不搬、不去跟踪；不把手工挑几份导出当作默认素材。若用户批准提取 curated defaults，先证明不再有 deploy 回写到该不可变源，并用合成数据验证时间优先/并集/未知保留；真实数据读写另外授权。
- ONNX/AssetPackages：先确认来源、模型+data+tokenizer 依赖、体积与本地保留/获取/可分发决定。不从网上自动补下载、不转 LFS、不把 ONNX 放 ZIP、不写游戏/外仓。无许可保持原位 HOLD，不借“logical owner 已标注”声称物理迁移完成。
- 在主台账逐类记录已迁、原位保留、待批准及其阻挡范围；有原范围未完成则整体 `J15_PARTIAL_HOLD`。如用户明确缩小范围，把批准依据写清再作有限验收，不自行改完成定义。

### J15d：同一候选的最终验收与交接

1. 固定产品 commit/dirty 状态、内容映射、源资源 hash 集合；原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建，记录实际引用版本、marker 与 DLL hash。需要变更产品/资源/项目/脚本后重跑受影响门禁，不混用旧候选证据。
2. 验证四实现 DLL 的 7 个资源名/字节；迁前后运行 target 集合和内容保持，Compile 无重复/漏入。运行本包投影与数据保护反例、受影响 loader/UI 引用测试、真实 overlay file-set 检查。
3. 获准时用原 Stage 分支做当前候选布局/hash 核对：唯一 SubModule/Bootstrap、两实现各自路径、静态内容完整、未知用户文件保护、无额外模块/游戏 DLL。无 Stage 授权明确 NOT-RUN，不用 synthetic layout 冒充原 Stage 通过。
4. J14 回归选当前 V1 四 DLL 元数据/旧 Native ABI及相关三渠道源码链接消费者；Prompt 迁移涉及三渠道规则读取时运行既有相关 Prompt/标签回归，保证内容 hash 不变。保存 identity/profile 和 Bridge 契约依改动范围复用，不重新开产品职责包。
5. 更新主台账详细证据、范围图受影响资源/消费者与代码地图（仅确有代码坐标变化时）；执行地图两模式及 `git diff --check`。指南只改受影响的内容源定位，不批量重写历史报告。
6. 本地提交交付；HANDOFF 写清已完成部分、HOLD、下一具体动作。所有必要退出门满足就停止 J15，不提前 J16。LIVE 1.3/1.4、真实旧档、provider、UI 渲染/点击/焦点、音频、实测帧性能与发布分别 NOT-RUN，除非各自实际执行且有授权。

## 7. 验证入口与证据级别（历史参考；当前执行用第 9.9 节）

以下**已有入口**已核实路径，执行前读各自参数/写入行为；不是本轮已运行的清单：

```powershell
# 只读基线、定位；每条命令单独检查退出码。
git rev-parse --show-toplevel
git branch --show-current
git rev-parse HEAD
git status --short
python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py
python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree
git diff --check

# 后续执行：确认 SDK、缓存/输出目录和测试清理授权后。
$root = 'E:/AnimusForge-refactor-continuation-20260831'
$dotnet = "$root/local/dotnet/8.0.425/dotnet.exe"
& $dotnet run --project "$root/tests/modules/AF.Module.Prompt/Configuration/PromptConfigurationLoaderTests.csproj" -c Release
python -X utf8 -B tools/test_repository_source_inventory.py
python -X utf8 -B tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py
# J15 对照本文指定的 J14 产品基线；脚本默认 d4cb1467 早于既有 WarStats，不能混作本轮基线。
python -X utf8 -B tools/PersistenceIdentityAudit.py --baseline e58f3558cddfe473f396bb7f11470b05e30afce9
python -X utf8 -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet --legacy-abi
python -X utf8 -B tools/ModuleFrameworkApiTests/run.py --dotnet $dotnet --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts --legacy-v1 tools/NativeModuleSubmissionTests/.generated/legacy-abi/Baseline/Consumer/bin/Release/net8.0/NativeModuleUnderTest.dll
```

构建参数沿用主台账 J14 的已验证入口，但执行前再次确认 SDK/依赖存在、版本与引用来源。需要在**当前进程**配置 PATH/DOTNET_EXE/DOTNET_ROOT 和仓内 CLI home/NuGet 缓存，不修改全局配置。规划时记录的可用参考为仓内 8.0.425、`_deps_auto`、`local/bannerlord-refs/1.4.7.117484`；游戏只读依赖路径为 `D:/steam/steamapps/common/Mount & Blade II Bannerlord`，不是部署许可。

```powershell
# 仅获准四个清理目录后运行；Debug / Release 顺序执行，不并发共享输出。
& 'C:/Program Files/PowerShell/7-preview/pwsh.exe' -NoProfile -File "$root/一键编译覆盖推送/build_single_module.ps1" -ProjectRoot $root -BannerlordRoot 'D:/steam/steamapps/common/Mount & Blade II Bannerlord' -Bannerlord13ReferenceDir "$root/_deps_auto" -Bannerlord14ReferenceDir "$root/local/bannerlord-refs/1.4.7.117484" -RuntimeDependencyDir 'D:/steam/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge/bin/Win64_Shipping_Client' -HarmonyCorePath 'D:/steam/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client/0Harmony.dll' -Configuration Debug
# 再将 Configuration 改为 Release；不擅加 -Stage 或 -Deploy。
```

不使用临时脚本替换原构建流程。复杂 PowerShell 按本机 PowerShell skill 使用 encoded helper 或既有固定脚本，避免嵌套双引号解析。新 `tests/content/` 的具体入口由 G0 的最小测试设计确定，写入后才加入可执行命令，不在此把尚不存在的 runner 当已有工具。

| 门禁 | 正例与必须拒绝/保持的反例 | 不可外推 |
| --- | --- | --- |
| 映射 | 每项唯一可达；缺源/目标冲突/路径穿越/reparse/未知旧副本拒绝，失败不部分发布 | 不是实际加载 |
| 资源 | 字节 hash、7 LogicalName/WithCulture、四 DLL 资源表、无卫星资源漂移 | 不是 provider/游戏行为 |
| loader | 原 disk/embedded/fail-safe 各分支、版本/BOM/损坏、旧用户数据不改写 | fixture 不等于真实存档 |
| 组装 | 同 helper 的 Stage/Deploy 分支投影、旧 target/hash、用户同名/未知保护；原 Stage 单独标结果 | synthetic 不是游戏部署 |
| GUI/XML | movie/sprite/atlas/语言/Items/mbproj 引用可达且 ID 不变 | 不等于 Gauntlet 点击/渲染 |
| 兼容 | 双配置六构建、实际 DLL API/资源、保存与受影响 Bridge 契约 | 不等于实机/全仓 READY |

## 8. 停止条件、回退与交接格式

- 发现 dirty 同文件冲突、敏感信息、未批准大规模搬移/覆盖、外写、来源未知或无法恢复的资源，停止**该批次**；可继续无关的清单/测试工作，不扩大扫描玩家数据。
- 相关失败不得以删断言、mock 整个生产入口、更新旧 hash 或留两套源绕过。记录失败命令/退出码/原因和后续修复，保留原失败证据。
- 每个完整批次用独立本地 commit，包括源迁移、全部实际消费者和定向测试。回退用审查过的 inverse commit，一起恢复映射、csproj、接线和源文件；不 hard reset、不覆盖其他作者，也不自动回滚游戏/外仓/玩家数据。
- 主台账证据：基线/产品提交、实际迁移清单、owner/消费者一基坐标、资源前后 hash、四 DLL/marker、命令/退出码、授权、HOLD/NOT-RUN。私密数据的路径细表留本地，不提交玩家正文或密钥。
- HANDOFF 只链接计划/台账/范围图并写下一步，例如“G0 白名单待批准”“a 已验，下一批为 b 的具名 GUI 组”“静态完成、c 用户数据/模型 HOLD”；不能留模糊的“继续重构”。

<a id="j15-af2-final-state"></a>

## 9. J15 完整收尾施工规格（计划已细化，产品停在 F3）

### 9.1 目标与有意改变的边界

本节是最新请求对应的**完整终态设计**，取代“原位排除保护项即可收尾”的建议；不是用新名称把剩余工作放到别处。既有 107 项内容迁移保留，未完成的数据/素材必须逐项关闭，不重做已经成立的基础工作。

这是对第 2、5、6 节旧有限迁移方案的明确扩展：将改变用户文件的存储位置、默认与覆盖读取、部署回写和发布输入规则。它们需要真实实现和新候选验证，不能继续声称“只有源码路径变化”。不改变玩法、Prompt 正文、序列化身份、V1 API、同 DLL 内部接口、三渠道业务语义、双实现/Bootstrap/单模块输出；不借 AF2.0 名义追加全局 ModuleHost/profile 平台或 J16/J17 全仓重构。

终态不是所有文件都塞进 `content/`，而是每类文件有唯一权威位置与生命周期：

| 类别 | 最终位置与职责 | 禁止事项 |
| --- | --- | --- |
| AF 可编辑静态默认、GUI/XML、已获准素材 | 现有 `content/modules/<owner>` / `content/foundation/<owner>`；同一源由映射投影。设计源可放 owner 的 `AssetSources`，但不进运行文件集合 | 不维护另一棵可编辑默认树；不从运行目录反向同步 |
| 唯一模块模板 | 暂保留 `AnimusForge/SubModule.xml` 单文件稳定入口，显式纳入组装；旧 `AnimusForge/` 整树不再是复制源 | 不为目录外观改模块 Id、版本或 DLL 身份；模板旁出现文件不自动随包 |
| 模型及外部依赖 | 受 hash/版本约束的本地依赖库；仓内开发副本放已忽略的 `local/`，安装模型放独立数据根的 `Models/` | 不作为可编辑默认；模型不进入统一客户端 ZIP，不隐式下载 |
| 玩家可写内容 | 独立 `AFDataRoot/UserData/`，含 `PlayerExports/`、`Overrides/` 和明确允许的本地设置 | 不在源码、Stage、发布 ZIP 或可替换程序目录内 |
| 缓存、日志、迁移恢复材料 | `AFDataRoot/Cache/`、`Logs/`、`Recovery/`，各有独立策略 | 不能把唯一知识/记忆数据误当缓存；不自动清理 Recovery |
| 构建/Stage/ZIP | 保留原脚本的 `bin/<Configuration>/single_module_artifacts`、`single_module_stage/AnimusForge` 与明确输出目录 | 不是源码；不包含用户数据；不从已污染安装目录直接制作发行包 |

`AFDataRoot` 拟默认由 Windows 用户目录 API 定位到 `%LOCALAPPDATA%/AnimusForge`，不硬编码用户名、E 盘或游戏盘。允许一个显式、验证过的替代数据根，供多副本/测试隔离；不按 Bannerlord 补丁号拆分数据，不扫描并自动合并别的安装。游戏与导出编辑器必须共享同一定位约定。根不存在、不可写、落在源码/模块/Stage 内或经过未认可的 reparse 时给出明确错误，**不偷偷降级为向当前目录或模块目录写入**。`.sav` 仍由 Bannerlord 管理，不搬到此根。

### 9.2 路径、读写和默认覆盖是真职责，不只是新目录

- 在现有 Persistence/模块路径边界内增加窄 typed 路径解析能力，分别解析只读安装内容、可写用户内容、缓存、模型与恢复目录；不把 `GetCurrentModuleRoot()` 全局改成用户目录。现有公开签名和版本化 API 保持，确有路径行为变化的 helper 明确记录和验证消费者。
- `PlayerExportsStore.GetPlayerExportsRootPath()` 接用户数据根，MyBehavior/Onboarding/Kingdom 的业务导入导出仍归原 owner；编辑器默认定位、MCM“打开文件夹”和显式绝对路径导入一同适配。不得只改游戏端而让编辑器继续找旧源码目录。
- 普通/Policy CustomPrompts、TerminalSettings、日志与可重建缓存按真实 reader/writer 接正确位置，不通过全仓替换 `ModuleData` 路径误伤游戏静态 XML/GUI。**本轮范围决定取代原“含 FeatureBridges 的用户覆盖”要求**：FeatureBridges 是随版本管理的内部连接配置，不新增玩家覆盖系统；现有手工修改的处理及未来 AF 设置 UI 的边界见第 9.7 节。
- 用户配置读取为“有效用户覆盖 → 随包默认 → 该 owner 原有内嵌/fail-safe”；不能给所有 loader 强套同一种错误恢复。缺文件与损坏文件分开：损坏覆盖不被默认覆盖删除，按既有安全策略保留原件并诊断。保存编辑只写用户覆盖；“恢复默认”明确撤销覆盖，而不是覆写随包文件。
- 旧完整 Prompt 文件只有在旧版本基准 hash 能证明未修改时才能归为默认；无法证明的保守迁为覆盖。保留全部差异文本、未知字段与既有 Policy 格式迁移，不将 30 份默认重新写成内建常量。新默认更新不能静默抹去旧用户定制。
- 原本只支持显式导入的 VoiceMapping/UnnamedNpcProfiles 仍只显式导入；迁磁盘文件不等于把它自动注入当前战役。campaign 记忆、人物状态和保存键仍由原行为负责，不搬进通用路径层。
- 实际 writer 也要满足保护要求：当前 `PlayerExportsStore.WriteJson` 存在先删后写，`ClearJsonFiles` 会先清旧 JSON；新路径接线不能只换字符串而保留失败即丢旧导出的风险。改用同目录临时文件/校验/替换；成组导出先完成候选包再发布，不先清空唯一旧包。业务选择、JSON 内容与显式删除语义不借机重写。
- MCM/provider 凭据继续由现有 owner 管理；没有读完其路径与存储契约前不做批量迁移。清单、日志、测试、包和默认文件不得包含 token/API key 或玩家正文。

### 9.3 可恢复、可重复的数据迁移

运行兼容迁移与开发仓库整理是两项操作：前者迁已安装模块中的个人数据，后者处置仓库中已跟踪的 3139 项资料、旧 profile/VoiceMapping 和诊断文件。不能从任意源码副本自动导入玩家的活动数据，也不能把这 3139 项直接认证为默认包。

迁移流程固定为：**确定输入/目标 → 只读盘点 → 私有备份 → hash/大小/文件集合验备份 → 复制到隔离候选目录 → 逐项校验/处理冲突 → 原子激活 → 写完成记录**。

1. 真实操作前冻结精确源、数据根、私有备份路径与预计文件/字节清单。仅列必要元数据；含个人文件名的完整清单留私有恢复目录，不进入 Git。用户数据外写/批量迁移统一在执行清单中确认，不逐 helper 提问，也不由本设计擅自执行。
2. 在游戏/编辑器无写入的维护窗口执行，或取得进程间排他所有权。再次比较源 hash/mtime；期间变化则中止/重建候选，不发布混合时间快照。检查磁盘空间、只读、锁定、越界、ADS、符号链接、路径大小写碰撞和异常长度。
3. 每个逻辑文件保留来源、hash、原 mtime 与归属；导出包目录 mtime 也要保留/验证，避免“空名称导入最新包”的选择改变。未知文件、坏 JSON 和不同版本旧数据不能静默丢掉。
4. 新数据根已有的活动内容不被旧文件自动覆盖；冲突的旧版本进入私有 Recovery 并报告。多个旧来源之间按已经确认的时间/来源规则提出候选，遇到用户数据冲突保留双方，不用 `/MIR` 或文件名判断内容可丢。新旧 Policy 格式由现有模块迁移逻辑解释，基础设施不改业务 schema。
5. 迁移记录带 schema、来源身份、文件清单 hash 与完成状态；读路径切换只在整批验证成功后发生。中断、磁盘满或失败不写成功标记，下次可安全续作；重复执行不产生双份活动数据。数据根内使用同卷候选与激活记录，不假设跨盘目录 rename 原子。
   检测到相关旧数据但迁移未完成时，必须显示“待迁移/恢复”的真实状态，不能把空的新根当成迁移成功，或先用默认生成写入它再压过旧用户值；只限制受影响的数据操作，不阻塞不相关功能。启动只做轻量状态检测，全量迁移在明确维护流程运行。
6. 迁移成功后只有新位置可写；旧位置不再参与日常 fallback、默认生成或双向同步。保留显式旧路径导入能力，不用永久双写或 junction 维持假整洁。历史原件保留到验收，清理须按具名清单另行确认；未完成退役不能把结构收尾标完成。
7. 回退不简单恢复最初快照：AF2.0 已产生的新数据先完整保留，再由导出/反向迁移方案生成旧版可读副本并处理冲突；旧程序不认识的新格式不得被静默截断。代码回退与用户数据回退分别验证。

### 9.4 Build / Stage / Deploy / Package 的单向数据流

保留现有一键入口、参数分工、双版本编译与 Bootstrap 选择；修改其内部数据边界，不另造第二套构建系统。

```text
源码 + 唯一静态默认 + 已验证依赖
    → 原双版本构建
    → 只含明确交付条目的干净 Stage + 文件清单/hash
    → 安装程序文件                 → 从同一干净 Stage 生成 ZIP

旧个人数据 → 私有备份/候选 → 独立用户数据根
                              ↕ 原业务 owner / 编辑器
                         不回流源码、Stage 或 ZIP
```

- `deploy_module.ps1` 不再 `robocopy` 源模块整棵树，不再要求源码必须有 ONNX/PlayerExports；仅由内容映射、模块模板、已验证 DLL/private dependency 白名单组装。删除 Stage 主动复制源码 PlayerExports 的路径。
- 在新数据 writer 和迁移都接通后，退出 `Sync-PlayerExportsBackToSource` 及其成功后调用；不能先删回写再让数据失联。纯 Stage 不读安装玩家目录，部署只替换被程序清单管理的文件，独立数据根永不进入替换/回退/清理集合。
- 遇到旧安装中的用户目录、未知文件和手工修改，先按迁移/冲突策略处理，不能为了纯程序目录整根 `/MIR` 清掉。部署 rollback 只恢复程序，不回滚或清空迁后玩家数据；首次安装/升级/中途失败分别有完整证据。
- `package_mod.ps1` 从验证过的 Stage 与逐文件 allowlist 生成 ZIP，不再对安装目录全扫后靠少数排除规则保隐私。包中含未知文件、个人资料、覆盖设置、凭据、日志、模型或未获分发批准的资产时直接失败；并验证没有遗漏必需的合法资源，不能靠空包通过。
- 原 ZIP 单模块根、两个 `AnimusForge.dll` 路径、Bootstrap、原版 DLL 禁入和 ONNX 禁入政策保留。原 `SourceModuleDir`/版本读取与 overlay 的实际消费者同步核验；破坏性的参数语义变化要明确报告，不静默改调用者的行为。
- 无用户内容的两次 Stage 应有相同的规范化文件集合/hash（编译时间或生成元数据等差异单列），且不受开发机个人资料、游戏安装残留影响。不以 ZIP 时间戳字节完全一致代替内容可复现。

### 9.5 每类剩余资源的退出方式

| 剩余范围 | 最终处理与退出证据 |
| --- | --- |
| PlayerExports 3139 项、旧 profile/VoiceMapping、异常 HTML | 私有备份+恢复验收后移出活动源码输入；玩家内容只归用户根或私有历史归档。明确获准的 curated 内容才按 owner 成为 shipped defaults，且无反向 writer。诊断文件不进入发行包；不读正文凑资源白名单 |
| CustomPrompts 等可写配置 | 随包默认与用户覆盖分别有实际 reader/writer；升级、坏文件、旧布局、未知字段、恢复默认、回退均通过。不能仍靠部署 `/MIR` 整个 CustomPrompts 维持覆盖 |
| embedding / reranker | 已有 `content/models.lock.json` 与 `content/runtime-dependencies.lock.json`，不再新建草案中的 `dependencies.lock.json`。模型锁约束全组字节；来源、revision、许可证依据与合法获取说明另需闭合。图、external data、tokenizer、config 作为一个单位；补无旧安装的显式本地初始化及恢复/锁升级校验，复用既有工具，见 F4-M。缺失/损坏要明确报依赖未就绪，不能静默停用既有功能算完成 |
| 两份 `pack0.tpac`、设计源 | 建立唯一权威 owner/源，所有活动构建/扩展工具从其投影，不继续双份手改。设计源不冒充运行包。所需动作/mesh/碰撞资源实际存在且两版本功能可用；同 hash 不能替代授权 |
| GUI 33 项与索引/atlas | 按实际 consumer 成组处理：活动内容与其索引/atlas 一起归位；未知来源补证或功能等价替换；证实废弃的内容按备份清单退出活动树。不能仅 grep 无结果就删除，需验证游戏自动加载与实际 UI；三个缺 sheet 的旧索引必须有明确退役或修复结论 |

**许可不是一个 `approved=true` 开关。**来源不明素材须获得有效依据，或改用具有明确使用依据且通过同等功能/视觉验收的替代；没有解决就不能宣布完整 AF2.0 发布就绪。更换 embedding 模型还需验证语义质量、向量维度/tokenizer，并版本化重建派生索引，保留原始知识/记忆；不能只改模型文件名。没有权限的分发只阻塞相关发布，不能再次阻塞不触及它的路径、迁移器与干净组装工程。

Git 忽略规则不清除已跟踪文件或历史。当前树退役、个人备份和历史泄漏风险分别验收；不自动改写历史、不向远端推送含既有私密历史的分支。源码交付必须从审核过的源码白名单导出，若以后确需历史治理，再单独制定授权与恢复方案。

### 9.6 有限实施批次与硬退出门

| 批次（内部仍属完整 J15 收尾） | 实际交付 | 当前边界 / 退出要求 |
| --- | --- | --- |
| F1 内容/数据边界与路径 | typed 根及分类 | 主体已有离线证据；保持源码/安装/用户根分离，不重写路径平台 |
| F2 真实接线与迁移器 | 现有 PlayerExports、编辑器、Prompt、TerminalSettings、日志/缓存、模型读写 | 主要切片已验；不虚构尚未接入的模块设置，不因本次重分类把未来 UI 宣布完成 |
| F3 单向组装与发布边界 | 干净 Stage、受管文件部署、ZIP allowlist | 已离线验收；不是资源齐全或实机验收，F4 变动后 F5 重新绑定最终候选 |
| F4 资源归位和旧树退役 | 第 9.8 节 C/U/A/D/M 五个责任组 | 各组有已实施与已验证结论，缺许可/删除授权只停相关项；有未处理资产仍不标 F4 全完成 |
| F5 同候选总验收 | 第 9.9 节离线、实机/旧档及交付检查 | 离线与 LIVE/SAVE 分开签收；无实机条件可完成离线子集，不冒充整个 F5 |

F4-C/U/A/D/M 是责任组标识，不是新主线、五套新框架或独立 agent 指令。先做 C 的范围/冲突边界和各组只读冻结，再按依赖完成具名已授权组；模型初始化可以与资产来源调查独立推进。F5 的准备/定向测试可以提前做，**最终签收必须绑定 F4 完成后的同一候选**。不重复搬迁已验的 3139 项 PlayerExports 或五个旧仓内模型。

性能约束：路径根初始化一次并缓存；默认/覆盖沿现有 reload/fingerprint 时机读取；全树盘点/hash 仅在显式迁移、组装、依赖验证时分批执行，有取消/进度/有界内存；模型 hash 不进入 Tick 或每次推理。文件 I/O 与游戏对象线程边界分离；UI 线程不整批扫描 3139 项，不以默认禁用迁移/功能满足性能指标。

每个完整可验证切片建立本地意图/检查点并提交结果，不为每个 helper 增加阶段或复制 harness。移除旧路径前证明新 consumer 接通；不删断言或只换 hash 获取 PASS。只改文档时只做文档验证，不顺带跑六构建、迁移或实机。

<a id="j15-scope-decisions"></a>

### 9.7 本次确定的设置范围与不变项

| 类别 | 本次确定的处理 | 不允许的扩张 |
| --- | --- | --- |
| 未来界面分工 | MCM 最终只展示 API 相关设置，功能模块设置进入 AF 自有 UI；这是后续具名 UI/模块接入工作包 | 不在 F4/F5 临时做完整 UI、移除全部 MCM 字段或制造空设置迁移 |
| 已有普通模块设置 | 当前 `TerminalSettingsRegistry` 读写 `DuelSettings`，`DuelSettings.TerminalSave.cs` 仍交给 MCM；现阶段保留这一权威存储，验证现有选择不丢 | “界面搬走”不等于“存储已独立”；不提前删除旧值、改全局设置 Id 或做长期双写 |
| 终端两个入口 | `AnimusForgeTerminalSettings.cs` 只负责热键入口和地图图标，继续用已实现的用户根 Settings | 两项迁移不代表整个模块设置系统完成 |
| 内部 FeatureBridges | 默认源仍归 `AF.Foundation.Runtime`，通过映射发布到 `ModuleData/FeatureBridges.json`；保持启动期一次读取、严格校验与 owner fail-safe | 明确撤销本计划此前要求新增该文件“个人覆盖层”的设计；不把内部连接与玩家功能开关做成两套可编辑 UI |
| 现有 Prompt / 玩家导入资料 | 保持已实现的默认/覆盖与显式导入语义，使用真实现有 consumer 验收 | 不把 VoiceMapping 或 UnnamedNpcProfiles 改成自动导入，不把战役状态搬到全局设置 |

“内部随版本配置”不是丢弃旧手改内容的授权。F4-C 在部署预检中对 FeatureBridges 检查**已知旧版默认 / 目标新版默认 / 未识别差异**：前两者可按受管文件更新；未识别差异或坏文件须在任何目标替换前报告冲突并停该次部署，按具名确认保留原件/备份后才决定覆盖。只校验这一具名配置，不引入通用配置中心、不在 Tick 比对、不改变缺失与损坏的运行期语义。现有 F3 的受管备份不是这项冲突识别已实现的证据。

未来每个模块设置接入时一起交付 UI → 模块设置 owner → 保存/读取 → 旧值接续 → 重启/升级验收；有旧 MCM 值才迁移，没有旧值的新选项使用默认。完成切换后只有一份权威值；API 凭据留既有 owner，不跟模块资料、日志、测试或发布包走。该后续包在总台账明确为未实施，不计为 J15 失败，也不计为 AF2.0 已完成。

<a id="j15-f4-execution"></a>

### 9.8 F4 执行清单

#### 开工先做一次冻结，不重做全仓盘点

从当前 Git/主台账重新核实已完切片；以 `tests/content/J15ContentContractTests/run.py` 的 `CURRENT_HOLD_PATHS`、两个具名 TPAC 路径、六个 AssetSources、剩余资料根为起点。检查相关 tracked/untracked/ignored、reparse 与实际 caller/loader/脚本/overlay，禁止按后缀猜用途。检查点 `5252c0ac` 仅记录本次文档工作开始，不是未来 F4 产品基线。

**执行表字段固定**：组 ID；旧路径；目标源路径/归档路径；不变运行路径；实际 owner、reader/writer/工具；大小/SHA；来源依据；动作（原样迁移/保留依赖/等价替换/退役）；备份/恢复方法；验证入口；所需授权；当前状态。安全静态清单记主台账，包含玩家文件名/正文的详细表只留批准的私有 Recovery。一项一个结论，不以“全部 HOLD”替代调查，也不把未决事项先填已完成。

| 组 | 已核实输入与责任 | 实施动作 | 退出门 |
| --- | --- | --- | --- |
| F4-C 配置边界 | 第 9.7 节；`Refactor/Runtime/FeatureBridgeRuntime.cs`、`一键编译覆盖推送/deploy_module.ps1`、`tests/content/J15ContentContractTests/ManagedDeployContractTests.ps1` | 固定内部默认分类；补具名配置冲突预检及已知基线证据；不实施未来 UI | 合成已知默认可升级、手改/坏配置在写入前拒绝、缺失采用新版默认；既有 MCM 与终端值不因配置分类改变；正常部署/回滚仍通过 |
| F4-U GUI | 旧 `AnimusForge/GUI` 中 33 项：两个 XML 索引；`SpriteParts/ui_account`、`ui_achievement`、`ui_subscribe` 共 25 张图；五张备选/旧图；`SpriteSheets/af_vassalage_notifications/af_vassalage_notifications_1.png` | 逐组追到 movie/brush/sprite/atlas 与自动加载；有活动消费者且来源可用的内容按实际 owner 迁到既有 content；旧/废弃项证据充分后才按批准表归档；来源不明的分发项另报 | 保留 XML ID、sprite 名、尺寸/布局和活动引用；不能因 grep 无命中认定废弃；索引引用的三个缺 sheet 各自修复或经证据退役；新 Stage 必需资源闭包完整，实际视觉检查归 F5 |
| F4-A TPAC / 设计源 | `AnimusForge/AssetPackages/pack0.tpac` 与 `extensions/AnimusForge.XihaiAction/AssetPackages/pack0.tpac`；`AnimusForge/AssetSources` 六项 | TPAC 归 `AnimusForge.XihaiAction` 实际 owner，比较字节和内容依赖，明确唯一权威源；两消费者只消费明确生成投影，不保留第二份手改源。金币设计源归 `AF.Module.Economy` 的 content/AssetSources，不进客户端运行包；同步工具/mbproj/overlay 的真实源定位 | TPAC 不同不得选大/选新直接覆盖；动作、mesh、碰撞引用成立；依赖许可有证据；Core 与 StaticVerifier 的相关历史失败逐条复验，不屏蔽 TPAC 失败。无许可只停相关分发，不阻无关源码工作 |
| F4-D 剩余旧资料 | `AnimusForge/ModuleData/UnnamedNpcProfiles` 六项、`AnimusForge/VoiceMapping.json`、旧根异常 HTML；执行前重验存在性，不输出正文 | 核对显式导入/export/save 消费者，默认视作私人历史资料而非 shipped defaults；列私有备份与归档清单；备份 hash/恢复通过且具名获准后退出活动输入/跟踪，不删除唯一副本、不改变自动读取行为 | 当前树不承载活动私密资料；原有显式导入仍可用，实际存档键不变；空新根不冒充迁移成功；当前树退出不代表 Git 历史已清除，禁止据此推送 |
| F4-M 模型完整生命周期 | `content/models.lock.json`、`src/AF.Persistence/AnimusForgeModelStore.cs`、`tools/af2_migrate.py`、`tests/AF.Persistence/DataPaths`；既有活动组与备份只读 | 按下方模型规格补无旧安装初始化、恢复及锁变更复验；调查锁中未核实的 reranker 上游身份；不再迁一次已完成的本机模型，不改检索模型/维度凑通过 | 新装、旧装升级、损坏/冲突、中断/重复、备份恢复及锁版本变化均有正反例；没有来源证据不虚构获取链接或再分发权；真实推理/检索归 F5 |

五张备选/旧图具体为 `af_courier/af_courier_scroll_version_a.png`、`af_player_notoriety/af_player_notoriety_background.png.tmp.png`、`af_player_notoriety/af_player_notoriety_culture_panel_patch.png`、`af_terminal/af_scroll_quill.png`、`af_world_diplomacy/af_world_diplomacy_notice.png`（相对旧 SpriteParts）；这只是定位，不是删除批准。六个设计源以当前 tracked 集合冻结；不遍历或清理无关外仓。

#### F4-M：模型首次初始化与恢复的最小实现规格

1. **只扩现有工具，不新增下载器/模型平台**。在 `tools/af2_migrate.py` 增加明确的本地准备目录输入：目录含 `embedding/`、`reranker/` 完整组，不要求伪造旧 `Modules/AnimusForge/ONNX`。输入路径由使用者明确给出；只读盘点默认，不隐式联网、不搬走源文件。
2. **拟增接口，不是现有可执行命令**：模型分支增加 `--source-kind prepared`、`--prepared-models <absolute-directory>`；增加经生产路径规则校验的 `--data-root <absolute-directory>`，缺省遵循 `ANIMUSFORGE_DATA_ROOT`/系统用户目录同一约定。显式参数优先于环境变量；输入参数互斥冲突、数据目标落在源码/Stage/安装根、越界/reparse 必须拒绝。不得用生产开关跳过测试根限制。当前 CLI 仍只有 installed/repo 来源，实施并验证前不能声称 prepared 已可用。
3. 准备目录与活动根分离；对每个完整组按锁识别允许的 variant，不用 source-kind 冒充模型身份。逐文件 SHA/集合匹配后复用既有候选、无覆盖激活、私有 manifest/completion/readiness 契约。已有不同字节不被替换；整组完整并复验后才标 ready，缺一项不产生成功状态。
4. 首装、旧装迁移和恢复最终都能由同一运行时校验通过。当前 `AnimusForgeModelStore` 依赖 `.af-models-ready.json` 及关联 `Recovery/.../completed.json`；恢复规格须保留所需记录与模型字节，或从完整锁定的准备目录重验并重建**真实**完成记录。不能手写成功标记、删除 Recovery 依赖断言或把任意目录当就绪。恢复时重新核验 SHA，再记录实际大小/mtime，不要求用户手工伪造旧时间戳。
5. 锁文件变化会使现有 readiness hash 失效：提供显式重新校验/升级完成记录路径；未改变组的活动字节仍保留，变更组需明确输入和冲突处理。不得让正常程序升级永久卡在旧 receipt，也不在启动/每次推理重算大文件 hash。
6. 合成组先测：空根首装、已激活重复、未知/少/坏文件、source 期间变化、磁盘满、中断续作、目标优先、坏备份、丢完成记录、恢复后 mtime 变化、锁变化；现有两来源迁移反例仍保留。真实模型只在独立批准的测试根验完整 SHA 和加载；本机当前活动库不是 fixture。
7. 初始化工具也必须可交付，不能要求新玩家拥有含私密历史的开发仓库。沿现有单模块 ZIP 白名单投影现有迁移脚本及其必要的只读清单/默认基线到具名维护工具目录（拟 `Tools/AF2Migration`），源码仍在 `tools/`；工具支持安装包布局，安装包中禁用仓库来源模式。逐项冻结所需文件/许可/运行时并验证脱离 checkout 可运行，不复制整个 tools/content 树、不增加第二个客户端模块或隐式安装 Python。说明实测支持的 Python 版本，缺少解释器就明确未就绪，不暗中安装全局依赖。若既有迁移工具不能直接复用包内默认路径，仅改窄定位接缝。
8. 发布说明明确模型不在主 ZIP、合法获取来源/固定版本/完整文件组及初始化命令；模型源码/本地字节锁不等于获取许可。无法确认 reranker 来源时可以完成合成与获准的本地技术验证，发布交付仍未完成；更换模型需另行评估质量/索引兼容并确认，不能静默替换。

#### 每组统一落地规则

- 新映射、唯一源、实际 loader/工具/overlay、相应测试同一切片更新；设计源不进运行映射。调整旧 HOLD 断言时，必须用该项迁移后唯一源/hash/消费者或已验证退役证据替代，不删整段保护。当前 J15 runner 仍要求旧 HOLD 原位，不能只移动文件后把失败称为测试过时。
- 归档目录只在具名清单批准后确定；源/目标最终绝对路径、大小、空间、reparse、恢复 hash 一起核实。默认不删除旧私有 Recovery，也不把未完成的游戏目录旧文件当作本次仓内清理对象。
- F4 的收口要求每项为“已验迁移 / 已验依赖方案 / 已验退役”之一；“等待来源/授权”是明确停点，不是完成。未映射且活动的合法资源不能因 ZIP 白名单干净而省略。

<a id="j15-f5-execution"></a>

### 9.9 F5：同候选验收步骤与证据

#### F5-0 冻结候选和执行环境

先记录产品 commit/dirty、content-map、模型锁/运行依赖锁 SHA、相关源内容集合、构建配置与精确游戏引用版本、DLL/PDB/marker、Stage/ZIP 清单。F4 后内容数量按真实映射重新推导，不硬凑 107/123。当前四实现各有 **8 项内嵌资源 = 原 7 默认 + ModelsLock**；从 csproj 和实际 DLL 双向对照完整集合，不借修改计数放过缺资源。

核实 SDK：仓内 `local/dotnet/8.0.425/dotnet.exe` 用于已有 net8 契约；编辑器 smoke 为 **net10.0**，另定位已安装的适用 SDK，不降目标框架。双版本沿原脚本核实 `_deps_auto` 与 `local/bannerlord-refs/1.4.7.117484` 等实际输入；名称不证明版本，以 BuildInfo/marker 为准。依赖库不是可启动的两个实机环境。

执行单先写明所有输出/清理路径：原四个 `bin/{Debug,Release}/single_module_artifacts`、`obj/single_module/{Debug,Release}`，项目内 Stage、测试产物和 ZIP 各自列出。现有内容 runner 使用 `--run-root` 新目录且拒绝覆盖；保留这种方式。历史测试存在递归清理/系统 Temp/真实用户根探针时先读参数与实际写入，再使用批准隔离根或给现有 runner 补窄路径参数，不另建平行测试体系，不关闭产品拒写源码的校验。

#### F5-O 离线矩阵（按顺序完成）

| 顺序 | 已有入口 / 操作 | 必须证明 |
| --- | --- | --- |
| O1 资源与迁移契约 | `tests/content/J15ContentContractTests/run.py --run-root <new-path>`；`tests/AF.Persistence/DataPaths/test_migration.py`；`tests/AF.Persistence/DataPaths/DataPathsTests.csproj` | 当前映射/引用/唯一源，非法映射零部分输出，F4-C 冲突预检；现有导出/Prompt/终端设置/模型与新增首装/恢复反例，数据不丢、重复不重复激活 |
| O2 编辑器 / 导入导出 | `tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/PlayerExportsEditor.SmokeTests.csproj` 的 `--path-contract`、`--path-contract-invalid`，再跑隔离合成数据的完整编辑/导出/恢复；`tests/AF.Persistence/PlayerExports/run.py` | 游戏和编辑器定位一致、拒写旧根、正常编辑保存、原有绝对路径显式导入/最新包选择不变；两个 path-contract 不能代替完整编辑 smoke |
| O3 六构建 | 原 `一键编译覆盖推送/build_single_module.ps1`，Debug 后 Release，两个 API + Bootstrap；不传 `-Deploy` | 绑定当前源码与精确依赖，程序集身份、单模块和编译都成立；记录全部退出码，不能借旧 DLL 或只测 1.4 |
| O4 当前 DLL / 行为 | `tools/NativeModuleSubmissionTests/run.py --legacy-abi`、`tools/ModuleFrameworkApiTests/run.py` 指向当前双配置 artifact roots；`tools/PersistenceIdentityAudit.py --baseline e58f3558cddfe473f396bb7f11470b05e30afce9`；`tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`；受影响 Prompt/Policy/Bridge 契约 | 四 DLL 的资源集合/源字节与 V1/旧 ABI、保存 type/key/behavior 保持；新增模型锁有明确身份；不重开无关业务算法 |
| O5 完整 Stage / 场景资源 | 原构建入口经批准加 `-Stage`；Xihai Core tests 与 `extensions/AnimusForge.XihaiAction/tools/StaticVerifier/AnimusForge.SceneActions.StaticVerifier.csproj` | 双配置完整 Stage，唯一 Bootstrap 和两实现；movie/sprite/atlas/TPAC/action/mesh/碰撞引用成立；StaticVerifier 以当前 Stage 和只读游戏根为输入，若工具还绑定旧路径则做最小接线而不是省略失败项 |
| O6 部署、ZIP 与恢复 | 现有 Stage 白名单、ManagedDeploy 契约；`一键编译覆盖推送/package_mod.ps1` 只读当前 Stage；虚构旧安装/全新安装、失败与中断场景 | 实际文件集合与允许内容/依赖一致、非 XML 字节与 Stage 相同；版本 XML 单列，不写源码。未知用户内容不动，配置冲突先拒绝；中断留下混合程序时能检查/恢复后再部署，不宣称跨文件原子 |
| O7 收口复查 | `verify_code_map.py` 两模式、`git diff --check`、受影响 inventory/overlay 实际 file-set | 最终源码/资源/产物证据是一组；发布包无私人数据/密钥/日志/模型/设计源/原版 DLL；剩余活动资源无漏项，J16/J17 和未来 UI 没有被偷标完成 |

O1/O2 的已知安全注意：DataPaths 测试含系统 Temp 写入；编辑器不带参数默认会解析真实用户根并有临时目录删除；PlayerExports runner 会清理固定 `artifacts/tests/persistence-player-exports/<mode>`。正式执行先隔离这些路径并取得需要的写入/清理批准，禁止对真实库无参跑完整 smoke。Policy 契约关联生产项目，不以直接 `BannerlordApi=1.3` 绕过统一构建的依赖验证。

**可复用命令模板（不是本轮执行记录）**：在实际仓库根直接运行；`<...>` 变量须在 F5-0 固定并检查存在性/目标范围，未填写时不得运行。`$dotnet8`、`$dotnet10`、`$pwsh` 是已核实工具绝对路径，`$runRoot` 是仓内全新目录。

```powershell
python -X utf8 -B tests/content/J15ContentContractTests/run.py --run-root $runRoot
python -X utf8 -B tests/AF.Persistence/DataPaths/test_migration.py
# 先处理上文 Temp / 输出路径准入，再运行以下测试；不得全局重写用户目录。
& $dotnet8 run --project tests/AF.Persistence/DataPaths/DataPathsTests.csproj -c Release
& $dotnet10 run --project tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/PlayerExportsEditor.SmokeTests.csproj -c Release -- --path-contract
& $dotnet10 run --project tools/PlayerExportsEditor/tests/PlayerExportsEditor.SmokeTests/PlayerExportsEditor.SmokeTests.csproj -c Release -- --path-contract-invalid
# Debug / Release 顺序执行；这里的游戏根仅作只读构建依赖，不是部署目标授权。
& $pwsh -NoProfile -File "$root/一键编译覆盖推送/build_single_module.ps1" -ProjectRoot $root -BannerlordRoot $gameReadRoot -Bannerlord13ReferenceDir $refs13 -Bannerlord14ReferenceDir $refs14 -RuntimeDependencyDir $runtimeDeps -HarmonyCorePath $harmony -Configuration Debug
# 同一参数组 Configuration=Release；O5 在 Stage 清理获准后对两配置使用原 -Stage 开关。
python -X utf8 -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet8 --legacy-abi
python -X utf8 -B tools/ModuleFrameworkApiTests/run.py --dotnet $dotnet8 --artifact-root bin/Debug/single_module_artifacts --artifact-root bin/Release/single_module_artifacts --legacy-v1 tools/NativeModuleSubmissionTests/.generated/legacy-abi/Baseline/Consumer/bin/Release/net8.0/NativeModuleUnderTest.dll
python -X utf8 -B tools/PersistenceIdentityAudit.py --baseline e58f3558cddfe473f396bb7f11470b05e30afce9
python -X utf8 -B tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py
# O6 对每个当前 Stage 调用；只生成本地验收 ZIP，不上传、不从游戏安装目录打包。
& $pwsh -NoProfile -File "$root/一键编译覆盖推送/package_mod.ps1" -ModuleDir $stage -OutputDir $zipOutput -NoBump
python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py
python -X utf8 -B .agents/skills/af-core-framework/scripts/verify_code_map.py --working-tree
git diff --check
```

每条命令单独检查退出码，失败立即停该验证链，不能因最后一条成功掩盖前面的错误。已有 Prompt/Policy/Bridge 运行参数复用主台账同源码切片的实际命令，开跑前核对依赖与输出；没有读取参数的 live probe 不混进离线矩阵。源码/资源/锁/脚本变化后重跑受影响项并重建受影响产物；原验收记录保留失败和修复过程。

#### F5-L / F5-S 实机与旧档矩阵

启动前填好：两个**实际可启动**的游戏版本完整 BuildInfo、游戏/模块根、依赖 MOD 版本与加载顺序、待部署 Stage/hash、获准独立 AF 数据根、各版本代表性旧档副本及其基线、输出另存名称、备份/恢复位置。只读引用 DLL、合成 save fixture 或另一版本的实测均不能替代。找不到某个环境/旧档就记该格 NOT-RUN，不擅自切换 Steam 分支或覆盖唯一存档。

| 两版本分别执行 | 操作与最小通过标准 |
| --- | --- |
| L1 新安装启动 | 无旧模块/旧模型依赖的隔离环境，按发布说明准备并初始化合法模型；Bootstrap 只加载对应实现；缺模型时明确可恢复诊断，准备完成后实际推理/检索可用 |
| L2 已有 UI 与配置 | 终端、信使输入/回信、周报、已受影响的政策/外交等 UI 显示/点击/输入/焦点正常；迁移图片/图集视觉不退化。修改现有 MCM 与两个终端入口设置后重启值仍在；不要求尚未接入的新模块设置 UI 出现 |
| L3 实际内容使用 | 导入/导出与编辑器读写一致，个性/知识/语音/记忆/Policy/债务等代表性数据按原 owner 被使用；资源变更涉及的动作、mesh、碰撞实际可用；三渠道受影响的 Prompt/标签/AFEF 语义不串线 |
| S1 旧档往返 | 从每条版本线的代表性旧档副本加载，核对预先记录的数据/关键功能，另存为新测试档并退出重载，再核对同一组状态；只看到加载界面不算通过，不覆盖原档 |
| L4 升级 / 重装 / 恢复 | 旧程序+旧资料副本 → 迁移 → 新程序，确认用户值与资料保留；模拟移除/恢复**受管程序文件**不触碰数据根；模型按 F4-M 恢复记录与实际字节。若返回旧版，先保存新期间数据并验证显式反向导出/冲突处理，不用最初快照覆盖它 |

实机日志/截图/旧档清单留批准的私有证据目录，Git 只记脱敏结果、版本、候选 hash、用例 ID、退出/异常及证据定位。真实 provider/TTS 网络可能付费或发出内容，只有具名授权后测；未授权的相关场景明确 NOT-RUN。不给配置正常、离线测试通过赋予 LIVE/SAVE 状态。

<a id="j15-approvals"></a>

### 9.10 只保留必要的用户确认，不反复问内部技术选择

| 确认点 | 执行者先准备什么 | 没有确认时可以做 / 必须停什么 |
| --- | --- | --- |
| A1 启动产品实施与具名文件操作 | 当前表、拟改代码/映射/测试、最终绝对源/目标、备份/预计字节、恢复办法；批量移动/去跟踪/删除与测试清理分别列清楚 | 本轮仅计划；后续获准产品实施后可做普通仓内小补丁和合成工作，不凭此批量移动/覆盖/删除 |
| A2 私有迁移、归档、模型真字节 | 精确用户根/隔离根与私有 Recovery、来源只读范围、现有内容冲突、维护窗口及所需空间 | 可以合成测试/源代码准备；不得擅写 LocalAppData、真实活动模型/玩家目录或清私有备份 |
| A3 来源缺失或必须换素材/模型 | 先自查仓内出处、原作者记录及实际消费者；提供缺证据的具体文件/来源问题，必要时给等价替代范围与视觉/质量验收 | 不请用户为每个文件决定 owner；需要原作者/许可证据时再问。授权“继续做”不等于获得第三方权利；不影响无关已确认组 |
| A4 游戏部署、旧档与网络 | F5-L/S 完整环境表、受管写入范围、旧档副本与另存目标、数据根/配置快照、恢复步骤；联网/provider 单列 | 可以离线验收与报告缺环境；不得自动写 D: 游戏、启动/切换游戏版本、改原档、付费调用或上传资料 |

把同一责任组的必需确认合并一次，确认后按批准清单连续做，不逐 helper 重问。确认范围变化、发现未知手改/新私密文件或新外部目标时只停相关操作。第三方出处未决不阻无关内部工程；必需资源未闭不能签完整交付。推送、发布上传、历史治理、外仓镜像仍不在本计划授权内。

### 9.11 完成定义、状态与下一动作

- **计划可执行**表示范围、顺序、输入、操作、验证、授权停点都已明确，不表示来源、权限和实机环境已经齐备，也不表示这些检查已运行。
- **F4 完成**：各组执行表都有实际迁移/依赖初始化/退役及相关证据，源码与运行内容无活动双源，历史私有备份有恢复办法；未来 UI 未接入明确移交，不伪造实现。相关素材分发证据缺失要单列，不把工程搬迁成功等同许可通过。
- **F5_OFFLINE_VERIFIED**：O1–O7 对同一最终候选通过；这是可单独交付的离线结果。任一 LIVE/SAVE 或必需交付条件缺失，F5 及完整 J15 仍未完成。
- **J15 完整完成**：当前源码不承载活动私人数据/多份默认，部署不回写源码，程序目录不作既有 AF 用户数据 writer，Stage/ZIP 私密零混入且合法必需资源齐全；模型新装/升级/恢复闭合；对应 1.3/1.4 实机、旧档与升级实际验收通过。发布许可分别签收；不能以本计划撤销未来 UI 或 J16/J17 的全项目责任。
- 当前唯一下一动作：用户确认开始产品实施后，先完成 F4 各组只读执行表与 F4-C/M 的最小代码/合成测试切片；涉及具名真实资料或批量资产动作按 A1–A3 确认，再逐组收尾；F4 完成后冻结 F5 候选，先 O 后 L/S。当前文档编辑不会自动恢复另一个任务、启动后台工作或改变“停在 F3”的产品状态。
