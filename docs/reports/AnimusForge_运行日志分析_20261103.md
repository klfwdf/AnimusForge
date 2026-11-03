# AnimusForge 人设开关修复与运行日志分析（2026-11-03）

> 2026-11-03 最新交付：人设与首次对话索引修复已完成双API/Bootstrap编译、1.8.0 CN/EN极限7Z打包、游戏部署及源码推送；没有测试、归档/哈希校验或修复后实机验收。旧日志结论保留，交付状态以[交付记录](../releases/AnimusForge_1.8.0_交付记录_20261103.md)为准。

状态：人设源码已修改、未构建、未部署；现有日志读取分析完成；检索异常已确认，具体根因未确认。本报告不代表修复后实机通过。

## 结论

本次运行不能判为正常。bekko 嵌入引擎有成功推理，但世界书知识的候选召回和最终注入都没有成功记录；外交取材还有独立的必需证据预算故障。没有把这些问题归因于模型文件缺失，也没有靠降低阈值制造命中。

读取范围为游戏模块 Logs 目录现有全部23文件，40,517,274字节、62,421行。日志对应北京时间2026-11-02 23:36:52至23:55:47；Observability/政策 JSON 中的 UTC 时间已按 UTC+8理解。对每个文件按读取开始时的大小完整读取，没有写入原日志或配置。

## 人设开关源码修改

根因是原开关主要控制生成协议，旧扩展字段仍能进入展示、编辑器、模型上下文与经历更新；关闭时重生成还会复制回旧字段。

- 关闭扩展人设后，共享展示/上下文入口返回空扩展块；正文仍通过原正文入口提供。百科和编辑器不显示扩展字段，已打开的扩展编辑回调也不再写回。
- 场景缓存的 PersonaContext 在消费时再看当前开关，避免旧缓存继续注入。
- 关闭状态下生成或重生成成功，清理该人物的特质、动机、价值观、恐惧、说话风格和最近变化字段。
- 单纯关闭开关不批量删除存档中的旧扩展值；声音、正文、经历队列保留。经历更新是独立设置，开启时仍可更新正文；其请求不再带旧扩展字段，关闭时也不回写最近变化。
- MCM、终端及CN/EN资源说明同步修改；1.8公告的旧说明同步更新。
- 开关读取复用已加载的MCM owner；没有新增Tick扫描、模型重算或磁盘读取。

主要入口：NpcPersonaGenerationApplicationAdapter.GetNpcPersonaContext/ApplyGeneratedExtras、PersonaEquipmentPromptCaptureAdapter、PersonaEvolutionPolicy.RequestPersona、MyBehavior.StartPersonaEvolution、PersonaEditorController、DuelSettings.ExpandedNpcPersonaEnabledForRuntime。源码位置见本机代码表 ../../.tmp/runtime-log-analysis-20261103/persona-code-map.json；修改文件清单 ../../.tmp/expanded-persona-disable-20261102/changed-files.json。

本次日志中的 EnableExpandedNpcPersona=false、EnableNpcPersonaEventEvolution=true 是两个独立选项。日志有22条经历更新完成记录，不能据此判断扩展开关应该禁止一切正文更新。本次安装DLL构建于2026-11-02 21:19:59（本地时间），不包含本轮人设修改。

## 检索：全部未命中

| 指标 | 实际记录 |
| --- | --- |
| HitRate知识检索结果 | 1,839条，hit=True为0 |
| queryId | 1,839个；它们是记录ID，不等于1,839次独立玩家提问 |
| 启动时规则为空 | 72条，rules=0 |
| 已加载规则仍未命中 | 1,767条，rules=415、reason=rule_miss |
| 无trace结果 | 1,828条，主要包含后台取材 |
| 带玩家对话trace结果 | 11条，分属2个对话trace；同样全部未命中 |
| 实体候选查询 | 3,020条，全部candidates=0 |
| 实体规则分配 | 1,395条，全部selected=0 |
| semantic_accept日志 | 0条 |

