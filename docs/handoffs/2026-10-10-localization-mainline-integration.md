# 全模组中文硬编码外置与双语主线

阶段：ACTIVE，实施意图。基线 main `338c88e7d`，当前工作树含他人及前会话未提交改动。

用户于本会话明确要求全模组去除中文硬编码并做双语本地化；UI、提示词、默认正文、事实/记忆模板、错误、日志均在范围内。协议/匹配/旧档中文也须资源化并保持兼容，不以转义、排除扫描或局部UI清零替代验收。

B/C已由用户派往外部：B负责英文JSON与世界书；C负责18缺键、快报固定文字、图库默认标题、标签目录显示。任务卡在`artifacts/localization/plan-B-external-translation.txt`及`plan-C-external-translation.txt`。主线不覆盖其文件或混提交其他作者内容。

首个主线切片：内战标签raw/display独立通道（契约、adapter、owner、经济HUD），复用现有内战资源；其后处理图库真实物品名、债务验收及全源码外置。每次原动作只执行一次，不新增Tick扫描、反射或后台游戏对象访问；原始事实、存档字段、动作资格、结果状态及事务语义不变。

盘点基线：1821个C#文件、22357个中文字符串/字符位置、590个含中文文件；1250个UI候选不等于漏译数。完整清单与16项发现见`artifacts/localization/audit-20261010-report.txt`及相邻JSON/CSV。全目标仍未完成。

退出门：实际消费者独立取显示投影；原业务及事实结果保真；相关契约/资源检查；全仓来源和未完成项可追踪。既有延后产品构建/Stage/部署/正式打包/Push边界保持，未执行不得记为PASS；当前源码双版本编译及实机/旧档验收另列NOT-RUN。

回滚：每个已验证切片精准本地提交，必要时focused inverse，禁止hard reset或整文件回滚共享文件。

## 已接通并验证的主线切片

以下为源码/静态/既有本地化DLL验证，不是当前产品编译、旧档或实机验收。

| 提交 | 实際变更与边界 | 已执行验证 |
| --- | --- | --- |
| `1df30479d` | 内战端口新增raw/display双返回；JOIN/RECRUIT/LEAVE/DETONATE一次执行生成HUD投影，原事实不变；ANSWER保留既有返回语义 | 26项跨模块静态合同，D既有合同，资源合同；既有1.3/1.4各456次Kingdom及4244次Diplomacy文本解析 |
| `91ef59a1f` | 图库新物品名来自现有中英资源；原公开创建签名保留，新增保名重载；同一已登记物品、入库和通知使用原名；无新存档字段 | 图库29项静态/后缀检查（词表外置后30项），既有双API各1372次Illustrator解析 |
| `710fcdb6c` | 接收前会话债务默认参与者/备注/玩家显示、独立日期投影和同一欠款双返回；8对资源；日志版本8仅加载边界升级 | 34项静态合同，DebtNormalization prepare-only生产接线，既有双API各1432次Economy解析 |
| `c273d038f` | 修复分块目录中的旧位置：内战保存/加载已在CampaignCivilWarPersistenceAdapter；只改目录source/variable | chunk-contract-only PASS：20分块字符串键、47扁平字典键；原1项OPEN风险未隐去 |
| `feddfa027` | 259个终端设置定义保留ID/类别/委托/范围/回调；移除755处中文显示fallback，另对齐4处英文fallback | 2552项静态资源/接线检查，非显示源码逆向一致；该文件1016→261中文字符串，分类匹配仍OPEN |
| `9a628c980` | 五个API测试入口的日志/测试Prompt共27字面量外置；20对新Foundation运行时资源；按钮线程捕获不可变文本，后台不新增本地化查询 | 36项静态合同，27对资源，既有双API各80次新资源解析；未调用真实API |
| `6e6e2fa1a` | C的`41a82ad2a`补足15决斗键后接收前会话决斗源码；声望、已付/未付赌注与债务HUD分离，原历史和效果状态保留 | 41项静态合同，债务34项；既有双API各12864次UI解析 |
| `8dc4bdc800` | 1470个中英物品识别词项移到GeneratedRpVocabulary.json；15数组/64装备规则/28食物规则保原值、顺序、类型、边界；嵌入资源仅初始化读取一次 | 与切片前源码逐值及非词表源码完全比对；245项静态/模拟合同；718食物、483装备后缀及误判保护保留；压力测试oracle改为实际JSON，完整压力测试未重跑 |
| `42fafce39` | 增加全量源码字面量扫描，不排除Prompt/协议/日志/已映射输入，解码Unicode转义 | 扫描CSV/JSON、文件hash与变化检测；不宣称C#全语义分析 |
| `d00e33d07` | 5份源码默认Prompt改用包内JSON及一次性缓存，保留用户覆盖优先级，完整英文fallback；无语言探测、无文件写入 | 41项静态合同；中文资产与旧源码默认值逐字对照（仅归一换行）；未改B资产 |

