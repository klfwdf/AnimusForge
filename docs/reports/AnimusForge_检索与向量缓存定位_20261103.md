# 检索与知识向量缓存定位（2026-11-03）

> 2026-11-03 最新交付：人设与首次对话索引修复已完成双API/Bootstrap编译、1.8.0 CN/EN极限7Z打包、游戏部署及源码推送；没有测试、归档/哈希校验或修复后实机验收。旧日志结论保留，交付状态以[交付记录](../releases/AnimusForge_1.8.0_交付记录_20261103.md)为准。

> 最新状态：按用户“首次对话必须建库”要求，空实体首次对话建库与未发布快照保护已在源码修复；未构建、未测试、未部署。此前分析和旧日志保留，修复范围以本文末节为准。

状态：源码及安装DLL只读分析；发现可确定的空索引固化缺陷；与上一轮实机日志一致，尚无当时索引内存状态证明它是该次运行的唯一原因。本轮没有修改产品源码、模型或缓存。

## 缓存仍有实现，但当前安装未生成

当前实现将知识种子的向量写在：

`F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/AnimusForge/ONNX/bekko/vectors/embeddings-<模型标识>.bin`

实际安装的 `OnnxEmbeddingEngine` 仍设置模型目录下的 `vectors` 路径。`EmbedDocuments` 调用 `GetVectorStore`，后者调用 `EmbeddingVectorDiskCache.Open` 创建目录，批量嵌入完成后追加知识种子向量。单条 `TryGetEmbedding` 的查询缓存只在内存中，不会创建知识向量磁盘文件。因此，嵌入引擎运行成功不等于知识库已经建立向量索引或磁盘缓存。

现有安装目录只有 model.onnx、tokenizer.json、config.json，没有 vectors。上一轮完整日志中也没有 `index_embed`、`vector_store opened`、`vector_store unavailable` 或 `onnx.embedding.batch` 记录。不能仅从缺少文件确定历史上由谁删除过缓存；可以确定上一轮没有知识批量嵌入完成的日志证据。

缓存代码由提交 `25ee2ae301` 引入，当前源码和安装DLL仍包含。新模型不携带旧模型的向量缓存是正常的；第一次需要知识检索时应自动重建。缓存缺失本身不应该导致所有检索永久失败。打包只带三个bekko模型文件，不代表运行时磁盘缓存机制退役。没有依据要求补回旧模型、旧重排器或 model.onnx_data。

## 明确发现的源码缺陷：未发布规则被固化成空索引

以下路径同时存在于当前源码和只读反编译的安装版DLL：

1. `KnowledgeLibraryBehavior.PreparePromptLoreRetrieval`：当 `mentions == null || mentions.IsEmpty` 时直接返回当前 `Index.Version`，跳过 `PublishPromptRules`。
2. `CollectPromptLoreCandidates`：收到这个正版本号后，只检查设置和版本；即使没有实体仍调用 `EnsureVectorIndex(preparedVersion)`、`EnsureOnnxIndex(preparedVersion)`。
3. `_rules()` 指向 `GetPublishedRulesForIndex`；没有发布当前版本快照时返回 null。
4. `EnsureVectorIndex` 对 null 规则生成空列表，并将 `_vectorIndexVersion` 标记为当前版本。`EnsureOnnxIndex` 又把 `rules == null` 算作 `noRules`，提交空列表并将 `_onnxIndexVersion` 标记为当前版本。
5. 后续 `PublishPromptRules` 只发布当前版本的规则快照，不增加索引版本，也不清除已标记就绪的空索引。两个 `Ensure` 看到版本相同直接返回，跳过真正构建。
6. `FindOnnxCandidateScores` 看到 entries.Count 为0即返回；稀疏回退索引也为空。没有进入 `EmbedDocuments`，所以不会创建 vectors，也不会输出建库批次日志。该状态持续到规则版本变化。

这条缺陷路径不要求模型报错，也不依赖量化误差或语义阈值。没有实体的正常请求不应让尚未发布的知识索引变成“完成但空”的状态。null 快照表示不可用，应与确实发布了0条规则的空快照区分。

主要源码位置（按本轮读取时的LF行号）：

