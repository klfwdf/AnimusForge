# AF 重构原计划对照与复核清单

本文件包含**通用审查方法**和**绑定源码版本的历史问题清单**。它是用户要求保留在 skill 中的复核资料，不是第二份项目执行台账、不自动授权修改源码，也不把历史问题永久当成当前缺陷。任务状态、当前分支和实施计划仍由经核实的项目台账/HANDOFF维护。

## 1. 每次对照前先核实

- [ ] 核实用户指定的仓库、分支、远端当前 commit；与本地工作树、旧构建产物、其他分支的未提交改动分开。
- [ ] 读取原计划、当前状态入口及最新明确范围决定；区分当前用户指令与仓库记载的历史授权。
- [ ] 逐原要求登记：已实现、部分实现、延期/HOLD、转交 owner、明确不在本任务范围；后两项不是已完成。
- [ ] 分开判断：确认缺陷/约束缺口、已知未完成、范围/顺序调整、文档状态冲突；没有证据不指责未经授权偏航。
- [ ] 验证实际 caller → adapter/provider → state owner → completion/lifecycle，而不只看命名、目录、状态枚举或测试数量。
- [ ] 按职责而非DLL数判断模块化；AF主体不等于Foundation，internal契约不必一律public化，薄adapter不自动成为游戏Bridge。
- [ ] 列出本次实际运行、仅查阅历史记录、未运行/受限的检查；不能把未挂载、sparse排除或shallow缺历史误报为生产缺失。
- [ ] 如果只读审查，保留工作树不变；发现问题不自动修复、重置、切分支或恢复自动化。

长期规则见 [模块架构](plugin-architecture.md)、[运行安全](runtime-safety.md)、[验证](validation.md)、[台账与交接](ledger-and-handoff.md)。

## 2. 2026-09-12 审查快照

- 仓库：`https://github.com/klfwdf/AnimusForge`。
- 分支：`codex/af-main-refactor-continuation-20260831`。
- 审查 HEAD：`bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8`；最后生产切片：`9040d184998120d3476426337160403f08fdfee9`。
- 对照：本 skill 更新前的原维护规则与原执行清单。审查时SKILL、repository-structure、plugin-architecture、validation、interaction-pipeline、known-debt六文件与目标分支副本逐字相同。
- 结论：未证实整体架构被擅自做歪；分支记载了目标收窄，当前成果是主体局部边界、同DLL内部接口和只读API，不是原完整模块平台或全阶段DONE。
- 下列行号和状态**只属于以上commit**。在另一个revision复用前重新查代码/调用链/验证，不复制旧状态成最新结论。

### 范围事实，不作为缺陷