HitRate中的queryNo分trace计数，最大1828不能作为全体事件数。11条玩家记录涵盖“你们好”和“前些年的日耳曼蛮子”的多次上下文构建；问候不需要世界书知识，单条未命中本身正常。但后台大量国家/人物/历史主题查询与全部候选为零的整体结果，说明世界书检索链路确有异常。这是日志选入率为0%，不是经人工标注的“检索准确率0%”。

定位到的边界：

1. LoreCandidateRetriever.SelectVectorRulesPerEntity 中，空候选日志发生在 FindRankedVectorCandidateScores 返回空之后，早于0.65名字阈值过滤。
2. KnowledgeRuleIndex 中没有重排器时会采用召回分数，并非直接清空候选。不能因 rerankerOk=False 就认定模型加载失败。
3. 已只读反编译当前安装的 KnowledgeRuleIndex、KnowledgeLibraryBehavior、WorldDiplomacyBehavior，确认安装实现含规则快照发布和后台索引构建入口；不只是凭当前源码猜测旧DLL。
4. 本次没有 KnowledgeIndexWarmup 日志；该日志入口属于读档，而这次是新游戏，缺少此日志本身不能证明没有建索引。实际查询路径也会调用 EnsureVectorIndex/EnsureOnnxIndex。
5. 当前日志没有记录查询时索引版本、有效条目数和构建失败原因。尚无法区分空规则快照、索引未就绪/空索引或内部失败等具体原因。尚未修改检索算法、阈值、缓存或玩家规则，不能宣称已修复检索。

安装的燃烬帝国导入源含414条规则，每条均有 Keywords 和 RagShortTexts；它是磁盘导入源，不足以证明当时内存里的415条规则快照完整。

## 模型文件与运行库：没有缺失的证据

当前 ONNX/bekko/ 只有：
- model.onnx：124,972,807字节；
- tokenizer.json：34,363,442字节；
- config.json：1,749字节。

当前解析器 AnimusForgeModelStore.ResolveEmbedding 明确读取这三个文件；OnnxEmbeddingEngine 从 tokenizer.json 读取分词信息，不依赖旧版 tokenizer_config.json。既有量化脚本明确 use_external_data_format=False，权重存入单个model.onnx；旧版model.onnx_data是另一种外置权重布局，不是必须补回的运行库。旧reranker按先前要求退役。

实际安装的 bin/Win64_Shipping_Client/ 中仍有：
- Microsoft.ML.OnnxRuntime.dll：206,256字节；
- onnxruntime.dll：10,807,216字节，文件版本1.18.20240515.2.4573740；
- onnxruntime_providers_shared.dll：22,048字节，同一文件版本。

日志 OnnxEmbedding 初始化成功，pooling=mean_all、dir=bekko。31个采样全部ok=true且维度384；三个已输出统计窗口合计699次推理、699次成功、0错误。各窗口平均4.99/7.78/30.62ms，最大106.98ms。这些窗口不涵盖整个运行期，采样也不等于全量调用；没有把它们相加重复统计。证据足以排除“因为ONNX目录文件少所以引擎完全没运行”的解释，但不能证明语义效果或知识注入正常。

未重新执行模型加载、推理、量化、基准对比或哈希计算；引用的是现有运行日志、目录和既有量化记录。

## 其他运行发现