| 文件与符号 | 行号 | 责任 |
| --- | --- | --- |
| src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs / GetPublishedRulesForIndex | 521–524 | 未发布当前版本时返回null |
| 同文件 / PreparePromptLoreRetrieval | 609–623 | 空实体跳过快照发布，却返回有效版本 |
| 同文件 / CollectPromptLoreCandidates | 627–644 | 空实体仍建库 |
| 同文件 / PublishPromptRules | 527–575 | 只更新快照，不使同版本空索引失效 |
| src/modules/AF.Module.Knowledge/Index/KnowledgeRuleIndex.cs / EnsureVectorIndex | 603–704 | null规则可提交空稀疏索引 |
| 同文件 / EnsureOnnxIndex | 732–826 | null规则被归为noRules并提交空ONNX索引 |
| 同文件 / FindOnnxCandidateScores | 1004–1022 | 空索引直接返回候选空 |
| src/modules/AF.Module.Knowledge/Semantic/OnnxEmbeddingEngine.cs / EmbedDocuments、GetVectorStore | 884–1024 | 只有批量知识嵌入走磁盘缓存 |

安装版同一路径见本机 `.tmp/runtime-log-analysis-20261103/installed-source/` 下的 KnowledgeLibraryBehavior、KnowledgeRuleIndex、OnnxEmbeddingEngine 反编译文件。只读反编译没有执行DLL或模型。

## 上一轮日志与这条路径吻合的部分

上一轮时段：2026-11-02 23:36:52—23:55:47（北京时间）。完整分析已保存到 `.tmp/runtime-log-analysis-20261103/summary.json`，不能用当前新日志重跑覆盖该结果。

- 1,839条知识结果全部未命中，其中72条rules_empty，1,767条rules=415但rule_miss。
- 3,020条实体候选查询全部candidates=0；1,395条分配全部selected=0；semantic_accept为0。
- 首轮玩家输入“你们好”的前处理返回空实体列表，随后首次带trace知识结果显示rules=415、mentionTerms=0。此时空实体请求具备上述错误建库的条件。
- 后续有实体的请求仍为空候选，且没有任何知识建库批次或向量仓库日志。
- 三个已输出单条嵌入统计窗口699次成功、0错误；它们不是知识批量嵌入的统计，也不能用来证明知识索引已完成。

上述是源码可推导的缺陷与实际现象的一致性，不是一次重新执行的复现；当时的已发布快照版本、索引版本和条目数没有记录，因此仍不能把这条路径宣称为实机已重现、已排除所有其他原因。

## 新日志已经换轮，不能混用

调查期间，游戏日志被外部新启动重置，最新日志头为 `Game started 2026-11-03 00:28:45`。00:30:09读到的72条知识结果均为 rules=0、rules_empty。这一刻只是新游戏尚无导入规则的证据，不能把它报告为“新一轮415条规则仍失败”，也不能把上一轮1839条结果当成当前计数。

本轮没有启动游戏、操作导入或要求用户重复试验；原日志由运行中的游戏自行变化。

## 修复应落在索引生命周期

建议的最小修复范围是：空实体请求在后台建库前直接返回；两个索引构建器拿到null快照时保持未构建，不能提交“空但就绪”。已发布的确实0条规则仍可视为合法空库。建库异常应记录版本、阶段与原因，避免吞异常后只剩rule_miss；日志按构建/版本输出，不引入Tick扫描或每查询全量日志。

不应通过降低相似度阈值、强行返回无关条目、恢复旧重排器或复制旧向量缓存来处理这个问题。模型不同的向量不能混用。当前未实施上述产品修改，也未改打包流程。

## 执行边界

按用户要求仅调查检索模型、知识索引与缓存。本轮未修改产品源码、模型、安装目录、玩家配置或原日志；未运行构建、离线测试、模型推理、校验或哈希验证，没有部署、打包或推送。仅新增本报告及追加台账/交接；回退本轮文档时只移除此报告和对应追加条目，保留其他作者改动。


## 2026-11-03 00:33 新一轮已成功建库和知识命中（后续实测记录）

此补记替代上文“当前安装无vectors”的时点状态，不改写上一轮1839条全miss的历史。新一轮仍使用相同构建时间的1.8.0 DLL，未由本任务部署修复。

