# AF 主体重构：从当前基线到收尾评审前

版本：v1.1，2026-09-13。**执行方式已改为完整功能大批次；当前 B1（P1-02/03/04 联合重构，未整批合格）。P1-01 完成层 OFFLINE_VERIFIED，B1 尚未实施完成，D-A–D-E 未决定项继续待审。**

### 当前执行记录

- B1 分段调度/直接提交续作 ACTIVE（2026-09-13 11:35），起点 `793f27ee` / 生产 `7f89e18d`。本轮主执行者统一改真实初筛/extra/cleanup为按job分片、当前片逻辑删除与结构验证后的纯引用整理、计划标记在capture同回调验证；补普通Daily/Recent提交和晚UI writer故障。保留完整源校验，单大源/取消索引原子成本仍明确测量，不伪称跨Tick原子或全局epoch。预期MyBehavior.cs、MemorySummaryInput/新Planning partial与独立测试；不进B2、不动默认/业务/游戏，集中复验后统一交接。

- **B1 ACTIVE / NOT_BATCH_ACCEPTED：生产联合候选 `7f89e18d`，本轮检查点 `30cd916b`（起点 `76a3504f` / 生产 `aece8f3d`）。** 10:34续作已集中完成typed copy/流式指纹、共用实际协作耗时、调度去重筛查、真实terminal组合与部分Apply异常提示；离线和六项Stage通过，单大源/record预算仍未过。继续同一B1，不进B2，准确续点及证据见第12节。下方意图/P1-01记录为历史，不覆盖当前。

- B1 ACTIVE（2026-09-13 09:33 自动运行），检查点基线 `90201155`。主执行者统一改记忆 owner/调度与三类总结接线，独立审查只读并行；预期涉及 `MyBehavior.cs`、`MyBehavior.MemorySummaryMainThread.cs`、直接记忆 partial 与现有相关测试。先保持原 provider/解析/重试/持久化语义，补精确来源接受和实际预算；最后集中真实业务/故障、兼容与六项 Stage。未通过整批门槛不进 B2，不推送/部署/操作真实存档。

- 首轮 P1-01 已完成本轮范围：意图检查点 `9a80c570`，测试提交 `2c90ef8a`；生产仍 `9040d184`，远端仍 `bd2ed35f`。单代理只改现有完成层测试与交接，未改生产、默认、Prompt、存档身份或构建。
- 实际旧/新 Process + Apply/Mark/cleanup/release 回放：当前 14/14 场景；旧 `e40c92d7` 编译执行后 2 PASS / 12 FAIL；14 个故障注入全部编译成功并运行失败。原 helper 17、UI 85、history 852 通过。完整范围、坐标和复现见第 10 节，不代表真实 Execute/输入快照或游戏验收完成。
- 用户要求加大单次交付范围：下一批统一执行 B1＝P1-02/03/04＋必要的 P1-01 回归扩展，贯通记忆预算、三类快照、来源校验、直接 writer 与接受计数，再集中检查合格后进入 B2。不再以 P1-02 单项、测试或文档提交为一轮完成；见第 11 节。原 12 次 Apply/过期成功计数缺陷均纳入 B1，尚未修复。

编制时只授权写计划；用户随后明确要求自动运行。现有 `af-7-8` 已设为 ACTIVE，每小时在当前任务从 P1 自动推进范围明确且依赖满足的工作，未决定的 API/扩展/融合/实机权限仍须确认。终点为 `READY_FOR_CLOSEOUT_REVIEW`（可进入收尾评审），不是阶段 8 DONE、已发布或零 BUG；到达终点或只剩外部阻塞时自动暂停并交接，不自动执行推送、部署、默认迁移或广泛删旧。

## 1. 编制时基线（历史，当前执行见顶部及第12节）

| 项目 | 已核实状态 |
|---|---|
| 工作区 / 分支 | `G:\AFMOD\AF-REFACTOR` / `codex/af-framework-skill-delivery-20260911` |
| 编制前本地 HEAD | `e3b092d2`；本计划检查点 `c06cded7` |
| 远端 / 运行代码 | 已合并 `origin/codex/af-main-refactor-continuation-20260831@bd2ed35f`；最后生产变更 `9040d184`。编制前 fetch 为 ahead 7 / behind 0 |
| 架构 | 同一 AnimusForge.dll：主体 + 制作组 internal 接口/薄桥 + 独立子 MOD public API |
| 双 Skill | 维护 Skill 0.1.1 加本地协调适配；`af-core-framework` 专门约束主体与内外接口 |
| 已有成果 | 三组 typed ports/目录、只读 Api.V1、Native 多项生命周期/记忆边界、Scene/Courier 部分闭环及恢复契约；不是从零重建 |
| 当前构建证据 | 本机 Debug/Release × 1.3/1.4/Bootstrap 六项构建、两套 Stage、相关回归及四 DLL 元数据通过；见 [同步构建记录](sync-build-progress-20260913.md) |
| 明确缺口 | 真实业务链回归不足；记忆提交实际工作预算不完整；summary 输入快照/source fingerprint、部分渠道准备与 TTS、内外接口完整接线、LIVE/SAVE 待补 |
| 验证边界 | 原 summary `--original` 是源码检测后主动失败，不是执行旧业务链；17 个 helper 断言不能替代完整流程回归 |
| 当前资料保护 | 两份 2026-09-06 用户草稿和仅本地简明 HANDOFF 不纳入我们的提交；来源分支中含排除文件的历史不得重新混入 |

**不再用粗略百分比估算进度。** 后续按功能、实际接线与验收证据勾选，文件数、测试数和目录 Ready 不代表整体完成。

## 2. 保持的目标与边界

```text
AnimusForge.dll
├─ AF 主体：对话、LLM、Prompt、标签、记忆、调度
├─ internal 制作组接口与薄桥 → 政策 / 宴会 / GCCZ 等
└─ public 版本化 API ← 独立子 MOD DLL
```

- 完成主体功能及其接口，不重写政策、宴会、GCCZ 等业务规则、数值、状态机、运输/寻路或业务存档。领域集成问题分清 AF 接缝责任与对应 owner 的业务责任。
- 内外调用使用同一真实主体管线与权威动作/记忆 owner；不另造缩水 LLM 链，不把新目录包装当完成重构。
- 主体可按批准需求演进；每次有意变化记录“旧行为 → 新要求 → 默认/配置入口 → 兼容 → 验证”。不冻结算法/提示词，也不为灵活性增加无人使用的抽象、全局开关或反射扫描。
- Foundation/Module/Bridge 是职责分区，不强制拆多个 DLL。当前同 DLL 方案不变；完整通用插件平台、任意热卸载和全量业务模块化不是默认附带目标。
- 保留 Bootstrap 单模块双实现和程序集/存档身份。已替代且无调用/兼容责任的死代码随对应改动清理；仍有真实责任的 facade 先列迁移方案，不按 Legacy/文件名批量删除。
- 原 20 领域作为主体兼容影响面，不恢复为“全部领域业务重写”。GitHub `prepare-af-restructure@03eb33f1` 的独有 Xihai 修改是否纳入需先决定，不擅自融合。

