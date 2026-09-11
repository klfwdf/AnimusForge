# AF 整个重构项目收尾执行台账

## 2026-09-12 当前切片：压缩记忆结果的主线程接受与提交（OFFLINE_COMPLETE / LIVE_SAVE_PENDING）

- 来源与提交：远端来源 `e40c92d7`，意图/checkpoint `909550d4`，聚焦生产与测试提交 `9040d184`。未 push、未部署、未覆盖游戏、未切默认入口。
- 旧行为：`TryStartMemorySummaryQueue` 从 Campaign tick 启动 `ProcessMemorySummaryQueueAsync`；第一次网络 `await` 之后，continuation 直接调用三类 `Apply*Success`、对应 `Mark*Failure`、队列清理和完成提示。它们会改写 `MyBehavior` 记忆/履历/总览 owner，却没有 Campaign 主线程接受边界。
- 本切片新行为：三批 post-await 结果接受、失败登记、队列清理、完成提示和 `_memorySummaryProcessing` 释放均经 `RunMemorySummaryMainThreadAsync` 发布，由 `OnEngineTick` 的 `ProcessMemorySummaryMainThreadActions` 消费。接受时复核物理主线程、`MyBehavior.Instance`、当前 Campaign behavior 与 save generation；入队前后双检避免 reset 后把 worker 永久挂到不再 tick 的旧 owner。
- 性能边界：这是低频日结完成队列；worker 每次等待接受后才继续发布，EngineTick 每次最多处理 2 个动作。没有热路径全量扫描、反射、锁或空转轮询；原 RPM 波次和 60 秒批间距不变。
- 渠道与兼容：Native、Scene、Courier 仍经原共享 daily draft owner 产出素材；不改三渠道 prompt、role、AFEF、动作、重试、成功/失败文本、默认入口、公开 ABI 或存档键。Api.V1 仍只读；政策/宴会/GCCZ 业务未迁移。
- 已验证：专用边界 17/17；精确旧源码 `e40c92d7` 和 3 个行为突变均按预期失败；HistorySnapshot 852/852、native 27/27、MemoryFailureUi 85/85、memory recovery、weekly material、团队端口 308 及 3 突变通过。持久化相对 `e40c92d7` 为 146/146 SyncData、36/36 CampaignBehavior、单一 `AnimusForge`/Bootstrap 身份；Debug/Release × 1.3/1.4/Bootstrap 六项构建和两套项目内 Stage 通过，四份实现 DLL 532 个元数据断言通过。
- 未覆盖：`ProcessMemorySummaryQueueAsync` 首次 await 前的调度快照，以及 `ExecuteMemorySummaryJobAsync` / `ExecuteMajorActionSummaryJobAsync` / `ExecuteMemoryOverviewJobAsync` 的 prompt/目标准备仍可能在 continuation 上读取 live owner/game 状态；本切片也未加入逐任务 source fingerprint。LIVE、真实 provider、真实 SAVE/读档晚返回均 `NOT_RUN`，阶段八未完成。
- 下一切片：为三类压缩任务在 Campaign 主线程捕获只读输入和精确来源 revision/fingerprint，后台只执行 provider/解析；接受时除本轮 owner/generation 外，再拒绝来源已变的结果。先做精确旧源码红例，不扩大到 Courier 或公共写 API。
- 回滚：按用户指示定向 inverse/revert `9040d184`，保留 `909550d4` 审计边界；不 reset/rebase、不覆盖其他作者改动。

## 2026-09-10 逐项审阅入口

[AF 主体架构待审清单](af-core-review-checklist-20260910.md)：A 主体、B 内部模块接口、C 子 MOD 公共 API、D 验收/清理/默认迁移。当前只拟清单，所有具体项待确认，未启动新接口实施。

## 后续主体任务范围已收窄（2026-09-09，用户确认）

最新架构交接：[AF 主体／内部模块接口／子 MOD 对外 API](../handoffs/2026-09-09-af-core-internal-public-api-handoff.md)。制作组模块继续编入同一主 DLL，独立子 MOD 经公共 API 对接；两类接口分开、底层主体管线共享。

本任务只重构 AF 主体与接口/薄桥，不重写政策、宴会、GCCZ 等模块业务。旧 20 领域清单改作兼容影响面参考。以下保留此前修复进展与验收事实，不能把其中“各领域逐项补齐”继续解释为全部业务重构授权。本次只写架构 HANDOFF，未实施新接口、未推送或部署。

## 最新状态（2026-09-09）