## 最新全量口径与未完成项

`artifacts/localization/runtime-literals-mainline-current.json` 当前快照HEAD `d00e33d07`、包含并行工作树：1824个C#文件，20860处中文字符串/字符，590个含中文文件，14567个去重值，扫描无变化。起始22357处到当前减少1497处；包含外部C并行改动，不能全部归因主线，也不是双语完成率。

UI词法快照`mainline-after-20261010.json`为较早`6e6e2fa1a`工作树：1246候选/135文件。它没有覆盖全部运行时文本，不替代全量口径。全部目标仍ACTIVE。

1. 仍需对20860位置按真实消费者继续资源化/双语接线；默认Prompt、事实/记忆正文、内部兼容值和日志不因本片完成而豁免。
2. `PlayerCustomPromptRule`源码默认比现有CN/EN JSON多固定资产例外和辨别口头陈述等规则，不能直接用不等价的文件替换。差异证据`artifacts/localization/packaged-defaults-mismatches-20261010.json`；本片未动此常量。
3. 兼容词表是双语输入数据，保留中文识别能力。现有EN门禁一律拒绝含中文的嵌入/内容资源，因此英文发行仍受阻；需要按实际资源用途建立严格验证，不能简单取消门禁或声明整包纯英文。
4. B/C仍在外部实施，后续要验收语义、覆盖和装配。世界书重复身份、JSON契约、旧中文原版损坏及英文入口接线尚未宣布解决。
5. 1.3/1.4当前产品编译、Bootstrap、PersistenceChunkReplayTests、编译回放、实机/旧档、Stage、正式CN/EN包、部署、Push全部NOT-RUN。既有DLL解析只证明语言资源能解析；新C#与更新后的压力测试oracle尚未获编译验证。

## 代码证据位置

以`8dc4bdc800`/`d00e33d07`为准，后续插入代码时优先按符号定位：

- `src/AF.Contracts/Internal/TeamModules/ICivilWarModulePort.cs:217-219`、`CivilWarModuleAdapter.cs:121-148`：两种TryApplyTag；`KingdomCivilWarOwner.cs:862-960`：四类动作的投影返回；经济`RewardSystemBehavior.cs`的`civilWarStatus`标签消费者保原facts、独立HUD。
- `src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs:8756-8805`：创建入口原签名与保名重载；图库`GalleryRpItemConverter.cs:16-106`：真实名称/通知/入库一致。
- `RewardSystemBehavior.DebtLedger.cs:664-697,1042-1090`：显示日期与单次欠款双返回；`DebtPromiseQuest.cs:47-60,162-190`：版本8及显示投影，原保存字段保留。
- `TerminalSettingsRegistry.cs:120-380`：259设置定义，CJK显示fallback移除；`ConnectionTestText.cs`和`DuelSettings.cs:6874,6906,6981,7063,7174`：五个按钮捕获点，后台只使用字符串。
- `RewardSystemBehavior.cs:263-279`及`RewardSystemBehavior.Vocabulary.cs`：原缓存数组仍供匹配方法使用；资源只在嵌套静态类型初始化读取。JSON不写入.sav，不新增分块键。
- `DuelSettings.PackagedPromptDefaults.cs`：5个Lazy默认值和已有严格读取器；用户覆盖仍由原TryReadLayeredCustomPromptTextJsonFile负责。

