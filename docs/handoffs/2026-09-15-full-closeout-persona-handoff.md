# AF 全范围收尾 HANDOFF — 共享 Hero 人设请求（2026-09-15）

## 1. 发布与当前阶段

- 已按用户顺序先推送：`origin/codex/af-main-refactor-continuation-20260831` 从 f03557fb 快进到 **10defeb4976f3ffa096a77e847fba254308f6aba**，ls-remote核对成功，包含上一候选代码/详细HANDOFF。main仍为437925b8；无合并、强推或用户草稿上传。
- 随后开始全范围收尾；checkpoint **94b675de**，当前新生产/测试 **043b62b4318670e602015ac96740de476b5f4224**。**本新候选仅本地，不是上述已经发布的代码。**
- 目录 G:/AFMOD/AF-REFACTOR；本地分支 codex/af-framework-skill-delivery-20260911。整体仍为阶段8/B1，任务 `CORE-CLOSEOUT-FULL-20260915` ACTIVE。
- 全范围指AF主体、内部双向接口和外部Native/Scene/Courier三渠道SDK；政策/宴会/GCCZ业务不重写。沿用[main验收矩阵](../phase8/af-core-main-closeout-matrix-20260915.md)与14职责计划，不新增一套阶段编号。

## 2. 本轮真实实现

旧正常Hero人设生成在worker读取Hero/配置/档案，await后还会直接写档案。旧finally只按HeroId释放：清理后同代新任务有被旧任务影响的风险。编辑器重生可覆盖生成期间的人工文本编辑，完成UI也可能跑worker。

现在：

```text
现有自动生成 / 旧公开Ensure入口 / 编辑器重生
→ MyBehavior原budgeted owner队列：事实/既有人设/配置捕获，预约id
→ 启动原异步辅助Gateway（不同步等待网络）
→ 后台解析与文本规范化
→ 原owner队列：owner/档代/当前Hero/lease重验，重生额外核对原文本
→ 原SaveNpcPersonaProfile和主线程UI
→ 独立NpcPersonaGenerationOwner按lease释放/冷却
```

- **实质拆分**：独立owner持有预约/冷却及锁；MyBehavior保留真实档案/引擎适配，新的partial编排请求而不复制存储。MyBehavior净减192行，删除旧三状态字段和旧生成体；不是只把相同方法挪个文件名就算完成。
- 维持原Prompt整块、parser、辅助API/模型配置、正常只补缺失字段、重生保留最新VoiceId、5分钟失败冷却和显式重试语义。英文标识符/中文提示照旧，未硬编码人物/机器路径。
- 有意修复：重生期间个性/背景被改动则保留玩家新文本并提示重试；只改VoiceId不阻止重生，最终保留最新音色。清理/目标替换让旧结果失效，不报重生成功；旧lease不能释放或给新lease加冷却。
- 旧`EnsureNpcPersonaGeneratedForExternalAsync(Hero,bool=false)` ABI保持，worker入口仅读取Instance身份，Campaign验证留在原主线程dispatcher。正常失败提示和编辑器完成UI也回到该队列。
- 队列仍是现有MemorySummaryDispatcher，名称保留但没有新增第二队列；共享owner调度，不让人设owner持有记忆数据。

## 3. 代码位置与真实消费者

以下坐标按 **043b62b4318670e602015ac96740de476b5f4224** 核实；105点地图只作导航，不是整文件完成证明。

| 位置（一基行号） | 符号 | 已覆盖 / 保留责任 |
|---|---|---|
| `Refactor/Runtime/NpcPersonaGenerationOwner.cs:10-76` | `internal sealed class NpcPersonaGenerationOwner` | 独立预约/冷却owner；不持有游戏对象/档案 |
| `MyBehavior.PersonaGeneration.cs:50-84` | `private NpcPersonaGenerationWork CaptureNpcPersonaGeneration` | 正常Hero事实/配置主线程捕获与原异步Gateway启动 |
| `MyBehavior.PersonaGeneration.cs:86-157` | `private async Task<string> GenerateNpcPersonaAsync` | 原队列捕获/提交；lease/owner/档代/人物/编辑重验 |
| `MyBehavior.PersonaGeneration.cs:32-48` | `public static async Task EnsureNpcPersonaGeneratedForExternalAsync` | 保留旧签名；不在调用worker查询Campaign |
| `MyBehavior.cs:49299-49339` | `private async Task RunHeroPersonaRerollAsync` | 原编辑器入口；异步完成UI回主线程，清理/编辑不报假成功 |
| `MyBehavior.cs:19165-19169` | `private bool IsNpcPersonaGenerationInFlight` | 原同步状态薄适配；外层调用者仍须线程审查 |
| `MyBehavior.cs:19171-19174` | `private void GetNpcPersonaGenerationRuntimeState` | 实际调用独立owner状态 |
| `MyBehavior.cs:2495-2539` | `private void ResetLocalTransientRuntimeForLoadedSave` | 读档清理委托Reset；其余逻辑保持 |
| `MyBehavior.cs:47862-47986` | `private void ClearAllDataForCurrentSave` | 清理全部数据先退役旧生成lease |
| `MyBehavior.cs:34300-34374` | `private async Task<ApiCallResult> CallAuxiliaryGatewayDetailed` | 原辅助Gateway保留，捕获中启动异步操作，不阻塞等待网络 |

