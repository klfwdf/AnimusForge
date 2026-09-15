# 当前连续收尾：真实 GameEnd 与待办退役（2026-09-15）

- 全范围任务仍ACTIVE；继续上一包807bc5b9之后的I1/C1生命周期，不停止于人设消费者完成。新包先绑定实际Game身份，拒绝旧GameEnd误伤新Game；结束/替换先推进generation，再分别退役主体owner及静态订阅。
- 已证实SubModule.OnGameEnd只有地图按钮移除/base调用。Native与Courier有结果等待的主线程动作在清队列后仍只能靠deadline结束；拟复用现有队列并增加待办退役登记，不新建第二队列。未claim立即结算为失效，已claim保留真实回执；GameEnd永久停止旧owner新提交，读档/Mission重置只结束旧待办。
- 只处理AF主体和AF侧接线，不清用户存档/素材或修改制作组业务。保持目录Ready=adapter已装配的现有语义，不把GameEnd改成整个模块卸载；下一Game仍走原注册路径。
- 验证实际生命周期装配/失败隔离/旧结束回调/注册和reset竞态、三渠道守卫及相邻回归，最终六Stage和元数据。GameEnd仍不能替代全部preprocess/lore/B1/SDK/实机验收。

- 执行扩展到最终副作用队列：Native action 与 Courier 最终 commit 一并登记退役；Courier 引用旧代码只有读 expired、没有原子 claim，已用真实旧声明复现重复回调会二次 commit，改为一次 claim，deadline/退役只处理未 claim，原最终会话/入站清理 owner 未复制。
- 补充 MyBehavior 退役准入竞态：退出先推进 generation 但旧 singleton 还在清理，worker 可能持新 generation 在清队列后发布新待办。实际 dispatcher + 实际退役方法对照复现，现于清理第一步关闭 owner 准入，兼顾尚未懒加载 dispatcher 的路径。
- 当前离线证据：生命周期36/12有效反例，Native/Courier退役接线15，My竞态7/1反例，Courier最终commit19/3反例，Native action91/5反例、最终记忆184、待录历史111、历史852+27、主体队列37、三渠道132、人设169、Courier历史122/后处理39/owner phase16、内部ports308/3反例和原Campaign装配42/5反例。旧Game回调/Native退役/Courier重复提交各有旧红；不把编译错误、工具超时或fixture缺失计入行为反例。
- 最终同候选六Stage/API119+快照32+4DLL728元数据、实际Courier Host回放均通过；My源writer238项也通过。最终交接只采用final日志与产物hash，先前中间失败/工具超时单独保留不计行为反例。整体CORE-CLOSEOUT-FULL-20260915仍ACTIVE，未完成规则/lore/剩余角色资产消息主线程化、B1真实规模预算、外部三渠道SDK、内部双向服务和当前候选LIVE/SAVE。

## 以下为前一联合包与历史记录

# 当前连续收尾：三渠道人设消费与信使准入（2026-09-15）

- 用户要求全范围继续直到完成，任务CORE-CLOSEOUT-FULL-20260915保持ACTIVE；本包从4140bd04/生产043b62b4继续，唯一写入AF-REFACTOR，三份保护文件不动。已完成的单次GitHub推送不自动扩展为每批发布；不恢复自动化、不覆盖游戏/操作存档。
- 本包覆盖同一人设责任的全部消费者：Native等待、Courier回信/来信等待、Scene逐候选读取/失败回退与包更新。主线程读取人物/档案/状态、后台等待生成、主线程重验并接受；Native绑定原admission，Scene绑定原Mission/session/epoch，Courier绑定session/participant/generation。
- 连带收口信使入口Session/收信人/发信人/失败替代信的主线程准入，删除无调用的Native旧等待方法。保留Native失败中止、Courier失败降级、Scene只在两个字段都空时生成与缺字段事实回退的渠道语义，不另起LLM或队列。
- 通过实际新源码与原主线程dispatcher的物理线程/异步生成/owner与目标替换/空状态/冷却/失败/退场测试，保留精确全文件逆变换和main共同语义，最后联合双版本/Bootstrap、API、内部ports、身份验证。未修的preprocess/lore/升格同伴/B1/三渠道SDK不记DONE。

- 联查扩展C1：原Courier owner phase在动作已claim后仍把取消/超时当成未执行失败，独立实际方法16断言旧4失败/新全绿；改为未claim可退役、已claim等待真实结果（不伪造回滚），档代失效仍拒绝结果。既有39项回归有1项旧断言要求已执行仍抛取消，已明确按新回执契约调整为返回真实结果且副作用恰好一次；其余断言/反例不删弱。
- 完成人设等待deadline覆盖正在await的生成，而不仅仅覆盖生成后的状态轮询；Scene等待持续核对场景/候选，失效可退出，不取消其他渠道可能仍需要的共享生成。deadline是协作式，主线程调度延迟和真实HTTP取消仍单列，不冒充强制抢占。

- 本包联合离线验证完成：三消费者/信使准入169项、10有效反例、6全文件逆变换守卫；原消费者98项65失败且三声明与main一致。Courier执行回执16项/3反例通过（旧16项4失败），既有后处理39项及8反例、历史122项、原Hero生成125项、Native准备589项/主线程132项/历史852项、渠道132项、内部ports308项/3反例、API119/快照32/4DLL700元数据、实际Courier Host回放、六Stage与main存档身份146/36全部通过。
- 移除无消费者Native旧等待/两个live名称getter，原三消费者逻辑由真实新partial接线；Shout与Courier大类合计净减259行。deadline修复覆盖pending生成任务，但不取消共享生产请求；Scene无新固定超时，只在scope/candidate失效时退出。保留preprocess/lore/角色资产提示等其他live读、升格同伴、完整GameEnd/B1/内外SDK/实机门槛，整体任务不标DONE。

## 以下为已完成请求包与历史记录；当前执行以上方为准

# 当前任务：已推送后开始全范围主体收尾（2026-09-15）

- 用户明确授权“推送GITHUB，然后开始全范围收尾”。任务 `CORE-CLOSEOUT-FULL-20260915` ACTIVE；唯一写入G:/AFMOD/AF-REFACTOR，起点10defeb4/生产af754ab6。三份保护文件不动，不部署、不操作存档、不恢复自动化。
- 发布已完成：fresh fetch确认main437925b8不变、专用远端f03557fb；6 ahead/0 behind，排除保护文件和生成物并验证源码/产物/证据后，普通快进推送到origin/codex/af-main-refactor-continuation-20260831，ls-remote核对10defeb4976f3ffa096a77e847fba254308f6aba。此回执只证明上述已验证候选发布，后续新代码另行标明本地状态。
- 执行范围沿用main矩阵与14职责计划：主体完整功能/清理、三渠道和生命周期、记忆实际预算、内部双向服务/外部三渠道SDK、最终同候选验收与交接；政策/宴会/GCCZ业务与参考资料HOLD不扩围。完整责任包实现→回归→删除替代代码→联合检查，不凭单helper PASS放行整项。
- 首个真实跨渠道缺口：正常Hero人设自动生成/编辑器重生在await前后直接访问Hero/档案；Courier/Native可从worker调用，共享入口还先读Campaign。拟将事实/配置捕获和档案提交归原MyBehavior主线程队列，网络/解析在后台；拆出生成预约/重试owner，防止同代重置后的旧finally清掉新请求和重生回包覆盖玩家中途编辑。
- 先复现正常/单字段/重生/VoiceId保持/失败冷却/重复/编辑/换档/owner替换，再实现；维持原Prompt/辅助Gateway/公开签名与保存类型。正常Hero生成、消费者状态读取与升格同伴生成要分别标覆盖，不能把前者完成冒充整个人设/全部Courier准备完成。
- 验证：真实新源码+主后台线程/原dispatcher测试，main共同语义与旧缺陷复现、严格MyBehavior整文件逆变换、已有历史/渠道/内部ports/API/六Stage/身份；核实旧默认消费者未断，不拿测试替身冒充实机。

- 首条共享请求路径已OFFLINE_VERIFIED（全范围任务仍ACTIVE）：Hero正常自动生成/原外部入口/编辑器重生在主线程捕获事实和启动异步辅助Gateway，解析后主线程提交/UI；独立NpcPersonaGenerationOwner持有预约/冷却，清理后的旧lease不能覆盖或释放新请求。重生期间文本被编辑时保留新文本并报告失败，最新VoiceId保留。大MyBehavior净减192行，旧3状态字段及旧生成体被真实替代；不是只拆partial文件。
- 同固定main四个旧执行声明精确相同，旧114断言40失败，新125断言全通过；7有效行为故障、6逆变换守卫、原B1 15守卫与dispatcher37、history852/Native27、渠道132、Courier后处理39/历史122、内部ports308/3故障、V1/API119/快照32/四DLL680元数据、实际Courier Host回放、六Stage和main身份146/36通过。
- 未覆盖仍明确：Native/Courier外围同步状态轮询、Scene准备外围读取、升格同伴人设/技能、规则/lore、GameEnd完整释放、B1真实成本、内部双向服务及三渠道SDK、实机/旧档。原Prompt文本/解析器和存档接口保持；复用MyBehavior原MemorySummary命名队列，不新增队列，但不把每帧2callback冒充单job硬预算。

- 首包生产/测试043b62b4；105点地图、docs/handoffs/2026-09-15-full-closeout-persona-handoff.md与候选审计JSON已绑定。上一候选10defeb4已推送，新候选仅本地。

- 追加自审闭环：968ca283在扩展125项断言中发现2项排队清理假成功，修复为明确失败；最终043b62b4共125断言/7有效反例、六Stage通过，原114断言40失败仅作旧红证据。只使用带final后缀的最终候选构建/元数据日志。

## 以下为历史记录；当前全范围执行以上方为准

# 当前续点：Courier 双向历史捕获边界（2026-09-15）

- 用户继续主体收尾；任务 `COURIER-HISTORY-CAPTURE-20260915` OFFLINE_VERIFIED（仅历史子责任；整体收尾ACTIVE），起点f77fe5e4/生产73774a94，唯一写入G:/AFMOD/AF-REFACTOR；main比较基线仍437925b8。外部Native/Scene/Courier全部必交，但当前版本化提交SDK未完成。
- 实际发现：reply/inbound的Prepare在Task.Run内，两个Build...RequestOnMainThread仍直接执行live历史读取；其中还混有人设和同步preprocess/lore网络，不能把整个builder搬主线程。本切片只完整迁移双向历史捕获/检索责任，明确其余准备仍待办。
- 实施：persona阶段之后，用原Courier owner phase在主线程捕获交付事实和既有MyBehavior.CaptureHistoryContextWorkForHero快照；后台执行冻结历史检索，再在原owner phase核验session/participant/generation，request builder消费明确的prepared结果（已准备空历史也不重新读取）。删除两个旧历史读取块，不新增队列/复制检索算法/公共API。
- 验证：双向实际新helper+原owner phase的物理主/后台线程、旧新输入与空值/失效/替换/故障；既有HistorySnapshot、Courier后处理、ChannelCutover、内部ports、API/六Stage/存档身份。受影响整文件parity使用精确逆变换，不刷新hash豁免额外差异。
- 保留：人设/规则/lore/其余消息构造live读取、Courier运输与资产/事实提交时机不在此包改写；不声称整个Q1或SDK完成。三份保护文件不动，自动化PAUSED，不推送/部署/操作存档。

- 本候选结果：新helper+原owner phase双向122断言/4有效行为故障，整文件逆变换4守卫；既有历史852/Native27、渠道132、Courier后处理39、内部ports308/3故障通过。两个旧同步公开Capture消费者保持签名/默认参数和主线程同步契约；初次构建遗漏参数已修复，最终六Stage与4DLL648项元数据通过，actual Courier Host replay通过。
- 同固定main存档身份146键/36行为保持。日志位于.tmp/courier-history-20260915；只使用stage-debug-final.log/stage-release.log为最终候选构建证据，初始失败单独保留。旧同步入口可能阻塞、首次历史快照为随历史规模增长的主线程复制，均未冒充性能或整个SDK验收。

- 生产/测试提交af754ab6；详细HANDOFF为docs/handoffs/2026-09-15-courier-history-capture-handoff.md，100点地图/审计JSON绑定同源码；只读API与整体收尾状态未冒充完成。

## 以下为历史记录；当前实施以上方为准

# 当前任务：对照 main 的主体收尾与双层接口（2026-09-15）

- 用户授权开始收尾：仅复现/拆净AF主体，政策/宴会/GCCZ等玩法不重构；内部契约稳定，外部子MOD明确要求Native/Scene/Courier三渠道都开放。任务 `CORE-CLOSEOUT-MAIN-20260915` ACTIVE，唯一写入G:/AFMOD/AF-REFACTOR，起点f03557fb/生产f07cb2a2。
- fresh fetch：origin/main固定437925b856fae76b4e9ee207e96ba048f35d5a67；重构远端f03557fb与本地0/0。按此main主体功能建立缺口/保留/迁移/证据表，不拿旧测试基线代替main，也不复制main已知缺陷来凑相等。
- 本切片优先C1共用请求生命周期：InteractionRequestCoordinator与main相同，直接Cancel/Dispose有旧取消回调打断新请求、停机不能遍历其余渠道、在途token提前释放的风险。先旧红复现，再将CTS所有权/取消与完成清理拆到内部lease；协调器保留原公开构造/Execute/Cancel/Dispose签名及渠道/session规则，不新建第二套调度器。
- 通过真实共享facade接线验证三渠道；契约和普通/失败/档代/重复/取消语义对照main。外部三渠道已纳入必交，但当前Api.V1只读仍是未完成状态，不在闭环前虚报Supported，不新增缩水LLM/动作/记忆链。
- Courier的background prepare仍含persona/history/preprocess/live读；这是单列待修缺口，不能整段搬主线程造成网络阻塞。本轮不冒称此缺口、B1深复制/预算或Campaign/Mission全部完成。
- 验证计划：main旧红/当前绿与共同基线、故障注入、既有pipeline/装配/API/存档身份/六Stage，稳定公开签名与只读/制作组契约；更新main功能矩阵/符号迁移与HANDOFF。三份保护文件保持，自动化PAUSED，不推送/部署/改默认，不删除仍有兼容/存档责任的类型。

- 首个C1基础切片已OFFLINE_VERIFIED（整体收尾仍ACTIVE）：对固定main运行30项共同用例通过，5类缺陷旧红；新51项通过、3类有效反例被行为拒绝。原CTS字典/直接释放已替换为独立内部lease，只有请求结束且取消回调结束才释放；取消异常隔离、已取消不启动生成，旧public签名保持。
- 实际验证：旧main编译消费者不重编译、换新核心程序集后Native/Scene/Courier调用通过；现有pipeline40/提交边界69/回执39/async18/匿名prompt13/Native失败4、Courier后处理39、内部ports308+3故障、API119/256并发/32快照/3故障、4DLL584元数据、最终源码六Stage及main身份146/36通过。NuGet漏洞数据离线获取有NU1900警告，不声称已完成包漏洞审计。
- 主体范围与三渠道外部必交矩阵已写docs/phase8/af-core-main-closeout-matrix-20260915.md；只读API没有被改成假Supported，政策/宴会/GCCZ业务未动，外部三渠道SDK、Courier线程准备、完整Campaign/Mission和B1预算/深复制仍未完成。测试与本轮修复不等于整个main主体已完全验收。

- 本轮生产73774a94；详细入口docs/handoffs/2026-09-15-main-closeout-lifetime-handoff.md，96点地图与审计JSON绑定该候选。仅本地，整体收尾仍ACTIVE，未推送/部署。

## 以下为历史记录；当前收尾以上方为准

# 当前任务：推送已验证重构交付（2026-09-15）

- 用户明确授权“推送到GITHUB”。任务 `GITHUB-DELIVERY-20260915` PUBLISHED；起点1345b0bc，生产f07cb2a2；目标仅origin/codex/af-main-refactor-continuation-20260831（klfwdf/AnimusForge），不触碰main/legacy远端、不强推/改历史。
- fresh fetch基线af618912，14 ahead/0 behind；已核实快进关系、60条变更路径、无新构建/临时产物，3份保护文件未改变且待推送历史不触及。本地专用Native HANDOFF不在分支树或整个祖先历史中。
- 同候选既有测试/六Stage证据复用，91点地图与差异空白检查通过，无源码改变不重跑构建。仅发送已提交代码/文档；自动化PAUSED、不部署游戏。已正常快进推送af618912→1345b0bc，ls-remote核对远端完整SHA=1345b0bce8c2f73de6a6dfe8a8d87330280de681；本次再提交当前交付说明，不改变生产源码。

## 以下为历史记录；当前交付以上方为准

# 当前任务：框架内外边界收口与对照链修复（2026-09-15）

- 用户要求“继续直到完美”；继续实际拆分与验证，不承诺零BUG，不自动推送/部署/恢复自动化。任务 `FRAMEWORK-SNAPSHOT-BOUNDARY-20260915` OFFLINE_VERIFIED；起点06a457c3，生产955a6be3，唯一写入G:/AFMOD/AF-REFACTOR，保留三份保护文件。
- 先处理上一轮完整port测试阻断：确认既有Memory source_parity可严格恢复到90201155，接入旧owner比较前；保留完整文件相等与证据哈希门禁，不能略过未审查差异或只刷新hash。
- 实际拆分：ModuleFrameworkRuntime不再引用Api.V1，持有内部生命周期状态并捕获不带游戏/活目录引用的只读目录快照；Api.V1侧独立投影为既有public DTO。唯一真实Directory/注册入口保留；所有旧枚举、原因码、顺序、Stopped不评估gate、公开表面保持。
- 生命周期核对：读档generation与Mission结束清理仍归原owner，注册不等于读档可用；本轮不假造统一GameEnd清理。公共投影拆分只处理模块目录快照作用域，不冒称Campaign/Mission生命周期完成。新增中间快照仅在显式API查询分配，小表有界；不引入Tick/轮询/反射。
- 验证：旧失败/新完整port回归和有效故障；源隔离、快照不可变/并发/旧新public输出、装配回归、六Stage与实际DLL元数据、存档身份、地图/HANDOFF。明确实机和B1原深复制/预算未完。

- 结果：新增纯内部ModuleFrameworkSnapshot/ModuleBindingSnapshot与内部生命周期枚举；Runtime删除Api.V1依赖/公开DTO构造/映射，净减19行；AfV1SnapshotProjection在API侧独立投影。Capture在原装配锁内，投影在锁外，仅按显式查询分配有界小表，无新任务/注册器/游戏引用。
- 历史port完整链已恢复：接入原B1严格逆变换，4个owner整文件+SubModule历史对照、13签名/31调用点、308断言和3有效故障全部通过，未放宽hash或跳过缺失方法。预期失败曾触发测试进程挂起/EXE占用，改测试Main受控非零退出并只清理核实路径下的失败测试进程；重跑通过，失败日志保留。
- 本候选：无API引用的CoreOnly真实编译、32快照边界/128并发、119公开API/256并发、3快照故障、42装配/5故障、15记忆逆变换守卫、六Stage、4DLL元数据584、SyncData146/行为36均通过。公开V1语义/存档/默认和制作组业务未改。
- 工程师差异审查通过：唯一Directory/原状态锁保持，跨停止/重载快照不可变，不在投影时二次求gate；玩家视角只做源码推演与可见API反馈对照，未进行游戏内实测。完整Campaign/Mission生命周期、主体其他职责迁移、B1深复制/预算、实机/旧档/live Economy/AFEF仍未完成，不标项目“完美”。

- 最终生产/测试f07cb2a2；新详细HANDOFF为docs/handoffs/2026-09-15-snapshot-boundary-handoff.md，91点地图与审计JSON绑定同源码。仅本地，未推送/部署。

## 以下为历史记录；当前实施以上方为准

# 当前任务：框架装配职责真实拆分（2026-09-15）

- 用户明确要求“编排好了吗，那开始拆分”。任务 `FRAMEWORK-COMPOSITION-EXTRACTION-20260915` OFFLINE_VERIFIED；起点812b34b0，生产基线61d57892；唯一写入G:/AFMOD/AF-REFACTOR。fresh fetch远端af618912，本地8 ahead/0 behind；不融合/推送。
- 先实现I1装配切片：SubModule原36个CampaignBehavior与4个模型包装注册移到专门装配owner，现有ModuleFrameworkRuntime作为唯一委托入口；制作组typed目录注册从runtime生命周期状态提取。只搬移装配，不搬移领域规则/存档类型，不新增注册器/队列或无消费者接口。
- 保持原顺序、每次Campaign回调新建实例、最后一个非AF模型作为inner/默认模型fallback、逐模型失败继续与行为注册异常传播；非Campaign/no-op。模型注册成功不等于读档完成，目录Ready语义不改，旧Campaign/Mission清理路径不伪造。
- 验证：固定旧源码抽取对照、当前真实装配方法+引擎stub执行、顺序/隔离/失败/重复调用/故障反例；既有API/并发/拒绝访问、六项Stage、存档身份与代码坐标。保留未覆盖的完整生命周期/三渠道业务/B1深复制工作。
- 修改范围：SubModule、Refactor/Modules装配类，相关源码级测试及文档。自动化PAUSED，不部署/操作存档/改默认/制作组玩法；两份用户草稿和指定本地Native简明版不动。

- 本切片结果：CampaignComposition实际承接36个行为、CampaignModelComposition承接4个包装模型、TeamModuleRegistration承接3组typed目录声明；SubModule净减148行，ModuleFrameworkRuntime净减25行。旧实现已从原位置移除，无第二套清单/注册器，公开接口/Saveable身份未变。
- 验证：装配42项+5类有效故障反例，原/新整文件逆变换与4个模型方法/注册顺序对照；API119+并发256+外部访问拒绝、4DLL元数据556、Debug/Release×1.3/1.4/Bootstrap六Stage、SyncData146/Behavior36保持。首次Debug因误移除仍被UI使用的PolicyEffects using失败，已恢复并重跑成功，失败日志保留。
- 已知阻断：历史TeamModulePortParityTests完整入口仍因61d57892就已缺失的ProcessMemorySummaryQueueAsync源码定位失败，未通过/未豁免；独立13签名/308真实port断言与组合后的历史SubModule逆变换通过。不把部分检查写成全仓合格。
- 生产/测试955a6be3；详细入口docs/handoffs/2026-09-15-composition-extraction-handoff.md已记录创建释放表与验证，代码地图86锚点绑定本候选；完整Campaign/Mission生命周期、公共投影进一步分离、三渠道业务拆分和B1深复制仍待办。本轮切片完成不等于阶段8或整个框架DONE。

## 以下为历史记录；当前实施以上方为准

# 当前优先级：先做整体框架编排蓝图（2026-09-15）

- 用户最新要求“先进行框架的编排”。任务 `FRAMEWORK-COMPOSITION-BLUEPRINT-20260915` COMPLETE（仅设计/文档，生产编排实现未完成）；本轮暂停继续深复制与细部业务拆分，先厘清装配根、作用域、模块依赖、启动/停止及对话执行编排。不是把B1验收跳过，也不等于已经实现完整Host。
- 源码基线61d57892，当前HEAD16af548b；唯一写入G:/AFMOD/AF-REFACTOR。范围为新增编排蓝图与现有计划/总HANDOFF的优先级链接，不改变C#、接口签名、游戏默认或存档，不创建空模块/第二注册器/第二套队列。
- 实际参照：SubModule的初始化/停止调用、ModuleFrameworkRuntime目录装配、TeamModuleServices三个typed桥、LegacyInteractionPipelineComposition与InteractionRequestCoordinator、MemorySummaryDispatcher及Host。记录已实现/待实现，避免把目录Ready解释成Campaign可接单。
- 验证：源码坐标/相对链接、编排与原职责计划一致、无生产diff、3个保护文件hash；文档轮不重跑无关构建。自动化仍PAUSED，未授权推送/部署或制作组玩法变化。

- 产出：`docs/architecture/af-core-composition-blueprint-20260915.md` 已区分装配Composition/对话Workflow、已有/目标、四层生命周期与单一owner、启动/停止、内部/public端口与后续实施出口。根HANDOFF/原P0–P6及14类职责清单均链接新优先级，未建立竞争台账或修改代码。
- 核查：当前C#仍61d57892；蓝图源码坐标/链接和3个保护文件hash检查；不重跑无关构建。后续先实例创建/释放表和现有装配入口演进，不把蓝图当可发布实现；仅本地提交。

## 以下为历史执行记录；当前优先级以上方为准

# 当前任务：M1/M2 捕获与接受调度职责提取（2026-09-15）

- 用户授权按职责计划开始实施，接口稳定、细致拆分。本轮任务 `B1-DISPATCH-OWNER-20260915` OFFLINE_VERIFIED（本切片交付完成，整体B1仍VERIFY）；唯一写入G:/AFMOD/AF-REFACTOR，分支codex/af-framework-skill-delivery-20260911；起点b7c90201，前生产9617f96a，fresh fetch远端af618912，本地3 ahead/0 behind，不融合/推送。
- 真实前置责任：MyBehavior.MemorySummaryMainThread目前持有捕获/接受共用队列、CAS待办状态、每tick额度/耗时和异常完成。先把它们提取为独立runtime owner + 窄internal host契约，MyBehavior仅留引擎身份/线程/设置/诊断适配和既有调用入口；迁移全部读到旧预算字段的规划调用，不新增第二套队列或兼容死字段。
- 原行为保持：同步与排队共用2操作/实际执行耗时预算，FIFO/档代和owner拒绝、reset退役未开始任务、部分完成异常准确抛回，不伪造网络取消/事务回滚。公开V1/制作组ports、存档DTO/键、Prompt/动作规则不变；纯runtime不引用游戏程序集。
- 范围：Refactor Contracts/Runtime新调度owner，原MemorySummaryMainThread适配与MemorySummaryPlanning预算读取；相关实际helper/captured/business/planning/writer/terminal/sealing测试接入新真实组件，源码守卫/地图/交接。验证旧新相同行为、真实故障控制、同候选六项Stage/API/存档身份。
- 此包是M1/M2的线程接受基础提取，不冒称首次整图capture/copy已分段，也不宣称全部14包完成。深复制/完整writer/原子尾步仍待下一包；B1未合格不进B2。自动化PAUSED，不部署、不操作存档、不改制作组业务。两份用户草稿与指定本地Native简明版保留。

- 当前结果：实际队列/claim-retire/预算/异常完成owner已移出，Host从156行降为57行；规划2处耗时读取改为新owner，未保留旧队列/计数器。原32项共同调度对照绿、当前37项绿、7个有效故障控制；captured116/business36/planning24/writers238/sealing88/terminal85/commit51和六Stage已通过，API/身份及最终材料收口中。
- 契约说明已写 `docs/architecture/af-memory-dispatch-contract.md`。本包只完成线程接受基础责任；首次capture/copy与深来源/完整writer仍待做，不能标M1/M2整体DONE。

- 最终候选61d57892：API119+并发256、4DLL元数据532、SyncData146/behaviors36保持；81点地图记录/工作树通过，214份冻结材料与六产物hash在新验收JSON。统一入口 `docs/handoffs/2026-09-15-memory-dispatch-owner-handoff.md`，契约在 `docs/architecture/af-memory-dispatch-contract.md`。本轮仅本地提交，未推送/部署，自动化仍PAUSED。

## 以下为历史任务；当前实施以上方为准

# 当前任务：修复内层同数量变动，并细化收尾前职责拆分计划（2026-09-15）

