# 当前交接：主体 J13d2 增量扫描 owner 有限切片完成，继续主会话/Issue（2026-09-25）

- **状态**：分支 `codex/af-main-refactor-continuation-20260831`，产品/回放 `6eb3d170`；J13a–c/d1 及 d2 opening、冷却、扫描/Issue 派遣 pending 各有限片有离线证据，**J13d2/J13 仍 `ACTIVE`**，不提前进入 d3/J14。
- **本片证据**：唯一 Social owner 负责扫描实例、批大小、统计/排名、精确完成；原每帧 16 队伍/1.5ms 上限保留。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `507BE34AEDA844518AC537DC43E05C773D96DDA1FEACB36EBF43DB919FF0580C` 的 Phase8 全通过，V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 546 锚点两模式通过。实机帧耗时、Quest/旧档/provider/UI/音频仍 `NOT-RUN`。
- **下一条具体动作**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)读主动主 session 启动/失效/取消/完成的全部消费者并归属 Social，再关闭 Issue offer/in-progress/turn-in/完成回执，按[原计划](docs/plans/j13-domain-owners-plan.md)的 d2 聚合退出门验收。未 push、Stage、部署、打包、游戏/外仓写入或改自动化；`.dotnet-cli-home/` 不动。

## 以下为 d2 冷却切片交接（历史）

# 当前交接：主体 J13d2 冷却/扫描节流有限切片完成，继续 d2（2026-09-25）

- **状态**：实际分支 `codex/af-main-refactor-continuation-20260831`，产品/回放 `43566f0f`；J13a–c/d1 保持有限 `OFFLINE_VERIFIED`，d2 opening/派遣 pending 首片及本冷却片有限 `OFFLINE_VERIFIED`，但 **J13d2/J13 仍 `ACTIVE`**。用户恢复推进的请求不授权 push、Stage、部署、打包、外仓写入、自动化或 J14。
- **本片/证据**：唯一 Social owner 接管 Hero/类型/外交话题冷却与 global/lastScan、存档导入和小时裁剪；host 保留 Campaign/MCM/DTO 适配。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建均零警告错误；当前 Debug 1.4 SHA256 `D8C3AC41C447D13CE6DB3FA2D6F270C4CF811D3BB3EE6E61E736A8394E9A51D2` 的 Phase8 全通过；V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 539 锚点两模式通过。真实 Campaign/Quest/旧档/provider/UI/音频/帧性能仍 `NOT-RUN`，不可当实机验收。
- **下一条具体动作**：按[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)先读 `ProactiveNpcRequestBehavior.cs:410,528,4288` 的资格/增量扫描/主 session 转换并做下一个可回放闭包，随后 Issue offer/in-progress/turn-in/完成回执；再依[原计划](docs/plans/j13-domain-owners-plan.md)推进 d3/d4、e、f、g。不要以本片替代完整 J13 离线目标。`.dotnet-cli-home/` 未跟踪且原样保留；需撤销以定向 inverse/revert，不 reset/改历史。

## 以下为 d2 前一切片交接（历史）

# 当前交接：主体 J13d2 生命周期首片离线验证，d2 整包继续（2026-09-25）

