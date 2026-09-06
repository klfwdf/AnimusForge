# 阶段 8 功能对照修复（2026-09-06）

基线：`220b1dd5`。本轮新建的跟踪文档随意图提交建立基线；生产代码改动前仍保持原状。

## 验收目标

| 功能 | 必须保留/恢复 | 状态 |
|---|---|---|
| 臣属管理 | 全量臣属选择、非纳贡状态、按 agreementId 查流水、返回 | 已接线；VM 回放 PASS，实机待复核 |
| 信任查询 | 全量英雄/定居点、搜索、分项详情及商人信任、返回 | 已接线；VM 回放 PASS，实机待复核 |
| 终端生命周期 | ESC 退出、输入恢复、百科可见、子页面切换 | 已接线/双 API 编译，Gauntlet 实机待复核 |
| 战争记录 | 和平归档、再次宣战使用新记录、旧历史不丢、存档兼容 | owner 归档/列表往返 PASS，原生事件/旧档待复核 |
| 部署检查 | Bootstrap 与双实现实际一致、缺失/过期失败、XML 实际入口 | 离线 PASS |
| 存档契约 | WarStats 的 47 个键/类型被追踪，旧契约保留 | 142 keys / 168 bindings PASS |
| 标签字典 | 全量分页/搜索、完整说明/来源、索引说明、刷新、导出当前快照、返回 | 第二批生产 VM 回放 PASS；实机待复核 |

## 清理边界

只删除由调用链确认失效且功能已有替代的路径。三渠道 legacy facade、旧存档迁移、双 API 兼容与原版参考树不在本轮盲删范围。真实游戏验收未完成时，不以编译或 fixture 替代 LIVE/SAVE，不承诺零 BUG。

## 证据

实现、测试、删留清单和人工步骤在完成后补充。未进行游戏部署或运行。

### 部署门禁修复

删除 Bootstrap-only 哈希判定和 XML 子串判定，替换为三份 DLL 哈希与唯一 Bootstrap 加载声明。既有测试 fixture 的假 DependedModule 也已纠正。新增回归在旧实现上出现 11 个失败子场景，修复后 6 个测试方法（含 11 个负向子场景）全部通过；未访问真实存档或部署游戏。命令：`python -B -m unittest discover -s tools/LiveHostReadinessAudit -p test_*.py`。

## 本批实现与玩家视角核对

- 终端复用一套菜单列表，打开查询时枚举一次当前英雄/定居点快照；保留所有在世 NPC（包括旧入口支持的儿童）、信任等级格式、定居点合并信任 clamp、商人详情与人物/势力图片。搜索只过滤快照，分页每页 50 项，不再截断为 30。图片仅对当前页创建。无每帧全量扫描。
- 恢复全部臣属选择、宗主国管理说明与非纳贡状态。按选择的 agreementId 查流水；返回保留当前搜索/页码。空记录显示说明，长贡赋明细改为纵向排版，避免固定 Y 偏移互相遮挡。
- 终端移到原版百科 310 下方的 309，沿用原独立战争页的层级策略；增加暂停请求与配对释放、ESC/屏幕切换关闭、失败打开清理。战争页改为用户选中时才创建。每帧仅检查活动弹窗与输入，不在 Tick 查战争数据。
- MakePeace 事件立即结束对应 pair；事件和原有 Reconcile 共用同一归档/移除路径，重复结束无副作用，下一次宣战不再拿到旧 active record。未新增或更名 SyncData key/type，保留 WarStats v1-v5 读取。

## Removed / Kept

已删除：
- 旧信任查询 inquiry 路由、英雄/定居点浏览器和两个详情弹窗包装；保留并复用信任计算/详情格式器。
- 旧臣属 inquiry 选择/独立流水打开/文本 fallback 包装。
- `TerminalVassalageTributeHistoryPopup.cs` 与同名 XML；保留被统一终端使用的 `TerminalVassalageTributeHistoryPopupVM` 及记录 VM。
- 截断式 `TerminalTrustItemVM`、TrustQuery 专用列表绑定/旧 XML，以及未使用的 NPC trust 临时变量。
- Bootstrap-only 部署比对和 XML 子串入口检查。