## 场景动作输入词表切片（2026-10-10）

- 基线`ace292b7d`：`CommandParser.cs`、`SceneActionFrameworkV1.cs`、`SceneActionFrameworkV3.cs`的49个静态字符串数组移入`content/modules/AnimusForge.XihaiAction/ModuleData/SceneActionVocabulary.xml`，721原始词项（684含中文）；中英别名、拒绝/否定词及顺序逐值保持。动作契约、输入权限、匹配循环未修改。
- `CoreProject/SceneActionVocabulary.cs`嵌套静态缓存只加载一次嵌入XML，使用BCL XmlReader/XmlDocument，无新增包依赖、磁盘轮询或游戏对象访问。统一模组与独立Core项目均嵌入同一资源；content-map登记同一逻辑名。
- 迁移反向重建三份源文件与基线一致（换行标准化），证据`artifacts/localization/scene-vocabulary-migration-20261010.json`和`verify_scene_vocabulary_20261010.py`。`SceneActionVocabularyContractTests.py` PASS 110检查；资源合同PASS 27对；diff空白检查通过。Core编译/动作解析测试及双实现产品编译仍NOT RUN。
- 这只是识别输入外置，不代表动作模块提示词/日志/显示全部双语完成；规则内嵌别名和正则仍待后续处理。双语兼容数据的英文发行门禁仍OPEN。

## 场景动作双语目录与分类提示词（2026-10-10）

- `SceneActionFrameworkV3/V4`的27个动作名称/语义、115条正反例及`AfV130AuxiliaryTextClassifier`四段提示词移入`afscene_catalog_strings`中英对（115键）。CN逐值对照Git基线`f90f9128b`完全一致；原动作键、匹配词、执行权限及协议保持。`SceneActionCatalogText`按语言一次性读取嵌入XML，独立Core和主程序集使用相同资源，无新依赖或Tick读取。
- V3/V4的`BuildClassifierDefinitionBlock`、`NpcReplyDirectiveTagV1.BuildCatalogText`保留旧签名并新增显式语言重载；统一后处理`SceneActionDirectiveController.BuildSceneActionDirectiveRules`选择当前语言，两个分类器transport从`AfCompatV130.TryInstall`接收安装时语言快照。后台只用不可变字符串。分类器运行期间更改游戏语言需要重建provider/重启；未做实机语言切换验收。
- 命名占位符经Core转换为string.Format索引。初次数字占位符被真实TaleWorlds解析为数字，已修复并复验；不以修改测试oracle绕过。`SceneActionBilingualCatalogContractTests.py`493静态检查、原文比对、词表110、连接测试36及资源28对PASS。既有1.3/1.4各460次目录解析、各84次运行时资源解析PASS。
- Core测试Program.cs新增27动作双语/白名单/中文旧格式/协议断言，但编译测试NOT RUN；上述资源解析不加载本次新C#。当前产品双编译、LLM真实判定、实机、正式包仍NOT RUN，英文嵌入资源门禁仍需后续语义分类。
- 全量源码新快照`runtime-literals-mainline-current.json`（HEAD `85ca920e4`含本切片工作树和外部B/C）：1827文件、19952中文位置、589含中文文件、14063去重值，无扫描中变化；不能把所有减少归功主线，不能当成双语完成率。

## 用户转交“B报告”的固定界面包验收（2026-10-10）

主线另补`CourierLetterReplyPopup.Show(senderName, bodyText, onClose)`两处中文默认文案，复用已登记`AF_CONV_UI_Courier_ReplyTitle/Wrote`；正文/回调/关闭参数原样转交。现仓库真实调用使用其他重载，此项补齐公开短重载的固定显示边界，不夸称修复了已观察到的游戏泄漏。既有1.3/1.4各1348次会话资源解析与diff检查PASS，产品编译/实机未跑。

用户转交报告附件`c619e6f8-e543-413f-914b-d512744894d5/已粘贴的文本.txt`，内容对应原任务卡C的固定界面范围；为避免混淆，按下列提交和内容接收，不把它计作世界书/英文JSON包交付。