- **工作区与状态**：实际分支 `codex/af-main-refactor-continuation-20260831`，最新产品/回放 `580a1466`。J13a Weekly、J13b Kingdom、J13c Persona、J13d1 Notoriety/Romance/Recruitment 维持有限 `OFFLINE_VERIFIED`；d2 的主动 opening 与原版 Issue 同伴窗口迟到回调首片有限 `OFFLINE_VERIFIED`，但 **J13d2/J13 仍 `ACTIVE`**。此前暂停/推送交接只作历史，不授权本轮再次推送。
- **本片证据**：原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `DDFBE3B5BE81CCED67D110524EE01F9AADDAC273D5974184E1367A3D49401300` Phase8 含新增生产回放通过，V1 119/四 DLL metadata 1060、PersistenceIdentity 142 key/type 对/36 behaviors、source inventory 7、代码地图 531 锚点两模式通过。真实 Campaign/Quest/party-screen、旧档、provider、UI/音频、帧性能仍 `NOT-RUN`；旧 Persona/Channel source-parity 与 terminal harness 问题未由本片解决。
- **下一步**：按[主台账当前段](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[原计划](docs/plans/j13-domain-owners-plan.md)继续 d2 主动资格、冷却、session 生命周期，以及 Issue offer/in-progress/turn-in/完成回执；再按既定顺序推进 d3/d4、e、f、g，不缩小 J13 离线目标或提前 J14。[代码范围](docs/architecture/af-framework-code-scope.md)和[代码地图](docs/architecture/af-framework-code-map.json)标出已迁与保留 host。`.dotnet-cli-home/` 未跟踪且原样保留；未 Stage、部署、打包、写游戏/外仓、修改自动化或推送。需撤销时对 `580a1466` 作定向 inverse/revert，不 reset/改写历史。

## 以下为历史交接，不作为当前执行指令

# 历史交接：J13 暂停于 Weekly a2（2026-09-25）

- **停点与半途改动**：当前分支 `codex/af-main-refactor-continuation-20260831`；截至本交接前的最新提交 `66eda319`。本次中断只发生在阅读/设计下一步多波协调归属期间，尚未编辑生产或测试代码；无未提交的已跟踪改动。唯一未跟踪的 `.dotnet-cli-home/` 保留原状，不提交、不清理。
- **实际完成**：J13a 的 a1 调度/材料已有限 `OFFLINE_VERIFIED`；a2 请求/完成生命周期仍 `ACTIVE`。最近产品提交 `78b87434`，回放提交 `c0221d02`：部分提交与异常恢复、双排队波次源失效均有受控回放。产品切片经原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8、V1 119/四 DLL metadata 1060、入口 11/source 7、492 锚点地图已通过。交接本身未重跑构建；这些证据不等于实机或完整多波验收。
- **继续时的第一步**：先按[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)复核 Git、代码地图和候选新鲜度；完成 a2 真实 minute orchestration/Campaign tick、部分提交＋UI 显式重采＋一次发布组合、提交工作量/积压和余下动态源门禁，随后 a3，再依计划 J13b–g。不要把已排队两波 fixture 当作真实 60 秒多波。实机、旧档、provider、音频、帧性能仍 `NOT-RUN`；本交接不授权 Stage、部署、打包、写游戏/外仓或 J14。

## 以下为双排队波次切片交接（历史）

# 当前交接：J13a a2 双排队波次源失效回放（2026-09-25）

- **最新切片**：`c0221d02` 为真实 `EnqueueWeeklyWaveLaunchAsync`/`ProcessPendingWeeklyWaveLaunches` 补受控双排队波次回放：每次 pump 只启动队首，同周源变化使第二波网络前取消。当前 Debug 1.4 SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8 全回放通过；产品未改，沿用前片原脚本六构建通过。**没有**实测 60 秒延迟或真实 Campaign 多波；a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE。实机、旧档、provider、音频、帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：补真实 minute orchestration/Campaign tick 与部分提交、UI 显式重采、一次发布组合，并量化提交工作量/积压。见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为部分提交与异常恢复切片交接（历史）

# 当前交接：J13a a2 部分提交与异常恢复切片离线闭合（2026-09-25）

- **最新切片**：`0a1d6c8d` 补真实 pending commit 的“已有胜出者 + 另一目标 RPM 失败”组合回放；`78b87434` 使提交异常只对未完成目标生成需显式重采的恢复上下文、结算等待者，并尝试幂等补发已写记录通知。原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `2268E88063FA5BC640EB61976A554203E4C147E6859EA7F4264603A23EC8493A` 的 Phase8 显式候选、V1 119/四 DLL metadata 1060、入口 11/source 7、492 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；live 弹窗、实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：补多 wave/Campaign tick、真实 UI 显式重采/一次发布组合与提交工作量上界；核对余下动态源投影，再判定 a2 有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为批量失败恢复元数据切片交接（历史）

# 当前交接：J13a a2 批量失败恢复元数据切片离线闭合（2026-09-25）

- **最新切片**：`683987dd` 修复批量失败到暂停恢复 UI 的分类丢失：按失败目标对应批次传递真实 RPM/配额/Retry-After/尝试数，HTTP 后续成功清除旧 429 标记。四目录复核后原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `C22AC98C51B314330927FB848E80D9B2C9D80F75E01AE85C81BB9EE50187C7C4` 的 Phase8 分类/目标隔离回放、V1 119/四 DLL metadata 1060、入口 11/source 7、490 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；live 弹窗、实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：补多 wave/Campaign tick、部分成功/失败 UI/显式重采和一次发布的组合回放；核对剩余动态投影源状态，再判定 a2 有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为王国拓扑事件失效切片交接（历史）

# 当前交接：J13a a2 王国拓扑事件源失效切片离线闭合（2026-09-25）

- **最新切片**：`dc2dd917` 将六个已注册的王国/家族变更事件接到 Weekly 修订 owner，补足无同周材料写入时当前统治者、归属等 live 投影的失效信号。四目录复核后原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `2D1E29B437B67FF955BF2511A3749B3FB8CFC54B46E0723F65FBAA746294C942` 的 Phase8 真实空王国事件负例、V1 119/四 DLL metadata 1060、入口 11/source 7、487 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：覆盖多 wave/Campaign tick、部分失败 UI/显式重采和一次发布的组合回放，核对余下动态投影的源重验；再决定 a2 是否满足有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为源素材修订切片交接（历史）

# 当前交接：J13a a2 源素材修订门禁切片离线闭合（2026-09-25）

- **最新切片**：`d4443bcb` 将同周事件素材、NPC 行动和开局概要的请求期修订快照接入自动/手动批量周报、每波发起及写前重验。获准四目录复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `1560FCA28974CEE714465BCF8B042BC8777B5A012B30625D2F4DAC5F683799E5` 的 Phase8 真正 block 写前拒绝回放、V1 119/四 DLL metadata 1060、入口 11/source 7、484 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：核对 live Kingdom/统治者等未入修订 owner 的动态素材投影；补多 wave/Campaign tick、部分结果 UI 与一次发布的组合证据，之后才评估 a2 有限退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)和[代码地图](docs/architecture/af-framework-code-map.json)。`.dotnet-cli-home/` 不动。