[2026-09-09范围交接:12–48](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/handoffs/2026-09-09-af-core-internal-public-api-handoff.md#L12-L48)记载主体+制作组internal接口/薄桥+子MOD public API范围，旧20领域改为兼容影响盘点，不再授权全部业务重写。同DLL策略本来符合原skill，不能据此要求拆出大量程序集。

[原清理台账:463–466](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/animusforge-refactoring-and-repository-reorganization-plan.md#L463-L466)和[决策表:84–90](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/animusforge-repository-boundary-decision-table.md#L84-L90)记载清理HOLD、来源许可未决。保留参考/用户资料不是自行违规；但HOLD也不等于仓库门禁已完成。历史决定不代替下一次会话的实施授权。

## 3. 复核项目与验收条件

以下checkbox表示**尚未在本审查中证明关闭**，不是自动待执行命令，也不改变项目暂停状态。

### R01 — 主线程提交的真实工作预算

**分类：已确认实现约束缺口；未实测游戏卡顿。Owner：Memory / scheduler boundary。**

- [ ] 复核并为实际jobs/records或耗时建立主线程预算，而不只限制回调个数。
- 证据：[MyBehavior.MemorySummaryMainThread.cs:39–42,92–109](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/MyBehavior.MemorySummaryMainThread.cs#L92-L109)每tick最多两个delegate；[MyBehavior.cs:4988–5048](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/MyBehavior.cs#L4988-L5048)一个delegate遍历三类全部累积结果；[波次实现:5133–5164](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/MyBehavior.cs#L5133-L5164)按RPM发请求但跨波累计至全批返回。队列sanitize去重/排序，没有任务数量上限。
- 对原约束：runtime-safety的最大工作项/时间预算、queue/backpressure要求；“2 callbacks”不能证明一帧工作量有界。
- 关闭证据：积压多任务、一个delegate含多结果、读档恢复队列的真实处理数量/耗时测试；其他tick贡献仍能推进；分块保留原顺序/结果接受/唯一提交语义，跨块重验owner/generation/source。若需要原子批次，必须声明最大批量与成本，不能盲目拆事务。

### R02 — 原总清单与新交接的“当前状态”冲突

**分类：已确认文档状态缺口。Owner：Repository / handoff。**

- [ ] 确立一个当前状态摘要，其余总台账/phase台账/HANDOFF指向它；旧段明确标历史或被替代。
- 证据：[原公共台账:3–14](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/animusforge-refactoring-and-repository-reorganization-plan.md#L3-L14)“最新”仍指旧审查；[closeout:15–23](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/phase8/project-closeout-execution-20260908.md#L15-L23)仍有未实施说法；[新框架执行记录:32–44](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/phase8/framework-v1-execution-20260911.md#L32-L44)已记录生产初版。最新HANDOFF可消解优先级，但旧入口仍易误导。
- 关闭证据：从原skill首选台账和根HANDOFF两入口得到同一revision、scope、pause/active和下一门禁；保留旧失败/授权/结果原文，不改成“当时已完成”；排除范围不勾DONE。

### R03 — 目录声明尚未达到完整运行manifest/组合闭包

**分类：文档已承认的未完成；不是“完全无依赖校验”。Owner：Foundation / composition。**

- [ ] 若任务仍要求原完整模块平台，补实际owner、模块/契约版本、required/optional能力、profile、持久化namespace、activation/effects等声明，并验证真实生产组合；否则显式留作延期。
- 证据：[InternalModuleDirectory.cs:35–60](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/Refactor/Modules/InternalModuleDirectory.cs#L35-L60)现有ID/契约版本/能力/必需依赖；[ModuleFrameworkRuntime.cs:112–120](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/Refactor/Modules/ModuleFrameworkRuntime.cs#L112-L120)真实三模块注册未提供依赖。现存缺依赖/版本/环算法是进展，不是假实现。
- 关闭证据：真实模块声明驱动生产校验而非仅fixture；required/optional缺失、版本、profile/namespace冲突覆盖；不以增加无消费者占位manifest凑齐字段。单DLL仍允许。

### R04 — Adapter目录不等于生命周期/故障隔离Host

**分类：已承认的过渡边界，原目标未完成；未证明为新回归。Owner：Foundation / lifecycle及各贡献owner。**

- [ ] 将真实启动、失败、停止/需重启和可逆资源所有权接到Host；或明确当前交付只承诺目录/adapter。
- 证据：[TeamModuleServices.cs:3–9](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/Refactor/Modules/TeamModuleServices.cs#L3-L9)三个固定无状态adapter；[ModuleFrameworkRuntime.cs:43–80](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/Refactor/Modules/ModuleFrameworkRuntime.cs#L43-L80)Ready/Stopped目录状态；[SubModule.cs:755–826](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/SubModule.cs#L755-L826)仍顺序调用旧owner Tick，外层异常重抛。目录没有因此管理全体运行资源。
- 关闭证据：真实Host部分启动失败释放可逆注册；provider失败→对应Failed/依赖Blocked；无关模块继续；stop/restart行为诚实，存档/不可逆引擎副作用不伪称可回滚。不能把所有异常改成吞掉来凑隔离PASS。

### R05 — 三渠道与后台输入仍未完全收敛

**分类：明确未完成，最新交接已承认。Owner：Conversation/Courier/Memory与GameAdapter线程边界。**

- [ ] Courier双向准备在所属主线程捕获游戏/session/persona/history输入；后台仅网络/纯计算；主线程再验收。
- [ ] 三类memory summary任务捕获不可变输入和source revision/fingerprint；同generation内来源变化也拒绝/显式协调。
- [ ] 按Native/Scene/Courier真实入口分别验证Prompt/role/规则/后处理/动作/AFEF/展示与重入，不由局部Native修复宣称三渠道完整。
- 证据：[CourierDeliveryBehavior.cs:4510–4561](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/CourierDeliveryBehavior.cs#L4510-L4561)Task.Run中仍读取session/Hero并调用名为OnMainThread的builder；[最新暂停交接:85–98](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md#L85-L98)列出三个Execute job的剩余live读取/fingerprint缺口。
- 关闭证据：强制yield/retry时读写线程见证、同代输入变更、换owner/目标、load/reset、晚返回；保留接力/旁听/玩家发言去重、权威后处理和事实唯一owner；不以短链/fake host替代真实主流程。

### R06 — 原仓库整理/来源门禁仍HOLD

**分类：有明确历史决定的延期，不是删除授权。Owner：Repository / content与依赖owners。**

- [ ] 保留HOLD与许可未决状态；只有新明确范围/来源决策后才分批整理，之后证明clean-clone与package闭包。
- 审查Git tree为21,963文件，根C#314；两参考树C#16,365不算AF生产源码。仍tracked的PlayerExports3,139、浏览器缓存252、工具dist9等只作结构计数，未读取私有载荷；sparse本地缺席不代表远端不存在。
- 原0/1未完成与后续主体接口进展分开，不能把本地另一分支的49源码迁移/419文件整理计入这个远端候选。
- 关闭证据：确切清理范围/用户数据保留/原备份、依赖来源与许可、干净检出准备、包allowlist/hash；未改变决定就继续HOLD，不催促删除参考树。

### R07 — 公共API与最终验收的完成边界

**分类：诚实的首版/未完成项，不是虚假成功。Owner：External API / compatibility / validation。**

- [ ] 若后续批准提交/写入/扩展能力，沿同一真实主体授权/执行/记忆链开放，并有版本、线程、生命周期与失败语义；否则维持显式NotSupported。
- [ ] 新候选的双API/Bootstrap构建、实际加载/子MOD二进制升级、代表性旧档、Campaign/Mission及真实provider分别验收，旧记录不自动覆盖新候选。
- 证据：[AfApi.cs:17–55](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/Api/V1/AfApi.cs#L17-L55)只读Catalog，其余能力NotSupported；[暂停交接:155–165](https://github.com/klfwdf/AnimusForge/blob/bd2ed35fef0b68eb9c9b56e6376d1594ecf6f0a8/docs/handoffs/2026-09-12-af-framework-pause-transfer-handoff.md#L155-L165)明确fixture/source/build/PE与LIVE/SAVE分层。
- 关闭证据：模块加载前/后/停用、版本不兼容、旧客户端、真实受支持请求闭环；公开DTO不泄露live对象/凭据，不重放已开始副作用。Ready只表示装配时不声称游戏可调用。

## 4. 当次证据的可信范围

已实际运行：源码坐标28项recorded/current均PASS、内部owner签名13项一致、设计ModuleCatalog校验PASS。渠道提取测试13通过、1项因shallow缺历史提交报错，属于部分结果。完整C# runner具有固定repo内输出等限制而未在只读审查中强行运行；未重跑Windows六构建、实机、provider或真实旧档。没有改源码、部署、提交或恢复自动化。

正向成果必须保留：真实typed adapter调用、明确不支持的API能力、唯一版本Bootstrap拓扑，以及Native/Memory局部真实主线程与提交边界。不要因本清单有未关闭项就推倒重来或批量删除仍有责任的旧入口。

## 5. 后续使用与更新规则

每次接续先把条目复制/链接到**项目唯一当前台账**的授权任务下，核实当前revision后再决定状态；本历史快照保持版本含义。若问题已修复，在后续审查记录注明新commit与验证，不把旧报告改写成从未存在。新增源码操作、切默认、部署、发布与清理仍需当前用户授权。