## 3. 推进顺序和阶段出口

P0 是已具备的起点；P1–P6 为本次新计划。每个阶段下的完整工作项可独立审阅/回滚，不要求一口气重写所有文件，也不把半成品提交算阶段完成。

| 阶段 | 要达到的结果 | 主要依赖 | 出口 / 当前状态 |
|---|---|---|---|
| P0 基线与范围 | 锁定功能对照来源、发布范围和可复现构建 | 当前已确认主体范围；D-A/D-B 只阻塞相应外部扩展 | 基线已具备；范围清单待补，不能直接开始删旧 |
| P1 记忆与线程可靠性 | 真实业务回归、实际处理预算、输入快照与来源重验 | P0 | 三类来源/完成/真实writer组合已验证；实际record预算和剩余终端/调用方仍待收口 |
| P2 三渠道主体闭环 | Native/Scene/Courier、Prompt/标签/记忆/TTS 责任收口 | P1 的共享记忆安全边界；各功能基线 | 原主体功能逐项对齐，真实调用链与失败语义明确；未开始 |
| P3 内部模块接口稳定 | 明确登记/生命周期、贡献、动作/结果 owner 与薄桥 | P2 的共享协议；现有三组 ports | 所选制作组接缝真实接入且隔离可证明，不改其业务；未开始 |
| P4 子 MOD 最小可用 API | 经选择的提交/结果/取消及生命周期，兼容旧公开面 | 对应 P2 渠道 + P3 稳定边界；D-A/D-B | 独立示例消费者走完整主体链；未开始，开放范围待确认 |
| P5 候选综合验收 | 功能对照、性能/异常、双版本、旧档、子 MOD 加载与可复现输出 | 所选 P1–P4 工作完成；测试部署/存档授权 | 对具体 commit/产物给出证据；缺 LIVE/SAVE 则保留待验收，不放行 |
| P6 收尾前材料齐套 | 删除/默认迁移候选、兼容保留清单、回滚与交接包 | P5 与已选范围的全部必要门槛 | `READY_FOR_CLOSEOUT_REVIEW`，到此停止；最终收尾动作另批 |

顺序是依赖关系，不是每阶段才第一次测试。每项实现时立即做相关回归；有获准真实环境时尽早做对应玩家验收，P5 再汇总最终同一候选，不能等全部写完才首次看游戏。

## 4. 具体工作包

### P0 — 不重新发明基线，补齐功能责任表

- **P0-01 功能基线。** 为第 5 节每项登记真实入口/符号、owner、现行行为、历史比较点和受影响渠道；以当前 `9040d184` 为现行运行基线，修缺陷时另保留对应修复前版本。相同最终代码/环境的既有有效验证可复用。
- **P0-02 范围选择。** 确认首版公开渠道、可选扩展、其他分支融合及最终兼容保留原则。原清理 HOLD/许可未决单列，不能用“未纳入”勾成完成。
- **出口。** 不再有“这个功能到底是否算主体/哪个 owner 负责”的关键歧义；未选内容明确延期或待决，不能偷偷降低目标；公共开放范围未定，不阻塞已明确获准的 P1/P2 主体可靠性工作。

### P1 — 先修“怎样证明正确”和记忆线程底线

| 编号 | 具体要做 | 验收条件 |
|---|---|---|
| P1-01 真实业务红绿回归 | 让测试执行旧/新 `ProcessMemorySummaryQueueAsync` 及实际 Apply/Mark/cleanup/release 接线，而不只执行队列 helper；外部 provider/游戏 API 可用明确标注的替身 | 旧问题由运行时线程/状态见证触发；把主批写回 Task.Run、绕过 source/owner 守卫、漏清理或重复提交的反例必须失败；保留现有 helper/历史测试，不仅刷新 SHA |
| P1-02 实际主线程预算 | 把限制落到每帧实际 job/record/耗时；同时检查结果应用、额外 overview 规划、队列整理、失败汇总与收尾，不能只限制两个回调 | 积压、单回调多结果、恢复队列都有实际处理量见证；不丢任务、不重复/乱序，跨批重验上下文，无关 Tick 可推进；预算数值依据测量确定，不凭空保证帧时 |
| P1-03 三类输入快照与 source fingerprint | 在当前 Campaign owner 主线程捕获 `ExecuteMemorySummaryJobAsync`、`ExecuteMajorActionSummaryJobAsync`、`ExecuteMemoryOverviewJobAsync` 所需输入；后台保留原 provider/解析/重试；提交前核对确切来源 | 强制 await/retry、同 generation 内草稿/重大履历/blocks/settings 变化、换 owner/读档都不会用过时结果覆盖新数据；过期结果明确丢弃或有界重排，不无限重试 |
| P1-04 共享记忆写入责任 | 核对参与上述来源的 draft、压缩块、overview、AFEF/周报素材 writer；定义与捕获/提交的顺序或同步责任，保留原持久化结构 | 不只证明捕获后副本隔离，还覆盖捕获时并发修改；明确运行期接受、部分写入、持久化及恢复各自责任，不承诺无证据的全局 exactly-once |

P1-02 不能把不可分割的业务动作强行拆半：先确定原子单元；若一个单元本身过大，需拆纯准备/受控提交或声明并验证最大批量。时间预算是协作式让出，不伪称能抢占已执行的同步游戏调用。

输入契约根据真实读取集合设计，不在计划中写死类名/参数列表。指纹优先复用现有可靠 revision；没有才对明确输入做稳定指纹，不能用模糊标题或仅 generation 代替来源一致性。

### P2 — 三渠道和主体服务收口