## 以下为跨批次部分结果结算切片交接（历史）

# 当前交接：J13a a2 部分结果跨批次结算切片离线闭合（2026-09-24）

- **最新切片**：`d6fea0e6` 将跨批次已结算 ID 和暂缺目标归 Weekly owner；重复缺失只登记一次，后续批次解析成功会移除早先暂缺，最终只计仍未恢复目标。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `3587CB727BBE95A2B5F3B3CD4289165B41C0204288A6280937B2CC9AB3BAFCF4` 的 Phase8 owner/真实 finalizer 回放、入口 11/source 7、V1 119/四 DLL metadata 1060、477 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：设计并验证与周界/目标相关的 live 素材源提交重验，不能靠目标事件记录状态代替；随后补多 wave/Campaign tick、部分结果 UI 与一次发布组合回放，判定 a2 退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为自动重试配置主线程边界切片交接（历史）

# 当前交接：J13a a2 自动重试配置主线程边界切片离线闭合（2026-09-24）

- **最新切片**：`4721460f` 保留首轮同波并发，第二/三次批量自动重试经 Campaign tick 主线程读取当前 MCM 配置并启动原 gateway；读档结算等待者，旧代/退役 owner 在配置读取前拒绝。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `47452DC940B1E6D4199E6449928088A8303D06BDDDF83D8B934F49F2F0145369` 的 Phase8 强制后台/清理负例、入口 11/source 7、V1 119/四 DLL metadata 1060、472 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：核对独立于已保存目标记录的 live 素材源变化；补多 wave、部分失败、积压与一次发布的组合回放，再判定 a2 退出门。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为 minute wave 主线程发布切片交接（历史）

# 当前交接：J13a a2 minute wave 主线程发布切片离线闭合（2026-09-24）