- 用户已明确授权修复本轮已复现的覆盖问题并制定后续模块化计划。任务 `B1-INNER-STRUCTURE-20260915` OFFLINE_VERIFIED；本轮修复与规划交付COMPLETE（整体B1仍VERIFY）；单代理，唯一写入 G:/AFMOD/AF-REFACTOR，分支 codex/af-framework-skill-delivery-20260911，起点 af618912 / 生产4d6994bc。自动化仍PAUSED，不推送、部署、操作存档、切默认或开展未经本轮审查的大范围搬迁。
- 旧行为/根因：DailyMemoryDraftEntryNormalization 在跨窗口时只看内层列表引用/count，64→64替换、删补、换位漏失效，最终发布旧_lineResult。上一检查真实抽取封存调用已复现2个对照绿、3种同数量变化红；不是实机症状归因。
- 意图：在现有内层游标上绑定实际List结构版本（含lines/trigger bind的相邻边界），O(1)校验，变化走既有失效重封；保留同步规则、单权威owner和实际预算，不重写净化规则、不引入无消费者接口。预计改MemorySealing、sealing runner/harness与精确源守卫/地图；按影响面验旧红/新绿/故障反例、相邻回归和原六项Stage。
- 规划：沿原P0–P6/B1–B3细化高内聚owner/typed端口/调用者迁移/旧符号删除/最终验收，不把partial、空接口或行数减少当拆分完成。补充可审查的职责包及迁移表模板，区分本轮实际修复与未来实施，不改制作组玩法或public范围决定。
- 兼容/保护：无存档字段/类型、Prompt/API/玩法/默认/原构建脚本变更；两份用户草稿与指定本地Native简明版不改不暂存。代码保持英文，说明/提示词可中文；每阶段在同候选证据和回滚点齐套后才放行，不承诺零Bug。

- 本轮修复结果：内层line/trigger的List结构版本探针已接入，移除两份post-Done无用line发布状态；88/0，旧4d同88例80/8，三个有效故障控制与captured116/business36/terminal85、13项精确源守卫通过。原六项Stage已通过；首次构建选到系统runtime-only dotnet，临时PATH切已有G盘SDK后成功，原失败日志保留，不改脚本。
- 规划结果：收尾前职责拆分设计已写入 `docs/phase8/af-core-responsibility-decomposition-plan-20260915.md`，沿原P0–P6而非另起阶段；当前仍B1未整批合格。新计划不等于这些职责已迁移，自动化继续PAUSED，本轮不推送。

- 最终候选：9617f96a；本轮API119/并发256、4DLL元数据532、146 SyncData键/36行为保持，六DLL/marker/Stage比对通过；78点导航绑定当前修复。统一交接见 `docs/handoffs/2026-09-15-inner-structure-fix-and-modularization-handoff.md`，原计划第18节链接新的职责拆分设计。无后台任务在继续实施，自动化保持PAUSED；代码/文档均仅本地提交。

## 以下为历史记录；当前授权及状态以上方为准

# 当前自动实施：B1 深 line/trigger 预算（2026-09-14）

- 本切片离线联验完成，整体B1继续VERIFY；单代理；写入本 Codex worktree（detached HEAD `4d6994bc`），检查点`eb6389f4`，前生产`86805518`。指定远端仍`origin/codex/af-main-refactor-continuation-20260831`。按第16节接续，不重做owner记录额度/typed raw/排序/共享窗口/索引，不进入B2。
- 意图：把单draft内1024行净化与weekly trigger bind改为共享metadata计费；draft身份仍一次expensive。抽出`BindDailyMemoryDraftWeeklyTrigger`/`SanitizeDailyMemoryDraftLine`供同步Sanitize与续跑共用。未完成draft的line/trigger列表保持私有，列表引用/count变化失效重封。
- 并发/语义：line/bind可跨窗口提前可见；trigger列表`SanitizeWeeklyMemoryMaterialTriggers`仍一次原子。不是整draft事务。同步oracle仍是40b92e67原`SanitizeDailyMemoryDrafts`。
- 预计路径：MyBehavior.cs helpers、MemorySealing entry续跑、sealing harness/runner/parity/审查表、代码图与交接。无Prompt/玩法/存档字段/API/默认或原构建脚本变化。
- 验证：当前76/0；旧40b同76例61/15；`unbudgeted-line-normalize`与`ignore-line-source` BUILD_PASS后断言红；12项源守卫；代码图77锚点记录提交与工作树通过。本轮未重跑captured/六Stage/API/存档身份；LIVE/SAVE=NOT_RUN。
- 剩余限制：trigger列表sanitize、首次capture/复制、全owner/raw/最终绑定、Apply仍未硬切分。不用删除数据或只改数字宣告B1完成，不自动推送/部署或操作存档。

- 结果：生产/测试`4d6994bc`；1×1024行9窗、max_lines=127；owner 257/65记录额度不变。下一步直接处理首次capture/copy。自动化保持PAUSED。

## 以下为历史暂停与实施记录；最新续点以上方为准

# 当前状态：用户暂停自动化，整体审查与 GitHub 交接（2026-09-14）

- **生产开发 PAUSED；阶段 8 / B1 未整批验收，不是 DONE。** 最新用户要求关闭自动化、说明整体进度与拆分、上传代码和详细 HANDOFF、保留本地简明版。本入口覆盖下方历史 ACTIVE / 自动继续安排。
- 调度已通过应用工具把 `af-7-8` 改为 PAUSED，并读回确认；旧 `af` 也为 PAUSED。没有恢复、另建自动任务或继续生产改动。
- 交付任务 `PAUSE-DELIVERY-20260914`：COMPLETE（仅暂停/审查/文档/GitHub交付；生产重构仍PAUSED且B1未合格）；执行者为本任务单代理，唯一写入区 G:/AFMOD/AF-REFACTOR。起始 HEAD `8f0e3ab8`，最后已离线验证生产/测试 `86805518`；指定远端 `origin/codex/af-main-refactor-continuation-20260831`，fresh fetch `3f00fefa`，本地 ahead 17 / behind 0。
- 意图范围：核对原始 AF 与当前大类家族/真实接口/剩余工作；更新根 HANDOFF、原 P0–P6 计划最新暂停入口、详细交接及审计 JSON；本地简明版放 `.tmp/` 不上传。既有两个用户草稿和指定 Native 本地简明版不改不暂存。
- 风险与验证：无 C#/Prompt/玩法/存档/默认/构建脚本变更；复用同源码86805518的已执行证据，重新核对源码、74点地图、冻结日志与产物 hash、文档坐标/链接和 outgoing 历史。推送前再fetch检查祖先关系/排除文件，普通快进，远端ref核验；若分叉停止，不融合或强推。
- 本轮不部署、不读写真实存档、不删旧实现、不改制作组业务；B1深来源/原子尾步、B2 Courier/完整三渠道、B3生命周期及public选择/最终实机门槛仍保留。后续实施须用户明确恢复。

- 本轮文档成果：新整体暂停 HANDOFF、原计划第 17 节、根当前入口、整体审计 JSON；本地主体简明版 `.tmp/AF主体简明HANDOFF-20260914.md` 不提交。源码/测试仍86805518，结构复核52个Refactor文件、20个owner额外partial、三大家族115983行；B1未整批合格，不能宣称拆完或只剩实机。
- 发布门禁：74点地图双模式、当前源码和既有六项产物/183份冻结材料hash、3个保护文件、文档链接/坐标与outgoing排除检查；这些是本轮重新核查，不冒称全量业务或游戏重跑。Git引用核验后普通推送，实际结果由工具及本地push-receipt记录；生产开发保持PAUSED。

- 发布结果：已普通推送 `3f00fefa → dcc17ee70832e2c63725bf08b0a284f9a94429d3`，19提交/41差异文件，git ls-remote核实一致。本条为随后的文档回执，不更改生产86805518；全局重构不标DONE，后续开发仍须用户明确恢复。

## 以下为历史实施记录；最新暂停状态以上方为准

# 当前自动实施：B1 owner草稿净化记录预算（2026-09-14）

- 本切片离线联验完成，整体B1继续VERIFY；单代理、唯一写入G:/AFMOD/AF-REFACTOR；当前生产/测试86805518，前生产40b92e67，fresh fetch远端3f00fefa未变。按第16节接续，不重做raw摘要/排序/共享窗口，不进入B2。
- 意图：把封存末尾单owner整个草稿列表净化改为按实际draft逐条授予预算，并复用稳定排序组件。原同步Sanitize入口保留；单entry净化体原样提取（主线程原地、后台clone、先占key再判断empty、标签/AFEF/marker规则不改）。
- 并发/语义：仅规范化元数据可分记录提前可见；源列表的删除/去重/排序结果完成后才发布。持续检查owner列表引用/结构，发布前校验每条key/日期以及空winner是否长出新lines，拒绝过时删除；失效重走原owner封存与索引。保留原引用、别名副作用顺序、同日稳定顺序，不能把整owner改成假事务。
- 预计路径：MyBehavior.cs的Sanitize与单entry helper、MemorySealing状态与真实caller、captured/sealing/terminal等源提取适配、源码精确inverse/地图与交接。无Prompt/玩法/存档字段/API/默认或原构建脚本变化。
- 验证：真实40b旧实现大owner预算反例；主线程与后台净化原行为oracle、empty-first去重/别名/大小序、追加/替换/同slot/修改key/新lines/同步排空/异常和Campaign累计授予；保留既有60/116等语义，对应反例/相邻与最终六Stage/API/存档身份。
- 剩余限制：一个draft内的lines/trigger文本净化仍原子；key/empty guard、单字符串/全owner绑定和Apply也未硬切分。不用删除数据或只改数字宣告B1完成，不自动推送/部署或操作存档。

- 结果：75/0、旧40b同75例62/13、27有效故障反例、12项精确源守卫与相邻回归/最终六项Stage/API/存档身份通过。257与65记录owner由一次全净化变每窗口最多8；单draft1024行仍原子，深line/初捕获/全owner绑定/Apply继续待做，不进入B2。

## 以下为上一已验证切片（历史）

# 当前自动实施：B1 完整raw来源摘要成本（2026-09-14）

- 本切片离线验证完成，整体B1继续VERIFY；单代理，唯一写入G:/AFMOD/AF-REFACTOR，当前生产/测试40b92e67，前生产8bcde78b；fresh fetch远端3f00fefa未变。按计划第16节继续，不重做排序，不进入B2。
- 意图：完整raw来源hash由JSON序列化改为显式字段/列表有界buffer编码，去掉反射装箱/属性名和JSON转义的重复成本；保留所有字段、列表顺序/null元素、null/empty/absent区别。不用不完整writer epoch替代原数据校验，不改变Prompt或权威提交。
- 预计路径：MyBehavior.MemorySummaryInput及私有DTO编码边界、Refactor/Runtime纯摘要组件，captured/terminal/sealing直接测试适配、原字段反例与严格inverse/地图/交接。原通用ComputeMemorySummaryFingerprint（计划/编辑器/上下文）继续原JSON契约；只有瞬时来源指纹格式改变，存档/API/模型字段身份不变。
- 风险与验收：字段遗漏、长度/类型分帧冲突、Unicode、state presence、源/owner/generation/重试/接受要有正反例；原8bc真实执行成本对照，不以常量或砍字段伪造提速。未来DTO新字段由反射字段覆盖测试阻止漏编入；摘要仍完整原子O(N)，不当作深记录硬预算。
- 验证：新旧同数据/全字段变更/明确故障反例、实际capture→execute→final check及相邻commit/封存/规划/UI/history/native、现有六项Stage/API/持久化身份。只本地提交，不推送/部署/碰存档/改原一键脚本或制作组玩法。

- 结果：116/0、旧8bc同套112/4、35有效故障反例、294递归字段修改、9向量/5守卫、相邻回归、11项精确inverse以及最终六项Stage/API/存档身份通过。1000记录×12摘要分配约降96–98%；仍为完整原子O(N)，不进入B2。下一步继续初捕获/净化/owner绑定/Apply预算，不重复本轮typed raw摘要优化。

## 以下为上一已验证切片（历史）

# 当前自动实施：B1 队列原子排序（2026-09-14）

- 本切片离线联验完成，整体B1仍VERIFY；单代理，唯一写入 G:/AFMOD/AF-REFACTOR；当前生产/测试8bcde78b，前生产9158132c；fresh fetch远端3f00fefa，无新变化。
- 意图：将Daily/Major封存尾部的稳定排序提取到有真实消费者的可续跑纯运行时组件，复用Campaign预算；同步调用、先pending再去重、原地净化、引用身份、文化排序及同键稳定次序保持。正常净化仍主线程原子执行，允许净化元数据先于排序发布，不启动第二条总结链。
- 路径：MyBehavior.cs/MemorySealing、Refactor/Runtime排序组件、直接sealing/business等测试适配及精确inverse/地图/交接。存档DTO不搬迁，Prompt/玩法/public/默认/原构建脚本不改。
- 风险与验收：不得发布跨tick过期列表/元数据/culture；排序每次实际比较/移动计费、同步可排空；旧915真实执行红例、变异与原40场景、相邻回归和六项Stage/API/存档身份。数组分配/净化/键捕获/最终标量绑定仍原子，不用本切片宣称B1硬预算完成。真实游戏/存档未运行，不部署/推送。
- 验证完成：封存60/0，旧915的46/14，20有效故障反例；相邻24/36/109/238/85/51/54/23、UI/history/native/channel、10项strict inverse与最终六项Stage/API/存档身份通过。规划extractor曾缺2个真实helper（编译失败非红例），已补齐并复跑。下一续点是剩余初捕获/raw/净化/绑定/Apply原子成本，不进入B2；本轮详见最终排序HANDOFF。

## 以下为上一已验证切片（历史）

# 当前自动实施：B1 Campaign维护共享预算（2026-09-14）

- 本轮共享Campaign窗口与延迟启动修复已完成影响面离线验证，整体B1保持VERIFY；本轮生产/测试9158132c，当前主代理单独实施，起点2fc32b6d、前生产73a6977c，检查点01e24b9e。启动fetch远端3f00fefa，无新协作覆盖。
- 目的：把OnCampaignTick内主维护与deferred维护接到同一有限时间窗口，封存的metadata/expensive授予跨多次调用累计；抽出真实独立预算组件，保持显式同步/无限预算语义。EngineTick摘要回调仍是独立既有窗口，不冒称全游戏帧预算。
- 关联正确性：窗口耗尽时已完成封存的summary启动意图须留到下一窗口，不能同日丢失；绑定save generation，退场/异常不泄漏旧窗口。保留正常、空/终止任务、重复调用、异常/耗时/同步/读档等对照。
- 预计路径：MyBehavior.cs、MemorySealing/新的预算接缝与Refactor运行时组件、直接business/sealing tests、精确source inverse/定位图及交接。不得改Prompt、provider/存档身份、制作组玩法、默认/public入口或原一键脚本。
- 验证：先用原73a6977c真实Campaign维护段证明重复授予/重复deadline和迟到启动缺口，再实现并复测真实链与故障反例；保留已有30/23等语义用例；相邻回归、六项Stage、API/持久化身份，按最终源码绑定。源码范围/已验/未验见[本轮交接](handoffs/2026-09-14-b1-campaign-budget-handoff.md)。
- 自动化继续ACTIVE；下一续点为剩余深来源与原子尾步，不重做本轮已共享的Campaign窗口。40场景、14反例、旧版35/5和相邻回归已通过；最终六项Stage通过，接口/身份检查及最终交接落盘。未推送/部署/操作真实存档；用户草稿原hash保持。

---

# 每小时自动执行入口（2026-09-14）

用户已授权完善计划并恢复自动推进。现有 `af-7-8` 已经应用工具更新并读回确认 **ACTIVE，每小时一次**；沿用当前任务和原调度，不另建任务。当前入口为[根HANDOFF](../HANDOFF.md) → [原计划第16节](phase8/af-core-precloseout-plan-20260913.md)。

- 本次配置完成：只改计划/当前入口/自动化提示词，没有新增生产修改；配置起点55af6d3e、意图检查点e70ef0cc，生产仍73a6977c。
- 后续自动实施范围：从B1深来源/原子预算与真实主体职责提取继续，整批验收后才进B2/B3；不重做已完成9项集成和素材索引。不把本次“配置完成”当作生产阶段完成。
- 双Skill、单代理、同DLL内外接口/制作组业务边界保持；不自动推送、部署、操作真实存档、切默认、融合分叉或广泛删旧。
- 完成获准可做工作或仅余外部阻塞时，写技术/制作组两份HANDOFF并通过应用工具暂停。未来用户暂停/停止指令立即优先。
- 已验证：计划链接/续点、受保护草稿hash、源码未变、工具ACTIVE回执及提示词/原频率/目标任务读回一致。下方旧暂停/本机手动配置状态保留为历史，不覆盖当前自动执行入口。

---

# 当前实施入口（2026-09-14：B1 集成与主体职责提取）

**用户已明确恢复主体重构。本机手动执行，自动化保持暂停。** 唯一当前状态为 [根 HANDOFF](../HANDOFF.md) → [本轮交接](handoffs/2026-09-14-b1-index-owner-integration-handoff.md)，本段替代下面旧审查/暂停入口，不改变历史证据。

- 基线 `3f00fefa`，生产 WIP `c21523f8`；工作区 `G:/AFMOD/AF-REFACTOR`，本地分支 `codex/af-framework-skill-delivery-20260911`。远端 fresh fetch 0/0。
- 执行者：当前主代理，单代理实施。现有 B1 / P1 本轮范围 VERIFY：生产73a6977c已闭合9个原未审符号的精确inverse/证据并提取素材索引独立运行时职责，保持原 AF 的键/重复记录/副作用和存档语义。
- 范围：主体源文件、直接运行时组件、相关测试/代码图与交接；政策/宴会/GCCZ 业务、public 写 API、默认入口、构建覆盖脚本不改。不部署、碰真实存档、推送或恢复自动化。
- 验证：保留精确原行为/当前 WIP 对照和有效故障反例；相邻主业务/历史/Native、严格 inverse 与代码地图，最终同候选 Debug/Release×1.3/1.4/Bootstrap 项目内 Stage。未验证层不标 DONE。
- 原始 AF 功能起点 `d4cb1467`，B1 局部迁移参照 `62abfdb3` 与 `c21523f8`；批准修复与应保留语义分开。两份用户草稿和指定本地专用 Native 简明版 hash 保持。
- 状态：本轮素材索引职责提取与 WIP 精确集成已完成影响面离线联验；54 声明/2 删除/组件锁和 8 门禁测试通过，素材23/7反例、封存30/8反例、相邻回归、六项Stage、API/存档身份通过。B1整体保持VERIFY（深来源预算/实机等未完），阶段8未DONE；源码/证据/回滚在本轮最终HANDOFF固化，不进入B2。

---

# 当前审查入口（2026-09-13，状态核对与详细交接）

- 当前任务：核对原始 AF → 现有拆分、实际阶段、功能复现/缺口，编写详细 HANDOFF 和既有计划的收口修订，并按用户授权普通推送专门重构分支。
- 起点 HEAD `007dbeee`；生产现场 `c21523f8` 为暂停 WIP，最后完整离线联验生产 `62abfdb3`。本次审查/文档 ACTIVE，不代表开发或自动化恢复。
- 范围：只改交接/计划/审计记录；不改生产、测试实现、配置、默认入口或游戏文件。两份用户草稿、本地专用 Native 简明版不改、不暂存。
- 验证：Git/源码坐标与原始基线对照、聚焦渠道回归、原地图/当前地图与严格 inverse 门禁、文档链接和限定 diff、推送祖先/远端 ref 核验。下方阶段标题为历史，不代表当前全项目已完成。
- 执行状态：审查与文档已完成；当前入口为[详细 HANDOFF](handoffs/2026-09-13-af-stage-architecture-parity-detailed-handoff.md)，后续沿用[原计划第 15 节](phase8/af-core-precloseout-plan-20260913.md)。本轮渠道 132/0；旧图 53 点 PASS，工作树图/严格 inverse 确认未集成 WIP。生产未改，自动化 `af-7-8` 保持 PAUSED；GitHub 交付以远端核验回执为准。

---

# AnimusForge 重构执行清单

> **2026-09-13 当前状态入口：** [根 HANDOFF](../HANDOFF.md) → 当前收尾前计划及执行证据（当前同一B1来源/预算/writer/接受联合重构，未整批合格，不进B2）。下方旧“当前/最新/未开始/未推送”段落均保留为当时记录，不能覆盖新用户授权或当前 Git 状态；本轮不把历史门禁、旧失败或未完成项改写成 DONE。

> 本文件是 AF 重构的公共进度台账。它记录目标、阶段、当前状态、验证证据和交接信息；不替代 `.claude/skills/animusforge-maintainer/` 中的长期工作规范。

## 最新指令：暂停自动化并全面检测（2026-09-08）

- 用户明确要求关闭自动化；`af-7-8` 与旧 `af` 均已确认 PAUSED。下方自动接续段落为历史记录，不构成继续自动运行的授权。
- 检测锁定源码 `35524b04`，未修生产/正式测试、未推送或部署。确认 4 个 P1 与 2 个 P2 功能问题，另有 Bridge/目录不一致及未验证风险；构建通过不能将阶段八升级为 DONE。
- 完整报告：`G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-08-full-refactor-audit-35524b04.md`。后续修复按报告给用户确认，不由已暂停的自动化继续修改。

## 检测收尾交接（2026-09-08）