| 编号 | 具体要做 | 验收条件 |
|---|---|---|
| P2-01 Native 剩余准备 | 沿完整现行入口处理 persona、规则、独立周报绑定和后续游戏对象读取；复用已做 admission、pending history、动作/记忆接受与展示边界 | 普通输入/流式/主动开场都保留；共享 busy、排队、关窗、换目标、读档、晚返回有明确语义；不能拿 detached 短链代替完整 Native |
| P2-02 Courier 双向准备 | 分开主线程捕获、后台网络/计算、主线程完成；同时核对默认路径和仍活跃的外部捕获入口 | 回信/主动来信都无后台 live 读取；区分“已准备且为空”和“没准备”，避免重复前处理；保留 persona、历史、事实及原运输业务 |
| P2-03 Scene 闭环回归与接缝整理 | 保留现有完整后处理、多人接力、旁听与队列；只收拢实际重复或缺失接缝 | 玩家发言去重、NPC assistant 语义、旁听记忆、动作/AFEF 唯一提交、退场与旧回调隔离逐项通过；不重写成熟场景实现来凑统一 |
| P2-04 Prompt / 标签 / ActionPlan / 回执 | 明确前处理→正文→权威后处理→规范化/资格→计划→执行前重验→回执→记忆的责任；复用既有 Gateway/Coordinator/Committer | 消息顺序、预算、规则资格和 raw/visible 分离可对照；未知/冲突/未授权/重复/过期动作不执行，部分成功/未知不盲重试；批准的有意变化单列 |
| P2-05 展示与 TTS | 清查剩余直接回调的主线程/订阅/释放责任，沿用现有合成、播放、Overlay 和 UI | 同 Agent FIFO、取消重播、口型/气泡/文字等待、切场景/读档不串轮；生成完成、动作/记忆完成、实际播放结束不混为一谈；实际音频单列验收 |

**P2 出口：**三渠道都能解释同一输入如何到达真实 owner 和最终效果；不要求所有渠道显示形式相同，但语义差异必须是明示规则，不能无意丢功能。网络取消只能承诺底层真正支持的部分。

### P3 — 制作组内部接口真正稳定

| 编号 | 具体要做 | 验收条件 |
|---|---|---|
| P3-01 登记/激活/失败责任 | 在现有目录与装配根上补实际所需 owner、能力/契约版本、依赖/激活条件、失败/停止责任；状态必须来自真实接线与生命周期 | 重名/缺依赖/版本冲突拒绝；不把 adapter 非空当游戏就绪；对 AF 接缝的部分失败有明确降级/失败状态，无关调用不被误阻断；不虚构可卸载的存档/Harmony owner |
| P3-02 主体贡献与动作结果协议 | 为真实消费者定义 typed 背景/话题/规则贡献与动作结果/延迟完成接点；明确作用域、顺序、预算和历史/AFEF 归属 | 未激活模块零注入；normalize 不偷偷 execute；只有一位动作 owner、一位事实提交 owner；空上下文、未支持和异常不能都被吞成正常空串 |
| P3-03 三组现有薄桥适配 | 核对政策、宴会、GCCZ 已选接缝的全部实际 callers、参数/ref/out、资格和结果；新增接缝必须逐名列入 | 普通主体、相关模块单独、共同启用、桥缺失/失败的组合按实际门禁验证；GCCZ 不污染普通场景；需要改模块业务则转交对应 owner，不在 AF 桥复制业务 |

同 DLL 内保留 `internal` typed 契约；不强制所有接口公开。只实现本次接缝需要的生命周期/故障责任，不把它夸大为所有模块的完整插件 Host。更广的 profiles/SafeMode/模块资源托管若仍属于原长期目标，应标为明确延期或另立获准范围，不能自动算 P3 已完成。

### P4 — 子 MOD 从“能查询”到“所选能力可用”

| 编号 | 建议的首版范围 | 验收条件 |
|---|---|---|
| P4-01 查询与兼容契约 | 保留当前版本/能力/只读目录；按真实需要补有限状态查询，目录 Ready 与 Campaign 可用分开，不能重新解释已有 V1 字段 | 启动前、退场、缺能力、未知 ID、版本不符有准确结果；不泄露密钥、私有记忆、可变 Hero/Agent/Behavior；旧消费者兼容 |
| P4-02 所选渠道提交/结果/取消 | **建议先 Native 完整普通文本入口**；Scene/Courier 外部提交由 D-A 决定是否同批。用关联 ID、明确结果和受控取消接现行完整主体链 | 内外入口在规则/动作/记忆上等价；非法目标、重复、忙碌、取消、换存档/目标及晚返回明确处理；没有另一套网络/动作执行器 |
| P4-03 生命周期通知与示例消费者 | 定义请求各阶段、失败/部分结果、展示完成、解绑和回调线程；提供独立外部 DLL 编译/使用示例 | 事件与实际阶段一致；订阅者异常不影响其他有效通知；解绑/读档后不串旧请求；二进制/实际加载验证与 API 目录测试分开 |

P4 的具体方法签名、DTO 和版本扩展方式在实现前定稿，不在这里写死。当前 V1 只读不是永久上限；未选择或未实现的能力继续诚实 `NotSupported`。

**可选、默认不纳入第一轮实现：**C04 子 MOD 自定义上下文/话题/动作注册、C05 制作组具体能力对外转接。两者需逐能力授权、owner、命名空间/作用域/预算及组合测试；绝不开放任意 raw 标签/记忆写入或全部模块私有方法。若用户把它们选为本次交付目标，就必须补齐再进入 P6，不能中途偷偷降级为只读。

### P5 — 最终同一候选的综合验收

1. **P5-01 功能/故障对照。** 跑第 5 节已选功能的真实方法/Host 接线回归，覆盖成功、拒绝、空返回、格式错误、provider 超时/失败、取消、重复回调、部分副作用、同代来源变化及 load/reset；保留正反例，不以总断言数判完成。
2. **P5-02 构建与复现。** 使用现有脚本验证 Debug/Release × 1.3/1.4/Bootstrap、实际 DLL ABI、单模块布局、必要依赖来源和产物 SHA；从干净检出复现准备步骤，不能隐含依赖作者机器私有目录。受保护依赖/参考资料不因整理擅自打包或删除。
3. **P5-03 性能与玩家体验。** 对正常与积压工作量测 Tick 实际成本、输入响应、队列等待/内存、网络等待和取消恢复；按同环境基线给改前/改后结果。不得删历史/减少原功能来换绿灯；帧时目标依据基线确定。
4. **P5-04 真实验收。** 获得测试部署及存档授权后，备份并用测试副本分别验证支持的 1.3/1.4 Campaign/Mission、旧档往返、生成中读档/退出、live Economy/AFEF、音频与独立子 MOD 加载/升级。只有引用 DLL/fixture 不能记为实机；某版本或材料缺失时对应门槛保持未通过。