- **最新切片**：`5ca5e5c3` 把每分钟每波的 UI 通知和初次请求启动排入 Campaign tick 主线程，旧代/读档结算未发波次等待者，原 60 秒间隔与批次汇总不变。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `114EFBCB2B1053BEA585E06AE99A8BE99F79B0CFE04DFAB7D4466D0747960288` 的 Phase8 来源/新鲜度及原队列回放、入口 11/source 7、V1 119/四 DLL metadata 1060、469 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；真实多 wave、实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：审查自动重试延迟后的可变 API 配置读取，把配置快照/执行边界闭合；再处理独立 live 素材源变化及部分失败、积压、一次发布组合回放。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为旧代自动重试拦截切片交接（历史）

# 当前交接：J13a a2 旧代自动重试拦截切片离线闭合、多 wave 待核（2026-09-24）

- **最新切片**：`39606755` 把原请求 generation 传入批次自动重试，发 API 前与回包后拒绝旧代，避免读档后延迟完成又外发旧请求。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `9A6376610A6CE7223AEEDC8FE509B69606A1FFF69ED54FDDF334BF7627C2D303` 的 Phase8/网络前旧代负例、入口 11/source 7、V1 119/四 DLL metadata 1060、466 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：审查后续 minute wave 的主线程通知/配置快照，独立 live 素材源变化，以及部分失败、积压和一次发布的组合回放；a2 全门禁未过。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为批次 Prompt 主线程准备切片交接（历史）

# 当前交接：J13a a2 批次 Prompt 主线程准备切片离线闭合、请求全链路待核（2026-09-24）

- **最新切片**：`f812ec1b` 避免手动/重试后续 minute wave 在后台构建含 live Kingdom/统治者的 Prompt；Campaign tick 按预算逐批准备，旧代/读档结算等待者，worker 未准备就在网络前拒绝。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `E68FF3F12D19C426E3D6611CC1BD24D2BB977838946A94E3689DBA3C13C30962` 的 Phase8/worker 负例、入口 11/source 7、V1 119/四 DLL metadata 1060、465 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：检查多 wave 调度的后台 UI 通知、API 配置快照及 live 源变更边界，补实际队列积压/部分失败/发布组合回放。此切片不代表 a2 收口。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为显式重采切片交接（历史）

# 当前交接：J13a a2 目标变更显式重采切片离线闭合、源状态与批量生命周期待核（2026-09-24）

- **最新切片**：`240c10aa` 将目标编辑冲突与 API/RPM 错误分开；用户明确点击后才重新采集当前周界内原失败分组，任一失踪则不发请求，原已完成分组不重跑。四授权目录复核后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `E7738A4EE42B65C625D3C65D9748A7ADB582BF1CFD0077664B91C0A71834112C` 的 Phase8/选择反例、入口 11/source 7、V1 119/四 DLL metadata 1060、462 锚点地图通过。UI 仅源码接线，live 弹窗/API NOT-RUN。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：核对并补足独立于目标记录的 live 材料源变更防护，以及 minute burst/部分失败/发布全链路；a2 必要门禁未过不标完成。见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为批量胜出者切片交接（历史）

# 当前交接：J13a a2 批量完整/短报胜出者切片离线闭合、恢复 UI 待施工（2026-09-24）

- **最新切片**：`32a8379c` 使迟到批量回包遇到同周同目标完整报/短报已有完成内容时计为已满足，不覆盖、不重复通知或发布；未完成编辑仍失败。四授权目录复核后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `9650264CA89CEFC48FD5D7FA9C52A66FEAF3AFB8395FCCF402DF76EAC064D6BE` 的 Phase8、全文/短报/错误周界反例、入口 11/source 7、V1 119/四 DLL metadata 1060、459 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：不完整编辑的失败 popup 仍无正确“重新采集素材”入口；先闭合这条恢复语义，再核对独立 live 源变更、minute burst/部分失败与一次发布。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 不动。

## 以下为手动重试防覆盖切片交接（历史）

# 当前交接：J13a a2 手动重试旧素材防覆盖切片离线闭合、恢复 UI 待施工（2026-09-24）