- 用户要求做结尾工作；仅归档检测与交接，不修生产、不恢复自动化、不推送或部署。源码验收仍绑定 `35524b04`，4 个 P1 与 2 个 P2 功能问题尚未修复。
- HANDOFF：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-08-audit-closeout-handoff.md`；制作组短文：`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-08-audit-closeout-team-brief.md`。
- 本地证据 ZIP 已通过完整性和逐文件 SHA-256 校验；索引见 `G:\AFMOD\AF-REFACTOR\docs\audits\2026-09-08-audit-evidence-manifest.json`。证据包不是可安装 MOD，未进入远端。

## 自动接续重启（2026-09-08）

- 用户已批准“设置自动化开改”；复用当前任务 heartbeat `af-7-8`，每30分钟，不创建重复自动化。唯一代码工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 已 fetch 并安全快进到共享重构分支 `aefa02ad15758222b87e4e240a85c52eb3f913d9`。两份2026-09-06 integrated-phase8-handoff/team-brief本地草稿原样保留、不暂存；不改其他工作区、游戏或真实存档。
- 任务 `P0-CUTOVER-20260908` VERIFY（代码/离线验证完成，实机未测）：先修Scene/Courier外层在Host终态失败后重新请求的缺口。owner为Conversation/Courier调用边界。计划路径：ShoutBehavior.cs、CourierDeliveryBehavior.cs、定向外层回归工具及本计划/HANDOFF；不改存档key/type、玩法数值、Bridge开关、默认选择或官方构建脚本。
- 验证：先对生产外层控制流作故障注入红测，修后回放成功/失败/空回复/回退/取消/异常，补既有Host契约与官方1.3/1.4/Bootstrap项目内Stage。真实LIVE/SAVE仍未在本轮执行。
- 当前实际Native已恢复完整旧入口；Scene/Courier接入不等于三渠道等价完成。13 wired与历史PASS不可提升为全领域实机通过。具体计划及20领域清单见 `docs/phase8/refactor-execution-plan-20260908.md`。
- 回滚基线 `aefa02ad`；本地意图提交后执行，后续按定向逆提交回滚，不reset/rebase/force-push。推送、部署、新默认切换及广泛删旧另待明确方案批准。
### P0-CUTOVER-20260908 首轮结果

- 意图提交 `bdeeadd8`，修复只改Scene/Courier现有外层控制流。Scene以Host接管状态替代空回复判断，终态失败break至现有收尾；Courier保留送达前预生成，失败先seal既有PostprocessConsumed/清残留文本再推进返程，保护已排队legacy完成和已完成回信。
- 从真实生产连续block抽取编译的44案例：基线 `aefa02ad` 19 PASS/25 FAIL，修后44 PASS/0 FAIL；提取器5 tests通过。Host/队列/网络依赖为stub，不冒充完整游戏状态机。
- Debug/Release × 1.3/1.4/Bootstrap六项官方构建各0 warning/0 error；Interaction 40+69+39+4通过，生产Configured/Detached/Courier Host回放通过；Bridge16/13/3、20自测、入口10自测通过。持久化校验确认远端已有3处导航行号过时，仅校正行号后142键/168绑定通过，无存档key/type变化。
- Interaction runner有NuGet漏洞元数据获取NU1900警告，测试正常完成；不修改源配置或关闭审计掩盖警告。
- 独立源码审查无本轮阻断；日志 `.tmp/cutover-20260908/`、`.tmp/channel-cutover-boundary/`。完整命令/哈希/未验证项见 `docs/handoffs/2026-09-08-cutover-terminal-safety-handoff.md`。
- 下一精确任务 `P0-SCENE-PARITY` TODO：从完整Scene现有前处理/消息/动态PostprocessRules到detached重新捕获链做差异回放，优先复用完整上下文，不重复计算或偷偷切Native默认。自动化保持ACTIVE；没有推送或部署。

### P0-SCENE-MAIN-PARITY-20260908 VERIFY（主请求交付离线通过）

- 基线 `92ad625a`；fetch后远端仍为aefa02ad，本地ahead3，无远端新提交。两份旧草稿不变。
- 初查确认默认Scene先调用BuildStrictSceneMessagesForNpc消费当前AFEF并构造完整role消息，detached随后重新捕获，主请求不再使用该完整messages；信任、当前事实、场景标签及历史顺序会丢失。后处理另有真实PostprocessRules/资格/归一化/relay时机缺口，不能混称本轮全部等价。
- 本轮意图仅闭环主请求保真：冻结已准备的messages并沿现有Scene端口交给Gateway；保留public工厂ABI与无prepared的opt-in行为，不复制另一套Prompt逻辑，不改Postprocess/Action/Memory/default/存档或GCCZ。后处理所需旧捕获暂留，不宣称已消除重复前处理。
- owner为Conversation.Scene/Prompt adapter。拟修改ShoutBehavior.cs及定向source-linked/外层回归与文档；验证同一角色/顺序/文本/事实/当前输入/5000token完整进入main请求、不可变性、无跨请求重用及非Scene路径不变；原44外层故障回归、相关Host契约与官方六项构建。
- 先做新旧实际factory/callsite反例，再修复并清理不再使用的默认主消息重组选择。本轮实机NOT_RUN，不推送/部署/切Native默认。

### Scene 主请求保真结果与下一项

- 意图提交 `8c30424e`；生产仅ShoutBehavior 21行差异：默认每轮将已经完整准备的messages冻结为PromptPackage传入同一Scene ports。原public二参数factory保留，委托private factory；无prepared时保持旧组合行为。main不再丢弃原AFEF/信任/role消息，也不再额外拼接snapshot输入。
- 实际CreateChatMessage是匿名{role,content}，LegacyPromptPackageAdapter只支持字典，不能拿它转换该真实请求；本轮复用已有LegacyConfiguredChatGateway.BuildPromptPackage，不新增反射/消息序列化实现、不改全局CreateChatMessage。
- 扩展原外层harness实际执行production factory/callsite/匿名converter：基线92ad625a 46 PASS/6 FAIL（原44全通过），修后52 PASS/0 FAIL；抽取7 tests通过。六项官方Debug/Release双API/Bootstrap各0warning/0error；Interaction与Configured/Detached/OptIn生产回放、142键/168绑定、Bridge16/13/3通过。NU1900仍为本机NuGet元数据网络警告，不隐藏。
- 验证仅证明已准备main消息交付保真；未执行BuildStrict场景构造、真实API或LIVE/SAVE。postprocess保持原delegate，复capture/正文规则冒充tag_rules/逐领域归一化/relay/firstTurn及NPC回复参数缺口仍在，不能标Scene全部等价或删旧。
- 下一精确任务 `P0-SCENE-POST-PREP` TODO：在原Queue调用时机下把TryRunSceneUnifiedActionPostprocess原实现分离成prepare/network/normalize，先保持旧入口可重放；禁止直接复用缺少Scene relay/summon/guide的Courier builder或提前以空回复生成post prompt。详见新HANDOFF。Native诊断CompareMainMessages对匿名消息的遗漏纳入P1，不依赖字典fixture的PASS直接迁移。
- 日志 `.tmp/scene-main-20260908/` 与 `.tmp/channel-cutover-boundary/prompt-*`，独立审查无本轮新增阻断。完整证据见 `docs/handoffs/2026-09-08-scene-main-prompt-fidelity-handoff.md`。自动化继续；未推送、未部署、未操作存档。

### SCENE-POSTPROCESS-MILESTONE-20260908 VERIFY（Scene 定向离线通过，全局门禁仍有失败）

- 用户明确授权 Scene 默认改为仅生成正文，再由完整后处理统一执行动作/接力和原听众记忆 owner；不切 Native、不改开关、不部署游戏。原基线 `d40808b3`、意图 `17151d6b`。
- 完成同类型 partial 的完整 prepare/network/complete 拆分并接回默认流程；保留原动态规则/资产/债务/候选/normalizer，取消早期 Host commit、重复玩家历史与漏旁听 owner。主线程 prepare/dispatch、后台只传网络字符串，原 public opt-in ABI 不变。
- 同里程碑修复正文标签播放旁路、relay 与 speech 互等、generation/session/epoch 跨档发布、五处会话清理晚回调、request deadline 及旧 gate/waiter 干扰新请求；移除重复原实现与失效提交屏蔽条件。没有借此删仍有调用的 facade。
- 验证：Channel 132 / extraction 14，原方法差分 71 + guard 2 / mutation 5 / extraction 8，Queue 37 / mutation 7，Gate 6（原版三种竞态实际红测）；Interaction 40+69+39+4，生产 DLL 七套回放通过，Duel 双 API 35；官方 Debug/Release 的 1.3/1.4/Bootstrap 六项 0 warning / 0 error，构建前后输入指纹一致。
- 集成基线含其他作者 `4a239d95` / `cec3877a` 的遭遇安全修复，未覆盖或归为本轮成果。其移除 InteractionComponentSafePatch 可选 gate 后，runtime-game-adapter 清单仍声明 gated wired，当前 Bridge validator FAIL；20 个自检中的 1 个仓库基准 error 同因。未恢复安全 gate 或降低检查掩盖。下一项先核对 mandatory safety 与 optional bridge 的真实边界。
- 入口清单已登记新 partial；LIVE/SAVE 不升级。保留两份既有草稿；未推送、未部署、未操作存档、未跨工作区写入。真实游戏/旧档/live Economy/AFEF/TTS NOT_RUN，不把本里程碑或测试数量当阶段八 DONE。
- 完整 HANDOFF 与制作组简报：`docs/handoffs/2026-09-08-scene-postprocess-milestone-handoff.md`、`docs/handoffs/2026-09-08-scene-postprocess-team-brief.md`。后续继续前段 capture、Native 完整 Prompt/流式/主动开场 parity，以及 BattleSpeech 异步回退重入验证；自动化按完整模块接续。

## GitHub 融合交接推送（2026-09-06）

用户明确授权“融合然后推送”。已完成本地 `38c72484` 与共享远端 `8f1fa8db` 的正常合并，代码提交 `fb01c03c`；三个终端冲突按功能融合，保留API引导/设置与本地功能修复。融合后六项构建、终端/周报/Duel/Gateway/Host及相关契约回归通过。新交接为 `docs/handoffs/2026-09-06-merged-refactor-handoff.md`，制作组文案同目录 `2026-09-06-merged-refactor-team-brief.md`；普通推送目标仍为 `refactor/prepare-af-restructure`，不覆盖main、不force push。

两份原handoff/简报的本地未提交占位草稿保持原样；本次不部署游戏、不操作存档、不切默认入口、不恢复自动化。阶段八整体仍未标DONE；最终发布位置见GitHub交接记录。

## 当前整体收尾任务（2026-09-06）

用户要求不再逐小批次交付，统一完成阶段 8 的功能对照、接入、清理和回归。基线 `97515f3f`；本轮保持同一工作区/分支，内部可使用回滚 checkpoint，但不以每个 checkpoint 作为交付终点。

- 整体范围：旧终端剩余功能与入口、三渠道实际默认路径/替代覆盖、Bridge fallback 隔离、存档与领域验收目录、项目内 Debug/Release 双 API/Bootstrap 和现有相关回归。
- 先确认实际调用链，再清除已替代实现；不按 Legacy 命名删活跃 owner、不改变存档 identity、不通过重命名或弱化测试虚报完成。
- 使用并行子任务分别核对主入口、终端与验收边界，统一集成/构建，避免多个任务同时改同一文件或共享 Stage。主任务负责最终验证与提交。
- 认可用户报告的制作组既有实测基线；新增修改仍按实际证据区分源码/回放/编译与实机。此前整体修复时没有对新 DLL 的部署、真实存档操作或推送授权；现用户仅追加 GitHub 交接推送授权，仍不覆盖游戏、不恢复自动化。
- 状态 `VERIFY / 整体迁移未完成`：已统一修复周报正文/主线程与读档边界、终端外部交接/战争fallback、RAG开关隔离及部署/入口清单漏检，删除29个无入口私有方法和被替代弹窗；六项构建与相关回归通过。实际默认三渠道迁移、Scene detached规则等价及ModuleHost接入仍有实现缺口，不能宣告阶段8 DONE。集中交接：`docs/handoffs/2026-09-06-integrated-phase8-handoff.md`。

## 当前本机切片（2026-09-06 阶段 8 功能对照修复）

- 基线 `220b1dd5`；工作区 `G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 用户授权继续阶段 8、对照旧代码恢复功能并清除确认失效的实现。本切片先处理终端查询/臣属选择、百科与退出、战争归档，以及部署/持久化验收缺口。
- 替代路径未完成功能对照之前不删除；仍承担三渠道默认玩法、存档迁移或双 API 兼容的代码不得仅因名为 legacy 就删除。后续默认切换和清理必须有对应 LIVE/SAVE 证据。
- 计划复用现有契约、补最小回归，构建 1.3/1.4/Bootstrap；不修改官方构建脚本、不部署、不写真实存档、不推送、不恢复自动化。
- 第二批标签字典替代与旧菜单清理 `VERIFY`：基线 `b8757240`，意图 `6cbc73e0`；统一分页/搜索/完整来源详情、刷新反馈和当前快照导出；六项构建及生产回放通过，详见同一功能对照文档。未触及实际游戏或发布。
- 本批状态 `VERIFY`：部署门禁、终端查询/臣属替代和已失效路径清理、即时和平归档及 WarStats 契约清单已实现；Debug/Release 六项构建与定向回放通过。用户确认制作组已完成既有验收；本批新增改动仍区分 offline 与 LIVE/SAVE。功能对照与清理证据见 `docs/phase8/functional-parity-closeout-20260906.md`。阶段 7/8 均未宣告 DONE。

## 当前任务（2026-09-03 Bridge 接线收尾接续）

本轮离线责任认领/入口复核计划已单独记录于
`docs/phase8/offline-owner-bridge-closeout-plan-20260904.md`；它是 `PROVISIONAL_AUTHORIZED`
准备态，不会把正式目录晋级为 `ASSIGNED/COMPLETE`，也不会解除 LIVE/SAVE、默认切换或发布门禁。