00:31:38出现vector_store opened；00:31:43批量建库完成：texts=3098、fromStore=0、computed=1859、persisted=1859、failedBatches=0、耗时4644.9ms。3098为含重复种子的索引项数，1859为去重后实际计算并持久化的向量数。ONNX/bekko/vectors/下已生成embeddings-8ab9487df19895bae156181442b393f6.bin，读取时2,941,311字节（约2.8MiB）。文件名为既有运行时标识，本任务未计算任何哈希。

玩家“你们对北边的日耳曼怎么看”的本轮2次上下文知识查询成功，每次实际匹配2条：be_jazyg_kingdom_geopolitics与be_quadian_kingdom_internal，共4条variant_hit及2条knowledge/__query__成功。不能把这6条日志记作6次独立提问；guardrail命中另计。读取时知识域75条query记录包括导入前72条rules_empty、建库后1条rule_miss、2条成功，不用总计率衡量模型准确率。

本轮仍有明确过滤现象：查询“日耳曼”最佳raw=0.384，低于名字门槛0.65且literalRescued=0，因此被滤为空；“多瑙河”0.693和“夸迪人”0.818则入选。当前可确认建库、模型召回、变体匹配和缓存写入已跑通，不能泛化为所有问法召回均合理。空快照固化缺陷仍未修，不应因新一轮成功而删除此前代码问题；也不能再称检索每轮必然全失败。

证据另存.tmp/runtime-log-analysis-20261103/latest-success-0033/retrieval-summary.json，原始上轮summary.json保留。本轮只读游戏自行产生的新日志和目录，仅补文档；未运行测试、构建、模型实验或校验，没有改源码/模型/玩家数据，没有部署。


## 2026-11-03 源码修复：首次对话无论是否命中实体都建库

SOURCE_FIXED / NOT_BUILT / NOT_TESTED / NOT_DEPLOYED。按用户最新明确要求，首次正常对话没有实体也必须建库。本节取代此前“空实体请求在建库前直接返回”的建议，以及此前“缺陷尚未修改”的源码状态；此前实机成功是旧DLL的证据，不能作为本修复实机验收。

改动仅限两个产品文件：

- KnowledgeLibraryBehavior.PreparePromptLoreRetrieval 不再根据 mentions为空/null跳过 PublishPromptRules。只要知识库owner存在，就先发布当前版本脱离游戏对象的规则快照；owner不存在时返回0，不能返回一个看似准备完成的正版本号。
- CollectPromptLoreCandidates 保留原顺序：后台先 EnsureVectorIndex / EnsureOnnxIndex，再按实体选择候选。输入“你好”或前处理返回空实体，也会先建立知识索引；有旧磁盘向量则沿用原缓存读取。
- KnowledgeRuleIndex 两种构建器收到null快照时保持未构建，不再把尚未发布的规则提交为同版本完成空索引。已明确发布的0条规则或无种子规则仍允许合法空库；未导入知识时不能凭空生成条目。
- ONNX构建过程中快照/种子捕获异常不会被当成无种子成功；种子已捕获之后的嵌入失败继续沿用原60秒冷却路径。失败不会被声明为建库成功。
- 既有检索开关继续有效。未调整0.65名字阈值、相似度算法、规则资格、模型或退休重排器；这不是“日耳曼”泛称召回问题的修复。

运行频率：快照按规则版本只复制一次，两个索引就绪后沿用原版本判断复用；冷构建沿原请求工作阶段执行，复用批次16、4线程限制和模型目录vectors缓存，无新增Tick扫描、配置IO、后台游戏对象读取或每次对话重新全量嵌入。无新增玩家文案/Prompt键，CN/EN共享同一实现，没有新增单语资源。

按用户要求未运行构建、测试、校验门禁、模型推理或哈希。仅回读本轮源码差异和保存文件；不等于编译或实机通过。未改动安装目录或既有发行包，未部署、打包、推送，产品修改保持未提交。

改前空检查点4a5cde59d；两文件原始字节快照.tmp/first-dialogue-knowledge-index-20261103/before/，任务意图和源码行号见同目录intent.json、source-map.json，差异文件change.patch。回滚以该差异做局部逆向修改，保留其他作者dirty，不回退整仓或覆盖其他改动。