- **最新切片**：`8b1f703d` 将批量失败的原目标状态传入显式手动重试；当前目标变化时发请求前拒绝旧素材，未变组可重试。四授权目录复核后，Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `231869AC9A7D7854B5066C08D0D350E14EE8B5DD0F6A10ACE02E8DB0CD448F05` 的 Phase8/重试准入反例、入口 11/source 7、V1 119/四 DLL metadata 1060、458 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**，其余 J13 不抢跑；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：目标变更后的失败 popup 仍沿 API 修复/重试 UI，无正确的重新采集材料恢复动作；先闭合该恢复语义，再核对独立 live 源状态、minute burst/部分失败和一次发布。详细证据见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。`.dotnet-cli-home/` 保持不动。

## 以下为目标记录提交防覆盖切片交接（历史）

# 当前交接：J13a a2 批量目标记录防覆盖切片离线闭合、源/重试门禁待施工（2026-09-24）

- **最新切片**：`d59848d1` 在 `MyBehavior.cs:42429–42464,45326–45397,45558–45742` 捕获目标事件记录原状态，pending commit 期间重验当前 owner、generation 和目标记录；编辑/已完成胜出者/保存素材变化不被迟到回包覆盖。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 SHA256 `FCAB4573DD0A2DE6FC9524BF8A786C93D7AC1C9A35738C46ABFA47FC30FAA066` 的 Phase8、目标状态反例，入口 11/source 7、四实现 DLL metadata 1060、456 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**；实机/旧档/provider/音频/帧性能 NOT-RUN，未 Stage/部署/打包/推送。
- **下一条具体动作**：继续核对批量请求捕获时点之外的 live 材料源和手动/自动重试：目标被用户编辑后显式重试不能重新捕获并覆盖；再处理 minute burst/部分失败/popup 恢复，之后才评估 a2 有限退出门。见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，保持不动。

## 以下为分块材料游标切片交接（历史）

# 当前交接：J13a a2 分块材料游标切片离线闭合、源重验待施工（2026-09-24）

- **最新切片**：`bd972386` 将批量周报单 block 材料克隆下标/结果归 Weekly owner，主线程仍按预算逐项调用并保留原 Upsert。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `01AB3C5A0069C223B3A06104BF45F8947E7B6CE09CA6DFAEC076239EB6867F99` 的 Phase8/游标回放、入口 11/source 7、453 锚点地图通过。**a1 有限 OFFLINE_VERIFIED，a2/J13a/J13 ACTIVE**。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：先复现活动批量 `MyBehavior.cs:45517–45667,44217–44261` 现有记录被迟到结果覆盖、源材料变化未重验的反例；审阅自动/手动重试的捕获时点和目标身份，再补主线程 owner/目标/源门禁。该缺口未过之前不报 a2 完成。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 commit 队列切片交接（历史）

# 当前交接：J13a a2 批量 commit 队列切片离线闭合、a2 继续（2026-09-24）

- **最新切片**：`e4a78829` 将批量周报 worker→主线程 commit 队列的 FIFO、无工作快路径、队首隔离、读档清理等待者归 Weekly owner；原私有嵌套 DTO、generation 与主线程写入保留。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `CF2279401002FABF2F140289AF834C7806836631F6E198783EFAF5129BDEAD86` 的 Phase8/新队列回放、入口 11/source 7、451 锚点地图通过。**a1 已有限 OFFLINE_VERIFIED，a2/J13a/J13 仍 ACTIVE**；请求发送/重试、分块提交/源重验、失败恢复、a3 和 J13b–g 未完。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：核对 `MyBehavior.cs:42876–42972,45302–45373,45513–45650,45668–45752` 请求与分块提交状态，先将有界分块提交的状态转换移交 Weekly owner，验证大积压、旧代、部分失败；之后继续 a2 其余模式。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 a1 收口交接（历史）

# 当前交接：J13a a1 离线闭合、a2 请求生命周期待施工（2026-09-24）