> **2026-09-04 较早工作树校正（已被下方后续复核覆盖）：**本机实际工作区为
> `F:\\AnimusForge-main`，该次记录的 HEAD 为 `34b3f35811130e26b60a5407451d169de3667dbb`。
> `PersistenceIdentityAudit --json --quiet` 在该次记录的 HEAD 上实际返回 `PASS`（sync 99、behavior 35、
> module `AnimusForge`）；“缺 89 个基线 blob / FAIL”仅适用于此前另一 partial worktree 的历史记录。
> 当时的 Debug unified Stage 已按授权部署到 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`，
> 三份 DLL 与项目 Stage 哈希一致，`installedMatchesStage=true`、`gameRunning=false`。本机仍未启动游戏，
> LIVE/SAVE、默认切换、facade 清理和发布仍未完成。详见 `docs/handoffs/2026-09-04-live-host-prep-and-current-state.md`。

> **后续复核覆盖：**随后仅为离线验证重新生成了 Debug/Release Stage，未再次部署；游戏目录仍是较早
> 的 Debug Stage，因此当前 `installedMatchesStage=false`。实机开始前以
> `docs/handoffs/2026-09-04-live-host-prep-and-current-state.md` 的 04:20 覆盖记录为准。

- `LIVE-HOST-PREP-20260904` VERIFY（较早部署快照，已被后续复核覆盖）：当时 Stage 已构建并完成 scoped Debug 部署；部署只更新统一
  `Modules\AnimusForge`，保留 `CustomPrompts`/`Logs`/`PlayerExports`，不启动游戏。下一步是制作组在
  隔离存档完成 1.3/1.4 Campaign/Mission 的 LIVE/SAVE 和 rollback evidence；没有这些证据不得升级
  阶段 7 或执行阶段 8 的删除、默认切换、Release 发布。

- `OFFLINE-GAP-20260903` VERIFY：已完成 Bridge caller/body validator、Bridge 隔离运行时配置测试、Phase 8 入口候选 inventory、PersistenceIdentityAudit 性能/进度契约和 ModelCatalog 稳定错误码/双语映射；不启动游戏、不读写真实存档、不部署、不切换默认入口、不删除 facade、不修改发布结构、程序集身份、SubModule.xml、SyncData key/type 或构建脚本。owner 为 Foundation/Bridge runtime、Phase8/Tools 与 ModelCatalog adapter/既有 UI owners。阶段 7 保持 `VERIFY`，阶段 8 保持 `BLOCKED`，本次本地提交已 push。实现提交：`552d8b9`、`f1a17f7`、`4feac3c`、`4a8e929`、`8f12298`、`ab6ce72`；意图记录提交：`01e7bc1`。

- `BRIDGE-CONFIG-20260903` VERIFY（本轮离线收尾已完成；历史记录中的普通 push 授权未在当前工作树执行）：承接 checkpoint `13e21560` 的 Bridge 运行时接线，已完成显式依赖 Production replay、全量离线回归、文档同步和最终安全审查；当前为 `10 wired / 6 declared-only`。owner 为 Foundation/Bridge runtime 与各已接线领域；本轮不启动实机、不读取或写入真实存档、不部署、不切默认入口、不删除 facade、不修改一键编译/覆盖脚本，也不把 offline/compiled 证据提升为 LIVE/SAVE。已审阅路径包括 `Refactor/Runtime/FeatureBridgeRuntime.cs`、已接线 adapters/behaviors、`AnimusForge/ModuleData/FeatureBridges.json`、`docs/phase8/bridge-binding-manifest.json`、`tools/BridgeBindingContractTests/` 及相关总纲/阶段8/handoff；验证包含 Bridge validator/unit、Production/compiled suites、双 API Debug/Release/Bootstrap Stage、git diff/凭据/产物审查。阶段 7 总体仍 `VERIFY`，阶段 8 执行仍 `BLOCKED`。

- `PUBLISH-20260902` DONE（仅指自动化关闭与GitHub制作组交接，不代表阶段7/8 DONE）：用户明确要求关闭自动化并把当前重构分支、HANDOFF和制作组简报推送GitHub。`af-7-8`与旧`af`均为PAUSED；收尾编写前工作树clean，本地HEAD `19e5d6b1`，`origin/refactor/prepare-af-restructure`为`9566bf3b`，ahead 19 / behind 0。最终发布只增加交接文档，通过普通fast-forward push同步到同一远端分支，完成后要求本地HEAD与远端分支完全一致；禁止force push、部署、覆盖游戏、切default或把离线证据提升为LIVE/SAVE。本轮发布总交接为`docs/handoffs/2026-09-02-github-publish-and-team-handoff.md`，制作组短文为`docs/handoffs/2026-09-02-stage7-stage8-team-brief.md`。

- `LOCAL-7-C3` VERIFY（2026-09-02，LiveHostReadinessAudit explicit-root portability 已完成）：`--game-root` 改为显式必填，删除 F 盘默认路径，保留 repo-derived `--project-root`，新增纯 fixture/CLI 契约；C3 测试 4/4、Python 编译与 `git diff --check` PASS。工具仍只读，不启动游戏、不部署、不读取存档，真实 LIVE/SAVE 仍 NOT-RUN。

- `PERSISTENCE-OFFLINE-20260902` VERIFY（2026-09-02，Persistence/Profile/Identity scanner/catalog 收尾）：排除 `.tmp`、artifacts、缓存和依赖输出，支持跨 partial 文件解析唯一字符串常量，catalog 同步 44 个 flattened key；Persistence/Profile/Config `95 literal / 121 typed / 8 types / 3 profiles / 44 flattened` PASS，Identity `sync=99 / behavior=35 / module=AnimusForge / bootstrap=1` PASS。不改生产 SyncData/key/type、程序集或部署流程，真实旧档仍 NOT-RUN。

- `DEBUG-DEPLOY-20260902` VERIFY（2026-09-02，用户授权的 Debug 测试编译与统一模块部署）：来源为本地 `F:\AnimusForge-main` 的 `refactor/prepare-af-restructure`、HEAD `109835cd18fee09ebd591fa254f0af1aa913acb4`，不是 `main`；统一脚本构建 1.3/1.4/Bootstrap 均 0 warning / 0 error，并事务替换 `F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`。部署时 Bootstrap/1.3/1.4 哈希为 `BF57E46CF3C095FB3205DBA4A7428339A1C574BC30B3B8DE882822E4ACC2AAE9` / `5F66A4932AB1948BBB71D38C80C6AADC63AD3F5F508004B1F2469FB13544E970` / `D28931E9129E3E6F441BC5297466BA99FC886BD9DD15A5C3484B7EFCF598D16C`，部署当时项目 Stage 与安装完全一致，合并 PlayerExports 4753 个。随后重建当前 Debug Stage 的实现哈希为 1.3 `BB157A03F97F606158203E3A68F53AEC7687F6BFD5850728760446285CFC2ABE`、1.4 `F43DFD482596BA58501A48723225CF6999E3C2143B0E7029B4363410ED6A5376`；安装目录仍为部署时实现哈希，只有 Bootstrap 仍相同。readiness 的 `installedMatchesStage` 只比较 Bootstrap，实机前须重新部署当前 Stage。未启动游戏、未读写存档，Release/默认切换/最终发布仍 BLOCKED。

- `RELEASE-OFFLINE-20260902` VERIFY（2026-09-02，Release Stage/Production Duel/ZIP 离线闭环）：统一脚本重建 1.3/1.4/Bootstrap Stage 均 0 warning / 0 error；Production Duel Release replay `35/35 PASS`、1.3/1.4 parity PASS；ZIP `F:\AnimusForge-main\.tmp\packages\release-final-20260902\AnimusForge_v1.3.7.2_20260902_100952_233.zip` 共 4919 entries，SHA-256 `1215A88666E6FCCD949BE413C75719B2C96BCA061546FCAD86DB9AB0F805ACE5`，Bootstrap-only XML/双实现 marker/hash/ONNX 与旧模块排除均通过。未安装 Release、未启动游戏、未读写存档，真实 Release/LIVE/SAVE/默认切换/最终发布仍 BLOCKED。

- `BRIDGE-CONFIG-20260902` VERIFY（2026-09-02历史快照，功能 Bridge 配置与安全接线）：新增 `docs/phase8/bridge-binding-manifest.json` 与 `tools/BridgeBindingContractTests/`，逐路径/逐 symbol 对齐 canonical 16 组 Bridge，当日快照为 `3 wired / 13 declared-only`。仅在当时已审阅的 `AfGcczShoutBridge.IsActive`、`WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved`、`SceneActionsIntegrationBoundary.InitializeRuntime` 三个一次性/事件边界接入 `FeatureBridgeRuntime` Gate；禁用/失败保留原版或各自 owner fallback，不新增 Tick 全量扫描、存档字段或 live 对象跨边界。该快照不代表当前接线或 LIVE/SAVE。

- `BRIDGE-CONFIG-20260903` VERIFY（2026-09-03，Bridge 配置与安全接线离线收尾）：在既有边界上接入 `conversation-gateway`、`conversation-action`、`action-memory`、`action-economy`、`policy-world-diplomacy`、`conversation-siege`、`conversation-courier`、`memory-social-reports`、`gateway-knowledge-profile`、`ui-runtime-integration` 共 10 组 source-bound Gate；`bootstrap-host`、`host-runtime`、`runtime-game-adapter`、`persistence-domain-owners`、`scene-duel`、`tools-content-release` 保持 `declared-only`。同时修正配置缺失/损坏时的 fail-closed 诊断、拒绝非规范大小写 ID，保持 NoOp/native/owner fallback、无 Tick 扫描、无新增 save key/type 和无 live 对象跨边界。Bridge validator `16 bindings / 10 wired / 6 declared-only / configEnabled=10`、相关纯测试与双 API Debug/Release/Bootstrap Stage 均 PASS；真实 Campaign/Mission、LIVE/SAVE、默认切换和发布仍 NOT-RUN/BLOCKED。

- `LOCAL-7-C2` VERIFY（2026-09-02，ShoutNetwork SSE replay dependency closure 已完成）：基线 `8bf0c1e4`，意图checkpoint `28ad96f2`，实现 `ae49e3c8`，远端仍为 `9566bf3b`。红契约证明 `ShoutNetworkSseReplayTests.csproj` 缺shared Import且仍硬编码 `F:\SteamLibrary` / `Modules\**`复制；现已完整删除local copy target，只导入既有 `BannerlordReplayDependencies.targets`。新增source-only五consumer契约，要求net8.0、exact shared Import并拒绝project-local target、机器绝对盘符、Modules/Workshop递归和AnimusForge implementation复制；README登记第五consumer与安全复现路径。未改`Program.cs`、shared helper、生产C#、Stage、官方脚本或游戏。source contract 5/5、原helper 9/9、缺property fail-closed、Shout Debug/Release runner六类SSE业务断言均PASS；两份78项依赖manifest hash一致，绑定M2 fresh Debug 1.4 Stage `D806B988...` / MVID `337e6131-fc69-4f31-a86b-ced3e1a65acd`，终审P0=0/P1=0。Release只证明Release runner加载同一Debug AF Stage；helper不会拒绝所有未选中stale DLL，故未来机器必须先审计并可逆隔离旧runner bin/obj，本轮迁移前两目录均不存在。证据在`.tmp/validation/shout-sse-dependency-c2-final-20260902-031458`。这是tool-only离线闭环，不产生LIVE/SAVE证据，阶段7仍VERIFY、阶段8执行仍BLOCKED。

- `LOCAL-7-M2` VERIFY（2026-09-02，Duel exact detached dispatch provenance 已实现并完成离线/compiled 验证）：基线 `3522dc3e`，意图 checkpoint `17f617a5`，实现 `b93f93df`。新增 internal `IRequestBoundActionPlanExecutor` 与 data-only `DetachedDuelDispatchContext`，在 stale gate 和 commit reservation 后、任何 Economy/legacy 副作用前重新计算并校验 canonical request/action fingerprint，Queue 唯一 DuelId，再把同一 context 精确转移到 meeting pending、arena/local、wilderness 与 conversation-exit delayed host。显式状态为 Rejected、Queued、Started、UnknownAfterStart；它们终结本次Interaction commit，Queued/Started/Unknown均non-retryable，但底层owner仍可沿同一DuelId记录后续session/outcome，绝不回填原commit。Native/Scene 启用 exact owner；Courier 没有真实 `PrepareDuel` owner，防御性 exact Rejected；`Duel+Mood` companion 保留，但任何可能已发生的 Mood effect 只报 Unknown，不伪造 NoConfirmedEffect。queue 先于 Economy/gameplay，Economy失败或异常释放 queued Duel；host 只在 holder publication 后标记 accepted，delayed consumer 必须同时满足 Queued+HostAccepted；load 清理 pending trigger/runtime/UI/menu/queue 并把 active receipt 转 Unknown，不重放 Mission、stake、death、Economy 或 Memory。实际结算的三条路径必须先成功写入同一 result receipt，才能继续 Memory/renown/stake/death；outcome owner为64 active/512 retained，host另以4096 process-lifetime exact-ID seen tombstone阻止rollover复活并在满容量时fail-closed。public `IActionPlanExecutor`、原 executor 构造器、Duel public ABI、默认入口、`_duelCooldowns` key/type、Saveable ID、Fourberie optional seam 与 M1 legacy-unbound路径均未改。Duel Dispatch 16/16、Duel Outcome 18/18、Interaction/Host/receipt/Economy/Persistence/Profile/Migration/Identity、Production Duel Debug/Release 35/35 与六项 Stage 均 PASS，独立终审 P0=0/P1=0；证据在 `.tmp/validation/duel-dispatch-m2-final-20260902-021935`。真实 Campaign/Mission、旧档、live death/stake/Economy/AFEF/Fourberie 与默认切换仍 NOT-RUN，因此阶段7保持 VERIFY、阶段8执行保持 BLOCKED。

- `LOCAL-7-M1` VERIFY（2026-09-01，Duel actual-session typed owner/outcome/readback 已实现并完成离线/compiled 验证）：基线 `9955658b`，意图 checkpoint `fc3cd722`，实现 `16f3cbef`，远端仍为 `9566bf3b`。新增 process-local、bounded、NOT-RECOVERABLE 的 `DuelOutcomeOwner`；meeting、arena/local、wilderness 三条实际开始/终态路径均接入 typed owner，成功绑定的 session 在 Memory、renown、stake、death、UI 前先锁定 `ResultIdentity`，再以 Confirmed/Partial/AttemptedUnconfirmed/Unknown 分量收尾；reserve失败只保留legacy玩法并fail-closed为无typed readback。stake/debt/after-lines 现在只接受同一回复中的精确 `[ACTION:DUEL]`，一次绑定实际 `DuelId`，失败或终态清理，禁止跨 Duel 泄漏。detached legacy Duel callback 不再伪报玩法成功，而返回 terminal `UnknownAfterStart` / `duel.outcome_pending`，保留已确认 Economy subset 且禁止 fallback/replay。legacy路径继续使用明确的 `Domain / legacy-unbound` 合成 provenance，不能冒充某个 ActionPlan request 的因果证明；exact detached binding已由后续M2 `b93f93df`独立补齐，未修改该兼容语义。legacy public void ABI、`_duelCooldowns : Dictionary<string,float>`、Fourberie optional guard、默认入口和所有存档 identity 均未改；Duel contract 16/16、Interaction/Host/receipt、fresh 1.3/1.4 production replay 32×Debug/Release、Persistence/Profile/Migration/Identity 与 Debug/Release 六 Stage 均 PASS。真实 Campaign/Mission、旧档、live death/stake/Economy/AFEF/Fourberie 与默认切换仍 NOT-RUN；阶段7保持 VERIFY，阶段8执行保持 BLOCKED。

- `LOCAL-8-A` VERIFY（2026-09-01，阶段8非破坏性完整领域准备已实现）：基线/已推送远端 `9566bf3b`，意图checkpoint `9a088f2f`，实现链 `b1c5a81a`→`1e341c43`→`f4a02018`→`6b1d16f1`→`8bdd9363`。`full-domain-readiness-catalog.json`以canonical 20个**验收责任桶**（不是20个物理DLL）记录英文ID、role owner、代表性真实入口、Prompt/ActionPlan适用性、存档/fallback/default/current evidence、blocking gates和canonical 16组Bridge；早期8-ID design catalog与Pending entry type保留。20个maintainer均为`ROLE_PLACEHOLDER`、entry coverage均`REPRESENTATIVE`，团队确认`ASSIGNED`且补齐`COMPLETE`前real readiness必定BLOCKED。16 Bridge区分13组`PAIR`与3组`CROSS_CUT`，证据必须显式`bridgeIds`，并在OFFLINE、LIVE 1.3/1.4、SAVE 1.3/1.4覆盖对应case。`cleanup-candidates.json`逐真实symbol登记12 KEEP/3 HOLD/3 REVIEW_REMOVAL；工具验证symbol存在、同文件按candidate ID独立，audit/replacement/rollback evidence都必须绑定candidate+owner domain，candidate/global/inventory checkpoint一致且严格早于HEAD。本轮零删除。红基线证明旧49-record fixture只覆盖8-ID仍可绿；最终62个纯fixture测试、Bridge10/6、Composition18/24、ModuleCatalog8/3/16/8与all-missing full-20 BLOCKED均PASS。未改生产C#、key/type、默认入口、GCCZ/NEW-10/游戏，六Stage按纯工具切片规则N/A；真实Campaign/Mission、旧档、live Economy/AFEF/Notoriety及发布仍NOT-RUN，阶段7保持VERIFY、阶段8执行保持BLOCKED。

- `LOCAL-7-L` VERIFY（2026-09-01，Notoriety exact line/session outcome代码与离线验证完成）：基线 `68dce8e9`，意图 checkpoint `cddc7628`，实现 `80729cb9`。审计确认旧 owner 的 read路径可提前roll、active只按Hero且不入档、void line/finalize吞错、finalize先删active再写aggregate。现只为拥有 H recoveryId/payloadHash/part + memory session identity 的 detached line建立 `AFNR1` exact owner：duplicate line在任何roll前命中；read roll只冻结到active，首个实际line owner commit再把known-state与witness同存既有 `_af_player_notoriety_state_v1` JSON；session finalize冻结绝对 sessions/bonus/day target，readback后Applied。不同exact session先收尾旧session，迟到旧finalize不能消费新session；legacy/exact混用将L receipt终止为Unknown并回旧语义；零line prompt roll不再伪造完成session。loaded Open→Unknown且保留line tombstone，Confirmed只重放绝对data target，绝不重roll/重finalize。legacy void ABI、默认route、H/I/K wire与95 literal key/type均不变。AFNR1 contract 14/14、Interaction 40/69/39、Memory/Courier/Economy、fresh Production OptIn/三Host、Profile 95/121/42/40、Migration10、Identity99/35与Debug/Release六Stage均PASS；真实Campaign、MBRandom、save/load/crash、旧档与default仍NOT-RUN，故阶段7仍VERIFY。

- `LOCAL-7-K` VERIFY（2026-09-01 自动接续，weekly exact-intent/outcome owner 代码/离线验证完成）：基线 `da15241f`，意图 checkpoint `7cdf6435`，实现 `765b2386`。首版只接受 **Economy-only、whole ActionPlan、owner full Applied + full count + ConfirmedEffect + exact actual fingerprint + memory HistoryWritten**；canonical sidecar projection不调用可注入 gameplay planner，actual planner 每次 commit恰好调用一次。独立 `AFWM1` data-only ledger绑定 request/trace/channel/session/subject/runtime/save/Courier direction/turn/action/candidate/payload hashes，64 pending / 512 terminal；有效 journal的 durable identity probe先于 live payload重建，防止已 Applied/Confirmed 请求因债务消失或 foothold变化绕过 Duplicate/Conflict。只有 Confirmed可做 data-only Daily trigger attach；Prepared load转 Unknown，partial/unknown/rejected/mixed/legacy/不支持估值一律不发布。新增 symbolic `_af_weeklyActionOutcomeReceipts_v1 : Dictionary<string,string>`，不改 95 个 literal key、H seed/hash/wire、Courier `AFCI1`、public ABI或默认入口；坏 journal原样保留并禁用该 sidecar，人工修复前不提供K的跨重启防重。focused、8项 production/compiled回放、Profile/Migration/Identity 与 Debug/Release 六 Stage PASS，独立终审 P0=0/P1=0；证据在 `.tmp/validation/weekly-outcome-k-final-20260901-163221`。compiled/fixture不等于真实 Campaign/save/live Economy/AFEF，故阶段 7仍 VERIFY、default cutover仍 BLOCKED。

- `LOCAL-7-J` VERIFY（2026-09-01 自动接续，memory auxiliary recovery 边界代码/离线验证完成）：基线 `d2f37a8a`，意图 checkpoint `3436d739`，实现 `84e92f80`。已确认 legacy live 链为 `AppendExternalDialogueHistory → AppendDialogueHistory → AppendDialogueHistoryById → AppendDailyMemoryLineById`，而 detached facade 直接进入 H owner。H 的 Daily writer 现只发布 Daily marker/projection，不再读取、附着或删除无 request/outcome 身份的 pending weekly candidate，也不在 tick/load/`ExistingPending` 重放非幂等 notoriety。只有 brand-new `Began` 在同一次调用内完成全部 core receipt，且 user/assistant 的 Daily marker 精确匹配 recovery ID + payload hash + part 时，才各执行一次 current-runtime `NoteConversationLineForExternal` best-effort；异常与 core Completed 隔离，结果仍为 `attempted_unconfirmed / NOT-RECOVERABLE`。legacy live `Attach→Save→Note` 未改；H schema/seed/hash/wire、I `AFCI1`、SyncData key/type 均未改。focused/production/Profile/Identity 与 Debug/Release 六 Stage PASS，production 断言是 compiled-DLL reflection/IL 结构守卫，不是 live notoriety mutation/fault/save-load 证明；日志在 `.tmp/validation/memory-aux-boundary-20260901-141201-final`。既有 P1 仍在：legacy weekly candidate 早于 action outcome、无 turn/request 绑定且可能跨轮；detached 尚无 weekly owner。故阶段 7/default cutover 继续 BLOCKED。

- `LOCAL-7-I` VERIFY（2026-09-01 自动接续，Courier inbound completion 代码/离线验证完成）：基线 `0e276ce1`，意图 checkpoint `b5395164`，实现 `de3220b7`。Courier 在 memory owner 开始前把 `AFCI1` receipt 存入既有 `_af_courier_sessions_v1` 的 session JSON；receipt 绑定 opaque recovery ID、owner payload hash、session/sender/current-player/party 和冻结 visible letter，full-wire checksum 与 32,768 字符上限 fail-closed。MyBehavior 只公开 internal recovery identity/status seam，不调用 Courier；Courier tick 轮转且每 tick 最多处理一条，只有 owner payload-matched Completed/Applied/Duplicate 才补三字段并推进原状态机。Missing/Disabled/Quarantined/PayloadMismatch、坏 wire、pre-owner/无 receipt commit 均终止该 inbound session并释放等待暂停，不重放 ActionPlan/Economy/postprocess，也不复用 `PostprocessConsumed`。focused/production/profile/identity 与 Debug/Release 六 Stage PASS，独立只读终审 P0=0/P1=0；日志在 `.tmp/validation/courier-inbound-completion-20260901-124752`。默认 inbound 仍是 legacy、真实 Campaign/save/load/AFEF 仍 NOT-RUN，故阶段 7 不标 DONE。

- `LOCAL-7-H` VERIFY（2026-09-01 自动接续，memory-only 持久恢复代码/离线验证完成）：基线 `a8001b87`，意图 checkpoint `6f8d8cc0`，实现提交 `f6e5e694`；首次 fetch 曾因 `schannel: failed to receive handshake, SSL/TLS connection failed` 失败，收尾重试成功，远端仍为 `fc8c344e`、本地 ahead 25。MyBehavior 新增唯一 symbolic `_af_interactionMemoryRecovery_v1 : Dictionary<string,string>`，64 pending / 512 completed / 64 quarantine；opaque id、full-wire checksum、payload hash、process nonce、最多六个 Daily/Recent 步骤、单步骤五次失败后隔离、跨日 sealed draft、Scene provenance、non-Hero retarget/destroy 均 fail-closed。payload/tick 不含 ActionPlan/postprocess/executor/afterCommit，cache 不能绕过 ledger；旧六参/四 void ABI、95 literal/121 typed、99 identity/35 behavior、程序集拓扑与默认入口不变。本轮所列 focused suites 全 PASS；Debug/Release 六项 Stage 0 warning/error，独立只读复核未报 P0。真实 Campaign/AFEF/旧档仍 NOT-RUN；Courier inbound session completion 是独立 `afterCommit` P1，详见 `docs/handoffs/2026-09-01-memory-only-recovery.md`。

- `LOCAL-7-G` VERIFY（2026-09-01 自动接续，structured unknown 代码/离线验证完成）：基线 `c2a2be96`，意图 checkpoint `899effbb`，实现提交 `d765270a`；fetch 后远端仍 `fc8c344e`。尾增 `UnknownAfterStart=6` 和 additive effect receipt，保留旧 enum 0–5、outcome interface、executor 六参构造器与 `InteractionCommitResult` 唯一公开四参构造器。Port 将 callback throw/null/非法回执归为 fact-free unknown；Hero/Party/Merchant 以显式 mutation observation 贯穿物品/RP、固定资产、装备恢复队列/rollback 吞错路径，停止后续 action、保留此前已确认 count/facts。Executor/Committer/Host/cache/duplicate/in-progress 全部终态不重放；count0 不伪报 `ActionsExecuted`，dispatcher 不能用 fake success 覆盖 owner 回执，callback 未启动仍可安全 fallback。Port unknown 8、Economy-aware unknown 3/receipt 4、Host 69（三渠道）、request receipt 39、Production Economy/owner/三 Host/OptIn、95-key/121-binding profile、99-sync/35-behavior identity 与 Debug/Release 六项 Stage 全部 PASS；独立终审无 P0/P1。真实 live mutator fault、Campaign/Mission、AFEF、旧档仍 NOT-RUN；不含补偿、memory-only/afterCommit recovery 或 durable tombstone。完整证据见 `docs/handoffs/2026-09-01-unknown-action-effects.md`。

- `LOCAL-7-F` VERIFY（2026-09-01 自动接续，known-partial 代码/离线验证完成）：基线 `67603f18`，意图 checkpoint `7186048d`，实现提交 `8f22d737`；fetch 后远端仍 `fc8c344e`。Hero/Party/Merchant owner 用尾增 enum `PartiallyApplied` 返回短计数和真实 facts；旧 `Applied + short count` 在 port 兼容归一化。Additive outcome interface 不破坏旧 receipt/构造器；executor 对 known partial 或 Economy 后 legacy reject/throw 返回 `NonRetryableFailure`，只保留 Economy owner count/facts。Committer 只写 outcome facts，返回 `ActionsExecuted=true`；memory 失败仍终态，duplicate 不再执行 Economy/memory，Host 不 fallback/afterCommit。红测复现 facts/count 丢失；Economy-aware partial 4 + partial receipt 4、Port partial normalization 4 + enum ABI、Host 51（三渠道）、Production partial 2、相关 production/Interaction/Persistence 和 Debug/Release 六项 Stage 全部 PASS。真实 live Economy/AFEF/save 仍 NOT-RUN；`UnknownAfterStart`、durable memory-only/afterCommit recovery 未闭合。完整证据见 `docs/handoffs/2026-09-01-partial-economy-outcomes.md`。

- `LOCAL-7-E` VERIFY（2026-09-01 自动接续，opt-in owner 代码/离线验证完成）：基线 `3d9778d2`，意图 checkpoint `bbe35aa8`，实现提交 `b2542fdd`；fetch 后远端仍 `fc8c344e`。已证明 economy-only 在旧 executor 中直接 `Executed`、不调 Courier owner；现于任何 Economy Replay 前调用可选 channel gate。Courier gate 重解析 active session/recipient，验证 channel/session/subject、outbound、delivery、terminal/consumed；mixed 仅 prevalidate，economy-only 先置既有 JSON 字段 `PostprocessConsumed`。不新增 key/type/field，不从 raw 推导 visible reply，保留旧六参构造器二进制签名。Gate 五类顺序/失败 contract、production session fixture 18 assertions、Production Economy/Courier/Configured、Interaction 40+48+4+38、Economy port、95-key/121-binding profile 和 99-sync/35-behavior identity 均 PASS；Debug/Release 的 1.3/1.4/Bootstrap 全部 0 warning / 0 error。当前 detached Courier 仍无 production caller/default cutover；真实 save/load、live asset/AFEF NOT-RUN，因此只标 VERIFY。完整证据见 `docs/handoffs/2026-09-01-courier-economy-reservation.md`。

- 最新本机owner实现提交：Memory runtime receipt `5d3dc5f0`、Courier reservation `b2542fdd`、known partial Economy `8f22d737`、unknown effect `d765270a`、durable memory-only recovery `f6e5e694`、Courier inbound durable completion `de3220b7`、memory auxiliary boundary `84e92f80`、weekly exact owner `765b2386`、Notoriety exact owner `80729cb9`、Duel actual-session outcome owner `16f3cbef`、Duel exact detached dispatch provenance `b93f93df`；测试依赖闭环含第五个Shout SSE consumer到 `ae49e3c8`，阶段8完整领域门禁到 `8bdd9363`。`LOCAL-8-A`只完成准备态；`LOCAL-7-C3` 与 Persistence/Profile/Identity 离线收尾已完成。真实测试人员可并行按20领域包采集Duel及其他领域的LIVE/SAVE证据；真实Host/旧档/live Economy/AFEF前仍不得删除facade、切默认或发布。

- `LOCAL-7-D` VERIFY（2026-09-01 自动接续，batch runtime 代码/离线验证完成）：基线 `9b8cb509`，意图 checkpoint `3fe3f656`；fetch 后远端仍 `fc8c344e`。已复现旧生产 DLL 在缺 Campaign 时虚假 Applied；现由 MyBehavior 原写入实现返回 daily/recent 原始 owner 状态确认，batch facade 只在确认后缓存。保留四个公开 void API、原写入顺序、session/260 行窗口和 SyncData key/type；结果不代表原子事务/落盘。新生产缺 Campaign 7、receipt 7、线程 guard fixture 2、void 签名 4，以及 raw-owner 11 assertions PASS；Interaction 40+48+4+38、三类生产 Host、两类 Economy contract PASS；Debug/Release 1.3/1.4/Bootstrap 全部 0 warning / 0 error；Persistence/Profile 和 identity（99 sync / 35 behavior）PASS。仅校正两条 SyncData fixture 导航行号，未放宽断言。独立审查无阻断；日志在本工作区 `.tmp/validation/memory-owner-20260901-0504`，完整证据见 `docs/handoffs/2026-09-01-memory-owner-receipts.md`。真实游戏/旧档/AFEF 仍 NOT-RUN，无后台构建/回放继续运行；下一精确任务为 `LOCAL-7-E`。

- `FRAMEWORK-20260901` VERIFY（框架代码与本地验证已完成，真实 Host 未验收）：本机 canonical worktree 仍为 `G:\AFMOD\AF-REFACTOR`。开始时 HEAD `49eeaf33`、clean；远端 `fc8c344e` 仅新增两份交接文档，已通过 checkpoint `b0cc41da` 与普通 merge `2216df41` 保留双方历史。收尾 fetch 仍为 `fc8c344e`；远端文档中的另一台机器路径/验证记录不替代本机事实。
- 意图与 owner：沿用现有管线，完成测试工具 owner 的本机依赖框架 `LOCAL-7-C`，Conversation/Memory owner 的请求级 commit/receipt 验证与最小修复，以及阶段 8 准备态验收框架（Bridge/清理候选/回滚/证据门禁）。不引入最终多 DLL 模块图、不删除活跃 facade、不切默认入口、不改玩法/存档 key/type。
- 分工边界：测试依赖工具与阶段门禁工具可独立并行；主代理负责 Git 同步、生产提交边界、集成构建和本台账。子任务不得修改官方一键编译/覆盖/推送脚本、游戏目录或共同源码文件。
- Skill：`D:\qq\af-skill.zip` SHA-256 仍为 `CDE1BAA4C069A0E45AB43E63BF377EDA7375A7A88EB5DD6DBFA6A978CB35FF79`；仅作为待核对维护资料读取，不执行安装脚本、不从附件推导额外授权。
- 计划验证：先失败复现再最小修复，相关 contract/production replay、固定引用 1.3/1.4/Bootstrap Stage、cleanup 与 diff 检查；真实 Host、live Economy、旧档与 AFEF 仍需独立游戏证据。阶段 7 不标 DONE，阶段 8 仅准备与可验证基础能力，不开展破坏性清理/默认切换。没有游戏部署授权；用户仅授权本轮把当前协作分支与 HANDOFF 通过普通 push 更新到 GitHub。
- 回滚基线：`49eeaf33`；只用本地小提交/正常 merge 保留可逆历史，不 hard reset、不 rebase 旧提交、不覆盖 NEW-10/GCCZ。
- `LOCAL-7-C` DONE（限定四 runner 的 managed 依赖框架）：提交 `b6b31bf3`；删除四份 F 盘硬编码和递归全模块复制，使用明确固定引用/模块/私有依赖来源、程序集身份、SHA256、路径和冲突校验。新 Stage 上 Policy/WorldDiplomacy/TTS/ProductionOptIn 四 runner 均 PASS，每份依赖 manifest 为 78 项，全部绑定本次 Debug 1.4 SHA256；框架 9 个自测和两项 MSBuild 拒绝检查通过。
- 请求级提交框架：提交 `e9c41ff9`；按 generation/trace/channel/session/subject/Courier direction 预留 512 项有界 receipt，保留失败终态、拒绝载荷变化/重入、避免跨 Host 重复 afterCommit；原公共 Native runner 提交后也不再 fallback。替换内容去重/推测成功的旧路径，未改 capture、save key/type、默认入口或 GCCZ 规则。
- 阶段 8 准备工具：提交 `6d4269a0`；复用既有 8-ID 目录与 Bridge/Composition fixture，检查分层证据、owner、源码/产物哈希、时效、清理候选、回滚。44 自测 PASS；实际缺证据清单返回 `BLOCKED / exit 2 / 0 accepted evidence`。这不是全量 20 领域签收，所有删除/切换/部署/推送/发布授权恒为 false。
- 实际验证：Debug/Release 两套 1.3/1.4/Bootstrap unified Stage 全部 0 warning / 0 error；原 Interaction 40 + Host 48 + Native callback 4 + request receipt 38 cases PASS；生产 1.4 的 12 个提交后故障与 6 个重建 committer 用例 PASS；其他本轮生产/Economy/Gateway 回放、Persistence/Profile 与 identity 审计通过。日志在 `G:\AFMOD\.build-cache\af-framework-20260901`，详见 `docs/handoffs/2026-09-01-framework-continuation.md`。
- 当前明确未完成：`LOCAL-7-D/E/F/G/H/I/J/K/L` 均为代码与离线证据 VERIFY，live AFEF/旧档/Campaign/Economy/default 尚未验收。H 只修 core Daily/Recent memory，I 只修 Courier inbound session completion，J 隔离无证据 auxiliary recovery，K 只覆盖 Economy-only whole-plan exact outcome且只做 Confirmed data attach，L 只覆盖具备 H recovery/session identity 的 detached Notoriety line/session；均不承担 gameplay compensation，K不为 mixed/legacy/partial/unknown补写成功，L也不把 legacy line、marker或aggregate值提升为exact成功。H→I 中间存档仍无法还原旧 visible reply。真实 Host证据到位前不切默认、不删除 facade。
- 真实 Campaign/Mission、旧存档、live Economy、AFEF 与默认切换仍 NOT-RUN/VERIFY；阶段 8 仅准备工具通过、执行门禁仍 BLOCKED。以上为 2026-08-31 快照；2026-09-02 已另按用户明确授权完成一次 Debug 双版本编译与统一模块测试部署，详情见当前任务的 `DEBUG-DEPLOY-20260902` 条目；仍未启动游戏、未读写真实存档，也没有在后台继续运行构建或回放。

## 当前状态（2026-08-31 本机接续）

- 文档任务 `PROGRAM-20260831` DONE（仅指总纲编写完成，不是 AF 全量重构完成）：按用户要求新增 `docs/animusforge-complete-refactor-program-20260831.md`，覆盖 20 领域、P0–P4 接续、owner/集成/测试分工、分层验收、发布清单与回滚边界；本轮仅改两份文档。已 fetch 确认远端 `182da1db`，编写前本机 HEAD `d8c81b5e`、ahead 4、clean；两项独立只读审查和成文复核完成，领域编号/远端 SHA/引用路径/Markdown fence/`git diff --check` 均通过。未重新运行构建/回放/游戏验证，未修改生产代码、Skill、默认入口或部署状态，未推送；文中测试结果明确引用上轮证据。当前生产接续任务仍为 `LOCAL-7-C`，`LOCAL-7-A/B` 仍 VERIFY。

- 本机 canonical worktree：`F:\AnimusForge-main`；分支 `refactor/prepare-af-restructure`。
- 已 fetch 的远端基线：`182da1db4db4199cf65783f911f3cb6d46b18970`，`origin/refactor/prepare-af-restructure`；`a096c1b1` 仅作历史比较点。下面的 F 盘机器记录保留为历史，不代表本机部署或最新远端。
- `G:\AFMOD\NEW-10` 保持 `0006d45b`，`G:\AFMOD\GCCZ` 保持 `3849f6f`；接手时两者工作区干净。其他机器是否有未提交或正在进行的工作未知；本轮只在独立本地分支工作，不推送。
- 当前状态：`LOCAL-7-A` VERIFY（核心基线通过，扩展回归有 4 个环境阻塞）；`LOCAL-7-B` VERIFY（源码、双版本构建与生产 DLL 回放通过，真实 Host 尚未验收）。本线程无继续执行中的写入；阶段 7 总体验收仍未完成。
- 已完成纵切片 `LOCAL-7-B` 的代码部分：Conversation Host 提交边界；源码提交 `b24fdf4b`。沿用现有 Gateway/owner/facade，没有切换默认三渠道。
- 计划路径：本台账、`AGENTS.md` 的本地回滚/边界说明；若复现提交边界缺口，限 `Refactor/Adapters`、`Refactor/Runtime`、现有 focused runner 和 owner 文档。不改存档 key/type、程序集身份、GCCZ 规则或构建/覆盖脚本。
- 本机 SDK 为 `8.0.422`；游戏根为 `E:\steam\steamapps\common\Mount & Blade II Bannerlord`。真实 Campaign/Mission 未初始化、未获游戏部署授权；live Economy/旧存档/AFEF 验收保持 NOT-RUN。仅用纯测试和 production-DLL replay 证明相应边界。
- 回滚：源代码基线 `182da1db`；编码前建立本地 checkpoint，后续按小提交反向回滚，不改写历史、不覆盖 NEW-10、游戏、ONNX 或玩家数据。
- `LOCAL-7-A` 基线进展：checkpoint `8020112e`；Debug 1.3/1.4/Bootstrap Stage 各 0 warning / 0 error，InteractionPipeline 40 cases、Economy port/executor、Configured Gateway、PersistenceChunk、Economy owner/state fixture 和三渠道 production configured host PASS。Persistence/Profile/Config 首次 FAIL：`_patienceStates_v1` 两个 ref 的 fixture 行号为 37172/37181，当前源码为 37010/37019；121 条绑定中仅这两处行号不同，未发现 key/type/ref/source 变化。只校正导航行号，保留完整严格校验。
- `LOCAL-7-B` 原意图（已实现，VERIFY）：owner 为 Conversation host lifecycle。复现并修复 `DetachedInteractionHost.ExecuteAsync` 在 commit 已开始后因 memory failure、afterCommit/dispatch exception 或缺失返回值再调用旧 fallback 的路径；新增已有 InteractionPipeline runner 内的故障注入回归，并检查三渠道 production-DLL 路径。先写失败测试再改 host，不改变经济玩法、save、标签或默认入口。真实 Host/AFEF 仍 NOT-RUN。
- `LOCAL-7-A` 核心基线 PASS：校正 fixture 后 Persistence/Profile/Config PASS（95 keys / 121 bindings / 8 types）；Production configured host、Economy-aware commit、Hero/Party/Merchant owner factory replay PASS。readiness 为 PASS，但 `gameRunning=false`、`installedMatchesStage=false`（该字段只比较 Bootstrap），不代表游戏已验收。固定引用：1.3 `v1.3.15.110062`，1.4 `v1.4.6.115628`；本机游戏 `v1.4.7.117484`。
- `LOCAL-7-B` 红绿测试：旧 source-linked host 的 memory/commit/dispatch/late-callback/cancel 用例失败；旧 staged 1.4 DLL 实测 `NativeConversation/memory_throw` 错走 fallback 并返回 Succeeded（`production-configured-boundary-red.log`）。修复源码后原 40-case suite + 新 48-case matrix PASS；重建 Debug 1.3/1.4/Bootstrap 各 0 warning / 0 error，新 staged 1.4 的三渠道 12 个提交后故障回放 PASS。保护范围为一次 `ExecuteAsync` 的提交回调，不承诺跨请求或跨存档的经济事务 exactly-once。
- 扩展回归执行失败（环境加载，不能记为 PASS）：PolicyGateway / WorldDiplomacyGateway 缺 `MCMv5, Version=5.12.3.0`；TtsGateway / ProductionOptInEntry 缺 `TaleWorlds.CampaignSystem`。四个 runner 的 `.csproj` 仍从 F 盘复制依赖；本轮未改这些工具或官方脚本。其余已执行的 Gateway/生产回放/十项 Python 审计结果见本机 handoff。
- 下一精确任务 `LOCAL-7-C` TODO：在测试工具 owner 范围内审查这四个 runner 的依赖复制，改为显式本机路径/固定版本引用并验证闭包，重跑失败项；不修改官方一键构建流程、不把游戏依赖打入客户端 Stage。随后审查跨请求经济 receipt/重复记忆提交和真实 Host/旧档证据。
- 完整命令、构建哈希、清理说明与回滚：`docs/handoffs/2026-08-31-local-refactor-commit-boundary.md`；本机日志 `G:\AFMOD\.build-cache\af-refactor-20260831`。本轮只修改 AF 基础提交边界与测试/文档，未改 GCCZ 核心/桥接，不需复制 AF 主体到 GCCZ；未推送、未部署、未安装全局 Skill。

## 历史状态（另一台机器，2026-08-30）

- 项目：Mount & Blade II: Bannerlord AnimusForge mod
- canonical worktree：`F:\AF测试重构`
- 当前分支：`refactor/prepare-af-restructure`（原重构仓库分支；本地项目目录为 `F:\AF测试重构`）
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`
- 基线父提交：`96a1c60f1877813a9fb3440ddad068d6e92afa1e`（policy 功能基线）
- 当前工作 HEAD：`b1ce1d2b`（`test: align live host readiness deployment state`；本地已提交，当前领先 origin 3 个提交，推送仍因 GitHub HTTPS 443 连接重置失败）
- 当前阶段：阶段 7 ACTIVE，进入真实 Host 验收准备；阶段 4/5/6 契约、生产回放和 Economy owner/state fixture 保持通过（阶段 1 清理 HOLD；阶段 3 设计已完成；阶段 0 基线详细记录按用户决定跳过）
- 当前任务：在真实初始化 Campaign/Mission Host 可用时，验证 live Economy、三渠道主线程 commit、confirmed facts、旧存档和 AFEF；纯 contract、生产 1.4 回放和状态 fixture 已完成
- 当前负责人：Codex 重构会话
- 物理程序集策略：暂不拆分为多个玩法 DLL；先在单一 `AnimusForge.dll` 内完成逻辑模块化
- 旧存档目标：必须兼容；至少保持现有程序集身份、序列化类型和 SyncData key，必要变更必须提供迁移与证据
- 游戏基线策略：保留可复现的测试记录，但不要求现在由用户立刻完成全量手测；优先记录关键功能和重构前后对比结果
- BannerlordRoot：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord`
- 已安装模块目录：`F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`
- 主要游戏内测试版本：Bannerlord `v1.4.8.119303`（本机当前安装）
- 1.4 构建引用要求：按 `1.4.x` API 线管理；每个开发者可以使用自己的合法 1.4.x 安装，但构建记录必须写明精确 `BuildInfo`，共享验收使用固定代表性 overlay
- 最后更新：2026-08-30（live host readiness、Git 与本地化审查）
- 状态：IN PROGRESS
- 最近验证：LiveHostReadiness 审计 PASS（project stage、安装模块、Bootstrap-only、1.3/1.4 stage 均存在且匹配）；此前 Economy-aware executor、owner/state fixture、Production 1.4 commit、World Diplomacy intent-boundary、三渠道 host 和 Gateway 回放均 PASS；1.3/1.4/Bootstrap Debug unified stage 均 `0 warning / 0 error`。真实 Campaign/Mission 仍未验收。
- 依赖记录：实际外部模块路径已解析；当前机器游戏 BuildInfo 为 `v1.4.8.119303`，可复现 1.4 overlay 为 `v1.4.6.115628`。
- 阶段 1 阻塞：用户已决定先保持仓库现状；1.3.x/1.4.x 游戏源码参考仓库保留在 tracked reference plane，其他未决对象也不做清理，许可证/第三方 provenance 继续作为待确认项。

## 重要工作区事实

- 工作区在准备开始时并非完全干净：`AnimusForge/SubModule.xml` 有用户已有修改（版本从 `v1.3.7` 变为 `v1.3.7.2`，且文件末尾换行发生变化）。本次准备不回滚、不覆盖该修改。
- 项目当前采用一个 `Modules/AnimusForge` 模块、Bootstrap 加载单一版本实现的发布契约。
- 当前主实现项目是 `AnimusForge.csproj`，Bootstrap 项目是 `AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj`。

## 阶段总览

### 阶段 0：准备与基线 — IN PROGRESS

- [x] 创建本地准备分支
- [x] 安装项目级 `animusforge-maintainer` skill（未提交）
- [x] 创建基线报告
- [x] 创建本公共重构台账
- [x] 审阅 skill、基线报告和本台账
- [x] 完成当前仓库只读结构盘点
- [x] 确认代表性存档和游戏内基线方案
- [x] 形成第一版功能—owner—依赖—风险重构地图（见 `docs/animusforge-refactor-map.md`）
- [x] 完成第一版逐文件 owner matrix（见 `docs/animusforge-owner-matrix.md`）
- [x] 记录可运行的 1.3.x、1.4.x、Bootstrap 构建结果（Debug unified stage；1.4 使用 v1.4.6.115628 overlay）

### 阶段 1：仓库边界与可重复性 — IN PROGRESS

- [x] 盘点源码、内容、测试、工具、脚本、文档、引用、依赖和产物平面（见 `docs/animusforge-repository-boundary-audit.md`）
- [x] 确认 `.gitignore` 与用户数据/生成产物边界（历史 tracked 生成物仍需后续分批处置）
- [x] 固化构建、stage、package、deploy 的现状说明（见 `docs/animusforge-repository-boundary-audit.md` 与 `README_BUILD.md`）
- [x] 建立初版仓库边界与分发决策表（见 `docs/animusforge-repository-boundary-decision-table.md`；法律/许可证确认仍未完成）
- [ ] 确认许可证、分发和第三方文件处理原则

### 阶段 2：模块目录与所有权地图 — DONE（设计完成；生产迁移未开始）

- [x] 建立现有功能 → 当前入口/文件 → 目标 owner 映射（首条根 AF 基础 LLM 对话只读切片见 `docs/animusforge-phase2-root-llm-owner-slice.md`）
- [x] 建立 `SubModule.cs` 注册/调度分组清单（只读；未改变注册顺序或运行行为）
- [x] 设计 Host/Composition registry DTO 与独立 contribution groups（只读；报告见 `docs/animusforge-phase2-registry-dto-design.md`；未接入运行时）
- [x] 建立纯 validator 输入/输出 fixture（见 `docs/animusforge-phase2-registry-validator-fixtures.md`；未实现 validator，运行频率 0）
- [x] 建立阶段 2 影响面、候选 Bridge、模块非目标与回滚入口地图（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；仅首轮高层设计）
- [x] 首轮标注存档、Prompt、标签、Harmony、Tick、UI、主线程和版本影响（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；逐文件细化已在 Conversation/Memory/Action 切片完成）
- [x] 首轮标注跨模块行为和候选 Bridge（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；contract test 已有独立 fixture/runner）
- [x] 为每个目标模块建立非目标和回滚入口模板（见 `docs/animusforge-phase2-impact-bridge-rollback-map.md`；具体切片填写留待生产迁移）
- [x] 建立首轮 Conversation/Memory/Action contract 边界逐文件影响表与纯 contract test matrix（见 `docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；仅设计，测试 NOT-RUN）
- [x] 将 contract matrix 映射到真实方法/调用点并建立独立纯 fixture 目录（见 `docs/animusforge-phase2-conversation-memory-action-method-map.md` 与 `docs/fixtures/phase2-conversation-memory-action/`；YAML parser NOT-RUN）
- [x] 细化 Settlement/Siege 与 Policy/Diplomacy 候选 Bridge contract（见 `docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`；仅设计，未实现）
- [x] 为两组 Bridge fixture 建立纯 contract 验证矩阵/runner（见 `tools/BridgeFixtureContractTests/`；不接入生产 `.csproj`）