仍保留：真实三渠道默认 legacy facade、旧存档/版本迁移、1.3/1.4 兼容。它们有活跃用途，不能通过删名字实现功能对等。第一批保留的标签字典旧详情/搜索已在下方第二批完成替代并删除；其他未完成逐项对照的菜单仍不盲删。原版参考源码不属于废弃 AF 实现。

## 本机验证

- 官方脚本原样执行，Debug/Release × 1.3/1.4/Bootstrap 共 6 项构建均 0 warning / 0 error；只做 project-local Stage。
- 参考版本：1.3 `v1.3.15.110062`、1.4 `v1.4.6.115628`。这不是本机 1.4.8 游戏运行证明。
- `PhaseEightParityReplayTests`：加载真实 project-local Debug 1.4 DLL，验证 123 项分页、跨截断边界的精确选择、全量搜索、空结果、详情返回、贡赋返回、关闭，以及战争 owner 幂等归档和 v5 保存列表往返。
- `PersistenceProfileConfigContractTests`：142 literal keys / 168 typed bindings / 43 symbolic sources；原 95 keys 的 source/ref/type 身份不变，新增清单项全部来自 WarStats 已有代码。
- `BridgeBindingContractTests`：16 bindings / 10 wired / 6 declared-only；Phase8 entry inventory PASS；XML 可解析；已删除符号的 C#/XML 活跃引用清零；git diff --check PASS。
- 日志：`.tmp/phase8-parity-build-Debug-final.log`、`.tmp/phase8-parity-build-Release-final.log`、`.tmp/phase8-parity-replay-final.log`（本机忽略目录）。

## 制作组交接与剩余门禁

用户于 2026-09-06 确认其他成员已做过验收、报告没有问题。本轮承认该既有基线，不要求重跑全套，但该回复未提供本轮新 DLL 的实测绑定。建议只复核本批受影响操作：

1. 打开终端，分别查询排列在第 30 项之后的英雄/定居点；输入中文名称，查看商人详情，返回仍是原过滤结果；空搜索结果可清除后继续。
2. 有两个以上臣属时分别选中查看；非纳贡状态、无历史记录、长明细可读，返回不跳回无关菜单。
3. 从战争表打开国家/人物百科，百科可见可关；终端 ESC 退出后鼠标/按键/地图时间控制恢复；禁用 U 入口后经图标进入仍能 ESC 退出。
4. 有伤亡记录的两国和平后，在同日再次宣战：前一场归入历史，新战役从空统计开始；存档/重载后各自保持。

本轮没有实际启动 Bannerlord、没有部署或操作真实存档，不将 VM/owner 回放冒充 Gauntlet、原生事件或 IDataStore round-trip。阶段 8 为继续收尾，不标 DONE，也不承诺零 BUG。回滚用本批提交的定向 revert，基线为 `220b1dd5` / 意图 checkpoint `a8eadce0`；不 hard reset、不删玩家数据。

### Debug Stage 二进制绑定

- `AnimusForge.Bootstrap.dll` SHA256 `3414DA2579CD9955234BE5FB91EB0CF5637F5A27FCA3832C450DA4EFA7B4F400`
- `versions/1.3/AnimusForge.dll` SHA256 `441ED57993F86C631EC5BE7455903A005C39A719467402D07D91DCB64BAAEF6E`
- `versions/1.4/AnimusForge.dll` SHA256 `BD3487AED67646330A0EA59F207E33B241D4345A6475CAC7D3526ED3A08C3DFE`

## 第二批：标签字典功能对照（VERIFY；离线完成）

基线 b8757240；补齐旧标签浏览器的搜索、分类/说明/来源详情、索引说明、刷新和导出当前快照，统一复用第一批菜单分页与详情返回。删除已被替代的旧 inquiry 菜单与只显示摘要的重复 VM/XML。仍只在项目内构建/回放，不部署，不改 LLM 标签语义、来源扫描规则或存档。