实测提前按风险穿插，P5 负责确认最终同一候选仍满足。验收必须实际触发拟交付实现，不能仍走旧默认却声称新路径已验收；如需隔离测试入口/开关，先确认测试范围，不借测试切换发布默认。其他成员提供的 PASS 必须带源码/产物版本、环境、步骤、实际结果和证据；笼统“以前测过没问题”不能替代本候选。

### P6 — 到收尾前停住

- **P6-01 清理候选。** 逐符号列 `保留（实际兼容/存档责任）/已替代可删/尚未替代`；检查直接调用、反射/Harmony、Saveable/SyncData、旧外部消费者、模块加载。列出替代入口、验证和回滚；无法证明无责任的旧入口不得直接删。
- **P6-02 默认迁移与交付预案。** 按渠道记录当前默认、拟切目标、开关/状态影响、取消/读档/失败回退和回滚步骤；准备单模块包 allowlist、依赖/授权清单和候选摘要。最终批量删旧、默认切换和发布动作不在本计划自动执行。
- **P6-03 评审包。** 更新总 HANDOFF、代码坐标、内外接入文档/示例、功能矩阵、已知问题、兼容保留项、验证索引及制作组简明说明。需要只留本地的简明文档同时排除文件树和待推送历史；按当前用户要求决定具体上传范围。

**P6 出口：**所有已选必需项及其验收门槛有对应证据；没有未处理的关键丢功能、重复副作用、串档/串会话问题；高风险预算/线程缺口关闭；保留的兼容外壳和延期项经明确接受。若要求“旧实现全部删除”，必须先补齐外部消费者/旧存档迁移证据，不以“为兼容先留着”无限期代替该目标。

## 5. 主体功能对照清单（验收必须逐项挂证据）

| 功能组 | 至少核对的行为 | 责任范围 |
|---|---|---|
| Native 自由对话 | 普通输入、流式、主动开场、目标身份、忙碌/排队/关窗/晚返回 | 主体请求与展示，不缩成简单 HTTP 调用 |
| Scene 喊话 | 框选目标、多人接力、旁听、玩家发言去重、换 Mission/Agent | 主体闭环与历史 owner |
| Courier | 回信/主动来信、persona/history/rules、已准备空值、生成失败、晚结果 | AF 准备/生成/后处理；运输业务只做兼容回归 |
| Prompt / LLM | 话题资格、前/主/后处理顺序、规则/背景/知识来源、配置/缓存与 raw/visible | 共用主体服务；不擅改模块规则内容 |
| 标签与 ActionPlan | 正常、未知、冲突、无权、过期、重复、部分/未知结果 | 解析/路由/权威执行与事实，不开放裸标签执行 |
| Economy / Reward / Debt | Hero/Party/Merchant 的金币、物品、债务及回执在渠道中的一致性 | 主体接线、owner 与事实对照，不重写领域算法 |
| 历史 / 记忆 / AFEF | user/assistant 语义、草稿/压缩/履历/总览、失败恢复、来源变化、真实事实唯一写入 | 共享记忆与恢复责任，不把排队当已发生 |
| 展示 / TTS | 文本、气泡、口型、播放 FIFO、取消重播、完成/退场通知 | 现有 UI/音频边界，不重做 UI 或引擎 |
| 其他主体消费者 | 周报、主动 NPC、Issue 等实际 LLM/记忆调用者 | 盘点并验证主体接口影响；未改业务不虚报已重写 |
| 制作组/领域接缝 | 政策、宴会、GCCZ，以及 Duel/WorldMap/Diplomacy 等实际标签/对话接缝 | 按真实调用者逐项对照，业务修改交其 owner |
| 配置与存档 | 默认值、中文提示/英文 ID、加载/覆盖优先级、未知字段、旧档/失败保留 | 现有配置/持久化契约，不擅增兼容破坏 |
| 子 MOD | 所选公共调用、版本/缺失/失败/解绑、旧客户端二进制和实际加载 | public 契约，不把 internal/旧 ForExternal 全当稳定 SDK |

每项证据行需含：源码/产物、前置状态、输入、真实入口/owner、预期与实际效果、线程/会话/存档范围、日志及未覆盖项。受控回复可用于确定性对照，真实 provider 则单列验证；两者不能互相冒充。

## 6. 与旧清单的对应（不遗漏、不重置已完成成果）

| 原项目 | 当前定位 | 本计划承接 |
|---|---|---|
| A01 | 基础已有，完整功能责任表未齐 | P0、第 5 节 |
| A02 / A08 | 请求/记忆/恢复已有局部修复 | P1、P2-04 |
| A03 / A04 / A05 | Native/Scene 部分闭环已落地；Courier prepare 待补 | P2-01/02/03 |
| A06 / A07 / A09 | 共用服务、执行回执、TTS 部分已有 | P2-04/05 |
| B01 | 目录与依赖校验已有，不是完整模块 Host | P3-01；更广平台目标单列延期/范围决定 |
| B02 / B03 | 已选 typed 方法存在，通用贡献/结果协议未齐 | P3-02 |
| B04 | 三组 ports/13 方法/31 receivers 的既有接线保留 | P3-03，不以数字替代全部真实 caller 验收 |
| C01 | V1 只读已实现 | P4-01，保留已有承诺 |
| C02 / C03 | 未完成完整公共请求/生命周期 | P4-02/03，按 D-A 选定渠道 |
| C04 / C05 | 可选与逐能力审批 | D-B；未选择不写 DONE |
| D01 | 已有候选构建/离线证据，缺最终 LIVE/SAVE | 全过程 + P5 |
| D02 | 只能清理证实被替代的旧实现 | 日常定向清理 + P6-01 候选；最终删除另批 |
| D03 | 默认迁移保持单独决定 | P6-02 预案；实际切换另批 |
| R01 | 实际工作预算缺口 | P1-02、P5-03 |
| R02 | 已新增当前入口指向；历史仍保留 | 本计划/根 HANDOFF 统一当前状态，后续只更新相应项 |
| R03 / R04 | 目录/薄桥不是完整组合/生命周期平台 | P3 覆盖 AF 接缝必要部分；原更广目标不可自动勾完成 |
| R05 | 三渠道准备与 memory 来源仍待收口 | P1、P2 |
| R06 | 仓库/许可/用户资料仍受原 HOLD 决定约束 | P0、P5-02、P6 材料；若阻断复现/合法分发则阻塞评审，不擅删 |
| R07 | 公共能力与最终验收边界 | P4、P5、P6 |

## 7. 执行纪律与收尾前门槛