### 阶段 3：Contracts 与基础运行时 — DONE（设计完成；生产实现未开始）

- [x] 定义模块身份、能力、事件、DTO、契约版本（见 `docs/animusforge-phase3-af-contracts-design.md`；未创建生产项目）
- [x] 设计模块 manifest、profile、依赖和健康状态（见 `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`；未实现 Foundation/Registry）
- [x] 整理 Foundation、主线程调度、后台任务、诊断和 SafeMode（见 `docs/animusforge-phase3-foundation-runtime-contracts.md`；未创建生产项目）
- [x] 整理 GameAdapter 与 1.3/1.4 API 边界（见 `docs/animusforge-phase3-game-adapter-api-boundary.md`；未修改生产 helper）
- [x] 为上述 catalog/contract 建立纯 metadata runner（见 `tools/ModuleCatalogContractTests/`、`tools/AFContractsContractTests/`、`tools/FoundationRuntimeContractTests/`；不接入生产 `.csproj`）
- [x] 设计 no-op module、dependency-missing、optional-provider、SafeMode 和 failure-isolation 纯组合矩阵（见 `docs/animusforge-phase3-composition-matrix.md` 与 `docs/fixtures/phase3-composition-matrix/`；18 cases、24 invariants）
- [x] 建立 GameAdapter API boundary 纯 fixture/runner（见 `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/`；14 cases）
- [x] 进行阶段 3 最终设计审查并确认进入阶段 4（见 `docs/animusforge-phase3-final-review.md`；PASS WITH LIMITATIONS）
### 阶段 4：Persistence / Profile / Config — IN PROGRESS

- [x] 完成 95 个字面量 `SyncData` key、主要 JSON 根和 PlayerExports 分类的首轮目录；符号 key/chunk/字典类型仍待补齐
- [x] 建立持久化 namespace 与迁移目录（9 个逻辑 namespace、schema/lifecycle/owner 和 legacy-first 幂等策略 fixture；运行时迁移尚未接入）
- [ ] 保留现有程序集/类型/key 兼容性
- [x] 建立配置快照、模块开关和 profile 解析边界（仅建立不可变契约；暂不接管 DuelSettings/MCM）

### 阶段 5：Conversation 统一交互管线 — IN PROGRESS

- [x] 建立三条旧入口的首轮 detached snapshot/history facade（不替换旧调用点）
- [ ] 统一场景喊话、自由对话、信使的快照/资格/Prompt/历史结构
- [ ] 统一后处理标签、动作执行入口和 AFEF 事实写入
- [ ] 保留旧入口作为 facade
- [ ] 验证三渠道规则和记忆一致性

### 阶段 6：Memory / Prompt / Action — IN PROGRESS

- [ ] 提取 Memory 与事实服务（当前切片 active：统一 detached Memory/AFEF commit facade）
- [ ] 提取 Prompt/Rule 与前后处理规则
- [ ] 建立统一动作解析、授权、当前状态验证、主线程执行和结果记录
- [ ] 先迁移低风险动作垂直切片（当前已完成协议解析/白名单切片，真实 Economy/Reward/Debt 验收待进行）

### 阶段 7：领域模块渐进迁移 — ACTIVE

- [ ] 接入共享 Configured Chat Gateway（领域切片已逐步接入；真实游戏内回放与默认路径切换仍待验证）
- [ ] 接入 Policy / WorldDiplomacy / Economy / Courier / Duel / WorldMap / Siege 等领域专用 provider，同时保留各自 JSON、重试、预算和降级语义
- [ ] 接入 Knowledge/RAG、周报、主动 NPC、辅助分类器和 TTS 的统一 capability/diagnostic 边界

建议顺序（以实际依赖盘点为准）：

1. Economy / Trade / Debt / Reward
2. Policy
3. Courier
4. Duel
5. WorldMap
6. Scene
7. Diplomacy
8. Siege / Battle
9. Knowledge / UI

每个领域都必须保持旧入口可用，完成调用方、存档、双版本、渠道、profile 和组合验证后才删除旧实现。

### 阶段 8：Bridge、旧结构清理与最终验收 — TODO

- [ ] 仅为确有跨模块所有权的行为建立 Bridge
- [ ] 验证 A、B、A+B、A+B+Bridge、Bridge 故障矩阵
- [ ] 清理 God Object、重复注册、旧 facade 和临时代码
- [ ] 验证 1.3、1.4、Bootstrap、stage、package、存档和游戏内场景
- [ ] 记录所有 NOT-RUN 与剩余风险

## 目标逻辑模块（第一版，非最终物理 DLL 方案）

- `AF.Contracts`
- `AF.Foundation.Runtime`
- `AF.GameAdapter`
- `AF.Persistence`
- `AF.Profile` / `AF.Config`
- `AF.Module.Conversation`
- `AF.Module.Memory`
- `AF.Module.Prompt`
- `AF.Module.Action`
- `AF.Module.Policy`
- `AF.Module.Economy`
- `AF.Module.Courier`
- `AF.Module.Duel`
- `AF.Module.WorldMap`
- `AF.Module.Scene`
- `AF.Module.Diplomacy`
- `AF.Module.Siege`
- `AF.Module.Knowledge`
- `AF.Module.UI`
- `AF.Bridge.*`

第一阶段优先建立逻辑边界和公共契约，不为了目录图强行拆成许多 DLL。发布契约仍是一个 Bootstrap 加载一个版本化 `AnimusForge.dll` 实现。

## 每个重构切片的必填记录

- owner：Foundation / GameAdapter / 单一 Module / 联合 Bridge
- 改动文件与公共契约
- 影响的渠道、profile、Bannerlord API 线、Harmony/Tick/UI
- 存档 namespace、key/type 和用户数据影响
- 运行频率、缓存、队列上限、主线程边界
- 验证命令和实际结果
- 回滚 commit 或旧 facade
- 下一步和阻塞项

## 状态规则

- `TODO`：尚未开始
- `IN PROGRESS`：正在处理
- `VERIFY`：实现完成但验收未完成
- `DONE`：验收证据完整
- `BLOCKED`：有明确阻塞原因
- `NOT-RUN`：检查未运行，必须写原因；不能当作通过

## 变更意图记录