- `41a82ad2a`：18对Duel/Terminal/Weekly资源键。Duel源码由主线`6e6e2fa1a`独立接通；回滚资源前须协调该依赖。
- `d1f5b9214`：快报52对固定显示键，`WorldBulletinStateOwner.Presentation.cs:73-137,167-285`面板组装时投影，VM/prefab绑定不变；minor kind结果在本次面板构建内复用。原事实、日期原文及存档正文仍不是已翻译内容。
- `054b181c6`：图库3对默认标题/名称键；`IllustratorUiText.ProjectKnownDefaultTitle`与卡片、列表、通知连接。仅识别已知模板，原始SaveImage/DiskCache值不改。历史用户标题恰好符合模板时仍可能被当作默认标题投影，未做旧缓存实机验收；真实新物品名另由主线图库切片处理。
- `02eb72bad`：14对已知村庄动作显示键，列表/详情/导出共用`TagCatalogUiText`；未知描述与协议值透传。全动态快照未知项不属于已证明覆盖。
- 主线复跑PASS：资源28对、Illustrator24通知/入口键与346配对键、TagCatalog42键+3复用、Duel41检查。既有1.3/1.4分别13128次AF.UI解析及1384次Illustrator解析PASS，补足报告未做的资源加载验证。
- 验收结论：接收87对新增固定界面资源与相应显示投影。报告的27对为其交付时快照，主线新增场景目录后当前28对。没有当前C#产品编译、游戏视觉/语言切换或旧档实机通过证据，不声称全模组完成；没有推送/部署/Stage/打包。

## 历史设置提示词识别语料外置（2026-10-10）

- `DuelSettings.cs`的52个含中文历史常量及2个依赖别名改为同名私有static readonly字段，`DuelSettings.LegacyPromptDefaults.cs`一次加载`LegacySettingsPrompts.json`。54值/41920字符逐值不变，从源码移除236个中文字面量；仅`Previous*`/`Leaked*`，当前Default规则不在此批范围。
- 嵌套静态缓存避免partial初始化顺序问题；只读嵌入资源，不读用户文件、不接触游戏对象、不写存档、不新增Tick扫描。稳定原字段名以保留反射测试入口；ordinal完全匹配迁移逻辑与自定义文本保留逻辑未修改。
- 反向源码还原完全一致。语料SHA256 `e670050b8c5ac31ebb7d04e22451da404f50a326454cc52d442d833b8fe634bc`，`LegacySettingsPromptContractTests.py`117项、PackagedPromptDefaults41项、ConnectionTest36项、资源28对PASS。外交SmokeTests的旧默认读取改指JSON权威源，既有迁移安全断言逐项保留；此编译测试和真实旧配置回放NOT RUN。
- 语料属于历史识别输入，不应翻译后破坏旧配置匹配；英文发行门禁需区分这类数据与仍未翻译的当前输出。该任务与PolicySystem既有dirty改动独立。

## 固定资产显示参数明确化（2026-10-10）

- `EconomyUiTexts.FixedAssetStatusForDisplay`移除4处从中文句子贪婪拆名称的正则，改用`FixedAssetDisplayContext`捕获值进行完整ordinal匹配和资源变量投影；支持名称含原句分隔词/换行，不靠猜测分割归属。内部唯一HUD消费者改传context；未知原文仍透传。
- `RewardSystemBehavior.TryApplySettlementTransferEntryAction`在调用任何转移函数前捕获资产名/给付者名，透传经`TryApplySettlementTransferAction`到HUD；捕获失败不阻断授权动作。两层原有私有重载保留供外部入口/回放复用，mutationObservation仍传递；原状态与facts不改。
- `FixedAssetDisplayContextContractTests.py`156静态/模拟检查PASS；对Git `e732a3ab3`逐段验证Settlement/Workshop/Caravan三个实际转移方法与全部giver/receiverFacts.Add语句不变。既有经济词表245、债务34、图库30及28资源对PASS。真实转移、编译压力/回放、游戏仍NOT RUN。未将此项当作所有未知经济状态已翻译。

## 后续分工与持续 Jev 决策（2026-10-10）