- 本计划不固定“每天做多少行/多少文件”或总百分比。每个完整工作项按实际依赖推进，先可失败的基线，再实现、工程师自审、玩家视角验收，完成后记录；不为了每次少改一点留下断开的流程。
- 当前由主执行者统一设计、集成和验收；按宿主当前规则可并行委派独立只读分析/独立测试，不创建新的用户任务，不允许多人同时改共享 owner。制作组协作以接口/owner 边界进行，碰到同文件其他作者改动先确认来源。自动化仅使用已获准的当前任务 heartbeat。
- 每批有本地检查点、明确替换范围和相关回归；只提交自己文件。代码位置需版本化并随改动更新，旧测试/审计保留，不能改历史失败或只放宽断言。
- 构建使用现有一键单模块 Stage；六项重要候选构建与源码/配置/依赖绑定。相同有效证据不无差别重跑，受变更影响的检查则重新验证。
- `READY_FOR_CLOSEOUT_REVIEW` 至少要求：选定功能/协议真实接线、相应正反例、未授权行为不变、必要性能/生命周期门槛、最终双版本/ABI/存档/实机证据、依赖与候选清单、明确回滚，以及可接受的遗留项签认。
- 没有实机、旧档或已选公开能力的必要证据，保持 `IMPLEMENTED/OFFLINE_VERIFIED + ACCEPTANCE_PENDING`，不能写 READY/DONE。只有计划/目录/构建不会解除门槛。

**本计划到此为止，不自动执行：**广泛删除旧 facade、切默认入口、最终发布打包、GitHub 推送、游戏覆盖部署或恢复自动化。测试部署/存档操作也要先得到对应明确授权。

## 8. 需要你审阅的范围决定（现在不替你选）

| 决策 | 建议 | 影响 |
|---|---|---|
| D-A 首版公开渠道 | 先完整 Native 提交/结果/取消 + 生命周期；是否同时开放 Scene/Courier 由你定 | 对应渠道完成 P2/P4/P5 才能交付；仅只读若被选择，必须明确降低首版范围，不称完整调用 SDK |
| D-B 自定义扩展/制作组能力公开 | C04/C05 先延期，具体能力逐名确认 | 不阻塞已明确选择的最小 API；一旦选入就纳入必需验收 |
| D-C 其他分支融合 | 先只读核对 `03eb33f1` 的 Xihai 独有修改，再决定 | 若纳入，先更新基线与影响面；不顺手融合或覆盖 |
| D-D 真实验收环境 | 准备版本、测试存档、副本、允许部署范围及负责成员 | 缺失不妨碍早期获准源码工作，但阻塞最终实机/旧档门槛 |
| D-E 删旧/默认迁移 | P6 交候选表后逐项评审 | 不用“继续重构”代替破坏性迁移/发布授权 |

**推荐首个获准工作项：P1-01 + P1-02 作为同一可靠性目标的相邻提交**，先补真实业务执行证据，再修实际预算；通过后接 P1-03 输入快照，不从更大 API/模块框架开始绕开现有缺陷。

## 9. 现有代码定位和文档入口

下列坐标已按生产源码 `9040d184` 核对，仅用于本计划定位；后续实施重新查符号/更新位置。新增接口名、文件拆分和预算数值不在计划中写死。

| 责任 | 已有位置 / 符号 | 对应计划 |
|---|---|---|
| 总结完成发布/消费 | `MyBehavior.MemorySummaryMainThread.cs:44-47` / `RunMemorySummaryMainThreadAsync`；`:92-95` / `ProcessMemorySummaryMainThreadActions` | P1-01/02 |
| 整批结果接受 | `MyBehavior.cs:4995-4998` / `accepted = await RunMemorySummaryMainThreadAsync(...)` | P1-01/02 |
| 总结来源准备 | `MyBehavior.cs:5225-5228` / `ExecuteMemorySummaryJobAsync`；`MyBehavior.cs:5279-5282` / `ExecuteMajorActionSummaryJobAsync`；`MyBehavior.cs:5353-5356` / `ExecuteMemoryOverviewJobAsync` | P1-03/04，执行时核准每个真实读取 |
| Native 准入/准备 | `ShoutBehavior.NativeAdmission.cs:17-20` / `NativeConversationAdmission`；`ShoutBehavior.NativePreparation.cs:14-17` | P2-01 |
| Native 历史快照/接受 | `MyBehavior.HistoryPromptSnapshot.cs:35-38` / `CaptureHistoryContextWorkById`；`ShoutBehavior.cs:20201-20204` / `persisted_history_accept` | P1/P2，沿用已落地部分 |
| Scene 后处理 | `ShoutBehavior.ScenePostprocess.cs:25-28` / `SceneActionPostprocessWorkItem` | P2-03/04 |
| Courier 双向历史准备 | `CourierDeliveryBehavior.cs:4674-4677`、`:5102-5105` / `BuildHistoryContextForExternal(...)` 调用 | P2-02 |
| 记忆写入接受 | `MyBehavior.DialogueHistoryCommit.cs:12-15` / `CommitDialogueHistoryWithScene` | P1-04、P2-04 |
| 制作组 typed 接缝 | `Refactor/Modules/TeamModulePorts.cs:7-10,17-20,27-30` / 三个 port；`Refactor/Modules/TeamModuleAdapters.cs:7-10` 起 | P3 |
| 目录/装配 | `Refactor/Modules/InternalModuleDirectory.cs:137-140`；`Refactor/Modules/ModuleFrameworkRuntime.cs:14-17` | P3-01 |
| 公共入口 | `Api/V1/AfApi.cs:13-16` / `AfApi`；`Api/V1/AfApiContracts.cs:35-38` / `AfCapabilityIds` | P4 |

- [根 HANDOFF](../../HANDOFF.md)：唯一当前任务入口。
- [双 Skill 协调](../../.claude/skills/animusforge-maintainer/references/framework-coordination.md)：长期方法与本任务范围的分工。
- [当前基线/构建](sync-build-progress-20260913.md)、[验证 JSON](../audits/2026-09-13-sync-build-verification.json)。
- [完整代码范围图](../architecture/af-framework-code-scope.md)、[版本化坐标](../architecture/af-framework-code-map.json)。
- [内部接口指南](../architecture/af-internal-module-guide-v1.md)、[公开 V1 指南](../architecture/af-public-api-guide-v1.md)。
- [Courier 深层准备说明](../audits/2026-09-09-courier-thread-boundary-plan.md)。
- [旧 A01–D03 清单](af-core-review-checklist-20260910.md)：历史要求对照，不取代本计划的当前顺序/状态。