| 时间 | 任务 | 范围 | 风险 | 验证 | 状态 |
|---|---|---|---|---|---|
| 2026-09-02 | PUBLISH-20260902：关闭自动化并GitHub交接 | PAUSE `af-7-8`；新增总HANDOFF，更新制作组简报/总纲/台账；普通push当前分支到`origin/refactor/prepare-af-restructure` | 只允许fast-forward；不force、不部署、不切default；发布动作DONE不等于阶段7/8 DONE | push前fetch/clean/ahead19 behind0；文档/fence/link/diff；push后HEAD==remote且ahead/behind 0/0 | DONE |
| 2026-09-02 | LOCAL-7-C2：ShoutNetwork SSE replay dependency closure | 基线 `8bf0c1e4`，checkpoint `28ad96f2`，实现 `ae49e3c8`；五consumer source contract；ShoutNetwork只导入既有ReplayDependencies targets；仅改runner/tool文档与测试 | 拒绝F盘硬编码、Modules/Workshop递归扫描、implementation复制与模糊同名覆盖；不改Program业务断言、生产C#、Stage/官方脚本、游戏；future stale output须先隔离 | 红测shared Import缺失；source 5/5、helper 9/9、missing-property gate、Shout Debug/Release业务回放、78项manifest/hash/identity与cleanup/diff PASS；LIVE/SAVE N/A | VERIFY |
| 2026-09-02 | LOCAL-7-C3：LiveHostReadinessAudit explicit-root portability | `tools/LiveHostReadinessAudit/` source/README/纯fixture CLI 测试；`--game-root` 显式必填，repo-derived project root 保留 | 删除 F 盘默认选择；工具只读，不启动游戏、不部署、不读取存档；不把 readiness 提升为 LIVE/SAVE | C3 `4/4 PASS`；Python compile、旧路径扫描、`git diff --check` PASS；真实 Campaign/Mission/LIVE/SAVE NOT-RUN | VERIFY |
| 2026-09-02 | Persistence/Profile/Identity scanner/catalog 离线收尾 | `tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`、`tools/PersistenceIdentityAudit.py`、phase4 persistence catalog | 排除生成物/缓存，跨 partial 常量解析；不改生产 SyncData/key/type、程序集、CampaignBehavior 注册或部署 | Profile/Config `95/121/8/3/44 PASS`；Identity `99/35/AnimusForge/Bootstrap-only PASS`；真实旧档/SAVE NOT-RUN | VERIFY |
| 2026-09-02 | 用户授权 Debug 双版本编译与统一模块测试部署 | 本地 `refactor/prepare-af-restructure` HEAD `109835cd`；`build_single_module.ps1 -Configuration Debug -Deploy`；目标 `F:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord\\Modules\\AnimusForge` | 仅 scoped Debug 测试安装；保留 Logs/PlayerExports/ONNX；不启动游戏、不切 default、不把安装视为 LIVE/SAVE 或 Release；后续 Stage 重建后实现 DLL 需重新部署 | 部署时 1.3/1.4/Bootstrap `0 warning / 0 error`、事务部署 exit 0、三 DLL hash 一致、readiness PASS、gameRunning=0、PlayerExports 4753 合并；当前安装实现 DLL 相对最新 Stage 已过时 | VERIFY |
| 2026-09-02 | Release Stage/Production Duel/ZIP 离线闭环 | `build_single_module.ps1 -Configuration Release -Stage`、Production Duel Release replay、Release ZIP/package validator | 仅项目内 Stage/ZIP；不安装 Release、不启动游戏、不读写存档、不把离线工件当发布许可 | 双 API/Bootstrap 0 warning / 0 error；Duel 35/35；ZIP 4919 entries/hash `1215A886...ACE5`、Bootstrap-only/marker/ONNX/旧模块校验 PASS | VERIFY |
| 2026-09-02 | BRIDGE-CONFIG：16 组 Bridge 绑定与安全 Gate（历史快照） | `docs/phase8/bridge-binding-manifest.json`、`tools/BridgeBindingContractTests/`、`Refactor/Contracts/FeatureBridgeContracts.cs`、`Refactor/Runtime/FeatureBridgeRuntime.cs`；当日仅接入 `AfGcczShoutBridge`、`WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved`、`SceneActionsIntegrationBoundary.InitializeRuntime` 三个既有入口 | 历史状态 `3 wired / 13 declared-only`；禁用/失败保留原版或 owner fallback；不新增 Tick 扫描、save key/type、网络或 live 对象跨边界，不改终端 UI | 历史 Bridge binding `16/3/13 PASS`，Debug 双 API/Bootstrap Stage `0 warning / 0 error`；LIVE/SAVE、默认切换、Release/最终发布仍 NOT-RUN/BLOCKED | VERIFY |
| 2026-09-03 | BRIDGE-CONFIG：Bridge 接线安全收尾 | `Refactor/Runtime/FeatureBridgeRuntime.cs`、已接线 adapters/behaviors、`AnimusForge/ModuleData/FeatureBridges.json`、`docs/phase8/bridge-binding-manifest.json`、`tools/BridgeBindingContractTests/` 与总纲/handoff 文档；10 组 source-bound Gate，6 组保留 declared-only | 配置缺失使用内建审阅默认值，配置损坏/未知/非规范大小写 ID fail-closed；保持 NoOp/native/owner fallback、无 Tick 扫描、无新增 save key/type、无 live 对象跨边界；不改终端 UI | Bridge validator `16/10/6`、Python 单测 `15/15`、PhaseEightReadiness `62/62`、BridgeFixture `10/6`、Composition `18/24`、ModuleCatalog `8/3/16/8`、Foundation `6/8/16`、GameAdapter `14`、Persistence/Profile `95/121/44`、LiveHostReadiness PASS；Interaction/Duel/Economy/Gateway/Knowledge/Production suites 与双 API Debug/Release/Bootstrap Stage 均 PASS；真实 Campaign/Mission、LIVE/SAVE、默认切换和发布仍 NOT-RUN/BLOCKED | VERIFY |
| 2026-09-03 | OFFLINE-GAP-20260903：离线缺口全量修复（当前切片） | Bridge validator 真实方法体/顺序负例；纯 net8 Bridge runtime isolation runner；Phase8 入口候选生成器与 catalog `entryPaths`；PersistenceIdentityAudit 单快照/batch/progress/quiet；ModelCatalog stable error code/参数/中英文 formatter 与 UI 映射；新增契约/回放测试 | 仅工作区内可逆离线变更；不启动游戏、不读写真实存档、不部署、不切换默认、不删 facade、不改模块发布结构/程序集/SubModule/SyncData key/type/构建脚本；临时 Stage 产物留在忽略目录 | Bridge 20/20、隔离 9 场景、Phase8 68/68、Persistence 5/5、ModelCatalog replay；真实审计对 partial-clone 缺 89 个 blob fail-closed；all-missing readiness 保持 BLOCKED/exit 2；Debug/Release 双 API/Bootstrap Stage 均 0 warning / 0 error；提交 `01e7bc1`→`552d8b9`→`f1a17f7`→`4feac3c`→`4a8e929`→`8f12298`→`cebac17`→`aad83c3`→`ab6ce72` | VERIFY |
| 2026-09-02 | LOCAL-7-M2：Duel exact detached dispatch provenance | 基线 `3522dc3e`，checkpoint `17f617a5`，实现 `b93f93df`；internal request-bound executor、pre-effect owner Queue、显式 dispatch context、Native/Scene delayed holder、Courier reject、exact request readback；仅改 AF-REFACTOR | 不用 ambient/subject-latest；Queue 必须先于 Economy；同 context 显式转移到 actual start；Queued/Started/Unknown non-retryable；不改 public executor/Prepare/commit constructor ABI、default/save/Fourberie/M1 legacy-unbound | 红测requestId断链；Dispatch16/16、Outcome18/18、focused/三渠道fresh production、Persistence/Profile/Migration/Identity、Production Duel Debug/Release各35与Debug/Release六Stage PASS；实机/旧档/live Economy/AFEF NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-M1：Duel actual-session typed owner/outcome/readback | 基线 `9955658b`，checkpoint `fc3cd722`，实现 `16f3cbef`；pure bounded receipt owner、三 actual-start/terminal seam、ResultIdentity-first、component effects、exact DuelId artifact binding、additive readback | process-local/NOT-RECOVERABLE；legacy dispatch返回`UnknownAfterStart / duel.outcome_pending`而非玩法成功；当前live provenance仅`Domain / legacy-unbound`，M2补exact request绑定；不重放Mission/death/Economy，不改public void ABI、cooldown key/type、Fourberie/default/save identity | 红测旧void/aggregate/hero-only gap；Duel 16/16、Interaction/Host/receipt、fresh production Duel Debug/Release各32、Persistence/Profile/Migration/Identity与Debug/Release六Stage PASS；真实Campaign/Mission/旧档/live death/stake/Economy/AFEF/Fourberie NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-K：weekly exact-intent/outcome owner | Economy-only candidate/actual fingerprints、`InteractionResultCommitter`终态 gate、独立 `AFWM1` ledger、MyBehavior Confirmed-only Daily attach、additive symbolic SyncData与 focused/production fixture；基线 `da15241f`，checkpoint `7cdf6435`，实现 `765b2386` | 只接受 whole-plan owner full Applied/count/effect+memory；canonical sidecar不调用injected planner；有效 journal的durable identity在live payload前防重；Prepared load→Unknown，坏 journal保留并禁用且不再提供K防重；不存raw/action/callback，不改H/I wire/public ABI/default | red CS2001→weekly/economy/Interaction40+Host69+receipt39/Memory/Courier/Economy Port及8 production/compiled回放 PASS；Profile 95/121/42 symbolic/40 flattened、Migration10/corrupt2、Identity99/35 PASS；Debug/Release六Stage 0 warning/error；终审P0/P1=0；live Campaign/save/Economy/AFEF NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-J：memory auxiliary recovery boundary | `MyBehavior.MemoryRecovery.cs` 与 ProductionOptIn compiled-DLL guard；基线 `d2f37a8a`，checkpoint `3436d739`，实现 `84e92f80` | H 仅修 Daily/Recent；不读/附着/删除无 exact identity 的 weekly candidate；Notoriety仅 brand-new同步完成+exact marker后各part一次 attempted-unconfirmed，异常不污染core；legacy/H/I key/hash/wire不变 | Memory/Interaction69/receipt39/Courier/Economy/Production Host/OptIn/Profile/Identity PASS；Debug/Release六Stage 0 warning/error；独立审查本diff P0/P1=0；weekly exact-intent/detached owner NOT-IMPLEMENTED，live Notoriety/save NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-I：Courier inbound durable session completion | Courier-owned batch memory wrapper、`AFCI1` receipt、MyBehavior recovery identity/status query、load/delivery gate、one-per-tick scheduler；基线 `0e276ce1`，checkpoint `b5395164`，实现 `de3220b7` | receipt 先于 memory owner；绑定 owner payload hash防同 CommitId 冲突；invalid/quarantine/pre-owner/no-receipt均abort并解锁；不重放Action/Economy，不复用`PostprocessConsumed`；默认 caller不切 | contract含arm-before-owner/inner throw/5 outcomes/32k Unicode；production receipt/load/Applied恢复/fail-closed/tick one；Interaction69/receipt39、Memory、三Host、Economy、95/121 profile、99/35 identity、Debug/Release六Stage PASS；live NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-H：memory-only persistent recovery | MyBehavior additive journal/marker owner、逐组件 idempotent repair、SyncData、focused/persistence/production replay；基线 `a8001b87`，checkpoint `6f8d8cc0`，实现 `f6e5e694` | payload 无 ActionPlan/afterCommit/raw postprocess；opaque id+nonce+checksum、frozen provenance、64 pending/512 tombstone/64 quarantine、单步骤五次失败后隔离；坏 schema/hash/marker/超限 fail-closed；每 tick 最多一组件；旧 public ABI/default/identity不变 | 最多六步/12 fault、restart/corrupt/long Courier/nonhero、Production 3498、Interaction/Host/Economy、39-flat/95-key/121-binding、99-sync/35-behavior、Debug/Release六Stage PASS；live AFEF/旧档 NOT-RUN；Courier afterCommit另列 I | VERIFY |
| 2026-09-01 | LOCAL-7-G：UnknownAfterStart effect state / terminal receipt | Economy status/effect contracts、Hero/Party/Merchant replay-aware mutation observation、port、executor、committer、Host/cache 与 focused/production replay；checkpoint `899effbb`，实现 `d765270a` | post-callback malformed receipt 不可信且清空 count/facts；unknown action 不造 fact、不 fallback/afterCommit/重放；旧 helper ABI/save identity不变；无补偿、durable recovery、默认切换 | Port unknown 8、Executor unknown 3/receipt 4、Host 69×三渠道、receipt 39、Production Economy/owner/Host/OptIn PASS；Debug/Release 六项 Stage、profile/identity PASS；独立终审无 P0/P1；live NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-F：known partial Economy outcome/facts/terminal receipt | Economy contract、Hero/Party/Merchant owners、port、executor、committer 与 focused/production replay；checkpoint `7186048d`，实现 `8f22d737` | 仅 owner count/facts；不推断 legacy；memory fail 不重放 action；enum尾增/接口additive；无 save key/type；UnknownAfterStart 和跨重启 durable recovery未解决 | partial direct/commit/duplicate/memory fail、mixed reject/throw、Host 51、Port normalization/ABI、Production partial 2 PASS；双配置六项 Stage、profile/identity PASS；live NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-E：Courier Economy owner 前置 gate 与 economy-only 持久消费 | `LegacyNativeActionPlanExecutor`、`CourierDeliveryBehavior`、Economy-aware/Production replay 与 owner 文档；checkpoint `bbe35aa8`，实现 `b2542fdd` | 复用 `_af_courier_sessions_v1` 内既有 `PostprocessConsumed`，无新 key/type/field；reservation 后失败不可自动重试；mixed 后半仍可 partial；opt-in 无默认生产调用者 | gate ordering/fault 5、production Courier session 18 assertions、Production Economy/Courier/Configured、Interaction 和 Economy port PASS；Debug/Release 六项 Stage 0 warning/error；profile/identity PASS；实机/save NOT-RUN | VERIFY |
| 2026-09-01 | LOCAL-7-D：Memory owner runtime 回执 | `MyBehavior.cs`、`LegacyInteractionSnapshotAdapters.cs`、ProductionOptIn replay、owner/receipt 文档、SyncData 行号 fixture；先记录意图并 checkpoint `3fe3f656`，再红测与最小替换 | 无新 save key/type、无新管线或默认切换；公开 void 兼容保留；Failed 可能已有部分写入，禁止推断安全重试/回滚；真实游戏仍未授权部署 | production 缺 owner/线程 fixture/原始读回 PASS；Interaction、三 Host、两 Economy runner PASS；Debug/Release 六项 Stage 0 warning/error；95-key/121-binding profile 和 99-sync/35-behavior identity PASS；cleanup/diff 和独立审查通过；实机/旧档 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 7 Policy EventAndRebellion Gateway 取消传播（当前切片） | `PolicySystem/Npc/PolicyLlmClient.cs`、`Refactor/Adapters/LegacyPolicyLlmGateway.cs`、`tools/PolicyGatewayReplayTests/`；为旧 EventAndRebellion 重试入口增加可选 `CancellationToken` 并从共享 Gateway 贯穿到 HTTP/backoff，新增本地可控 provider 回放 | 保持既有 route/profile/JSON、thinking/兼容降级、重试、stale、Policy/王国创建主线程边界和存档 key/type；凭据仅留在旧 profile 发送边界；不切换三渠道，不改构建/部署脚本 | `dotnet run --project tools/PolicyGatewayReplayTests/PolicyGatewayReplayTests.csproj`：`PASS policyGatewayReplay callerCancellation=1 retryDelayCancellation=1 timeoutIsolation=1 credentialBoundary=1`；Debug/Release 1.3/1.4/Bootstrap unified stage 均 `0 warning / 0 error`；真实 Policy provider、旧存档和游戏内回放 `NOT-RUN`；未部署 | VERIFY |
| 2026-08-30 | 修正 Policy Gateway 重构后的契约测试断言（当前切片） | `tools/PolicyEffectModule.ContractTests/Program.cs`；将“必须在调用方显式出现旧三次重试调用”的静态断言改为同时接受共享 `LegacyPolicyLlmGateway.GenerateAsync` 路径，并继续要求旧路径具备有界重试 | 仅修正测试对已登记 Gateway 重构的表达，不放宽生产重试/失败语义，不改变存档、配置、默认入口或构建脚本 | 1.4 pinned reference 下完整 Policy contract runner：`PASS assertions=9031 modules=18 syntheticDescriptors=64 activeContributions=100`；Policy Gateway replay 已通过；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 6 Memory/AFEF receipt 失败可重试边界（当前切片） | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`tools/InteractionPipelineContractTests/`；将 detached memory commit 的 receipt 登记从 legacy history/AFEF 写入前移到成功写入后，避免写入异常后重试被错误抑制 | 保持旧 `MyBehavior` history/AFEF owner、SyncData key/type、user/assistant 语义和主线程边界；receipt 仍为有界进程内运行时数据，不进入存档；不改变默认三渠道路径 | InteractionPipeline contract `40 cases PASS`；Policy Gateway replay PASS；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实旧存档和游戏内 memory 回放 `NOT-RUN`；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 辅助规则路由接入共享 Gateway（当前切片） | `AIConfigHandler.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将辅助规则路由的固定 system/user prompt 经共享 Gateway 发送，并保留 auxiliary thinking 控制、400 plain retry、配置化 token/temperature 和规则 owner 的解析/重试 | API key 只在发送边界；响应正文不进入共享 DTO；不改变规则资格、提及实体发布、格式重试、fallback 或三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实辅助路由 HTTP和游戏内规则回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 AI 错误分析与 XihaiAction 辅助分类器接入共享 Gateway（当前切片） | `AiErrorAnalysisInquiry.cs`、`extensions/AnimusForge.XihaiAction/src/Runtime/AfV130ConfiguredGatewayTransport.cs`、`extensions/AnimusForge.XihaiAction/src/Runtime/AfCompatV130.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；错误分析和 SceneActions/Consent/BattleSpeech 分类器通过共享 Gateway 发送，保留原反射 transport fallback、闭集解析和生命周期 | API key 只在发送边界 resolver 闭包；分类器仍由原 single-flight/battle-speech flight 控制；缺少辅助配置回退原 transport；不改变动作白名单、consent、存档或默认三渠道 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；SceneActions Core `88 passed / 0 failed`；Static Verifier `13 passed / 0 failed`；真实 HTTP、取消和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 周报 Event/WeeklyReport 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将周报批量/单组/完整周报/第 0 周短摘要的冻结 system/user prompt 通过共享 Gateway 发送，保留周报 owner 的批量、解析、重试、降级、限速和主线程写入 | 仅在显式周报调用点启用；EventAndRebellion route 的 URL/model/key、thinking/plain retry 和配置化 token/temperature 仍由旧配置 owner 解析；响应正文不进入共享 DTO；不切换三渠道或周报默认调度 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实周报 HTTP、旧存档和游戏内周报回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Memory 压缩/重大履历/Memory Overview 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将三个后台非流式摘要入口的最终 system/user prompt 复制为 immutable `PromptPackage`，通过 Auxiliary route Gateway 发送，保留 owner 的资格、重试、标签/JSON 解析、过期检查和主线程提交 | API key 只在发送边界；force-thinking-disabled、配置化 token/temperature、失败降级和原有内存/AFEF 存储语义保持；后台不携带 Hero/live 对象；不切换三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Memory HTTP、旧存档和游戏内摘要回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 主动 NPC LLM owner-only 审查（当前切片） | `ProactiveNpcRequestBehavior.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、`MyBehavior.cs`；核对主动 NPC 的候选扫描/需求判定/开场 prompt 与三渠道、Memory owner 的边界，确认不存在独立 HTTP/LLM transport | 主动 NPC 只负责低频增量候选扫描和状态机；开场生成复用 Native/Scene/Courier facade，摘要复用 Memory owner；不新增平行请求、规则或存档字段；默认路径不变 | 静态调用图核对完成；无独立 transport 可迁移；真实主动 NPC、三渠道和旧存档回放仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Persona/升格同伴人设与技能接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将 NPC Persona、升格同伴 Persona、升格同伴技能的最终 system/user prompt 通过 Auxiliary Gateway 发送，保留 JSON 解析、fallback、stale 和主线程存储 | API key 只在发送边界；保留 Auxiliary URL/model、thinking/plain retry、配置化 token/temperature、原有资格与失败语义；Gateway 不携带 Hero/live 对象；不改变 Persona/技能存档结构或三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Persona HTTP、旧存档和游戏内生成回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 叛乱王国命名 EventAndRebellion 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将叛乱建国命名的冻结 system/user prompt 通过 EventAndRebellion Gateway 发送，保留 60 秒超时、三次重试、格式/重名校验和王国创建主线程边界 | API key 只在发送边界；保留专用 URL/model、thinking/plain retry、配置化 token/temperature、命名失败中止和原有重试/限流语义；不改变王国/存档结构或默认三渠道路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实命名 HTTP、旧存档和游戏内叛乱回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 外部 Auxiliary API facade / PlayerNotoriety 摘要接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`PlayerNotorietyBehavior.cs`；将 `CallAuxiliaryApiTextForExternal` 及其 PlayerNotoriety 摘要调用转到统一 Auxiliary Gateway，保留非阻塞/失败弹窗、force-thinking-disabled、原摘要解析和过期语义 | API key 只在发送边界；不改变外部 facade 的返回契约、PlayerNotoriety 存储或三渠道默认路径；Gateway 不携带 Hero/live 对象 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 PlayerNotoriety HTTP、旧存档和游戏内摘要回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 AIConfigHandler ACTION 后处理与 Auxiliary Simple Dialogue 接入共享 Configured Chat Gateway（当前切片） | `AIConfigHandler.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；统一动作后处理和简单辅助对话的标准请求、鉴权、超时、thinking/plain retry 与 assistant extraction，保留原调用方的阻塞重试、协议解析和 fallback | API key 只在发送边界；动作标签闭集、响应格式校验、历史/AFEF、领域资格和默认三渠道保持；响应正文不进入共享 DTO；不改变 ActionPostprocess/Auxiliary 配置来源 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实动作后处理/辅助对话 HTTP、旧存档和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 外部 Auxiliary API facade / PlayerNotoriety 摘要接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`PlayerNotorietyBehavior.cs`；将 `CallAuxiliaryApiTextForExternal` 及其 PlayerNotoriety 摘要调用转到统一 Auxiliary Gateway，保留非阻塞/失败弹窗、force-thinking-disabled、原摘要解析和过期语义 | API key 只在发送边界；不改变外部 facade 的返回契约、PlayerNotoriety 存储或三渠道默认路径；Gateway 不携带 Hero/live 对象 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 PlayerNotoriety HTTP、旧存档和游戏内摘要回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 叛乱王国命名 EventAndRebellion 接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将叛乱建国命名的冻结 system/user prompt 通过 EventAndRebellion Gateway 发送，保留 60 秒超时、三次重试、格式/重名校验和王国创建主线程边界 | API key 只在发送边界；保留专用 URL/model、thinking/plain retry、配置化 token/temperature、命名失败中止和原有重试/限流语义；不改变王国/存档结构或默认三渠道路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实命名 HTTP、旧存档和游戏内叛乱回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Persona/升格同伴人设与技能接入共享 Configured Chat Gateway（当前切片） | `MyBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将 NPC Persona、升格同伴 Persona、升格同伴技能的最终 system/user prompt 通过 Auxiliary Gateway 发送，保留 JSON 解析、fallback、stale 和主线程存储 | API key 只在发送边界；保留 Auxiliary URL/model、thinking/plain retry、配置化 token/temperature、原有资格与失败语义；Gateway 不携带 Hero/live 对象；不改变 Persona/技能存档结构或三渠道默认路径 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；XihaiAction Core `88 passed / 0 failed`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Persona HTTP、旧存档和游戏内生成回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 AI 错误分析接入共享 Gateway（当前切片） | `AiErrorAnalysisInquiry.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将现有辅助 API 的固定 system/user prompt 复制为 immutable `PromptPackage`，通过共享 Gateway 发送，保留错误分析结果展示、超时和失败回调 | API key 仅在发送边界 resolver 闭包内；不改变错误详情脱敏、辅助 API 配置、60 秒用户可见超时语义或非阻塞展示；响应正文不进入共享 DTO/日志 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实错误分析 HTTP 和游戏内弹窗 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Knowledge/RAG 短句生成接入共享 Gateway（当前切片） | `KnowledgeLibraryBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将 RAG 专用短句的既有 system/user prompt 复制为 immutable `PromptPackage`，经共享 OpenAI-compatible Gateway 发送，再由知识 owner 继续解析和确定性降级 | API key 仅在发送边界 resolver 闭包内；不改变知识文件、存档 key/type、Prompt 内容、解析/fallback、UI 阻塞时序或默认三渠道；该 UI 操作仍为单次同步调用，不进入 Tick | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；此前 7 个阶段 Python runner、InteractionPipeline `40 cases PASS`、GiveAssetTagCodec `80557 assertions PASS`；真实 RAG HTTP、知识文件写入和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Policy 实际调用方接入共享 Gateway（当前切片） | `Refactor/Adapters/LegacyPolicyLlmGateway.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs`、`PolicySystem/Core/CustomPolicyBehavior.Generation.cs`、`KingdomStrategicProfileBehavior.cs`；将 NPC ruler draft/effect/repair、玩家政策 main/postprocess/repair、王国战略建国卡的既有调用方接入统一 `ILlmGateway` 契约，保留原 profile/JSON/重试/兼容降级与测试 override | Gateway 只复制字符串 role/content，凭据留在旧 domain client；不改变 Policy 资格、存档 key/type、同步字段、主线程提交或默认三渠道；后台不携带 live 游戏对象；不引入新的玩法动作 | 7 个阶段 Python runner PASS；InteractionPipeline `40 cases PASS`；GiveAssetTagCodec `80557 assertions PASS`；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Policy HTTP、旧存档和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Town Ambient 接入共享 Configured Chat Gateway（当前切片） | `Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`TownAmbientAiClient.cs`、`docs/animusforge-phase7-domain-gateway-boundary.md`；保留 Town Ambient 的开关、预算、缓存、多人 JSON 解析和纯文本降级，将标准 OpenAI-compatible HTTP/鉴权/超时/取消/assistant extraction 统一到 Gateway | 凭据只由入口在发送边界解析，不进入 contract/snapshot/log/save；不改变默认三渠道、TTS、场景动作或 Town Ambient 开关；gateway 仅处理字符串 Prompt，不解析/执行游戏对象 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline 仍 `39 cases PASS`；真实 Town Ambient HTTP/游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 World Diplomacy 接入共享 Gateway contract（当前切片） | `Refactor/Contracts/LlmContracts.cs`、`Refactor/Adapters/LegacyWorldDiplomacyLlmGateway.cs`、`WorldDiplomacyBehavior.cs`；排队发送点先冻结 JArray 为 PromptPackage，再经 Gateway adapter 调用既有 WorldDiplomacy provider/client，保留领域重试、thinking fallback、stale、token/cache/truncation metadata | 领域 client 仍是 route/credential/retry authority；共享 DTO 不携带 key/response body/live 对象；当前 adapter 的取消受旧 client 边界限制；默认外交行为和存档结构不变 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline `40 cases PASS`；真实 World Diplomacy HTTP、读档和游戏回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Policy Gateway adapter 与 Persona Gateway 接入（当前切片） | `Refactor/Adapters/LegacyPolicyLlmGateway.cs`、`ShoutUtils.cs`；为 NPC Policy/事件叛乱保留旧 profile/JSON/重试 authority，并将无名 NPC Persona 的 ShoutNetwork 调用通过统一 Legacy Gateway；Persona JSON/存储仍由旧 owner 负责 | 不改变 Policy/Persona 现有资格、存储、配置或默认入口；Policy adapter 尚为 opt-in contract，Persona 仅切换既有 ShoutNetwork 传输包装；API key 不进入公共 DTO | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline `40 cases PASS`；真实 Policy/Persona HTTP 和游戏回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 Dedicated TTS 接入 Gateway contract（当前切片） | `Refactor/Contracts/TtsContracts.cs`、`Refactor/Adapters/LegacyVolcTtsGateway.cs`、`TtsEngine.cs`；将火山 V1 请求 payload、鉴权 header、响应 code/base64 音频解码纳入 Gateway，保留 TtsEngine 的开关、队列、音频解析、播放和失败回调 | Token 仅作为发送边界参数；不进入 TTS contract、存档或普通日志；保留旧 V1 header 映射和 code=3000 语义；真实语音服务和游戏内播放仍未验证 | InteractionPipeline `40 cases PASS`（含 TTS bytes detach）；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 直接 LLM transport 盘点（当前切片） | `MyBehavior.cs`、`PolicySystem/Npc/PolicyLlmClient.cs`、`WorldDiplomacyLlmClient.cs`、`TownAmbientAiClient.cs`、`DuelSettings.cs`、`ModOnboardingBehavior.cs`、`ShoutNetwork.cs`；逐项标记共享 Gateway 已覆盖、legacy 主链路 facade、配置/向导连通性验证和无调用 dead path | 不删除旧私有入口；不切换三渠道；不把 API key/响应正文带入公共 contract；仅对确认存在且不改变流式/配置验证语义的运行时入口安排后续切片 | 静态扫描完成：Policy/World/Town/TTS/AIConfigHandler 新辅助路径已有 Gateway adapter；Scene/Native/Courier 主链路保留 legacy facade；`CallUniversalApiDetailed` 无生产调用者；DuelSettings/ModOnboarding 为用户主动配置验证；真实 HTTP、旧存档和游戏内回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 ShoutNetwork 流式 Gateway 契约（当前切片） | `Refactor/Contracts/LlmContracts.cs`、`Refactor/Contracts/LegacyShoutNetworkGateway.cs`；为既有 ShoutNetwork SSE 主回复增加 `ILlmStreamingGateway` opt-in 契约，分离增量回调与最终结果，保留旧动态名称过滤、重试、stale 和取消处理 | 仅支持 `MainReply`；`Postprocess` 明确拒绝；不改变默认 Scene/Native/Courier 调用点，不重复提交 onDelta/onComplete，不携带 live 对象、凭据或响应正文进入公共 contract | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；InteractionPipeline `40 cases PASS`；7 个 Python runner、XihaiAction Core `88 passed / 0 failed`、GiveAssetTagCodec `80557 assertions PASS`；`ConfiguredChatGatewayReplayTests` 本地 HTTP 回放 PASS（success/thinking retry/5xx/cancellation）；真实 ShoutNetwork SSE、取消/stale 时序、旧存档和游戏内回放仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | 全量现有 LLM 重构启动（阶段 4 → 阶段 6/7） | `F:\AF测试重构` 独立工作树；全量现有 LLM 调用、三渠道、Memory/AFEF、Prompt/Action、领域适配、TTS/辅助模型；先做 Persistence/Profile/Config 和公共契约，最终一次性切换默认路径 | 保持单一 `AnimusForge` 发布模块、Bootstrap、程序集/序列化类型/SyncData key、旧入口 facade、三渠道、主线程和 1.3/1.4；不新增玩法、不删除用户/参考/生成物、不立即拆物理 DLL | 首批验证台账一致性、key/type/JSON/fixture；后续必须通过契约、组合、1.3/1.4/Bootstrap、stage/package、旧存档和游戏内分渠道/领域验收 | ACTIVE |
| 2026-08-30 | 阶段 6 Memory/AFEF 统一 batch commit facade（当前切片） | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Runtime/InteractionResultCommitter.cs`、`Refactor/Runtime/MemoryCommitReceiptCache.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`tools/InteractionPipelineContractTests/`、`docs/animusforge-phase6-memory-afef-commit-boundary.md`；以一次性 user/assistant/facts commit 接入旧 MyBehavior，保留旧 Append fallback | 不改变 MyBehavior 的既有存储格式、SyncData key/type、AFEF 文本协议、默认三渠道入口或 Courier 时序；receipt 仅内存有界缓存；动作拒绝/stale/cancel 不写 confirmed AFEF；后台不持有 live 对象 | InteractionPipeline `40 cases PASS`；统一 `build_single_module.ps1 -Stage` 的 1.3/1.4/Bootstrap 均 `0 warning / 0 error`；unified stage 成功且未部署；真实三渠道、旧存档、网络和游戏内写入仍 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 6 Action 协议平衡解析与 Economy/Reward/Debt 有限白名单（当前切片） | `Refactor/Adapters/LegacyActionTagParser.cs`、`Refactor/Adapters/LegacyActionTagCatalog.cs`、`Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、`tools/InteractionPipelineContractTests/`、`docs/animusforge-phase6-action-protocol-and-economy-boundary.md`；修复嵌套/冒号资产 token，并将 detached executor 从 ACTION:* 收敛到既有有限协议目录 | 不改变 RewardSystemBehavior 及领域 owner 的主线程资格/资产/债务校验；未授权协议拒绝；保留 GCCZ 数字动作、旧 A/AD/ADP/ATT/ATP 协议；不改变默认入口、存档、SyncData、程序集或发布结构 | InteractionPipeline `40 cases PASS`；GiveAssetTagCodec `80557 assertions PASS`（20,000 fuzz/25,000 pressure tags）；1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；未部署；真实三渠道 Economy/Reward/Debt、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 4 Persistence/Profile/Config 首轮目录与纯 runner | `docs/animusforge-phase4-persistence-profile-config-catalog.md`、`docs/fixtures/phase4-persistence-profile-config/`、`tools/PersistenceProfileConfigContractTests/`；95 个字面量 SyncData key、17 个 owner 文件、40 个符号 SyncData 来源、PlayerExports 分类、9 个 persistence namespace、3 个 profile 和 5 个配置快照案例 | 只读生产源码；不改变 key/type、存档程序集、配置运行时、生产 C#、项目/脚本、SubModule 或游戏目录 | `python tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`（普通与 `--json`）PASS：`literalKeys=95 sourceFiles=17 symbolicSources=40 symbolicFamilies=4 profiles=3 cases=5 credentialFieldsExcluded=5 namespaces=9 migrationIdempotent=True unknownDataPreserved=True`；`git diff --check` PASS；生产/构建禁止路径 NONE；chunk/字典字段、真实存档和游戏验证 NOT-RUN | VERIFY |
| 2026-08-30 | AF Contracts 第一版生产边界 | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Contracts/LlmContracts.cs`、`docs/animusforge-phase4-llm-contract-boundary.md`；定义三渠道快照、Prompt/Action/Result/Trace 和 LLM Gateway 输入输出，不接入旧 Behavior | 新类型不得携带 TaleWorlds live 对象、API key、可变全局配置或私有模块类型；保持单一程序集、旧 facade、主线程复核、1.3/1.4 common surface | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；纯 Persistence runner PASS；静态检查无禁止路径；接入、旧存档、游戏内和网络验证 NOT-RUN | VERIFY |
| 2026-08-30 | Legacy ShoutNetwork LLM Gateway adapter | `Refactor/Contracts/LegacyShoutNetworkGateway.cs`；将不可变 Prompt contract 适配到现有 `ShoutNetwork`，保留旧重试、generation、DuelSettings 配置和错误文本；不替换调用点 | 过渡期 provider snapshot 仍由旧 DuelSettings 实际解析；不得把新 endpoint/model 宣称已生效；不得携带 live 游戏对象或凭据进入 DTO | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；网络真实调用、三渠道接入、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Shared InteractionPipeline contract/runtime seam | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Contracts/InteractionPipeline.cs`、`docs/animusforge-interaction-pipeline-boundary.md`；定义规则选择、Prompt 组装、LLM 生成、可见文本规范化、ActionPlan 输出的共享顺序；不执行动作、不写存档、不接旧渠道 | 后台只接收不可变 envelope/provider snapshot；动作和 AFEF 必须由主线程 facade 后续处理；旧三渠道、旧 tag/parser、存档 key/type、1.3/1.4 不得改变 | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；纯 Persistence runner PASS；真实 provider、三渠道接入、Action 执行、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | InteractionPipeline fake contract tests | `tools/InteractionPipelineContractTests/`；以 fake selector/composer/gateway/normalizer/postprocessor 验证顺序、无资格跳过、stale/cancel 映射、不可变 snapshot、三阶段 RAW/FINAL、后处理隔离、ActionPlan 提交和 Native facade；不引用 Bannerlord 或生产程序集 | 测试不能证明真实 Harmony/HTTP/主线程/存档行为；不得把 fake 通过当作三渠道接入完成 | `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；真实 provider、三渠道接入、Action 执行、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | RuntimeConfigSnapshot 第一版 | `Refactor/Contracts/ProfileConfigContracts.cs`、`docs/animusforge-phase4-config-snapshot-boundary.md`；定义 profile、模块开关、provider 元数据和请求 generation 的不可变配置快照；暂不接管 DuelSettings/MCM | 快照不含 API key、凭据或 live 游戏对象；reload 只影响未来请求；需要存档/Harmony/CampaignBehavior 的模块仍非 runtime-toggle-safe；不改变现有配置读取和存档 | 1.3/1.4/Bootstrap/unified stage 均 0 警告、0 错误；snapshot smoke PASS（由 InteractionPipeline runner 的 `configReloadIsolation=true` 覆盖）；真实 MCM reload、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道旧入口 detached adapter 首轮 | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`docs/animusforge-phase5-legacy-facade-adapters.md`；从旧 ShoutBehavior/MyBehavior/CourierDeliveryBehavior 公共 facade 捕获不可变 snapshot/history，先不替换生产调用点 | 只在主线程捕获 live 游戏对象并复制字符串/ID；后台不得持有 Hero/Agent/Campaign/Session；不改变旧 Prompt、Action、AFEF、SyncData、配置 authority 或 courier 时序；适配层异常必须返回空/降级快照而不影响旧链路 | `git diff --check` PASS；InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；Persistence/Profile/Config runner PASS：`literalKeys=95 sourceFiles=17 symbolicSources=40 symbolicFamilies=4 profiles=3 cases=5 credentialFieldsExcluded=5`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS，含 `versions/1.3` 与 `versions/1.4`；三渠道真实接管、网络、存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 统一交互请求协调器 | `Refactor/Runtime/InteractionRequestCoordinator.cs`；在公共管线外层统一 provider 解析、模块开关、同会话替换取消、外部取消和读档 generation/stale 复核；不接入旧调用点 | 只接收不可变 `InteractionEnvelope`/`RuntimeConfigSnapshot`；不得持有 live 游戏对象、凭据或可变配置；请求结束后必须释放 CTS；不得把取消、stale 或配置缺失变成动作执行；旧三渠道和旧 gateway 行为保持不变 | `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实网络、旧存档、三渠道接管和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 旧规则/Prompt/Action 共享组合根 | `Refactor/Adapters/LegacyInteractionPipelineComposition.cs`、`docs/animusforge-phase5-interaction-request-coordinator.md`；以显式 ports 连接旧规则选择、Prompt 合成、后处理上下文、标签解析和可见文本规范化到兼容单阶段或三阶段 `IInteractionPipeline`，不复制规则、不接入调用点 | ports 不得携带 live 游戏对象或私有模块状态；具体渠道必须继续保留自己的资格和主线程复核；组合根不可单独宣称三渠道已切换 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap/unified stage PASS；三渠道真实接管、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三阶段 LLM 管线 | `Refactor/Contracts/FullInteractionPipeline.cs` 及相关 Contracts；在兼容单阶段管线旁增加前处理规则选择、主回复生成、可见文本 FINAL、后处理 Prompt/标签 ActionPlan 的共享顺序；不接入旧调用点 | 后处理失败不能吞掉已生成的可见主回复；RAW/FINAL 与 trace 必须分离；动作仍只能由主线程 facade 复核执行；取消/stale 不得写入历史或执行动作 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实网络、三渠道、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 主线程 Action/Memory 提交边界 | `Refactor/Runtime/InteractionResultCommitter.cs`；对完成的 `InteractionResult` 在主线程复核并执行 ActionPlan，分离可见回复历史写入与成功 AFEF 写入；不接入旧调用点 | 不接受 stale/cancel/失败结果；不执行空 ActionPlan；executor 失败不能伪造事实；memory 写入顺序固定为 user → assistant，且不携带 live 对象；旧三渠道保持不变 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实 Action executor、旧存档、三渠道和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native Conversation facade 旁路垂直切片 | `Refactor/Adapters/LegacyNativeConversationFacade.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`；串联 Native snapshot capture、三阶段 coordinator 和主线程 commit，旧规则/Prompt/Action 通过 ports 注入；不改 `ShoutBehavior` 现有调用点 | facade 不保存 live 游戏对象；capture/commit 必须在主线程；默认不切换旧路径；generation 二次校验必须先于动作执行；旧历史可能已有 pending user input 时由调用方关闭重复写入 | InteractionPipeline runner PASS：`cases=15 immutableSnapshot=true configReloadIsolation=true coordinatorGeneration=true cancellationIsolation=true compositionRoot=true threeStage=true postprocessIsolation=true commitBoundary=true nativeFacade=true`；1.3/1.4/Bootstrap 均 0 警告、0 错误；unified stage PASS；真实 Native 接入、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native Conversation 显式 opt-in 宿主接线边界 | `ShoutBehavior.cs` 新增生产侧 facade 创建/配置快照入口；`LegacyChannelInteractionFacade` 提供三渠道共用生命周期；只提供主线程 capture 与显式 ports 注入，不替换现有 `SubmitNativeConversationTextInternalAsync` 默认路径 | 不在宿主层复制规则或持有 live 对象；不得把凭据写入 `RuntimeConfigSnapshot`；调用方必须负责主线程 Commit、旧 pending user input 去重和失败 fallback；未注入 ports 时不可启动新管线 | InteractionPipeline runner PASS（16 cases）；Persistence/Profile/Config runner PASS（95 literal keys、9 namespaces）；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署；真实 Native 端到端和游戏内验收 NOT-RUN | VERIFY |
| 2026-08-30 | Legacy Gateway 主回复/动作后处理分阶段路由 | AIConfigHandler.cs、Refactor/Contracts/LegacyShoutNetworkGateway.cs；主回复保持 ShoutNetwork，Postprocess 走现有非交互动作后处理 API；不改变默认旧入口 | 不弹阻塞重试窗口；凭据仍由旧 authority 读取；后处理底层旧 API 为同步 HTTP，取消时由 coordinator 丢弃结果但不能中断底层请求；真实三渠道仍未切换 | InteractionPipeline runner PASS；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署；真实网络/取消端到端 NOT-RUN | VERIFY |
| 2026-08-30 | Detached rule selector 与三渠道 lifecycle facade | Refactor/Adapters/LegacyDetachedRuleSelector.cs、Refactor/Adapters/LegacyChannelInteractionFacade.cs、MyBehaviorMemoryFacade；统一请求生命周期，前处理只读取 immutable snapshot 字符串/ID，memory 不跨异步边界持有 Hero | 规则检索仍由旧辅助 API authority 执行；真实 rule/prompt/action 全量 ports、三渠道默认接入和真实游戏验证未完成；Hero 解析只允许交互边界，不能进 tick 热路径 | InteractionPipeline runner PASS（17 cases）；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署 | VERIFY |
| 2026-08-30 | Legacy PromptPackage adapter | Refactor/Adapters/LegacyPromptPackageAdapter.cs、LegacyShoutNetworkGateway.cs；在旧 role/content 消息与不可变 PromptPackage 间做边界复制，供三渠道共用 | 空消息丢弃、非法 role 归一为 user；不携带 live 对象；仅完成消息形状适配，Native/场景/信使完整 Prompt 组装仍未迁移 | InteractionPipeline runner PASS（18 cases）；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署 | VERIFY |
| 2026-08-30 | Detached PromptPackage 与 ActionTag ports | Refactor/Adapters/LegacyPromptPackageAdapter.cs、LegacyActionTagParser.cs；统一旧 role/content 消息复制和受 allowlist 约束的 ACTION 解析，供三渠道复用 | 只产生不可变 PromptPackage/ActionPlan；不执行动作、不写 AFEF；完整 Prompt 组装和各领域 Action executor 仍待接入 | InteractionPipeline runner PASS（19 cases）；Persistence/Profile/Config PASS；1.3/1.4/Bootstrap 各 0 警告、0 错误；unified stage PASS，未部署 | VERIFY |
| 2026-08-30 | 共享 detached Prompt composer | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Adapters/LegacyDetachedPromptComposer.cs`、`ShoutBehavior.cs`、InteractionPipeline fixture；冻结交互边界生成的 system/prefix/suffix 字符串块，按场景喊话权威消息顺序生成共享 `PromptPackage`，并提供 Native opt-in overload | 不携带 live 游戏对象、凭据或可变配置；不重建提示词、不把 ACTION 标签注入主链路；旧 Native/Scene/Courier 默认路径不切换；仅在交互边界复制，后台无扫描/轮询 | `dotnet run --project tools/InteractionPipelineContractTests/InteractionPipelineContractTests.csproj` PASS：`cases=19`；`git diff --check` PASS；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；stage 成功且未部署；真实 rule/prompt/action ports、网络、旧存档和游戏内三渠道验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached Prompt sections capture 扩展 | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`；让 SceneShout/Courier 与 Native 使用同一 `DetachedPromptSections` envelope 输入，保留各自目标/送达时序 | 只复制交互边界生成的字符串块；不解析/持有 live 游戏对象，不改默认入口、Prompt 文本、ACTION、AFEF、SyncData 或 courier 时序；当前输入由渠道明确标记是否已写入历史，避免重复 | InteractionPipeline 19 cases PASS；Persistence/Profile/Config PASS；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；stage 成功且未部署；真实三渠道、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Detached action protocol port | `Refactor/Adapters/LegacyActionTagParser.cs`、InteractionPipeline contract fixture；在 detached 边界覆盖既有 `ACTION/A/AD/ADP/ASS/GUI/ATT/ATP/RELAY/FOL/STP/END` 标签族，输出不可变 `ActionPlan` | 仅接受显式 allowlist；参数化模板只允许有限具体实例；不解析 AFEF/CONTENT，不执行动作、不写 AFEF；默认三渠道链路保持旧 parser/执行器 | InteractionPipeline 19 cases PASS；`ACTION` 旧拆分兼容，协议族 allowlist/参数化模板/拒绝路径通过；真实动作执行、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Detached postprocess Prompt port（当前切片） | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Adapters/LegacyDetachedPostprocessPromptComposer.cs`、InteractionPipeline fixture；冻结后处理 system/tag rules、history/AFEF、runtime target facts 和 latest visible reply，按既有三段式顺序生成后处理 PromptPackage | 后处理 sections 必须由渠道 owner 使用现有规则/事实 helper 生成；composer 不猜规则、不读取 live 对象、不把 raw reply 当可见文本；默认三渠道链路保持旧后处理 | InteractionPipeline 21 cases PASS，包含后处理 composer 顺序和 raw/visible 隔离；真实网络、动作执行、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached lifecycle 组合入口 | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`ShoutBehavior.cs`；Native 支持主线程 Prompt sections provider，SceneShout/Courier 暴露共用 `LegacyChannelInteractionFacade` 工厂 | provider/capture 只在交互边界运行；不携带 live 对象、不改变默认入口、Prompt、ACTION、AFEF、SyncData 或 Courier 时序；未注入真实 ports 时不可启动新管线 | InteractionPipeline 21 cases PASS；Persistence/Profile/Config PASS；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；stage 成功且未部署；真实三渠道、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Atomic main/postprocess detached sections bundle | `Refactor/Contracts/InteractionContracts.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`ShoutBehavior.cs`、InteractionPipeline fixture；Native provider 一次返回主 Prompt 与后处理 Prompt sections，避免两阶段快照错配 | bundle 只含不可变字符串 sections；provider 只在 capture 边界执行；不携带 live 对象、不改默认入口、规则文本、ACTION、AFEF、SyncData 或 Courier 时序 | InteractionPipeline 22 cases PASS，含 atomic bundle contract；统一 1.3/1.4/Bootstrap stage PASS；真实 Native/网络/旧存档/游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native detached sections 实际组装与 parity 旁路 | `Refactor/Adapters/LegacyNativePromptParity.cs`、`ShoutBehavior.cs`、`docs/fixtures/phase5-native-prompt-parity/native-message-order.json`、`docs/animusforge-phase5-native-prompt-parity.md`；以 Native 现有最终 role/content 和后处理 system/user 字符串为权威，生成 detached main/postprocess sections，记录哈希 parity 并汇合 atomic bundle | 只复制最终字符串/稳定消息；不复制规则文本、不把 ACTION 标签放入主链路、不携带 live 对象/凭据；parity 显式开启且异常 fail-open 到旧 Native；默认入口、旧历史、AFEF、SyncData、TTS 不变 | InteractionPipeline runner PASS：25 cases（含 Native main/postprocess parity 与 atomic bundle）；统一 `build_single_module.ps1 -Stage` PASS：1.3/1.4/Bootstrap 各 0 warning、0 error；Stage 成功且未部署；真实 Native 网络、detached facade 发送、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native detached provider 实际旁路接入 | 在不改变默认入口的前提下，将 parity 已确认的 Native sections 接到真实 detached provider/coordinator/action executor；建立失败回退、stale/cancel、主线程执行和 old-vs-detached 网络请求对照 | 真实 rule/prompt/action ports 尚未完成前，不得切换默认 Native；不得让 live 对象跨异步边界；不得改变现有后处理领域资格和执行顺序 | 已完成 opt-in runner/provider 边界与 parity 旁路；真实 provider/游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native opt-in coordinator/fallback runner | `Refactor/Adapters/LegacyNativeConversationOptInRunner.cs`、`ShoutBehavior.CreateNativeConversationOptInRunnerForExternal`；将 detached Generate、主线程 commit 回调、基础设施失败回退和 stale/cancel 隔离闭合为显式 Native runner | runner 不持有或解析 live 对象；commit 必须由渠道宿主在主线程回调；stale/cancel 不重试旧路径；默认 Native 入口不切换 | InteractionPipeline runner PASS：28 cases（含 runner success、legacy fallback、cancel isolation）；统一 1.3/1.4/Bootstrap Stage PASS；真实 Native provider、网络、动作执行和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Native ActionPlan 主线程执行器接入（当前切片） | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`ShoutBehavior.CreateNativeConversationActionPlanExecutorForExternal`、InteractionPipeline fixture；将 detached ActionPlan 做严格 raw/tag 一致性校验后，复用现有 Native `ApplyNativeConversationGameActionsCore` 执行，并保持主线程目标复核 | 执行器只在宿主主线程创建/调用并闭包当前 live 目标；不接受 raw 中未进入 ActionPlan 的动作标签；stale/cancel/目标失效不得执行或写 AFEF；不改变默认 Native、旧标签、SyncData、程序集或 Courier 时序 | InteractionPipeline 31 cases PASS；Persistence/Profile/Config 及 5 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 0 warning/0 error；未部署；真实 Native 网络、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | SceneShout detached 记忆快照对齐（当前切片） | `Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`；在场景交互边界复制当前 Hero 或非 Hero memory namespace 的历史与稳定 memory id，供共享 detached envelope 使用 | 仅在 capture 边界解析 Agent/Character/内存 key；后台不持有 live 对象；保留默认 SceneShout、AFEF、SyncData 和场景时序；无法解析时降级为空历史，不伪造对象或事实 | InteractionPipeline 32 cases PASS；Persistence/Profile/Config 及 5 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 0 warning/0 error；未部署；真实 SceneShout、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached commit 编排（当前切片） | `Refactor/Runtime/DetachedInteractionHost.cs`、Native host adapter；统一 capture → coordinator → 主线程 commit → Memory/Action 的生命周期，不替换旧入口 | host 只接收渠道提供的 capture、主线程 dispatch、ActionPlan executor 和 memory facade；取消/stale/验证拒绝不得重试或写 AFEF；保留 Courier 送达/返回时序和三渠道旧 fallback | InteractionPipeline 32 cases PASS（含 detached host）；1.3/1.4/Bootstrap unified stage 各 0 warning/0 error；未部署；真实三渠道、旧存档、网络和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | SceneShout 单目标 detached Prompt/Action ports（当前切片） | `ShoutBehavior.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`docs/animusforge-phase5-scene-shout-ports.md`；从现有单 NPC 场景喊话组装点捕获不可变 main/postprocess sections，并提供显式 facade 与主线程 commit 接口 | 只在交互边界解析 Agent/Character/Memory；默认 SceneShout 不切换；ActionPlan 仍须由调用方提供 allowlist 和主线程执行器；不复制规则、不让 live 对象跨异步边界、不改变 AFEF/SyncData/Courier 时序 | InteractionPipeline、Persistence/Profile/Config、阶段 2/3 runners；1.3/1.4/Bootstrap 与 unified stage；真实 SceneShout 网络、动作、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | Courier reply/inbound detached Prompt ports（当前切片） | `CourierDeliveryBehavior.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`docs/animusforge-phase5-courier-ports.md`；复用现有 Courier reply/inbound message builder 的最终 role/content 顺序，复制为 immutable PromptPackage/history，并保留 Courier session 送达/返回状态机 | 只在交互边界解析 session/Hero 并复制字符串；默认 Courier 不切换；ActionPlan 仍须由渠道 owner 在主线程复核/执行；不复制 Courier 状态机、不改变 SyncData/存档/送达时序 | InteractionPipeline、Persistence/Profile/Config、阶段 2/3 runners；1.3/1.4/Bootstrap 与 unified stage；真实 Courier 网络、动作、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-30 | SceneShout/Courier detached ActionPlan 主线程执行适配 | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`；以稳定 session/Agent identity 在 commit 边界重新解析目标，复用既有场景动作与 Courier 领域执行入口，避免重复写历史 | 执行器只在主线程调用；stale/cancel/目标失效或渠道/subject 不匹配时拒绝，不执行动作、不写 AFEF；Courier 送达/返回状态机保持原样，默认三渠道入口不切换；ActionPlan raw/tag 必须严格一致且受 allowlist 约束 | InteractionPipeline `32 cases PASS`；Persistence/Profile/Config PASS；6 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`，stage 未部署；真实网络、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | 三渠道 detached baseline 资格与 opt-in host 闭合 | `ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`、旧 ports/composer/gateway；为无玩法规则命中的普通 LLM 对话提供仅生成文本的基线 RuleSelection，补全 SceneShout/Courier 显式 host/config/ports 入口 | baseline 不授权 ActionPlan，动作仍由明确 allowlist 和主线程 executor 控制；不重建 Prompt、不替换默认入口、不改变 SyncData/AFEF/送达时序；真实 HTTP/游戏内运行前保持 opt-in | InteractionPipeline 32 cases PASS；Persistence/Profile/Config PASS；6 个阶段 2/3 runner PASS；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`，stage 未部署；真实网络、旧存档和游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | Detached host commit/历史边界契约测试（当前切片） | `tools/InteractionPipelineContractTests/Program.cs`；验证成功 commit 回调只发生在 dispatch 内、`appendPlayerInput=false` 不写入 NPC seed、stale/rejected commit 不触发回调或旧 fallback | 纯 runner 只能证明 host 生命周期契约，不能替代真实 Courier/SceneShout/Native HTTP、主线程、旧存档或游戏内验收；默认三渠道入口保持不变 | InteractionPipeline runner `36 cases PASS`；`git diff --check` PASS；真实网络、旧存档、游戏内验证仍 NOT-RUN | VERIFY |
| 2026-08-30 | RuntimeConfigSnapshot 原子存储与 reload 边界（当前切片） | `Refactor/Runtime/RuntimeConfigSnapshotStore.cs`、`Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs`、`AIConfigHandler.cs` 及纯契约测试；以不可变快照替换 detached 请求读取时的可变配置引用，并保留旧 DuelSettings/MCM 作为来源 | reload 只替换未来 capture 使用的快照；进行中的请求继续持有旧快照；快照不含凭据/live 对象；不切换默认入口、不改变存档、SyncData、程序集或构建流程 | InteractionPipeline `40 cases PASS`（含 atomic reload/failure isolation）；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`；未部署；真实 MCM reload、网络、旧存档和游戏内验证 NOT-RUN | VERIFY |
| 2026-08-29 | 阶段 2 根 AF 基础 LLM 对话 Owner 映射 | `docs/animusforge-phase2-root-llm-owner-slice.md`；只读核对 Host、Conversation、Prompt/Rule、LLM Gateway、Memory/Persistence、UI adapter 边界 | 保持注册顺序、旧入口、存档 key/type、三渠道和单一程序集；不移动源码 | 已核对真实入口/方法/调用关系；未改生产代码 | IN PROGRESS |
| 2026-08-29 | SubModule 注册/调度分组只读清单 | `docs/animusforge-phase2-submodule-registration-catalog.md`；记录生命周期、Harmony、Model、CampaignBehavior、Mission adapter、ApplicationTick/EngineTick 顺序；不修改源码 | 注册顺序、失败隔离、主线程和 Tick 热路径是组合根风险 | 真实入口与顺序已抽取；运行频率 0；未改生产代码；清单验证通过 | DONE |
| 2026-08-29 | registry DTO 只读设计 | `docs/animusforge-phase2-registry-dto-design.md`；只定义 Host/Composition 元数据和 contribution groups，不接入运行时 | 不持有 Behavior 实例、TaleWorlds 对象、delegate 或 raw dictionary；保持旧 facade、注册顺序、失败隔离、Tick、存档和三渠道 | 设计文档完成；运行频率 0；未编译、未运行、未改变生产行为 | DONE |
| 2026-08-30 | registry validator 输入/输出 fixture | `docs/animusforge-phase2-registry-validator-fixtures.md`；有效快照、无效输入、依赖/顺序/owner/profile/线程/失败隔离输出样例；不实现 validator | 仅文档设计；不持有运行时对象；不改变 SubModule、程序集身份、SyncData key/type、三渠道或 1.3/1.4 构建策略 | 文档已写入；fixture 频率 0；`git diff --check` PASS；工作区边界检查无生产/脚本/配置路径；未编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 2 影响面、候选 Bridge 与回滚地图 | `docs/animusforge-phase2-impact-bridge-rollback-map.md`；首轮覆盖 Save、Prompt/Rule/Tag、Harmony、Tick、UI、线程、API、用户数据、候选 Bridge、非目标与回滚模板 | 只读设计；不移动源码、不接入 registry、不改变旧 facade、三渠道、存档、程序集或发布结构 | 文档已写入；频率 0；`git diff --check` PASS；工作区边界检查无生产/脚本/配置路径；逐文件 contract matrix、实现和实机验证未运行 | DONE |
| 2026-08-29 | Conversation/Memory/Action contract matrix | `docs/animusforge-phase2-conversation-memory-action-contract-matrix.md`；三条 contract 边界、逐文件影响、三渠道一致性、有效/无效 fixture 和纯测试矩阵；不实现 DTO/测试 | 保持旧 facade、三渠道、AFEF、存档 key/type、主线程和 1.3/1.4 contract；不移动生产 C# | 设计文档完成；测试 NOT-RUN；方法级映射与 fixture runner 已另行完成；未编译、部署或游戏验证 | DONE |
| 2026-08-29 | Conversation/Memory/Action 方法级映射与纯 fixture 目录 | `docs/animusforge-phase2-conversation-memory-action-method-map.md`、`docs/fixtures/phase2-conversation-memory-action/`；基于真实源码方法行号建立 contract 对应、有效/无效输入和预期输出 | 只读材料；fixture 不在 `.csproj` 中，不引用 TaleWorlds，不改变旧 facade、存档、三渠道或线程边界 | `git diff --check` PASS；生产/脚本/配置路径无变化；YAML 自动解析 NOT-RUN（当前环境无 YAML parser）；未编译/部署/游戏测试 | DONE |
| 2026-08-29 | Settlement/Siege 与 Policy/Diplomacy 候选 Bridge contract | `docs/animusforge-phase2-settlement-siege-policy-diplomacy-bridge-contracts.md`、`docs/fixtures/phase2-settlement-policy-bridges/`；定义两个候选 Bridge、现有可复用边界、A/B/A+B/A+B+Bridge/Bridge failure 组合和回滚 | 不新增平行动作/通知链；不改变 Policy save/receipt、Settlement/Mission 主线程、旧 facade、程序集、SyncData key/type 或 1.3/1.4 策略 | 文档与 3 个 JSON fixture 已写入；PowerShell `ConvertFrom-Json` 全部 PASS；`git diff --check` PASS；生产/脚本/配置路径无变化；未实现 Bridge/runner，未编译/部署/游戏测试 | DONE |
| 2026-08-29 | Settlement/Siege 与 Policy/Diplomacy Bridge fixture runner | `tools/BridgeFixtureContractTests/validate_bridge_fixtures.py`、`tools/BridgeFixtureContractTests/README.md`；独立标准库 runner，验证 10 个 A/B/A+B/A+B+Bridge/Bridge failure 案例和 6 项不变量 | 不引用 Bannerlord/生产程序集，不调用网络/存档，不接入生产 `.csproj`，不执行 Bridge | 普通输出与 `--json` 输出均 PASS；`bridgeFixtureCases=10`、`invariants=6`；未编译生产 C#、未部署、未游戏测试 | DONE |
| 2026-08-29 | 阶段 3 module manifest/profile/dependency/health catalog 与 runner | `docs/animusforge-phase3-module-manifest-profile-health-catalog.md`、`docs/fixtures/phase3-module-catalog/`、`tools/ModuleCatalogContractTests/`；设计 8 个逻辑 module/bridge、3 个 profile、依赖/能力/生命周期/health 规则和 16 个无效场景 | 设计-only；不创建 Foundation/Registry，不绑定 entry type，不改变程序集、SubModule、SyncData、存档、构建或发布结构 | runner 普通输出和 `--json` 均 PASS；modules=8、profiles=3、invalidCases=16、healthStates=8；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-29 | 阶段 3 AF.Contracts capability/event/DTO/version 设计与 runner | `docs/animusforge-phase3-af-contracts-design.md`、`docs/fixtures/phase3-af-contracts/`、`tools/AFContractsContractTests/`；设计 9 个 contract、3 个 typed event、6 个 capability 和 18 个无效场景 | 设计-only；不创建 `AF.Contracts` 生产项目，不暴露 live Bannerlord 类型，不改变程序集、SyncData、存档、三渠道或 API 线策略 | 普通输出与 `--json` 均 PASS；contracts=9、events=3、capabilities=6、invalidCases=18；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 Foundation runtime contract 与 runner | `docs/animusforge-phase3-foundation-runtime-contracts.md`、`docs/fixtures/phase3-foundation-runtime/`、`tools/FoundationRuntimeContractTests/`；设计 dispatch、background snapshot/cancellation、diagnostics/trace、SafeMode/lifecycle/health contract 和 16 个无效场景 | 设计-only；不创建 Foundation 生产项目，不接入 SubModule/Tick，不持有 delegate/live object，不改变程序集、存档、SyncData 或 fallback | 普通输出与 `--json` 均 PASS；contracts=6、healthStates=8、invalidCases=16；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 重构台账一致性审查与修正 | `docs/animusforge-refactoring-and-repository-reorganization-plan.md`、`docs/handoffs/2026-08-30-refactor-preparation.md`；修正阶段 3 条目误放阶段 2、阶段状态归属和陈旧验证记录 | 仅文档修正；不改变生产代码、程序集、存档、SyncData、构建/部署流程 | 4 个独立 runner 全部 PASS；`git diff --check` PASS；禁止生产/脚本/配置路径无变化 | DONE |
| 2026-08-30 | 阶段 3 纯组合矩阵与 runner | `docs/animusforge-phase3-composition-matrix.md`、`docs/fixtures/phase3-composition-matrix/`、`tools/CompositionMatrixContractTests/`；覆盖 no-op、required/optional provider、版本不兼容、SafeMode、stale、部分启动失败、Bridge failure、toggle 冲突和 health 边界 | 设计-only；不实现 Module Host、不接入生产 `.csproj`、不改变 SubModule、存档、程序集、Tick 或 fallback | 普通输出与 `--json` 均 PASS；cases=18、invariants=24；`git diff --check` PASS；未实现/编译/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 GameAdapter 1.3/1.4 API boundary 与 runner | `docs/animusforge-phase3-game-adapter-api-boundary.md`、`docs/fixtures/phase3-game-adapter-api/`、`tools/GameAdapterContractTests/`；设计 helper/capability、版本差异、missing member、Bootstrap marker、反射缓存、主线程和 unified package 边界 | 设计-only；不修改现有 helper、条件编译、构建/部署脚本、SubModule、程序集、存档或发布结构 | 普通输出与 `--json` 均 PASS；cases=14、apiLines=2、helpers=7；`git diff --check` PASS；未重新构建/部署/游戏测试 | DONE |
| 2026-08-30 | 阶段 3 最终设计审查 | `docs/animusforge-phase3-final-review.md`；核对阶段 3 checklist、5 份阶段文档、6 个 runner、14 个 JSON fixture、阶段归属、未验证项和禁止路径 | 结论仅覆盖设计/fixture；不代表生产 Foundation/Contracts/GameAdapter、旧存档、双版本运行时或实机验收完成 | 6 个 runner 全部 PASS；14 个 JSON fixture `ConvertFrom-Json` PASS；`git diff --check` PASS；禁止生产/构建/配置路径无变化；审查结论 PASS WITH LIMITATIONS | DONE |
| 2026-08-30 | 准备材料提交与推送 | 当前全部准备文档、fixture 和独立 runner 已暂存并创建本地提交；目标为 `origin/refactor/prepare-af-restructure` | 仅提交已确认的 docs/fixture/runner；无生产 C#、项目、脚本、配置或游戏目录变化 | 本地 commit 已创建；两次 `git push` 均因 GitHub 443 网络连接失败未完成；远端未更新，需网络恢复后重试 | VERIFY |
| 2026-08-29 | 用户决定先保持仓库现状 | 所有参考源码、生成物、用户数据、第三方依赖、工具发行物和归档保持原路径；不删除、不移动、不取消跟踪、不改 `.gitignore` | 暂不处理不会解决许可证/provenance 缺口，但避免误删用户/参考资料 | 用户明确选择 HOLD；未执行清理、移动或去跟踪 | HOLD |
| 2026-08-29 | 参考仓库保留边界确认 | `原版游戏本体代码1.3.x/`、`原版游戏本体代码1.4.5/` 作为用户确认的 tracked 游戏源码参考平面保留；不进入 AF 客户端 ZIP | 参考树与生产源码边界必须清晰；公开分发许可证仍未确认 | 已读取两套参考仓库目录和 tracked 数量；未执行删除/移动/去跟踪 | IN PROGRESS |
| 2026-08-29 | 阶段 1 初版仓库边界与分发决策表 | `docs/animusforge-repository-boundary-decision-table.md`；只建立保守处置分类，不执行删除/移动/去跟踪 | 缺少许可证/第三方清单；用户导出、参考源码、依赖 overlay、ONNX、工具发行物和归档不能默认发布 | 决策表已建立；许可证与 provenance 仍未确认，阶段 1 保持 IN PROGRESS | IN PROGRESS |
| 2026-08-29 | 阶段 1 仓库边界与可重复性审计 | `docs/animusforge-repository-boundary-audit.md`、只读扫描与现有 build/stage/package/deploy 说明；不清理文件、不改脚本 | 17,039 个 `.cs` 中 16,365 个位于原版 1.3/1.4.5 参考树；3,568 个 tracked 文件同时被 ignore 规则命中；许可证/第三方分发政策缺失 | 已完成分类统计、`.gitignore`/tracked-ignored 核对、构建流程读取；许可证/第三方原则、历史 tracked 生成物处置和实际存档/游戏基线仍未完成 | IN PROGRESS |
| 2026-08-30 | 第一版重构地图完成 | `docs/animusforge-refactor-map.md`：运行链、owner、持久化、交互、风险、顺序 | 不移动源码；目标仍为单一 `AnimusForge.dll`、旧存档兼容 | 3 个只读审计结果合并；构建仍被依赖闭包阻塞 | VERIFY |
| 2026-08-30 | 依赖闭包与 unified stage 构建验证 | 无生产 C#、脚本、程序集身份、SyncData key 或游戏目录变更 | 1.3 v1.3.15.110062、1.4 v1.4.6.115628、Bootstrap 均 0 警告/0 错误；stage 成功；实际安装游戏当前为 v1.4.8.119303，未冒充同版本验证 | 仍需旧存档、游戏内与精确 v1.4.8 overlay 验收 | VERIFY |
| 2026-08-30 | ShoutNetwork SSE 流式 Gateway opt-in 回放验证 | `Refactor/Contracts/LlmContracts.cs`、`Refactor/Contracts/LegacyShoutNetworkGateway.cs`、`ShoutNetwork.cs`、`tools/ShoutNetworkSseReplayTests/`；为保留的旧 SSE 传输建立可控 provider 回放，验证增量/最终文本、取消/stale、thinking retry 与 ACTION 隔离 | 仅 Debug transport override；默认 Scene/Native/Courier 三渠道不切换；不改变 API key、历史、AFEF、SyncData、程序集或部署流程 | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；7 个 Python runner、Configured Gateway、InteractionPipeline `40 cases`、XihaiAction Core、GiveAssetTagCodec 全部 PASS；SSE 回放 `success=1 thinkingPlainRetry=1 cancellation=1 stale=1 deltaFinalParity=1 actionIsolation=1`；真实游戏内 SSE、旧存档和三渠道运行时回放 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | Native/SceneShout/Courier 生产 opt-in capture/factory 回放 | `tools/ProductionOptInEntryReplayTests/`；直接加载 project-local 1.4 实现，调用三渠道生产 opt-in capture 与 detached ports factory，验证无活动游戏会话时 fail-closed、channel identity、Courier session identity、玩家输入冻结和 ports 非空 | 仅验证生产公开 opt-in entry 的 capture/factory，不启动真实 LLM、不执行动作、不写存档；默认三渠道、API key、历史、AFEF、SyncData、程序集与部署流程不变 | `productionOptInEntryReplay native=1 scene=1 courier=1 identity=1 failClosed=1 ports=1 noDefaultCutover=1`；未部署；已初始化游戏 host 的真实生成/主线程 commit、旧存档和游戏内回放仍 NOT-RUN | VERIFY |
| 2026-08-30 | World Diplomacy Gateway 调用方取消传播修正（当前切片） | `WorldDiplomacyLlmClient.cs`、`Refactor/Adapters/LegacyWorldDiplomacyLlmGateway.cs`；将 shared Gateway 的 cancellation token 贯穿领域请求、thinking plain retry 和 retry delay，调用方取消不再伪装成 timeout/retryable failure | 旧 API 签名保留可选 token 兼容；route、payload、thinking fallback、token/cache metadata、stale generation、存档和默认外交行为不变；只在请求边界取消，不进入 Tick | 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；`WorldDiplomacyGatewayReplayTests` caller cancellation、retry-delay cancellation、timeout isolation、credential boundary PASS；真实 provider、旧存档和游戏内外交回放仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | Dedicated TTS Gateway V1 本地 HTTP 回放 | `tools/TtsGatewayReplayTests/`；直接加载 project-local 1.4 实现，调用 `LegacyVolcTtsGateway`，验证 V1 payload、header 映射、code=3000/base64 成功、provider 错误、非法音频和 caller cancellation | token 只在 header 发送边界；payload 使用 legacy literal token，不泄露 credential；不改变 TtsEngine 队列、播放、Rhubarb、VoiceMapping、存档或默认路径 | `ttsGatewayReplay success=1 headers=1 credentialBoundary=1 providerError=1 invalidAudio=1 cancellation=1 malformedExtra=1`；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`；真实火山服务、游戏内播放和旧存档仍 NOT-RUN；未部署 | VERIFY |
| 2026-08-30 | Native/SceneShout/Courier 已初始化 host 真实生成与 commit（下一项） | 复用现有 opt-in host、detached ports、可控 provider 和主线程 commit seam，验证三渠道完整链路：规则命中、Prompt、LLM RAW/FINAL、ActionPlan、主线程复核/执行、历史与 AFEF | 仅 opt-in 验证；默认三渠道入口不切换；取消/stale/失败必须 fail-closed；不改变 SyncData、存档、程序集、构建/部署流程 | 尚未运行；需要已初始化游戏 host 或等价可控 host fixture | IN PROGRESS |
| 2026-08-30 | 三渠道旧主回复 transport 收口到 LegacyShoutNetworkGateway | `Refactor/Contracts/LegacyShoutNetworkGateway.cs`、`ShoutBehavior.cs`、`CourierDeliveryBehavior.cs`；将剩余 Scene/Native/Courier 非流式与流式主回复调用统一经过 Gateway 兼容边界，原样保留 token 统计、prompt retry、thinking、取消、SSE 回调和错误文本语义 | Gateway 仍以 `ShoutNetwork` 作为 legacy provider 实现；未伪造 detached capture、规则、ActionPlan 或存档迁移；不改变三渠道业务动作、历史/AFEF、SyncData、程序集或默认时序 | 外部直接调用点清零（仅 Gateway 内部保留 legacy transport）；1.3/1.4/Bootstrap unified stage 各 `0 warning/0 error`；Configured/Interaction/ProductionOptIn/SSE/TTS/WorldDiplomacy replay 全部 PASS；未部署 | VERIFY |
| 2026-08-30 | Primary legacy transport 可控回放与取消 seam | `ShoutNetwork.cs`、`tools/PrimaryLlmGatewayReplayTests/`；为非流式 primary send point 增加仅 Debug 的 scoped provider override，验证生产 Stage 程序集经 `LegacyShoutNetworkGateway` 的 thinking→plain retry、最终文本、credential body boundary 和 caller cancellation | 仅 Debug 测试 seam；发布路径仍使用 `DuelSettings.GlobalClient`；不改变 payload/解析/重试/错误文本、默认三渠道、存档或配置来源 | `primaryLlmGatewayReplay success=1 thinkingPlainRetry=1 credentialBoundary=1 cancellation=1`；未部署 | VERIFY |
| 2026-08-30 | 生产程序集 detached host 集成回放 | `tools/ProductionDetachedHostReplayTests/`；直接加载 project-local 1.4 Stage 的生产程序集，以动态端口接入真实 `LegacyChannelInteractionFacade`、`FullInteractionPipeline`、`DetachedInteractionHost` 和 `InteractionResultCommitter`，验证 main/postprocess/visible/commit 边界 | 仅使用 fixture Gateway 和内存代理，不解析/持有 live Bannerlord 对象，不写真实存档；不改变默认三渠道入口、动作执行或发布结构 | `productionDetachedHostReplay capture=1 main=1 postprocess=1 visibleFinal=1 commit=1 memoryBoundary=1 fallbackIsolation=1`；未部署 | VERIFY |