- **最新门禁**：`577f7cac` 用当前候选生产 owner 做同输入同步/分阶段材料顺序、模式、周界组合回放；结合调度/材料责任切片、schedule smoke、Weekly outcome contract、Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error、Phase8、四 DLL metadata 1060、449 锚点地图，**J13a 的 a1 有限 `OFFLINE_VERIFIED`**。它不代表 a2/a3 或 J13 全包。当前 Debug 1.4 候选 SHA256 `9FE600156D304C3E97B5682AFF1F720A8BFFA8A7DD7713E0A5B039354168349A`；实机/旧档/provider/音频/帧性能 NOT-RUN；初始 O(N) 快照及单组原子成本无帧耗时保证。未 Stage/部署/打包/推送。
- **下一条具体动作**：按[J13 计划](docs/plans/j13-domain-owners-plan.md)的 a2，先核对 `MyBehavior.cs:42400–42800,43000–43700,45500–46900` 自动/手动、多模式、minute burst、批次重试、按需全文、pending commit/发布/失败的真实生命周期和 SaveRuntimeGuard/源重验，再做第一个请求责任切片。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为材料游标切片交接（历史）

# 当前交接：J13a Weekly 材料三阶段游标切片离线闭合、a1 待整体校验（2026-09-24）

- **最新切片**：`cd37d2f2` 将聚合、PromptMaterials、Batch Prompt 三阶段游标与完成状态归 Weekly owner；宿主保留主线程操作和预算检查。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `9FE600156D304C3E97B5682AFF1F720A8BFFA8A7DD7713E0A5B039354168349A` 的 Phase8/游标回放、schedule smoke、Weekly outcome contract、四 DLL metadata 1060、入口 11/source 7、449 锚点地图通过。**a1 仍 VERIFY，J13a/J13 ACTIVE**：同输入同步/延迟整体材料 parity 尚未断言，a2/a3 和 J13b–g 未完成。初始化 O(N) 快照及单组原子工作未证明帧耗时；实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：补同步/延迟同一日期/材料下 eligibility、分组、顺序、全文短报及周界集成回放，并审阅 `MyBehavior.cs:6033–6065` 快照一致性；a1 退出门过后进 a2 请求完成。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 PromptMaterials 组装切片交接（历史）

# 当前交接：J13a Weekly PromptMaterials 组装切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`c2d8565b` 将全文/短报材料组装和劫掠归并归 Weekly owner，自动、同步和独立劫掠构造入口接通；原 host 保留 live 解析与专用素材文字 helper。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `C319221CE7B0D9C523EA305DBFA315B71FE6EABC46D612A86540812B1C348DE1` 的 Phase8/新增材料回放、Weekly outcome contract、入口 11/source 7、447 锚点地图通过。**a1/J13a/J13 仍 ACTIVE**：自动三阶段游标与初始快照预算、a2/a3、J13b–g 未完成。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：处理 `MyBehavior.cs:6244–6315` 的聚合/Prompt/Batch 三阶段推进状态与预算重入，定向验证空组/预算边界；再有限关闭 a1，转入 a2。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为全文/短报选择切片交接（历史）

# 当前交接：J13a Weekly 全文/短报材料选择切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`12f9e3d2` 将邻近前三王国全文、其余短报及空邻近回退规则统一归 Weekly owner；同步预览和延迟自动准备共用，后者缓存选择集合。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `AD1E082F0DC73ADE5E17AD6D0B87BC1A85916309CAA620FF2A8954B196397E92` 的 Phase8/选择规则回放、入口 11/source 7、444 锚点地图通过。**a1/J13a/J13 仍 ACTIVE**：PromptMaterials 具体构造与阶段游标、a2/a3、J13b–g 未完。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：检查 `MyBehavior.cs:37171–37240,6266–6321` 的 Full/Short PromptMaterials 构造、分批阶段和预算游标，转移一段完整材料责任并验证负例，然后推进 a2 请求完成。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为 action 游标切片交接（历史）

# 当前交接：J13a Weekly action 材料游标切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`8d934447` 将 recent/major action 游标和当前 Hero 缓存归 Weekly owner，单次至多跨一个 owner 边界或消费一条 action；连续 1000 个失效 owner 的预算反例回放通过。Debug/Release × 1.3/1.4 + Bootstrap 原脚本六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `E169B058AF9653A908B4814126CE09CB83CBFE6C35B8EE17903A67E845593537` 的 Phase8 回放、入口 11/source 7、442 锚点地图通过。**J13a/J13 仍 ACTIVE**；初始化 O(N) 快照及单组聚合仍未严格预算化，a2/a3 和 J13b–g 未完成。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：核对 `MyBehavior.cs:6039–6151,6244–6321` 一次性 source/owner snapshot 与单组聚合/提示准备预算，确定不改变源状态语义的增量化边界，然后继续 a2 请求完成、a3 回执发布。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为材料聚合切片交接（历史）