- 外交：25个不同事件最终 material_search_exhausted；reason为 event_required_evidence_budget_exceeded:retain_prior_cases_and_selected_sources。存在225条request_repair、225条job_suspended、150条technical_request_resumed；恢复取材流程不代表事件生成成功，最终记录仍是25个事件耗尽、未发布。另有event_material_stale_capture异常及挂起。不能据“有重试”声称外交恢复正常；也不能证明预算故障由检索零命中造成。
- 政策：2,011条结构化日志，1,618 info、360 warning、33 error。其中358条effect-state-snapshot-rejected；4个不同生成批次有generation-exception/失败记录，多次记录涉及同一个失败，不能当33次独立崩溃。明确跨多个外国目标的草案被拒绝属于权限/目标保护，不应为消除报错而直接移除。具体持久化/效果状态风险未在本轮修复。
- 异常哨兵：16条FirstChance事件，10条启动阶段NullReferenceException、5条InvalidOperationException、1条SslStream ObjectDisposedException。FirstChance只表明异常抛出，不能直接作为未捕获崩溃统计；网络异常最终是否影响具体请求，现有关联信息不足。
- 性能：Watchdog有10次FREEZE_GAP，9.184至70.273秒；同期有大量失焦/获焦，不能把所有间隔都当主线程死锁。另有一次明确的ShoutMissionBehavior.OnMissionTick.total慢scope，352.97ms。前处理慢阶段主要包含远端请求等待，不能算成ONNX推理耗时。
- 启动兼容：CriticalOK=74、CriticalFail=0；OptionalOK=3、OptionalFail=1，唯一可选缺失为MissionCameraFadeView旧/新路径。Bootstrap选择API1.4实现，实际游戏v1.4.8.119303。
- 生命周期：23:55:47有OnSubModuleUnloaded completed，记录支持卸载流程结束，不足以保证整个运行无故障。小日志中的军演1.4补丁跳过、SETS初始化/清理、俘虏护送清理等均已纳入读取，不能替代对应玩法实机验收。
- Token_Stats仅含36个请求记录；可见输入token合计294,279、输出2,773，不覆盖全部后台外交/政策/人设调用，不能作为整次会话总消耗或账单。

## 日志覆盖清单

| 文件 | 读取字节 | 行数 |
| --- | ---: | ---: |
| .adult_hero_persona_off_20261007 | 34 | 1 |
| .initial_api_guide_notice_v1 | 35 | 1 |
| .log_cleanup_3days_migration_v089 | 34 | 1 |
| .world_diplomacy_compression_800k_48k_migration_v089 | 46 | 1 |
| AF_NpcTributeVassalage_Diagnostics.jsonl | 20,538 | 19 |
| AF_Vassalage_Diagnostics.jsonl | 473 | 1 |
| AnimusForge.Bootstrap.log | 2,635 | 12 |
| animusforge_military_exercise.log | 174 | 1 |
| CampaignTick_LastCheckpoint.txt | 8,927 | 17 |
| Compatibility_Audit.txt | 11,076 | 104 |
| Event_Logs.txt | 4,402 | 64 |
| FreezeWatchdog_LastCheckpoint.txt | 27,301 | 271 |
| FreezeWatchdog_Timeline.txt | 226,274 | 209 |
| Game_Trace.txt | 645,288 | 4,278 |
| HitRate_Stats.txt | 642,416 | 1,842 |
| Mod_Logic.txt | 36,180,783 | 48,524 |
| NoblePrisonerEscort.log | 179 | 2 |
| Observability.jsonl | 17,086 | 86 |
| PolicySystem.txt | 1,181,151 | 2,011 |
| SceneActions.log | 6,515 | 36 |
| SETS.log | 1,071 | 11 |
| Token_Stats.txt | 1,540,435 | 4,925 |
| Vengeance.log | 401 | 4 |

统计与定位证据：../../.tmp/runtime-log-analysis-20261103/summary.json、diagnostic-excerpts.json；只读安装实现记录位于 installed-source/。诊断摘录不是原日志的替代品，也不是完整Prompt导出。

## 未执行、未解决与回滚

按用户本会话要求，未运行构建、离线测试、语言门禁、归档校验、哈希验证、实机或模型实验。没有部署、打包、推送，也没有改动游戏配置、原日志或运行库。本轮人设代码尚待编译和实机接受，既有发行包不包含本次修改。

检索的零候选具体根因、外交证据预算故障、政策快照拒绝仍未解决；本报告的完成是分析完成，不是这些故障已修复。

本轮人设修改前的空检查点提交：a8f2e9d8ee。逐文件原始字节快照：../../.tmp/expanded-persona-disable-20261102/before/。快照含修改前已存在的其他作者改动，回滚只能按本轮差异做局部逆向修改，不能整文件覆盖、git restore全文件或hard reset。当前产品修改未提交；修改文件与入口见上述清单和代码表。