| 2026-08-30 | 阶段 7 Knowledge/RAG Gateway owner 边界与可控回放（本轮） | `KnowledgeLibraryBehavior.cs`、`Refactor/Adapters/LegacyKnowledgeRagGateway.cs`、`tools/KnowledgeRagGatewayReplayTests/`；将 RAG 短句生成的 provider 配置、主回复阶段约束、取消和凭据发送边界收口到领域 Gateway，并以本地可控 provider 回放验证 | 保留现有 RAG prompt、最大 token 限制、禁用 thinking、解析/确定性 fallback 和知识数据写入时序；不改变 SyncData/key/type、默认三渠道、历史/AFEF、构建/部署脚本或程序集身份；API key 只在发送边界 | `knowledgeRagGatewayReplay success=1 empty=1 providerFailure=1 cancellation=1 nonMainExclusion=1 credentialBoundary=1`；Debug/Release 1.3/1.4/Bootstrap 均 `0 warning / 0 error`；真实知识库、旧存档和游戏内回放仍 NOT-RUN；未部署 | VERIFY |

- 最新 handoff：`docs/handoffs/2026-08-30-refactor-preparation.md`
- 下一位接手者先读取：`CLAUDE.md`、`.claude/skills/animusforge-maintainer/SKILL.md`、本文件、baseline 和最新 handoff。