**阶段八继续实施，整个项目 NOT_DONE。** 已恢复断线修改并形成实际修复提交 `9a4a26dc`，不是继续只写检测文档。既有修复验收记录：`docs/handoffs/2026-09-09-recovery-fixes-handoff.md`；后续架构接续以本页顶部的新 HANDOFF 为准。

- 唯一工作区：`G:\AFMOD\AF-REFACTOR`；分支 `codex/af-main-refactor-continuation-20260831`。
- 修复前 checkpoint `5ce8767a`；原红基线 `35524b04`。原审查报告/坏状态证据未改写。
- 自动化保持 PAUSED；修复验证时未推送。用户随后明确授权 GitHub 交付，发布分支为 `origin/codex/af-main-refactor-continuation-20260831`；原共享分支不动，详见同日 HANDOFF。未部署或操作真实存档，未切新的默认入口。
- 2026-09-06 两份用户占位草稿保留、不暂存；其他工作树与 GCCZ 主体不改。
- 本轮代码已验证、本地提交；推送、部署/真实存档操作、新默认切换和广泛删除按明确范围另行处理。已有用户“其他成员验收过”的反馈保留，但没有本修复候选的对应实机证据。

## 收尾清单

| 范围 | 本轮结果 | 仍需完成 |
|---|---|---|
| F1 Hero asset ALL | 指定资产、modifier、RP、市场 owner；67 场景与旧红/变异通过 | 真实资产/商人职业/旧档 |
| F2/F3 Courier | 完整匿名 Prompt、复用捕获、权威主线程 completion、资格/取消/同 ID 替换、可见文字与 raw 分离；39 场景 | 更早的前置准备线程边界 NOT_FIXED；失败 MOOD 兼容审查 |
| F4/F5 Scene/BattleSpeech | 请求冻结、框选回退、双 waiter；30 场景；分类成功路径 18 场景 | 实际分类/演讲、场景动作及 UI |
| F6 TTS | 请求级取消与合法 FIFO；43 场景；独立 Native 延迟兜底 14 场景 | 实际声音/口型/暂停/地图对话 |
| Gateway | 实际错误 envelope 不当成功，晚结果拒收；40 场景 | 底层同步 Action 网络可中断取消、真实 provider |
| Bridge/清单/旧测试 | mandatory safety 独立，16/12/4；混合默认明确；旧测试按真实当前协议/行为更新且保留反例 | 运行时/实机隔离 |
| 其余领域/Native/旧代码 | 没有广泛删旧或切 Native；公开 ABI/存档 owner 保留 | 补齐主体/Native 与受影响模块桥的兼容回归；额外业务问题转交对应 owner |
| 统一离线验收 | 六构建 PASS；37 个完整 C# runner PASS；Python 26 检查 PASS＋1 预期缺证据 BLOCKED；Policy 四安全子集 PASS | 三个 SDK 10 工具未运行；所有真实验收 |
| 文档交付 | 同日验证报告、证据索引、HANDOFF、制作组文案 | 发布/最终打包/真实回滚演练尚未交付 |

## 接续顺序

1. 先读 `docs/audits/2026-09-09-courier-thread-boundary-plan.md`，完整设计并实现前置 prepare/network/complete，保留 aux/semantic fallback、记忆选择 mode、persona 和规则资格。不把含同步网络的整个 builder 搬到主线程。
2. 依据新架构 HANDOFF 整理主体功能基线和内部/外部接入矩阵，再接续主体及 Native。旧 20 领域清单仅用于选择受影响接缝的兼容回归，不重写额外模块业务。
3. 清理只针对有调用/反射/存档/外部 ABI 证明的已替代路径。共享 facade、旧事件或 Legacy 文件名本身不是删除依据。
4. 对最终同一候选补齐真实 Campaign/Mission、旧档、live Economy、AFEF、TTS、第三方与打包回滚证据；最终门槛满足前不标项目 DONE。

## 验证来源与协作

本轮报告：`docs/audits/2026-09-09-refactor-recovery-fixes.md`；机器可读索引：`docs/audits/2026-09-09-closeout-verification.json`。原始日志、阴性对照、源码及产物摘要在 `.tmp/closeout-20260909/`，不上传真实日志/凭据或忽略目录。

根代理统筹集成/公共台账/构建；经济、场景、TTS 分范围实现并交叉复核。一次只由一个 owner 写同一区域，只有根代理操作 Stage/提交。全部代理已停写，最终源码摘要与 `9a4a26dc` 规范化换行后的 Git blob 一致。