# 当前交接：J13a Weekly 材料聚合切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`f54fbcf6` 将预览材料克隆、分类/事件键分桶和重排归 Weekly owner；自动/同步预览共用，live 渲染仍在 host。获准四目录核对后原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 候选 SHA256 `5FF0628720BA2D06A27F12C99096AC8D82A087A3134526CACFAF80F5727FF1C9` 的 Phase8、新增聚合回放、入口 11/source 7、440 锚点地图通过。**J13a/J13 仍 ACTIVE**；单组聚合 O(M log M) 未严格预算化，a2/a3、J13b–g 均未完成。实机/旧档/provider/音频/帧性能 NOT-RUN；未 Stage/部署/打包/推送。
- **下一条具体动作**：读取 `MyBehavior.cs:6051–6389` 的一次性材料/action snapshot 与每组聚合/提示准备预算入口，构造单次工作量反例并迁移预算状态转换；之后才推进 a2 请求完成和 a3 回执发布。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。仅 `.dotnet-cli-home/` 未跟踪，不触碰。

## 以下为材料分组切片交接（历史）

# 当前交接：J13a Weekly 材料分组切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`008a3f84` 将 Weekly 分组优先级及世界/全文/短报批次划分真实归新 owner；原 host 保留王国资格和邻近状态捕获。当前 Debug 1.4 候选 SHA256 `746E51D79EC3C7F4CBF5DF2114320BA9CCF1BD2281A22007D36078BFCA126655`，Debug/Release 双版本+Bootstrap 六构建 0 warning/error，Phase8 与新增批次身份/顺序回放、四 DLL API metadata 1060、source inventory、438 坐标地图通过。**仍为 J13a_ACTIVE**：预览聚合/游标、请求/完成、回执发布和 J13b–g 未完成；详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。未 Stage/部署/打包/推送；实机、旧档、provider、音频、帧性能仍 NOT-RUN。
- **下一条具体动作**：从 `MyBehavior.cs:1494–1547,6144–6389,38550–38594` 核对预览/聚合游标与每日预算的单次工作量，再设计并接通 Weekly 材料 owner；保留主线程 Hero/Kingdom 捕获、同一 context 游标及同步/延迟生成一致性。不能直接把整个 private 嵌套状态改名或仅拆 partial。其后才进 a2/a3。当前 Git 仅 `.dotnet-cli-home/` 未跟踪，未触碰。

## 以下为调度切片交接（历史）

# 当前交接：J13a Weekly 调度切片离线闭合、全包进行中（2026-09-24）

- **最新切片**：`d1697338` 已将自动调度与待处理周次真实归 Weekly owner，Schedule/Text helper 归位；同步/延迟/叛乱恢复真实消费者接通。定向 smoke、Phase8 inventory 11、source inventory 7、435 代码坐标、Debug/Release 双版本+Bootstrap 0 warning/error、当前 Debug 1.4 候选 SHA256 `D45D3320F3A7520FDF72F92220808D43A1BB641D3EC6897F40F1AE05E0F7F218` 的 Phase8/Weekly replay 均通过。**J13a 整包仍 ACTIVE**，材料/批量请求/回执未完，下一步按计划继续；详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。