- 用户要求再分小包给B；可直接转发`artifacts/localization/plan-B2-weekly-date-display.txt`，限定Weekly日期显示投影、已知格式与未知原文保留。当前只是任务卡已备好，未冒称已向外部agent发送或其已开始；主线避开此包的Weekly/AF.UI资源写入。
- 用户再次显式调用typesafe-ai并要求持续联动做决策。本会话后续按有证据的具体问题持续调用Jev；不是无限循环/无限API预算，也不把Jev置信度当编译验收。
- `work/localization-decisions-20261010-02`：live docs与网关OpenAPI经curl读取（web读取不可用）；先offline PASS，再一次4个Choice真实请求，model=jev、underlying_version_pinned=false。结果：B追加日期显示0.99；只接受已证明日期格式1.00；演讲语言采用显式主线程快照1.00；主线优先修英文接收/兜底1.00。3580输入/234输出tokens，返回cost=0.002148（币种未推断），needs_review=false。
- 新发现：BattleSpeechFrameworkV2.NormalizeNpcSpeechReply及听众解析有ContainsChinese门槛；听众英文池的FitFallbackAudienceReply仍附加中文填充和中文标点。下一主线处理显式语言传递与英文正文/兜底，保留动作白名单、内容安全和政治归属检查。

## 阵前演讲显式语言链路（2026-10-10）

- 在`BattleSpeechMissionBehavior`创建会话时捕获当前语言，传入reply claim、不可变prompt snapshot及计划分类请求。三个NPC提示词入口、联合/交战协议解析、正文归一化、政治归属检查、听众回应与兜底均接收同一语言；工作线程不查询游戏语言。旧Core方法签名保留中文兼容入口，旧听众池入口保留原按文本识别方式；真实运行时使用显式参数。
- 新增`afscene_speech_strings`100对键：85段原提示词片段、8个战场类型/阶段标签、6篇兜底演讲、听众/后缀/填充池及其他兜底。`BattleSpeechText`按语言只读一次嵌入资源并缓存列表。CN原片段与既有CN/EN听众池保持；English提示词要求English，英文兜底不再补中文填充/标点，未知外来事实标签继续保留。
- 英文正文/REPLIES不再被无汉字门槛直接拒绝；保留旧中文模式。新增英文元话语、对玩家称呼、纯控制协议检测；听众不能包含方括号标签。政治归属检查增加已知英文口号，`For <enemy>!`须出现在分句开头，避免把`Watch for <enemy>!`当效忠。未改行动/战术白名单、MCM执行门禁或任务代次判断。
- 验证：`BattleSpeechBilingualContractTests.py`131静态/模拟、场景目录494、词表110、资源29对PASS；既有1.3/1.4各400次演讲资源解析PASS。迁移脚本核对85段中文/原兜底池完全一致；相对`08aa81d3c`，动作/战术解析前缀与五组安全方法不变。CN资源SHA256 `2d562e62ff0b05a30a28ce38b1cdb4327378a5ab4bce81f5d2d2386bc548cee6`。Core测试新增中英归一化、协议拒绝、第三人称称呼、敌我口号、显式英文长填充、完整提示词/联合输出用例，但未编译运行。
- Jev持续复核：`work/localization-decisions-20261010-03`先offline后一次8个Choice，全部preserved，confidence 0.88–1.00；2755输入/374输出，返回cost=0.001653，底层版本未固定。此为译文语义辅助，不是编译、LLM真实生成或游戏验收。
- 全量新扫描（`08aa81d3c`含本片工作树）1829源文件、19600中文位置、585含中文文件、13794去重值，无扫描中变化。全模组任务仍ACTIVE，识别语料/其他模块Prompt、日志、事实与剩余显示仍待处理；编译/压力回放/真实LLM/实机/正式包全部未获本片验证。

## 终端分类路由资源化（2026-10-10）

