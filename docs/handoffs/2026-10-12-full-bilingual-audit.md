> 历史报告：用户此后确认 Policy 外部写入停止，并授权接手修复。本文 `POLICY_BLOCKED`、542 余量及门禁失败等判断已被[Policy 收口验收](2026-10-12-policy-bilingual-closeout.md)与[主台账新条](../animusforge-refactoring-and-repository-reorganization-plan.md#policy-bilingual-closeout-20261012)明确接续；保留原证据，不把历史失败删除或冒充当前状态。

# 全仓双语化审查与修复（2026-10-12）

阶段：总结。可写范围内已确认的缺陷已修复并完成离线验证；全仓英文发行仍被 PolicySystem 阻断。不能按原交付报告的“100% 全部完成、双版零警告”验收。

工作区 `F:/AnimusForge-main`，当前分支 `main`，审查 intent 修订 `a911d4d0de8964a393b6bdc6eaa83705b5218b02`。基线包含其他作者大量已暂存/未提交/未跟踪文件，以实际逐文件 SHA 绑定，不能用 HEAD 代表全部被验源码。主台账：[当前条目](../animusforge-refactoring-and-repository-reorganization-plan.md#bilingual-full-audit-20261012)；职责：[源码图](../architecture/af-framework-code-scope.md#bilingual-full-audit-20261012)。

## 权限与当前发行边界

- 用户授权两个子智能体并行审查，根汇总与修复；GCCZ 曾在施工，收到交付并经用户确认停止后才写其目录。
- PolicySystem 外部仍在修改，按用户“先审查并登记精确缺陷”的指令只读。其 542 个解码后汉字运行字面量、35 个文件仍存在；完整文件/行/值见[运行清册](../../artifacts/localization-audit-repair-20261012/runtime-literals-verified-final-r7.json)。
- 英文源码发行门禁实际返回 1，首批 12 个位置只是显示上限，完整余量不能用这 12 条替代。[门禁收据](../../artifacts/localization-audit-repair-20261012/runtime-source-gate-verified-final-r6.json)、[原始失败](../../artifacts/localization-audit-repair-20261012/runtime-source-gate-verified-final-r6.log)。
- 保留共享中文事实、旧存档身份、动作标签、玩家原文；仅在 UI 或请求边界投影英文。Prompt/世界书按发行包选择，UI 按当前界面语言选择。
- 本轮未部署游戏、推送、正式打包，也未镜像到工作区外的 GCCZ。构建复用原单模块入口；没有改一键编译/覆盖入口。

## 主线实际修复

| 问题与触发 | 当前修复及消费者 |
| --- | --- |
| 普通历史文本读取两个不存在的聚合兼容键，字典索引抛错 | HistorySectionProjectionOwner 改读已有玩家/近期前缀键，Lazy 缓存数组；场景历史真实组装路径已回放 |
| 外交拒绝原因的 `{0}` 被 TextObject 提前解析为 `0` | UI XML 使用 `{REASON}`，先解析原生模板再一次插入 literal 动态值；实际两版 UI 解析通过 |
| 新英译文改写 AFEF/execution 标记，破坏历史识别或后处理 | 恢复 canonical 中文 AFEF envelope 和 `[during]` / `[last_statement]`；自然提示词仍双语，协议严格保持 |
| 旧记忆事实在英文 Prompt 中直接输出中文 | 35 类 courier 事实和 191 条 WorldMap 模板在请求边界投影；含日期/场景/发言者前缀、未知/歧义保原、CRLF/混合换行守恒；存档写回边界未改 |
| 动态姓名/ID含 `{SPEAKER}`、`{player_gold}`、`$1` 时被后续替换再次解释 | Annexation、AIConfigHandler、ambient、Duel 使用缓存模板与一次 MatchEvaluator；ambient 原生解析保留未绑定 slot，Duel 先 native 选语言后插入 literal |
| 资源 reader 使用不存在的分隔符或兼容键 | AdapterPromptCaptureCompatibility 补 11 个原值；Illustrator 两用途分隔符各用正确配对键；删除 Taunt 同值重复键 |
| Weekly、稳定度提示词按错来源选择语言，英文 High 分支遗漏 | Weekly 大陆名称与稳定度文本按发行包选择；不通过 UI 语言改 Prompt 语料 |
| 世界书导入目录显示仍暴露固定中文模块目录名 | 仅映射四个已知模块目录的 UI 标题；实际文件路径、Player export 与未知目录保原 |
| 外交导出错误返回不存在的 UI ID | 改用已有 ProfileDevUi_ExportDirectoryInvalid，与真实 DevUI 投影入口一致 |
| 六个音色标签有 XML 但未进发行映射/语言注册 | content-map 增加两 XML；两 language_data 注册 Llm UI；VoiceMapper 的实际六个消费者闭合 |
| Memory 默认空姓名仍使用固定中文，错误弹窗仍显示 `（无）` | 默认 actor 在 Prompt 边界使用中英 JSON；仅固定空字段组在 UI 边界投影 `(none)`/原中文，unknown schema 和模型回复仍 literal |
| 发行契约漏查数字 slot，历史协议样例被误当正文；旧静态测试不识别资源迁移 | package_localization 增查 `{0}`，只规范 23 个已知协议样例；18 个旧 runner 增加精确原值逆投影、保留原断言/历史 fixture；21 个内存变异负控全部拒绝 |

25 个旧 Memory XML 候选逐项追踪：16 个已迁至 Prompt/UI、5 个为共享协议、2 个真实漏口现已修复、2 个无当前调用。旧 XML 未映射/未注册/旧 ID 无 source 消费；没有盲目把它们重新注册成活跃资源。[逐项报告与真实消费者](../../artifacts/localization-audit-repair-20261012/agent-resource/voice-memory-closeout/delivery.md)。

## GCCZ 交付复核与实际修复

外部报告声称 196 个 C# 与零汉字；实际初次交付为 192 个生产 C#，原始汉字 0，但解码后仍有 635 个汉字运行字面量，含 12,330 个 Unicode 转义汉字字符。21 个新增 Prompt key 仅声明属性，未接 Resolve/实际消费；75 个新增 UI ID 与主 UI owner 重复。上述口径不能作为功能双语化验收。

| 缺陷 | 修复与边界 |
| --- | --- |
| Unicode 转义、char、verbatim regex 仍在源码；兼容 reader 不满足标准 JSON 字符串解码 | 外置 482 个 string token 与 15 个 char 位置；两 reader 复用一次 flat JSON parser。原 612 个 key/value 逐值保留，新增后共 1046（Castle670/Siege376）。原612条在旧 reader 中并未损坏；负控证明的是等价转义 key 与 literal\n/regex\d 的合同缺口 |
| 中文复合处置 remainder/全角标点/然后等被英文重写破坏 | 保留原中文边界，合并英文 and/then；13 个中文分隔符的计划与原完整方法逐值一致，权限/数量/步骤上限保留 |
| 默认记忆 event kind、旧摘要字符丢失 | 恢复 canonical 处置、出售金额加号、因后空格、被强迫的强；原错误键不覆盖，新增 legacy 原值键并接当前消费者 |
| 结算摘要中的动态名字再次被 action/penalty 替换 | 一次 literal 格式化；CN 完整 Build 对照原完整方法守恒，EN 保留当前输出，未知 slot 不猜 |
| 21 新 key、104 原 Prompt 段未真正进入资源加载/调用链 | TownPromptCatalogResources 两份 Lazy 显式语言读取，Resolve 与 11 Profile 真消费；disk/embedded 两路径、深目录嵌入实际执行；EN 缺失使用英文默认，CN 缺失明确失败 |
| Prompt 迁移更改否定/授权/限制与事实身份 | 修复 Village/Destructive/Gather 中文分支、Castle 512 状态，保留 SETS request/start/cancel/100 上限；LocalCivilian 恢复原 12..32 字符与历史事实标题，真实 action tag 保留 |
| 75×2 个重复 UI ID、28 个宿主错误语料选择 | 删除 sets 新增重复150定义，原143逐值保留；75旧主 UI owner 值未改。28宿主表达式分离发行包 Prompt/当前 UI/固定事实，5宿主文件逆还原 byte 相同 |
| 交付测试编译清单缺真实 helper，const 假 getter 不代表当前运行代码 | 两 runner 补真实依赖，真实编译并显式执行新 DLL；保留原13/494断言。standalone csproj 补四嵌入资源精确名，Prompt WithCulture=false |

独占报告：[行为与兼容](../../artifacts/localization-audit-repair-20261012/agent-behavior/gccz-delivery-review/delivery.md)、[资源与包选择](../../artifacts/localization-audit-repair-20261012/agent-resource/gccz-delivery-review/delivery.md)。GCCZ 当前 194 个生产 C#，解码后汉字运行字面量 0；这是源码结果，不是全场景英译语义完成率。

## 最终离线验证

下列数字属于各自明确的检查范围，重复/压力断言不当成独立玩法情景。

| 验证 | 实际结果与收据 |
| --- | --- |
| 原统一 Release 构建，1.3.15 / 1.4.6 引用 | 两实现各363警告/0错误，Bootstrap0警告/0错误，双 Coup 最终 DLL 门禁 PASS；8887 输入哈希无漂移。[最终r6收据](../../artifacts/localization-audit-repair-20261012/build-unified-verified-final-r6.json)、[日志](../../artifacts/localization-audit-repair-20261012/build-unified-verified-final-r6.log) |
| 警告实际分类，每 API 同一组 | CS8632 343、CS0649 15、CS0105 3、CS0162 2；没有宣称零警告，也未禁用警告。[分类](../../artifacts/localization-audit-repair-20261012/build-warning-inventory-r6.json) |
| 全部当前选定 Python 静态合同 | 44/44 PASS，9029 输入前后 SHA 一致；[结果](../../artifacts/localization-audit-repair-20261012/contracts-verified-final-r7/results.json) |
| content JSON/XML、映射、静态 reader 接线 | 6651文件解析/重复键0；3499映射 source 缺失0；39 catalogs/173方法，固定 catalog key/embedded 名缺失0；动态 reader 另以实际 probe 覆盖。[manifest](../../artifacts/localization-audit-repair-20261012/resources-verified-final-r6/manifest.json)、[wiring](../../artifacts/localization-audit-repair-20261012/resources-verified-final-r6/wiring-summary.json) |
| 实际 TaleWorlds UI，两个 API | 39对资源、12735 ID，每 API 50940次解析 PASS，English→中文→未翻译French→English；[native1.3](../../artifacts/localization-audit-repair-20261012/resources-verified-final-r6/native-1.3.log)、[native1.4](../../artifacts/localization-audit-repair-20261012/resources-verified-final-r6/native-1.4.log) |
| 历史/状态、记忆事实、execution speech | 历史/状态58、事实1462及CRLF负控 PASS，execution真实parser两版 PASS；固定中文 AFEF 不因显示英译被重写 |
| 动态模板真实双API×CN/EN | 32224断言/12修前负控 PASS；5856普通 ambient 输出等价，旧slot丢失513例有证据。[专项](../../artifacts/localization-audit-repair-20261012/agent-behavior/dynamic-template-final/report.md) |
| Duel、Voice 与 Memory实际消费者 | Duel两API9572断言/24负控 PASS；Voice/Memory Prompt88、nativeUI174 PASS；旧完整 Memory class/source-line负控复现两漏口 |
| GCCZ兼容与原值守恒 | net472/net8各493218断言/35负控 PASS，含unicode与缓存压力重复；12纯迁移文件逆恢复逐字节一致。[最终07](../../artifacts/localization-audit-repair-20261012/agent-behavior/gccz-delivery-review/final-replay-07/summary.json) |
| GCCZ Prompt真实加载/consumer与原四suite | 1709真实probe断言 PASS（含42双语JSON变异、1317旧值、512状态），原4suite703断言 PASS；固定protocol/slots/numbers一致，EN正文CJK0 |
| 世界书完整映射与路由 | 6330JSON解析，3133源映射/3124不同EN目标，9合并组18源逐字相同；同Hero不同body保留两目标，4负控拒绝。4成对世界书＋1英文独立世界书，AD1259的72JSON无中文对照。[收据](../../artifacts/localization-audit-repair-20261012/worldbook-verified-final-r6.json) |
| 实际发行资产合同/隔离ZIP | 3262资产0失败；隔离 fixture 69检查 PASS，CN/EN各含双版本占位输入，source/Stage守恒，缺失/转义/中文负控拒绝；非正式模组包。[资产](../../artifacts/localization-audit-repair-20261012/package-contracts-verified-final-r6.json)、[fixture](../../artifacts/localization-audit-repair-20261012/package-fixture-verified-final-r6-preloaded.log) |

复现入口：`run-build-review.py single-module-verified-final-r6 --preload-utility` 调原构建；`run-contracts.py --output .../contracts-verified-final-r7`；`verify-final-content.py resources-verified-final-r6`；`audit-package.ps1 -Output .../package-contracts-verified-final-r6.json`。这些 wrapper/receipt 在 `artifacts/localization-audit-repair-20261012/`；receipt 保存完整实际命令。相同 RunRoot 不能覆盖重跑，要新建证据目录。

原失败证据保留：初审缺键/外交slot、GCCZ施工编译失败、18旧静态迁移失败、统一构建零参额外逗号失败（现已修）、r2及最后 fixture 的 PowerShell Get-FileHash 自动装载失败。审查 wrapper 显式预加载本机 Utility 后执行未修改原入口，最终构建/fixture通过；未用删断言、假getter、放宽整个门禁或覆写失败日志制造绿。r5构建期间有3个纯格式文件变更，已由 r6 的0漂移构建取代。

## 当前剩余与未验范围

- 全仓词法清册为1889源码、621处/36文件/560不同解码值：Policy542＋XihaiAction StaticVerifier工具79。该工具不在当前游戏产品编译中；不能说整个仓库只剩一个目录有汉字。src、GCCZ与其他可写生产模块解码汉字运行字面量0。
- 待消费者候选56：Memory25已逐项闭合，Policy31仍属外部目录审查范围；候选不等于56漏译。[候选](../../artifacts/localization-audit-repair-20261012/untranslated-verified-final-r7.json)。资源没有平行.en文件的31项含兼容/不变身份，实际发行角色/契约已逐项检查，不一概视为漏译。
- Policy 的 31 候选与35源文件需区分玩家UI、包Prompt、内部诊断和旧兼容身份。原 CRCRLF 文件曾造成 LF计数行号/sourceLine错位，两个盘点工具已修复：r7按真实CR/LF记录行/列/内容并保留lfLine；536运行位置与31候选定位纠正，621/56数量和分类不变，652原生Get-Content校验通过。[只读登记](../../artifacts/localization-audit-repair-20261012/agent-behavior/policy-readonly-closeout/report.md)。
- Policy确认PR01地方历史TargetText绕过已有native mapper（Management2682/2714→2736，LocalPolicyUi1339/1435）；PR02目标Description被序列化进入英文效果请求（Targets1191，Generation3820）。只读报告另记录共享legacy string战略画像请求的中文政策尾消息、英文治理词过滤及固定政策事实消费投影候选；普通JArray政策请求不经过该旧helper，不能概称全部请求有此问题。13旧错误匹配与9称谓别名已排除为漏译，既有EffectText/fief-type UI投影保留。[逐31分类](../../artifacts/localization-audit-repair-20261012/agent-behavior/policy-readonly-closeout/candidate-classifications.json)、[35文件责任队列](../../artifacts/localization-audit-repair-20261012/agent-behavior/policy-readonly-closeout/file-responsibility-queue.json)。
- 定位工具新增共享CR/LF索引，仅CLI扫描频率执行；LF/CRLF/CRCRLF/CR四种边界通过，旧sourceLine错误负控保留。[真实定位回放](../../artifacts/localization-audit-repair-20261012/inventory-location-repair-replay.json)。最终产品仍对应r6冻结构建；工具变更后44静态合同在r7重跑PASS，9029输入0漂移。
- 全量扫描包括122451世界书字符串叶、233939 content JSON字符串叶；未做每一句人工语义审校。机器契约/部分中文byte守恒不等于全部英译质量通过。
- 未验证实机攻城/对话/UI渲染、真实provider和全部三渠道动作、实际压缩队列、玩家旧存档、外部const调用生态或正式CN/EN分发包。受控Hero/Campaign/ledger叶与真实native DLL不代替这些验收。

## 性能、格式与回滚

新增资源解析/词表/Regex在 Lazy 或类型首次初始化缓存，磁盘/embedded读取只发生在既有资源边界；文本投影只在原请求/摘要/结算边界调用。不增加 Tick 文件IO、全量对象扫描、反射或锁/空转轮询。WorldMap projection 深度最多8，未知/歧义保原；一次格式化只扫描模板，不重新扫描插入数据。

格式仅修9文件：6GCCZ文件29行空行尾空格；VoteDeal本轮两行的多余CR；新WorldMap文件与逆投影helper归LF。C#全部string/char token与非空白代码相同，Python AST相同。[格式收据](../../artifacts/localization-audit-repair-20261012/formatting-receipt.json)明确取代相关agent旧SHA，不把纯格式后当前源码假称仍与旧freeze SHA相等；未批量改旧全仓换行。

本轮133份代码/资源/测试文件，加4文档；before/verified-after逐文件字节与SHA见[本轮清单](../../artifacts/localization-audit-repair-20261012/review-slice-inventory.json)。原私有/他人暂存区不混入本轮，审查切片保存在仅本地分支 `codex/bilingual-audit-20261012-slices`。该分支的baseline是当前dirty交付中本轮文件的before快照，不是完整可构建的全部WIP快照，不能直接整分支覆盖主线。

回滚用[focused inverse](../../artifacts/localization-audit-repair-20261012/rollback-focused.diff)，先比对after SHA再只逆本轮hunk；[forward](../../artifacts/localization-audit-repair-20261012/forward-focused.diff)与[提交/原index保全收据](../../artifacts/localization-audit-repair-20261012/local-slice-commits.json)支持复查。不用hard reset、整文件before覆盖或推送这条本地审查历史。单文件原值备份存在根与两agent的before目录；两份新FactRecall的before为本轮中间版，清单明确按new file处理。