正常生成的真实消费者仍为三渠道现有Ensure调用、成年Hero事件，以及编辑器重生。公开入口仍归原MyBehavior类型，不要求旧调用者改DLL引用。**消费者外围线程处理不等于本包全部完成**：Native/Courier轮询的同步状态接口、Scene其余准备与升格同伴独立流程仍保留未完成项。

## 4. 同候选验证

| 证据 | 结果 | 说明 |
|---|---|---|
| 旧代码真实执行 | 114检查，40失败 | 四个执行声明与main437925b8精确相同；40为失败断言，不是Bug个数 |
| 当前生成/外部/UI/预约 | 125检查通过 | 26请求场景及12预约检查；物理主/后台线程，强制异步回包 |
| 行为故障注入 | 7/7编译后被拒绝 | worker捕获/提交、跳过编辑检查、跳过lease、释放新lease、丢音色、排队清理假成功 |
| 精确MyBehavior逆变换 | 6守卫通过 | 原Prompt整块不变，未知变更不能混入；B1原15守卫保留通过 |
| 既有调度/历史 | dispatcher37、history852、Native27 | 实际共享dispatcher与原历史路径；游戏/网络替身 |
| 渠道/Courier | 渠道132、后处理39、历史122；实际Courier Host replay通过 | 未证明游戏中的实际运输/资产 |
| 内部接缝 | 308断言，13方法/31调用点，3故障 | 不重写或代验制作组玩法 |
| 外部只读/ABI | API119/256并发，快照32/128并发，3故障；4DLL680元数据 | 包括原persona入口参数/默认值与新类型私有性；不是提交SDK |
| 最终Stage | Debug/Release×Bootstrap/1.3/1.4 共6项 | 同源码、只在项目内Stage，不部署游戏 |
| main存档身份 | 146 SyncData键、36行为，无增删 | 不等于真实旧档往返 |

[审计JSON](../audits/2026-09-15-full-closeout-persona-verification.json)记录源码、6个DLL和24份冻结本地日志哈希。最终构建期间HEAD为中间候选968ca283、工作树C#与当前生产提交一致；随后只有文档变更。重放命令见`tools/HeroPersonaGenerationTests/README.md`；生成/原始日志放`.tmp/full-closeout-20260915/final-evidence-v2/`，不上传。

测试中的引擎、人物注册表、profile store、parser、UI和网络是替身；生产generator/state/dispatcher与重生UI方法是实际源码。parser未改由完整逆变换保证，不能将替身解析当成全JSON解析验收。fixture未赋值字段警告不代表游戏错误。

补充自审：初版968ca283在新增排队前/提交前清理用例中出现2项假成功提示，已复现后修复。最终125/0和7个有效反例绑定043b62b4；仅使用`stage-debug-final.log`/`stage-release-final.log`以及`api-final.log`作为最终构建/元数据证据，初版日志不冒充最终版本。

## 5. 工程师与玩家视角复核

- 无历史或已有半份人设：正常补齐仍保留现有字段和音色；解析/HTTP失败保留数据并进入原冷却。
- 生成时人工编辑：重生不覆盖刚改的文本，也不会显示“成功”；失败说明改为保留“现有数据”，不误称玩家编辑从未发生。
- 清理/换档/同id目标替换：丢弃旧结果；原id在新任务中使用时，旧finally不能取消新任务。
- 存储/保存类型和三渠道主回复、标签/资产/AFEF提交时机没有改动。以上为离线行为证明与源码审查，不是实机验收。
- 事实构造和完成时FindHeroById仍有随世界规模增长的主线程工作；本包只处理线程/结果归属，**不能用每帧2回调宣称单任务成本有硬上限**。共享队列容量、游戏退出等仍由后续B1/生命周期任务收口。

## 6. 全范围接续安排（不能缩水成只做人设）

1. **渠道准备完整闭环**：Native/Courier状态轮询与Scene准备、升格同伴人设/技能；继续Courier规则/lore/剩余消息捕获→后台→接受，清除误导方法名和无消费者私有fallback，保持多人/旁听/运输时机。
2. **生命周期+B1**：GameEnd/Mission/读档/订阅/任务释放，首次深复制/全部writer/实际预算与背压；按真实owner验证，不从目录Ready推导游戏可提交。
3. **内部+外部接口**：制作组调用主体的typed服务与现有反向ports；公开Native/Scene/Courier请求、结果、取消、能力探测，复用同一主体和唯一权威提交，不能只交CatalogRead。
4. **main全功能+清理**：按功能矩阵验证Prompt/标签/资产/记忆/AFEF/TTS/保存等；逐符号说明迁移/删除/必要ABI或Saveable保留，不凭旧命名删除活路径。
5. **候选验收交付**：同源码全回归、双版本、真实游戏/旧档/live Economy/AFEF/独立子MOD加载。最后更新总/技术/制作组说明；没有证据的项目保留未验证，不承诺零BUG。

生产回滚可针对043b62b4做审查后的逆向提交，以已发布10defeb4为前一候选；不hard reset、不回退用户草稿。三份保护文件原哈希保持。自动化未恢复、游戏/存档未操作；本地制作组简明版`.tmp/full-closeout-20260915/team-handoff.md`不入库。