- `TerminalSettingsRegistry.cs`259设置定义的6类分类值、All筛选值，及`AnimusForgeTerminalUiModels.cs`的8个页签/筛选/折叠路由，统一读取`TerminalCategories.json`中保持原字节的LegacyValue。原显示资源键及英文fallback一起登记；`TerminalCategories`只在静态初始化读取嵌入资源，路由走缓存字段，显示标签走8项ordinal字典。没有翻译内部身份或改默认页签。
- 另发现并修正RerankerDeviceDropdown为空时的中文CPU默认显示，复用`AF_MCM_Option_DeviceCpu`。除此显示补漏，两份消费者反向展开后与`192390cbb`完全一致（字符串转义标准化）；设置ID、getter/setter/invoker、范围、排序、状态、页签顺序保持。
- `TerminalCategoryContractTests.py`21检查、TerminalSettingsFallback2552检查/259定义、TerminalTagDisplay原全文件逆向断言与资源29对PASS。旧Tag测试新增精确逆向展开分类语料步骤，未删去原输入/生命周期/搜索/路由断言。注册表当前中文字面量词法扫描为0；不是全终端或全模组清零。当前C#编译、MCM/终端实机、正式包未跑。
- 发行门禁检查确认：`package_localization.ps1`仍先拒绝源码中文，再拒绝所有含中文嵌入资源。这会同时阻断有效双语表和兼容识别语料；本片未改放行规则，未来需具名资源身份/内容与用途验证，不能按目录整体豁免或取消DLL用户字符串检查。

## C 日期显示小包接收（2026-10-11）

- 用户明确纠正日期包实际交付者为C；覆盖前文任务卡B/B2的归属称谓，历史文件名保留。
- 基线 adcb385fc：核对共享 WeeklyReportArchivePolicy.ProjectDateForDisplay 的35行新增及三处消费者，接收既有工作树实现，不重做或回滚 helper。快报 masthead/meta 与周报列表投影已知日序及完整范围；阅读页复用 DateText。仅展示时解析日期，不新增 Tick 扫描、IO 或游戏对象读取。
- 原始 CreatedDate/CreatedDay、导入和归档生成算法不变；第0日有效。未知、自定义、混合正文保留；完整原生日历、外交年季日期及任意 Policy 日期仍未覆盖。中文历史格式识别字面量仍在 helper，本包完成展示投影，其源码语料外置仍待处理，不代表全模组完成。
- 去除测试中重复加入的同组日期模拟，保留全部不同断言（含 CreatedDay 排序）。重跑 WeeklyProjectionLocalizationContractTests：34键、19日期案例、来源/消费者边界及原始诊断 PASS；LocalizationResourceContractTests：29对 PASS；git diff --check PASS。
- 未新增语言键/注册，未做产品编译、DLL加载、Stage、游戏或旧档验收。接收范围仅 helper、两个消费者文件、契约测试及此记录；其他作者 dirty 保留。回滚使用本次精确提交的 focused inverse。

## 经济显示匹配语料与 C 后续计划（2026-10-11）

- PlayerRpForgeDisplayTexts.cs 82处、EconomyRecruitmentDetailTexts.cs 11处中文运行时字面量迁到 EconomyDisplaySources.json（92个不同值）；表内原文、正则、异常前缀逐值不变。EconomyDisplaySourceTexts 在首次使用时只读一次嵌入资源，静态表/既有 Regex 生命周期不变，少量方法内匹配改为缓存字典取值。无 Tick IO、游戏对象读取或语言查询。
- 新增资源由 AnimusForge.csproj 与 content-map 注册，不新增语言对。所有 AF_ECO_UI 键与英文fallback保持；未知原文、空值、多行和业务生产者不变。此为兼容输入外置，不能当作新增显示翻译或经济英文全覆盖。现有纯英文包门禁仍不区分具名双语/兼容资源，本片不放宽门禁。
- EconomyDisplaySourceContractTests 对 ebe438790 两文件完整反向源码还原通过，93引用/92值全部消费、资源唯一注册通过。既有经济词表245、固定资产156、债务34、图库30及29对资源契约 PASS，git diff --check PASS。原压力测试的显示键和入口断言核对不受影响，但C#压力套件、产品1.3/1.4编译、DLL加载、游戏未运行。
- 最新全量快照 artifacts/localization/runtime-literals-20261011-current.json：1831文件、19215中文位置、582含中文文件、13766去重值，无 problems/changedDuringScan。修订标记 ebe438790 含本片工作树；不能与历史扫描差值直接归功于本片，也不能当作漏译率。
- 用户要求外部 C 计划，已生成 artifacts/localization/plan-C3-issue-localization.txt：IssueRuntimePromptOwner 的任务上下文及 IssueDisplayTexts 的显示识别；保护标签、候选/拒绝规则、原始事实与未知文本。共享注册由主线集成。仅提供可转发任务卡，未向未知外部聊天发送消息，也未声称 C 开工。
- 持续 Jev：work/localization-decisions-20261011-01 先offline后一次2 Choice，636输入/93输出tokens，返回cost 0.0003816，底层版本未固定。原始事实保留/边界投影 project=1.00；scope返回 insufficient、confidence 0.36、needs_review=true，不当作通过。主线按真实 IssueRuntimePromptOwner 的任务状态、同伴白名单及正文标签禁令确认可划分，任务卡要求开工先查消费者与线程；未让低置信判断替代人工范围审查。
- 本片回滚通过精确提交的 focused inverse，其他作者的 Policy、人物JSON、HANDOFF dirty 不纳入。全模组目标仍 ACTIVE。