| 2026-08-30 | 阶段 7 Courier inbound/reply 生产 opt-in host 回放（本轮） | `CourierDeliveryBehavior.cs`、`tools/ProductionCourierHostReplayTests/`；通过生产 Courier capture、detached ports、facade 和 host 接入可控 Gateway，验证 reply 的主/后处理与 inbound 的 seed/history 边界 | 仅 opt-in/等价 host fixture；保留信使送达/返回状态机、旧 facade、user/assistant/AFEF 语义和失败回退；不切换默认入口、不改变 SyncData/key/type、构建/部署脚本或程序集身份 | `productionCourierHostReplay courierPorts=1 replyMain=1 replyPostprocess=1 replyCommit=1 inboundMain=1 inboundCommit=1 inboundNoUserSeed=1 cancellationBoundary=1 fallbackIsolation=1`；真实 Bannerlord host、旧存档和游戏内回放仍 NOT-RUN；未部署 | VERIFY |

| 2026-08-30 | 阶段 7 NPC Policy generation job 取消传播与读档清理 | `PolicySystem/Npc/NpcRulerPolicyBehavior.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Generation.cs`、`PolicySystem/Npc/NpcRulerPolicyBehavior.Persistence.cs`；为 generation job 建立运行时 CTS，贯穿 draft/effect/repair Gateway 请求，读档/新游戏取消旧 job，取消结果不进入 pending commit | 保持 Policy route/profile/JSON、重试、stale/version、存档 key/type、主线程提交和默认行为；CTS 不序列化；不切换三渠道、不改构建/部署脚本 | 代码与现有 Policy Gateway/InteractionPipeline 回归已通过；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 Policy provider、旧存档和游戏内回放 NOT-RUN；未部署、未提交、未推送 | VERIFY |

| 2026-08-30 | 阶段 7 auxiliary/event LLM 入口 owner 审查（已完成静态部分） | 首批只读审查 `MyBehavior.CallUniversalApiDetailed`、`DuelSettings`/`ModOnboardingBehavior` 验证入口及其他 auxiliary/event transport；确认共享 Gateway 覆盖、owner、凭据、取消、stale、fallback 和线程边界 | 不修改生产 C#、构建/覆盖/推送脚本、默认三渠道、SyncData/key/type、程序集身份或游戏目录；若发现缺口，先登记最小后续切片再改代码 | 计划：静态调用图、owner/credential 检查、必要的纯回放；生产游戏/真实 provider/旧存档仍需单独验收 | ACTIVE |

| 2026-08-30 | 阶段 7 DuelSettings 聊天连接测试 POST Gateway 收口（本轮） | `DuelSettings.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`tools/ConfiguredChatValidationReplayTests/`；将四条 MCM 聊天连接测试的 HTTP 发送收口到共享配置 Gateway 原始验证交换边界 | 保留各设置 owner 的 prompt、模型/温度/thinking 控制、后处理 JSON 校验、错误提示和 UI；凭据仅在发送边界；GET models、默认三渠道、存档、构建/部署和程序集不变 | `configuredChatValidationReplay success=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 MCM 与旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |

| 2026-08-30 | 阶段 7 ModOnboarding GET models/模型获取 Gateway 边界（本轮） | `ModOnboardingBehavior.cs`、`DuelSettings.cs`、`Refactor/Adapters/LegacyModelCatalogGateway.cs`、`tools/ModelCatalogGatewayReplayTests/`；将 onboarding Base URL 探测、带凭据模型列表请求和 MCM 模型刷新收口到模型目录 adapter | GET `/models` 保持独立于聊天 Gateway；业务 owner 继续负责 HTTP 状态策略、模型解析、排序/UI 和 `_baseUrlValidationVersion`/`_modelFetchVersion` stale 时序；凭据只在发送边界，不进公共 DTO、存档或日志 | `modelCatalogGatewayReplay probeNoCredential=1 fetchCredentialBoundary=1 httpFailure=1 cancellation=1 invalidConfig=1`；相关 Configured/Validation/InteractionPipeline 回归通过；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 onboarding、旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |

| 2026-08-30 | 阶段 7 ModOnboarding 聊天验证 POST Gateway 收口（本轮） | `ModOnboardingBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`；将组合验证 `ValidateApiTargetAsync` 和 MCM 单目标验证接入共享配置聊天验证 exchange，保留 provider-specific payload、响应解析、HTTP 状态/错误提示以及 `_apiValidationVersion`/CTS/stale/UI pending 时序 | 不混入 GET `/models` 协议；不切换 Native/SceneShout/Courier 默认路径，不改变 SyncData/key/type、存档、程序集、构建/部署脚本；凭据仅在发送边界 | `ConfiguredChatValidationReplay success=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；Configured/ModelCatalog/InteractionPipeline 回归通过；1.4 direct 与 Debug/Release unified stage 的 1.3/1.4/Bootstrap 均 `0 warning / 0 error`；真实 provider、游戏内 onboarding、旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 7 ModOnboarding provider-specific 验证失败语义（本轮） | `ModOnboardingBehavior.cs`、`Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`tools/ProductionValidationProviderReplayTests/`、`tools/ConfiguredChatValidationReplayTests/`；验证共享 validation exchange 原样保留 OpenAI/Anthropic provider 转换、响应解析与 YJ/Gemini thinking 控制，并修复已准备 JSON 的二次转换风险 | 保留 provider payload、thinking 控制、原始响应解析、HTTP 状态/错误提示、取消/stale/UI 时序；已准备 JSON 使用 raw validation 入口；不改变默认三渠道、存档、SyncData/key/type、程序集、构建/部署脚本或游戏目录 | `productionValidationProviderReplay openAi=1 anthropic=1 yjGeminiThinking=1 credentialBoundary=1`；`configuredChatValidationReplay success=1 preparedJsonPreserved=1 httpFailure=1 cancellation=1 timeout=1 credentialBoundary=1`；ModelCatalog/InteractionPipeline 回归通过；Debug/Release 1.3/1.4/Bootstrap unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 onboarding、旧存档仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 7 XihaiAction auxiliary classifier transport owner 审查（本轮） | `extensions/AnimusForge.XihaiAction/src/Runtime/AfV130AuxiliaryTextClassifier.cs`、`AfClassifierTransport.cs`、`AfV130ConfiguredGatewayTransport.cs`、`AfV130CallApiTransport`、`tools/XihaiClassifierTransportReplayTests/`；确认 Gateway owner、凭据解析、single-flight、caller/lifetime cancellation、可选依赖 fallback，并修复 Dispose 与已取得 `SemaphoreSlim` 的竞态 | 保留 classifier closed-set 协议、输出限制、可选扩展降级、战术安全规则、默认三渠道、ActionPlan、存档、程序集、构建/部署脚本和游戏目录；不改变发布身份 | `xihaiClassifierTransportReplay shortCircuit=1 closedSet=1 ordinarySingleFlight=1 consentLimit=1 battleSpeechLimits=1 lifetimeCancellation=1`；XihaiAction Core `88 passed / 0 failed`；StaticVerifier `13 passed / 0 failed`；主模块 Debug/Release unified stage 仍 `0 warning / 0 error`；扩展 runtime 独立编译因缺少 .NET Framework 4.7.2 Developer Pack `NOT-RUN`；真实扩展加载、provider、旧存档和游戏内分类器仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 SyncData key/type/owner/chunk 兼容审计（本轮） | `docs/fixtures/phase4-persistence-profile-config/persistence-catalog.json`、`tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`、`tools/PersistenceChunkReplayTests/`；对账 95 个字面量 key、13 个 chunked string 基础 key、38 个 flattened dictionary 基础 key、40 个符号来源，并验证真实 `CampaignSaveChunkHelper` 的 UTF-8 分块/恢复/损坏隔离 | 只做 fixture/validator/test 工具；不改变现有程序集身份、CampaignBehavior 类型、SyncData key/type、存档 owner、迁移运行时、构建/部署脚本或游戏目录；legacy inline fallback 和未知数据策略保持原样 | `persistenceProfileConfig literalKeys=95 symbolicSources=40 chunkedStringKeys=13 flattenedDictionaryKeys=38 chunkMaxBytes=12000 ... PASS`；`persistenceChunkReplay smallInline=1 utf8Boundary=1 missingChunk=1 oversizeCount=1 legacyFallback=1 dictionaryRoundTrip=1 corruptDictionary=1 safeSyncIsolation=1`；真实旧存档加载/typed runtime 仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 typed SyncData ref 绑定与 legacy save fixture（本轮） | `docs/fixtures/phase4-persistence-profile-config/syncdata-binding-catalog.json`、`tools/PersistenceProfileConfigContractTests/validate_persistence_profile_config.py`、`tools/PersistenceChunkReplayTests/`；为 95 个 exact key 对账 121 次 `ref` 绑定、8 类 C# 类型，验证同一 key 的 save/load 类型一致和真实 chunk helper 回放 | 只新增审计 fixture/validator/test 工具；不替换 `SyncData` 调用、不改 key/type、程序集身份、CampaignBehavior 注册、构建/部署脚本或游戏目录 | `persistenceProfileConfig ... typedBindings=121 typedBindingKeys=95 typedBindingTypes=8 PASS`；`persistenceChunkReplay ... PASS`；真实旧存档加载、SaveSystem typed binding、SafeMode 运行时仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 legacy-first SafeMode/缺失字段纯迁移 fixture（本轮） | `docs/fixtures/phase4-persistence-profile-config/legacy-first-safe-mode-migration-cases.json`、`tools/PersistenceMigrationContractTests.py`；验证 scalar/list/dictionary/TroopRoster/chunked storage 的旧表示、缺失 key、类型不一致、未知字段、失败不发布和幂等策略 | 纯 fixture only；不接入真实 SaveSystem，不修改生产 SyncData/key/type、程序集身份、CampaignBehavior、构建/部署脚本或游戏目录 | `persistenceMigrationContract cases=6 unknownRetention=1 missingOptional=1 typeMismatchRollback=1 chunkFailureClosed=1 idempotent=1 legacyFirst=1 PASS`；真实旧存档/SaveSystem/SafeMode runtime 仍 NOT-RUN；未部署、未提交、未推送 | VERIFY |
| 2026-08-30 | 阶段 4 存档 owner/type/程序集身份基线对账（本轮） | `tools/PersistenceIdentityAudit.py`；对比当前工作树与基线 `d4cb1467376c6e923f4295dcefc7878c11dbc7c1` 的 `SyncData` owner 类型、CampaignBehavior 类型名、`AnimusForge` 程序集名、SubModule/Bootstrap 注册边界 | 只读 Git/source/assembly audit；不回滚用户改动、不修改生产 C#、SyncData/key/type、构建/部署脚本或游戏目录 | `persistenceIdentity sync=99 behavior=35 module=AnimusForge bootstrap=1 PASS`；`syncAdded=[] syncRemoved=[] behaviorAdded=[] behaviorRemoved=[]`；Debug stage 实现程序集名均为 `AnimusForge`，Bootstrap 为 `AnimusForge.Bootstrap`；真实旧存档仍 NOT-RUN；未部署、未提交、未推送（本轮待提交） | VERIFY |
| 2026-08-30 | 阶段 8 本轮最终审查与回原重构分支推送（当前切片） | 已完成 staged 文件、凭据/私有路径、程序集/模块身份、SyncData key/type、构建/测试证据审查，并创建本轮提交；待将当前工作树无强制推送到 `origin/refactor/prepare-af-restructure`，本地项目目录为 `F:\AF测试重构` | 不 force push、不部署游戏目录、不修改构建/覆盖/推送脚本，不改变默认三渠道切换；真实旧存档/游戏 host/XihaiAction runtime build 的 NOT-RUN 风险保留在提交记录 | 本轮全量相关回归、Debug/Release unified stage、staged diff 审查均完成；剩余为 remote fast-forward 验证 | COMMITTED |
| 2026-08-30 | 阶段 7 configured-chat 流式 Gateway 与 Universal 遗留路径收敛（本轮） | `Refactor/Adapters/LegacyConfiguredChatGateway.cs`、`MyBehavior.cs`、`tools/ConfiguredChatGatewayReplayTests/`、`tools/ConfiguredChatValidationReplayTests/`、`tools/KnowledgeRagGatewayReplayTests/`；新增 adapter-local generation diagnostics 与通用 SSE 流式解析，保留 thinking plain retry、取消、错误状态、响应采样和凭据发送边界；`CallUniversalApiDetailed` 改由 shared Gateway 承接 | 不切换默认 Scene/Native/Courier；不改变 Prompt/Action/Memory/AFEF、SyncData/key/type、程序集身份、三版本发布结构或构建/部署脚本；ResponseBody/RequestBody 仅留在 legacy caller/adapter 诊断边界 | `configuredGatewayReplay success=1 streaming=1 thinkingPlainRetry=1 retryable5xx=1 cancellation=1 credentialBoundary=1`；Configured validation、Knowledge/RAG、Primary Gateway replay PASS；1.4 direct、1.3/1.4/Bootstrap Debug unified stage 各 `0 warning / 0 error`；真实 provider、游戏内 host、旧存档仍 `NOT-RUN`；未部署 | VERIFY |
| 2026-08-30 | 阶段 7 生产三渠道等价可控 Host provider/commit/fallback 验证（本轮） | `tools/ProductionConfiguredHostReplayTests/`；直接加载 project-local 1.4 implementation，使用生产 `LegacyConfiguredChatGateway`、`LegacyChannelInteractionFacade` 和 `DetachedInteractionHost`，以 loopback provider 验证 Native/SceneShout/Courier 的 main/postprocess、commit/history、credential、provider failure fallback 和 cancellation | 等价可控 Host fixture，不代表真实 Bannerlord campaign/mission host；不切换默认三渠道、不执行真实游戏动作、不改变 SyncData/key/type、程序集或部署流程 | `productionConfiguredHostReplay native=1 scene=1 courier=1 mainPostprocess=1 commitHistory=1 credentialBoundary=1 providerFallback=1 cancellationBoundary=1`；1.4 production stage 已加载；真实游戏 Host、live Agent/Hero、旧存档和游戏内 ActionPlan 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt 主线程 replay port contract（本轮） | `Refactor/Adapters/LegacyEconomyRewardDebtMainThreadPort.cs`、`tools/EconomyRewardDebtPortContractTests/`；建立主线程、目标快照、capability 排除、领域异常和 applied count fail-closed 边界，实际变更仍回调现有 domain owner | 仅完成 contract boundary，未接入 `RewardSystemBehavior`、未解析 live Hero/item/debt/settlement，不改变默认三渠道、SyncData/key/type、程序集或部署流程 | `economyRewardDebtPort valid=1 mainThread=1 staleTarget=1 capabilityFailClosed=1 nonEconomyExclusion=1 noApplicable=1 exceptionIsolation=1 countValidation=1 PASS`；1.4 production compile PASS；真实经济动作、旧存档和游戏内验收仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt Hero→玩家生产 owner adapter（本轮） | `RewardSystemBehavior.EconomyReplay.cs`、`Refactor/Adapters/LegacyEconomyRewardDebtMainThreadPort.cs`、`Refactor/Contracts/EconomyRewardDebtContracts.cs`、`Refactor/Adapters/LegacyEconomyRewardDebtAdapter.cs`、`tools/EconomyRewardDebtPortContractTests/`、`tools/ProductionEconomyOwnerReplayTests/`；复用现有 Hero 金币/物品/RP/债务/固定资产 owner，增加主线程与当前目标复核，补齐单参数 GIVE_GOLD、债务期限/备注 | Hero→玩家生产接线；非 Hero/商人/部队仍 fail-closed；不切换默认三渠道、不改变 SyncData/key/type、程序集、存档或部署流程 | `economyRewardDebtPort valid=1 mainThread=1 staleTarget=1 capabilityFailClosed=1 nonEconomyExclusion=1 noApplicable=1 exceptionIsolation=1 countValidation=1 debtMetadata=1 singleArgumentGold=1 PASS`；`productionEconomyOwnerReplay factoryFailClosed=1 productionType=1 noCampaignMutation=1 PASS`；双版本/Bootstrap Debug unified stage `0 warning / 0 error`；真实游戏内经济动作、旧存档和 AFEF 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt PartyBase→玩家 owner adapter（本轮） | `RewardSystemBehavior.EconomyPartyReplay.cs`、`tools/ProductionEconomyOwnerReplayTests/`；新增 PartyBase capture owner factory，复用既有部队金币/物品/RP 物品转移方法，执行前复核 stable subject、active party、主线程和实际数量 | 仅支持 PartyBase→玩家金币/普通物品/RP 物品；DebtCreate、DebtResolve、SettlementTransfer、商人路径仍拒绝；不切换默认三渠道、不改变 SyncData/key/type、程序集或部署流程 | `productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 productionType=1 noCampaignMutation=1 PASS`；Economy port contract `... debtMetadata=1 singleArgumentGold=1 PASS`；1.3/1.4/Bootstrap Debug unified stage `0 warning / 0 error`；真实 PartyBase、游戏内库存、ActionPlan、旧存档和 AFEF 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 6 Economy/Reward/Debt Merchant owner adapter（本轮） | `RewardSystemBehavior.EconomyMerchantReplay.cs`、`tools/ProductionEconomyOwnerReplayTests/`；增加 CharacterObject/Settlement owner factory，复用现有商人金币/物品/RP 物品和市场债务方法，执行前复核当前定居点、商人资格、主线程和实际结果 | 支持商人→玩家金币/普通物品/RP 物品/市场债务创建与解除；SettlementTransfer 仍拒绝；不切换默认三渠道、不改变 SyncData/key/type、程序集或部署流程 | `productionEconomyOwnerReplay factoryFailClosed=1 partyFactoryFailClosed=1 merchantFactoryFailClosed=1 productionType=1 noCampaignMutation=1 PASS`；Economy port contract PASS；1.3/1.4/Bootstrap Debug unified stage `0 warning / 0 error`；真实商人/Settlement、市场库存债务、游戏内 ActionPlan、旧存档和 AFEF 仍 `NOT-RUN` | VERIFY |
| 2026-08-30 | 阶段 7 三渠道 Economy-aware ActionPlan commit 接入（当前切片） | `Refactor/Adapters/LegacyNativeActionPlanExecutor.cs`、`Refactor/Runtime/InteractionResultCommitter.cs`、三渠道 owner factory 与 Economy replay contract；在不改变旧入口的前提下，把 Hero/Party/Merchant owner port 接入 detached ActionPlan commit，过滤已确认的 Economy tags，合并 owner confirmed facts | 只在主线程执行；拒绝 stale、缺 capability、部分/零应用和 raw plan 篡改；保留旧 action authority、三渠道 facade、存档/程序集/SyncData key/type、构建和部署流程；不切换默认三渠道 | 计划：新增纯 composite/receipt fixture，生产 1.4 回放覆盖 Native/Scene/Courier 的 Economy route 与 non-economy delegation；真实游戏 host、live inventory/debt、旧存档和 AFEF 仍 NOT-RUN；未部署 | VERIFY |

| 2026-08-30 | 阶段 7 真实初始化 Campaign/Mission Host 与游戏内 Economy 验收（下一切片） | 使用已完成的 owner/state fixture 作为前置，接入或验证真实初始化 Campaign/Mission host；覆盖 Hero/Party/Merchant live inventory、market/debt、三渠道主线程 commit、confirmed facts、旧存档与 AFEF | 保留默认三渠道与旧 action authority；不改变程序集、模块 ID、SyncData key/type、存档类型、构建/覆盖/推送脚本或游戏目录；无真实 host 时不得宣称已完成游戏内验收 | Economy owner/state fixture、生产 1.4 executor、三渠道 production host、World Diplomacy intent-boundary smoke、1.3/1.4/Bootstrap stage 构建已 PASS；真实 live 对象、旧存档、AFEF 仍 NOT-RUN；Host readiness 已通过，只读审计确认游戏未运行且未部署 | ACTIVE |
| 2026-08-30 | 阶段 7 Economy owner/state fixture（本轮） | docs/fixtures/phase7-economy-aware-commit/economy-owner-state-cases.json、tools/EconomyOwnerStateFixtureContractTests.py；固化 Hero、Party、Merchant、Courier-Hero、inactive/stale/missing-settlement/unknown-owner 边界 | 仅为可审计纯 fixture，不创建或修改 Bannerlord 对象；字符串/ID-only；不改变默认三渠道、程序集、模块 ID、SyncData key/type、存档或部署流程 | economyOwnerStateFixture cases=7 eligible=4 rejected=3 stringOnly=1 hero=1 party=1 merchant=1 courierHero=1 failClosed=1 PASS；真实 live host、旧存档、AFEF 仍 NOT-RUN | VERIFY |
| 2026-08-30 | 阶段 7 Live Host readiness 只读审计（本轮） | tools/LiveHostReadinessAudit/live_host_readiness_audit.py、README；检查游戏可执行文件、project-local stage、Bootstrap/1.3/1.4 实现、安装模块、SubModule Bootstrap 加载、游戏进程和标准存档目录 | 只读；不启动游戏、不部署、不读取存档内容、不修改游戏目录；installedMatchesStage=false 仅表示未部署 | liveHostReadiness gameRoot=1 exe=1 stage=1 bootstrap=1 implementation13=1 implementation14=1 installedModule=1 gameRunning=0 saveDirs=1 noDeployment=1 PASS；真实 Campaign/Mission、live Economy、旧存档和 AFEF 仍 NOT-RUN | VERIFY |

## 本地化状态（2026-08-30）

- 现有发布资源已有部分中英双语：`AnimusForge/ModuleData/Languages/sceneactions_strings.xml` 与 `CNs/sceneactions_strings-zh-CN.xml` 各 124 条；`sets_hostile_meeting_strings.xml` 与对应中文文件各 6 条；中文 GCCZ 说明书另有 7 条。
- 本轮审查发现 `Refactor/` 没有建立独立的本地化资源/错误码解析边界，`Refactor/Adapters/LegacyModelCatalogGateway.cs` 仍有 3 条中文硬编码诊断文案；因此不能宣称“重构代码已完成中英本地化”。
- 下一项本地化任务：先把对用户可见的 Gateway/配置错误改为稳定 error code + 英文/简体中文资源映射，后台只传 code 和参数，主线程 UI 再解析；不得把 API key、原始响应或凭据放入资源/日志。

| 2026-08-30 | live host 重启探测与本地化审查（本轮） | `tools/LiveHostReadinessAudit/`、安装模块日志、`AnimusForge/ModuleData/Languages/`、`Refactor/Adapters/LegacyModelCatalogGateway.cs`；核对当前部署匹配、游戏进程和中英文资源覆盖 | 只记录证据，不把主菜单/进程存在当作 Campaign/Mission 验收；本轮不改变程序集、模块 ID、SyncData key/type、存档类型或生产本地化调用 | 审计 PASS：`installedMatchesStage=true`、项目/安装 stage 齐全；重启后的 Bannerlord 进程已退出且没有产生新的 Bootstrap 日志，因此真实 live Economy、旧存档、AFEF 仍 `NOT-RUN`；资源覆盖为 SceneActions 124/124、hostile meeting 6/6，重构层仍有 3 条中文硬编码诊断，完整双语本地化 `NOT-DONE` | VERIFY |

| 2026-08-31 | 阶段 7 离线闭环与真实 Host 验收包（当前切片） | owner：Conversation/Memory/Action × Economy；先运行并补齐已有 Economy-aware executor、三渠道 production replay、Memory/AFEF receipt、Persistence identity/migration 和 Gateway 回归，随后准备 Native/SceneShout/Courier 与 Hero/Party/Merchant live 验收清单；不新建平行 pipeline | 只修改阶段 7 相关最小生产/测试/文档路径；保留用户已有 `.claude/settings.local.json`、`RuleBehaviorPrompts.json`、`DuelSettings.cs`、`NobleGatheringBehavior.cs`；不修改构建/覆盖/推送脚本，不部署、不提交、不推送，不切换默认三渠道，不改变程序集/模块/SyncData key/type/存档类型；纯验证不进入 Tick 热路径，回放按请求运行，receipt 保持有界进程内缓存 | 运行现有 contract/replay 与 `git diff --check`；如发现缺口只补最小 fixture/runner；必要时运行现有 unified stage，仅写 project-local 输出；真实 Campaign/Mission、live Economy、真实旧存档和安装目录加载明确记录 `NOT-RUN/BLOCKED` | ACTIVE |
| 2026-09-01 | 阶段 7 Notoriety exact line/session outcome owner（`LOCAL-7-L`） | `PlayerNotorietyBehavior.ConversationOutcomes.cs`、`Refactor/Runtime/NotorietyConversationOutcomeReceipt.cs`、focused contract 与 production opt-in replay；为具备 H recovery/payload/part 和 opaque memory-session identity 的 detached line 建立 `AFNR1` witness，duplicate probe 位于 active/RNG 前，aggregate 与 receipt 同存既有 Notoriety JSON，finalize 冻结绝对 target 并 readback | 保留 legacy public void ABI、默认入口、H/I/K wire、95 literal key/type 与程序集身份；不从 Daily marker、日志或 aggregate 反推成功；loaded Open 转 Unknown 且不重 roll/finalize；不部署、不切 default、不删除 facade | checkpoint `cddc7628`、实现 `80729cb9`；AFNR1 14/14、Interaction 40/69/39、Memory/Courier/Economy、fresh production hosts、Profile 95/121/42/40、Migration 10、Identity 99/35、Debug/Release 六 Stage 全 PASS。独立子代理终审因 usage limit 未执行；真实 Campaign/MBRandom/save-load/crash/旧档/AFEF/default 均 `NOT-RUN` | VERIFY |
| 2026-09-01 | 阶段 8 完整20领域准备态门禁（`LOCAL-8-A`） | `tools/PhaseEightReadiness/`、`docs/phase8/full-domain-readiness-catalog.json`、`cleanup-candidates.json`；保留早期8-ID设计目录，同时把canonical20责任领域、canonical16 Bridge与逐symbol清理/回滚纳入只读证据工具 | 20领域不是20个物理DLL；role/entry未认领保持BLOCKED；PAIR/CROSS_CUT分离；KEEP/HOLD/REVIEW均不授权删除；不改生产C#/default/key/type/玩法/游戏 | checkpoint `9a088f2f`、最终工具提交 `8bdd9363`；PhaseEightReadiness 62 tests、Bridge10/6、Composition18/24、ModuleCatalog8/3/16/8 PASS；all-missing=`BLOCKED/exit2/full20/auth false`；真实Campaign/Mission、旧档、live Economy/AFEF、default/release `NOT-RUN` | VERIFY |
| 2026-09-07 | FirstChance 48k 根因调查交接（当前切片） | `docs/handoffs/2026-09-07-firstchance-root-cause-handoff.md`；记录日志 `count=48220`、NullReference 堆栈、外部 `RelationshipSeaStopPreparation` 工作树路径、Policy history snapshot 异常及下一项定位任务 | 只更新交接/台账；不修改生产 C#、构建/覆盖/推送脚本、默认三渠道、程序集/模块身份、SyncData key/type、存档类型或游戏目录；工作区既有未提交修改全部保留 | 日志已解压审阅；当前仓库无 `RelationshipSeaStopPreparation` 类，故不能宣称根因已修复；真实 Campaign/Mission、旧存档、AFEF 和 FirstChance 修复验证 `NOT-RUN` | ACTIVE |