**历史：计划/自动化设置时快照（首次执行结果以上方和第 10 节为准）。** 计划编制本身仅核对 Git、交接、双 Skill、功能清单与坐标，没有实施生产修改。随后本次自动化设置已验证 ACTIVE、每小时运行、沿用原目标任务；每个获准工作项在实际开始时才标为 ACTIVE，完成须有证据，尚未决定的项目维持待决。设置自动化不等于 P1 已开始或任何功能已完成。

## 10. P1-01 完成层业务回归证据（2026-09-13）

### 完成内容与代码位置

本轮源码未改，代码坐标绑定生产 `9040d184`；旧方法比较点 `e40c92d7`。下表为实际提取/执行责任，不是整个 MyBehavior 已拆完。每个提取声明的行号/完整 hash 在生成 manifest 中；本轮提取声明相对旧版只有 Process 改变，其余数据模型/Apply/Mark/filter 保持同一源码。

| 位置（一基） | 符号与真实执行责任 | 尚未覆盖 |
|---|---|---|
| `MyBehavior.cs:4957-5131` | `ProcessMemorySummaryQueueAsync`，实际初筛、provider await 后应用、额外 overview 规划、终态清理、失败/成功提示和 finally 释放 | provider executor 替身；不是完整 EngineTick/实际请求准备 |
| `MyBehavior.cs:5948-5986,5988-6008` | `ApplyMemorySummarySuccess` / `MarkMemorySummaryFailure`，块/草稿列表改写、队列移除、失败字段与下游调用 | lower load/save、周报/声望/Native 历史末端替身，不验证序列化/实际周报 |
| `MyBehavior.cs:5723-5755,5757-5789` | `ApplyMajorActionSummarySuccess` / `MarkMajorActionSummaryFailure`，实际状态字典与失败归一化 | 游戏资格/目标清理末端替身 |
| `MyBehavior.cs:5569-5602,5604-5636` | `ApplyMemoryOverviewSuccess` / `MarkMemoryOverviewFailure`，实际 overview 状态/队列/失败更新 | block 深层 sanitize 和入队资格替身 |
| `MyBehavior.cs:5191-5223,26983-27010` | `FindMemoryDraft`、`HasMemorySummaryJobStillPending`、daily queue sanitizer，实际来源 owner/重试/队列筛选 | 非精确 source revision/fingerprint |
| `MyBehavior.MemorySummaryMainThread.cs:44-119` | 真正 publish/accept/drain/reset helper，与真实 `SaveRuntimeGuard.cs` 一起编译 | fixture 直接调用 drain；完整主游戏 Tick 未运行 |
| `tools/MemorySummaryMainThreadBoundaryTests/run_business.py:62-134,137-181`（`2c90ef8a`） | 提取/最小观测变换/调用点 mutation；分别编译和执行，保留 manifest/log | 非完整游戏程序集；脚本提取/编译失败不算预期红例 |
| `tools/MemorySummaryMainThreadBoundaryTests/BusinessHarness.cs.txt:108-170,238-385`（`2c90ef8a`） | 标记外部替身，14 个实际方法场景、状态/线程/顺序断言和处理数量观测 | 不是 provider、TaleWorlds 或存档模拟器 |

测试仅在生成文件加入六个 Apply/Mark 入口事件和 release 赋值前事件，并把 60 秒延时换成可控异步门；业务分支、实际写入、队列清理和 finally 不由 fixture 重写。真正 UI publish 可来自后台，其消费由原 UI owner 回归另行验证；不能把一个 publish 事件当玩家已经看到弹窗。

### 复现与结果

在本工作区设置 `$env:DOTNET_EXE='G:\AFMOD\.dotnet-sdk\dotnet.exe'`，使用 `G:\Python310\python.exe -X utf8 -B`：

| 脚本/参数 | 实际结果 |
|---|---|
| `tools/MemorySummaryMainThreadBoundaryTests/run_business.py` | 编译后 14 个场景通过 |
| 同脚本 `--original` | 编译并执行 `e40c92d7`，2 PASS / 12 FAIL；存在 tick 前写入、worker Apply/Mark/cleanup/release 等运行时证据，不是源字符串主动报错 |
| 同脚本 `--mutate <name>` | README 所列 14 种全部编译成功后出现断言失败；不是编译/提取错误 |
| 原 `tools/MemorySummaryMainThreadBoundaryTests/run.py` | helper 17/17 |
| `tools/MemoryFailureUiBoundaryTests/run.py` | 85/85 |
| `tools/NativeHistorySnapshotTests/run.py` | 852/852；保留提取 fixture 数据字段的 CS0649 警告 |

业务回放的独立测试 csproj 只对所提取模型的未赋值字段抑制 CS0649，不改变生产编译警告设置；不把 fixture 构建当六项产品构建。源码/配置/依赖未变，复用先前同步台账六项 Stage，不无差别重跑。原测试及故障反例保留；修正文档对旧 `run.py --original` 的“运行时”误导，没有删除它或弱化断言。

证据索引：[验证 JSON](../audits/2026-09-13-memory-summary-business-verification.json)。原始编译日志、运行日志、生成源码/hash 在忽略目录 `tools/MemorySummaryMainThreadBoundaryTests/.generated/business/<case>/`；邻接测试也留其 `.generated/current/run.log`。测试产物不会被打进游戏模块。

### 工程师/玩家视角与下一步

- 工程师自审：当前业务实际改变三类状态、失败标记、清理和释放；副作用只能通过真实调用一次；旧业务红例与回调/省略/重复/owner/generation/source-owner mutation 有效。源码声明差异核对、Python AST、定向清理与本轮文件 `git diff --check` 通过。全工作树 diff-check 仍命中两份用户旧草稿原有首行空白，本轮不修它们；保护文件 hash 未变。
- 玩家视角（受控回放，**未进行游戏内实测**）：正常一次完成提示与实际三类结果一致；混合失败保留成功块、失败草稿和错误状态，并发布失败通知；读档/换 owner 晚结果不写入，旧请求不能释放新 owner 的状态。不能据此宣称 UI 实际显示、旧档实际往返或完整 memory exactly-once。
- 新确认缺口一：12 个独立 daily 成果可在同一 Tick 内实际 Apply 12 次。两回调上限不等于 job/record/耗时预算；P1-02 要覆盖单回调内部、规划、初筛/整理、失败汇总与收尾，不能只拆 foreach 忽略剩余全扫描。没有测真实帧时，不先写死时间/数量目标。
- 新确认缺口二：三个类型全是 obsolete successful payload 时，实际 Apply 为 0，完成提示却为 daily 1 / major 1 / overview 2（overview 被额外规划一次）。后续按实际接受/拒绝回执形成计数，并校正过期重排责任；不能仅删提示或把它称为已修。
- P1-03 精确同代来源变动、三类真实 Execute 的主线程捕获、provider 重试/RPM、P1-04 writer 原子性尚未验证，后续继续；P1 整体及阶段 8 均 NOT_DONE。
- 回滚：检查点 `9a80c570`；如需撤本轮测试，定向 revert `2c90ef8a` 并同步撤销本次结果说明，不 reset/覆盖用户文件。无生产回滚/默认迁移需求；不推送、部署或操作存档。
- 自动化继续 ACTIVE，每小时推进 P1-02；仍有独立可做工作，不在本轮测试完成后提前暂停。完成获准工作或只剩外部阻塞再按既有规则暂停并交接。简明版只在项目忽略文件 `.tmp/af-core-precloseout-team-handoff.md`，不上传。