## 外交提示词生成器整文件双语接线（2026-10-11）

- 用户开启持续目标，并要求更激进、避免防御性编程。此轮从整份 WorldDiplomacyPromptComposer 进入实际英文提示词覆盖，不再只搬兼容语料。基线8adc66d61，237处文本去重为212对AF_DIP_Prompt_Composer资源，加入既有afdip_ui_strings两侧；不新建语言对、加载器或注册框架。
- IWorldDiplomacyPromptWorld新增LocalizePrompt叶子方法，由WorldDiplomacyBehavior.PromptContext的同步PromptWorld复用DiplomacyUiLocalization解析。生成器保留条件、顺序、世界状态查询、ID/标签、原案绑定、冷却/玩家例外、和平及威慑约束，不读取游戏本地化对象。AppendPlayerContext仅补传现有world。解析频率是请求组装，不在Tick中增加全量扫描/IO。
- 实际DLL发现JSON花括号直接进入TextObject会使整个提示词为空；变量注入花括号也会再次被解析。最终对3条含JSON/条款模板使用AF_JSON_OPEN/CLOSE文本标记，在本地化完成之后还原字面括号。没有异常吞掉/通用兜底。两版真实DLL均验证原JSON示例完整保留。
- DiplomacyPromptComposerLocalizationContractTests：237调用/212键、两侧协议token与数值、英文fallback、中文整文件反向还原、端口接线与JSON解析PASS。只剩BuildFallbackAnalysisJson中的原始分析失败原因没有迁移，该值进入结果数据，未当显示提示词翻译。
- LocalizationResourceContractTests PASS29对；PackageDLocalizationContractTests PASS（diplomacy1273、kingdom114，13处既有fallback漂移原样保留）；既有1.3/1.4 Localization DLL各5092次EN/CN/French/切回解析和6次真实字面JSON括号解析PASS，收据脚本work/verify_diplomacy_prompt_braces.ps1。git diff --check PASS。
- C# PromptWorldFixture实现新接口并使用嵌入的权威中文资源保持原业务预言，新增英文参与国必选/候选限制及JSON示例断言，原中文测试未删除。该SmokeTests及产品1.3/1.4编译没有运行；实际资源DLL测试不等于新C#已加载。没有部署/打包/推送。
- Jev work/localization-decisions-20261011-02：先offline后一次8项语义检查，均选择preserved但4项置信不足（37/99/134/137），needs_review=true，不能记作全通过；人工逐条复核条约角色、原案绑定、不可撤销既成效果及20/15威望扣除条件与原句一致。3029输入/359输出，返回cost0.0018174，底层版本未固定。
- 未覆盖：GetCommonDiplomacyContract、WorldDiplomacyPromptContractRules共享段、TextRules返回标签、政策/战争/亲属等外部事实片段、用户或存档公文正文；这些仍可能把中文带进最终请求。下一步继续处理这些真实上游，不以本文件237处完成声称整个外交链路全英文。回滚使用本片精确提交的focused inverse，C任务模块及其他作者dirty未触碰。