- **状态**：`J13a1_OFFLINE_VERIFIED / J13a_ACTIVE`，不是 J13a/J13 全包完成。`dd303847` 产品切片，`b240778f` 当前候选 replay 宿主。
- **门禁**：经用户授权且复核四目录后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 warning/error；当前 Debug 1.4 SHA256 `FEC1F04BB7F5B6BB4E4D025886A735FA44BE4A87E06331891FA377392F6E02F6`，Phase8 从该候选回放通过，错误 hash 负例拒绝；schedule smoke/outcome contract/source inventory/432 坐标地图通过。构建引用 1.3 `v1.3.15`、1.4 `v1.4.6`，不代表其他补丁版或实机。
- **下一动作**：按 [J13 计划](docs/plans/j13-domain-owners-plan.md)继续 Weekly 调度/材料、生成生命周期、回执发布，再按 Kingdom 等顺序推进。`.dotnet-cli-home/` 未跟踪且未触碰；未 Stage/部署/打包/推送。实机、旧档、provider、音频、帧性能仍 NOT-RUN。详细记录见[主台账当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)。

## 以下为首切片验证前状态（历史）

# 当前交接：J13a Weekly 首切片待完整门禁（2026-09-24）

- **状态**：`J13_ACTIVE / J13a_VERIFY`，J07–J12 仍 `OFFLINE_VERIFIED`。开工 `f3b79d4c`，产品切片 `83cc314b`，测试修正 `d12e8d65`；未标 J13a/J13 离线完成。
- **已改**：按需全文完成队列的锁、状态、owner/generation 受理、每 tick 两次提交、异常和清理等待者归 `WeeklyFullReportCompletionOwner`；`MyBehavior` 保留真实 UI/引擎薄入口、源材料重验及 Campaign/存档身份。未动自动/批量周报、玩法、Stage/部署。
- **已验**：相同 Program/生产源码的 net8 Weekly smoke（本机无 net6 targeting pack）、Weekly outcome contract、双 API Compile 集合、source inventory 与 432 锚点地图通过；均不能替代双版本构建/实机。
- **门禁与下一步**：原构建脚本会清理并重建工作区内 `bin/Debug/single_module_artifacts`、`obj/single_module/Debug`、`bin/Release/single_module_artifacts`、`obj/single_module/Release`，已核实位置/链接/内容并请求**仅这四目录**的清理确认；确认前不运行。之后跑 Debug/Release × 1.3/1.4 + Bootstrap、当前候选 replay，再继续 Weekly 自动调度/材料及 a2/a3。详细证据见[主台账 J13a1](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)与[代码范围图](docs/architecture/af-framework-code-scope.md)。`.dotnet-cli-home/` 未跟踪且未触碰；实机、旧档、provider、音频、性能均 NOT-RUN。

## 以下为规划交接（历史）

# 当前交接：J13 计划已就绪，尚未施工（2026-09-24）

- **状态**：J07–J12 保持 `OFFLINE_VERIFIED`；J13 为 `PLANNED`。本轮只有文档，没有新增产品验收结论。
- **新对话入口**：[J13 可执行计划](docs/plans/j13-domain-owners-plan.md)，第 1 节可直接复制作为启动指令；先 G0，再 Weekly → Kingdom → Persona → Social/Issue/WorldEvents/WarStats → 场景领域 → UI/Onboarding → 离线收口。
- **详细证据**：[主台账当前入口](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j13-plan-20260924)、[代码范围图](docs/architecture/af-framework-code-scope.md)。J12 产品终点 `5c3e7b0e`，保留责任和验证见 [J12 最终交接](docs/handoffs/2026-09-22-j12-final-closeout.md)。
- **实际定位**：规划基线 `0624d502`；工作树 `E:/AnimusForge-refactor-continuation-20260831`，分支 `codex/af-main-refactor-continuation-20260831`。新对话重新核实 Git；不使用历史 G: 工作树指令，保留未跟踪 `.dotnet-cli-home/`。
- **首包重点**：Weekly 的真实调度/材料/生成完成/回执发布 owner，保留唯一 Actions 提交、主线程捕获/回写、generation 和存档身份。不能只搬文件或拆 partial 就报完成。
- **已知前置**：核实本机 SDK/引用；处理测试旧路径和 Stage DLL 依赖。原构建脚本有产物目录清理，运行前需要精确范围确认；计划没有更改原构建流程。
- **未验证/未授权**：本轮未跑产品测试或构建；真实 Campaign/Mission、旧 SAVE、provider、音频和帧性能仍 NOT-RUN。未 Stage/Deploy/Package/push，未修改自动化、游戏、存档或外部工作树；不提前执行 J14。