## 11. 大批次执行方式（最新用户要求，2026-09-13）

**目的：减少碎片化交付，不降低验收要求。** 本次调整回滚基线 `2f1a8dea`；只改执行方式/自动化/当前交接，不冒称生产 B1 已完成。仍以本计划原 P0–P6 验收项目为准，B 编号只聚合交付节奏，不另起一套功能要求。

| 大批次 | 一次完成的范围 | 集中验收后的下一步 |
|---|---|---|
| B1 记忆可靠性 | P1-02/03/04 + 随变更扩展 P1-01；三类总结从捕获、请求、解析、来源校验到实际接受/写入、预算、提示和释放完整接通 | 代码/离线门槛合格才进 B2；不能只交一个 helper/一套测试 |
| B2 三渠道主体 | P2-01–05：Native/Courier 准备、Scene 完整后处理/多人接力/旁听、Prompt/标签/ActionPlan/记忆回执、展示/TTS 线程与生命周期 | 对照完整玩家流程集中检查，再进 B3；不重写各渠道成熟玩法 |
| B3 内部接缝与候选 | P3 实际内部接缝 + P4-01 只读兼容 + 当前可做的 P5/P6 组合验证、清理/默认迁移候选及交接准备 | 已获准代码工作收口；未决定 public 能力、LIVE/SAVE 和最终动作仍待授权/证据 |
| 经选择的公共 API 批次 | D-A/D-B 明确后，按选择完整实现 P4-02/03 及所选扩展/独立消费者，不以只读代替已选目标 | 纳入最终 P5/P6；未选择前不擅自开放或假报完成 |

### B1 必须贯通的真实责任

- 调度/写入：`MyBehavior.cs:4890-5438` 的 `TryStartMemorySummaryQueue`、`ProcessMemorySummaryQueueAsync`、`RunDailySummaryQueueItemsAsync`、三个 `Execute*JobAsync`，以及 `MyBehavior.MemorySummaryMainThread.cs:44-119` 的 inline/queued 两条执行方式。预算须同时覆盖实际结果应用、初筛/排序/整理、额外规划和收尾，不能只改 drain 的数字。
- 捕获不只冻住 Prompt：`MyBehavior.cs` 三类 `Build*Prompt` / `TryParse*Response` 对 Hero、trust、AFEF、周报素材、重大履历 cursor、overview IncludedBlockIds 的实际读取都纳入同一来源。后台仅保留允许的网络/纯计算，来源变化须明确丢弃或有界重排。
- writer 不只看 Save：核对 `MyBehavior.cs` 的 `SaveDailyMemoryDraftsById` / `SaveCompressedMemoryBlocksById`、`RecordNpcActionInternal`、原地编辑/导入/修复以及会原地改源的 sanitizer；与 `MyBehavior.DialogueHistoryCommit.cs`、`MyBehavior.MemoryRecovery.cs`、`MyBehavior.WeeklyActionOutcomeReceipts.cs` 的直接发布接缝统一捕获/提交顺序。不能只在两个 Save 方法递增版本。
- 六个实际 Apply/Mark 的回执区分接受、过期/拒绝、失败和部分效果；成功提示和额外 overview 规划以实际接受结果为准。保留已有 provider、重试/RPM、存档身份和业务语义，不造另一套恢复/记忆 owner。
- 本节坐标绑定当前生产 `9040d184`，为源码只读规划证据；实施时按符号更新定位。新数据结构/预算数值不能由规划文字写死，须沿真实读取集合及测量决定。

### 五道集中门槛

1. **真实链路与原行为**：实际三类 Execute、重试、解析、Process、Apply/Mark 均运行；保留原 Prompt/AFEF/素材/cursor/overview 语义，覆盖正常、失败、额外波次和同步完成。旧业务与故障反例不能删弱。
2. **线程/生命周期**：后台不读 live owner/game/source；请求、backoff、排队、跨 Tick 捕获和部分接受时换 owner/读档均可正确退休，不挂起或释放新任务。
3. **同代来源一致性**：真实追加、编辑、删除、恢复、素材发布、导入/合并使旧结果失效；旧成功和旧失败均不得覆盖新来源，深拷贝含嵌套数据，跨 Tick 捕获核对完整版本。
4. **实际工作量/接受结果**：积压、单 NPC 大来源、脏队列、全失败、同步 provider 都有 job/record/耗时见证；inline 与 queued 共用预算，无关 Tick 可推进，不重复副作用或虚报成功；不伪称抢占同步游戏调用。
5. **集成/兼容**：原记忆/UI/history/recovery/weekly 回归与持久化/API 身份检查保持，最终同一生产候选集中做现有 Debug/Release × 1.3/1.4/Bootstrap Stage；实机/旧档/实际音频/外部 DLL 验收独立记待验。

### 执行与汇报节奏

- 每小时是唤醒频率，不是只许完成一个小项；一轮尽量完成一个完整大批次。不因单个 P 编号、测试或文档提交已完成就停止；真实运行限制中断则保留同一 B 批次续点，不重做基线盘点。
- 实现中保留必要的局部编译/复现和可回滚逻辑提交；集中验收不是最后才第一次检查。完整批次验收失败就留在该批定向修复，不能带着未解决缺陷跳下一批。
- 批内不反复写长交接或对未变代码重跑全构建；验收后统一更新总 HANDOFF、证据和本地制作组说明。只在重要进展、完成、失败或需要决策时汇报，无变化不刷屏。
- 自动化 `af-7-8` 已更新为上述大批次方式，保持 ACTIVE、每小时、同一任务；未新建重复任务/自动化。当前允许独立只读审查/独立测试并行提速，主执行者统一合并，禁止同文件并行写入。
- 不自动推送、部署、操作真实存档、切默认、广泛删旧或变更制作组业务。全部可独立完成的获准工作结束/只剩外部阻塞时暂停并交接；更大批次不会把未验项目变成 READY/DONE。