### 第二批变更、删留与证据

- 标签字典不再另造一套列表：复用菜单的 50 项分页、全量搜索和详情面板。`SearchTerms` 保存完整说明/来源，卡片提示截断不会截断搜索范围；只在打开/刷新索引时建立快照，键入时只过滤内存，不触发重新扫描。
- 恢复分类、完整描述、全部来源路径、扫描文件数、所有来源根目录。详情滚动显示，不再只给摘要或将来源截到 12 项；返回保留搜索和页码。
- 工具条保留索引说明、刷新、导出；索引数量与更新时间提供可见反馈。刷新替换当前浏览器，不叠加返回层级；空索引也可刷新和返回。
- 导出向原 owner 传递正在显示的快照，而非 null 触发第二次扫描；成功显示完整输出路径，失败显示可返回的错误页。隐藏工具条后的残留命令不导出。未修改来源扫描、标签语义、导出目录规则或文件命名规则。
- Removed：`OpenTagCatalogBrowser`、`ExportTagCatalogToModuleTxt`、`OpenTagCatalogEntryDetail` 及不可达 fallback；`TerminalTagCatalogItemVM`、独立 TagCatalog view mode、专用列表属性/XML；失效的刷新提示 helper。需要的纯格式化方法迁入 UI owner，不留重复实现。
- 最终 Debug/Release × 1.3/1.4/Bootstrap 六项均 0 warning / 0 error、Stage PASS。日志 `.tmp/phase8-tag-build-Debug-final.log`、`.tmp/phase8-tag-build-Release-final.log`。
- 生产 DLL 回放新增 73 条标签、300 字符以上说明、15 个来源、4 个来源根；分页、完整搜索/详情、快照绑定、导出文本格式、空索引失败可恢复、刷新不重复压栈全部 PASS；第一批终端与战争 owner 回归同时 PASS。日志 `.tmp/phase8-tag-replay-final.log`。
- 存档契约仍 142 keys / 168 bindings PASS；Bridge 16/10/6 与入口 inventory PASS；XML 可解析；废弃标签 UI 符号活跃引用清零；diff --check PASS。
- 未执行真实模块扫描或非空导出的文件写入；导出回放只验证生产格式器和空快照拒绝路径。Gauntlet 搜索/滚动/刷新反馈仍需制作组按实际画面观察，不能以以上回放代替实机。

### 第二批后的继续顺序

先对照周报的国家选择/日期排序/完整文本/返回与空状态，再审独立战争旧弹窗是否还有动态调用，最后回到三渠道默认 facade 的真实替代覆盖。仍在用的 owner、存档迁移和 API 兼容不得按名称删掉。此文只对已完成切片负责，不宣告全仓旧代码清零或零 BUG。未 push、未部署、自动化保持关闭。

### 第二批 Debug Stage 二进制绑定（覆盖前一批 Stage）

- `AnimusForge.Bootstrap.dll` SHA256 `F885348AD81C76B8D3F54FBEAF0B70769535E8BFA2A976B288B0DE23C0FF49F1`
- `versions/1.3/AnimusForge.dll` SHA256 `6F3D9B1B6EE33006FF28A89C6BC9135E050543722D98A333858DAACF083DB203`
- `versions/1.4/AnimusForge.dll` SHA256 `5EE01CFA1770509335F16783F796FB05D199C64D5DA316ED03AAEB1F1D9D6A54`

## 整体任务覆盖记录

2026-09-06 后续已不再逐小批交付；当前统一实现/验证/实际剩余问题以 `docs/handoffs/2026-09-06-integrated-phase8-handoff.md` 为准。前面的独立周报/战争弹窗已完成替代删除，最新 Stage 哈希也以该总交接为准。仍有活跃默认路径未迁移，不能宣称全部旧代码清零。
