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