## 12. B1 联合候选与准确续点（未整批合格）

**生产/测试 `7f89e18d`；检查点 `30cd916b`，前生产 `aece8f3d`，B1基线 `90201155`。** 这是同一大批次的可回滚工作候选，不是B1结案。[联合验证JSON](../audits/2026-09-13-b1-budget-terminal-candidate.json)和[40点代码范围图](../architecture/af-framework-code-scope.md)绑定本候选。

### 已贯通的改动

- 10个私有数据模型的`CopyForSummary`加2种列表typed复制，字段/嵌套图完整；完整来源、Prompt、解析依赖流式写SHA256。没有新增存档字段、公开API、配置开关或缩减历史。
- inline/queued共享既有每Tick数量和`GetDailyMaintenanceFrameBudgetMs`实际累计Stopwatch耗时，超预算后不开始下一操作。TryStart/maintenance不再重复深扫来源；Process唯一初筛，保留无效队列清理、先过滤后去重、强制/节流候选扫描。
- 沿真实Daily append/Save、Recovery Daily→Recent、Weekly outcome/回读/ledger、Major记录、压缩块Save组合校验在途旧成功/旧失败。Recent-only不虚构为Daily来源变化。
- 审查发现Apply先发布块/删除草稿后，下游异常曾被helper吞为false导致静默return。本轮用Process专属`RunMemorySummaryCompletionAsync`保留异常，进入原通知、明确可能已有部分写入、停止本轮并释放；**不盲重放、不宣称事务回滚或尾项自动恢复**。旧capture/void façade错误语义保留。

### 核实代码位置（同一生产提交，一基）

| 位置 | 符号与责任 |
|---|---|
| `MyBehavior.MemorySummaryInput.cs:42-62` | `private static T CloneMemorySummarySource<T>(` — typed图复制，10模型方法定义保留在MyBehavior对应私有数据模型 |
| `MyBehavior.MemorySummaryInput.cs:64-139` | `private MemorySummaryInput CaptureMemorySummaryInput(` — 捕获完整来源/Prompt/解析依赖 |
| `MyBehavior.MemorySummaryInput.cs:143-156` | `private static string ComputeMemorySummaryFingerprint(` — 流式SHA256，省去全量嵌套JSON |
| `MyBehavior.MemorySummaryMainThread.cs:88-98` | `private async Task<bool> RunMemorySummaryCompletionAsync(` — 部分异常回执转入原Process通知，不动旧void façade错误约定 |
| `MyBehavior.cs:4975-5015` | `private void TryStartMemorySummaryQueue(` — raw入场，不重复扫描所有source |
| `MyBehavior.cs:5037-5177` | `private async Task ProcessMemorySummaryQueueAsync(` — 初筛、强制扫描、完成/部分错误、清理和释放 |
| `MyBehavior.cs:17879-17927` | `private void TryRunCampaignMemoryMaintenance(` — maintenance去除重复来源探测 |

`MyBehavior.MemorySummaryMainThread.cs:20`的`HasMemorySummaryMainThreadAllowance`与`TryApplyMemorySummaryMainThreadAction`负责实际计时；`ProcessMemorySummaryMainThreadActions`每Tick重置。完整导航见code-map，未列代码不代表可删。

### 集中验收

| 层 | 结果与边界 |
|---|---|
| helper / completion | 32断言 / 33真实业务场景PASS；7 / 27故障反例均编译后运行失败 |
| 捕获 / 数据图 | 61场景PASS；351标量检查、49可变节点分离、251逐字段来源改变均验证；15反例有效但部分绑定中途输入，manifest保留 |
| writer / terminal | 238 façade断言 / 47真实terminal组合PASS；3 / 15反例有效。终端发布不是游戏、全普通对话提交或所有编辑导入调用方证明 |
| 原代码 / 严格逆校验 | e40实际业务4 PASS / 29 FAIL；37声明精确inverse与3反例，保留原全文件/default断言，并绑定runner/harness hash |
| 相邻 / 构建 | UI85/history852；最终Debug/Release×1.3/1.4/Bootstrap六项Stage；6 marker与stage一致；API119/256并发、四实现DLL元数据532；146 SyncData/36 behaviors保持 |
| 大来源观察 | 同1000行/134016 Prompt字符，分配9,257,688→1,579,504 bytes；最新约9.591ms，其他运行约10–16ms。机器负载/JIT可变，不作实机帧时承诺，**单原子仍未切分** |

正常测试与最终构建绑定同一生产文件hash；部分旧反例只按其准确manifest复用，不谎称全部同源重跑。原始日志在项目忽略目录，复现命令见`tools/MemorySummaryMainThreadBoundaryTests/README.md`；Stage仅项目内输出，无游戏覆盖。

### 五道门槛后的剩余问题：继续B1

1. **预算仍不合格：** 单大源capture/check、初筛/排序/extra/cleanup、Apply内循环、空队列候选全ID扫描、maintenance预算前past-draft探测仍存在原子全扫。先做一致性可证明的来源发布/分段策略；不要跨Tick逐片比较后冒称原子快照，也不能只在两个Save计epoch。
2. **writer责任补齐：** 真实普通`MyBehavior.DialogueHistoryCommit.cs`→Daily/Recent的部分失败、编辑保存/导入/迁移调用方的线程与owner责任仍未整链验证；字段变动会失效不等于这些入口已安全。部分Apply提示不等于尾项恢复解决。
3. **验收层次：** 真实游戏/provider、旧存档往返、实际UI/audio和子MOD加载仍未测。D-A–D-E/默认/public/广泛删旧等未授权决策不自行执行。
4. 同一B1继续修复后再集中检查五道门槛，合格才进B2。不要重做P1-01或反复构建未改代码；不要每次重新写一套阶段计划。

自动化`af-7-8`本地回读仍ACTIVE/每小时，同一任务；尚有可独立做的B1工作，不提前暂停。未fetch新远端/推送/部署/操作存档，已知远端快照仍`bd2ed35f`，不是本次联网确认。两份用户草稿和local-only旧简明HANDOFF hash保持，不暂存。回滚需按用户指示定向revert `7f89e18d`并同步文档；不reset用户工作树。
