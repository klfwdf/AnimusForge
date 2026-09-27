# 当前交接：J15 F4-C/D、ONNX 冷竞态与 Xihai 桥接修复已离线验证，F4/F5 仍未闭（2026-09-27）

- **状态**：`F4-C/D_OFFLINE_VERIFIED / F4-M_COLD_RACE_FIXED_OFFLINE / F5_PARTIAL / J15_NOT_COMPLETE`。产品/测试 `63138958`、`688ad6b8`，八项私人旧源退役 `6b2c4373`。`OnnxEmbeddingEngine.cs:306,457–529` 完成初始化后才发布 volatile 状态，模块 ONNX 强制门禁不放松；`deploy_module.ps1:505–537` 在受管写入前拒绝未知手改/坏 `FeatureBridges.json`。详证见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-current-20260927)及[范围图](docs/architecture/af-framework-code-scope.md)，[795 锚点地图](docs/architecture/af-framework-code-map.json)仅导航。
- **已验**：获准四构建目录、双 Stage 与 ABI/API 生成根预检后，原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建各 0 warning/error；四 DLL 各 7 默认资源，Release 双 API ONNX 当前 DLL 各 33+5；J15 内容 107 映射/**33 HOLD**、F4-C 合成部署、DataPaths 66、个人迁移、PlayerExports 25+两业务变异、PersistenceIdentity 142/36、PersistenceProfile typed 168、编辑器两个路径模式通过。编辑器秒级备份撞名由 `3d5a37f8` 修复，12 次连续保存各留独立旧字节、坏 JSON 保留活动文件；完整数据 smoke 仍未跑。八项/39,960 字节私人旧源 SHA 复核归档到忽略的 `local/j15-private-archive-20260927/legacy-module/` 后仅从活动源码退役，inventory unknown=0。两 Stage 各 123 项、Xihai Core 88、Native 55/旧 ABI 41+41、公开 API 158/四 DLL metadata 1620、两本地测试 ZIP 各 123 项与 Stage 122 非 XML 字节相等、禁入路径 0；前候选完整 Release Stage 在全新虚构安装根安装 123 项、已知旧配置升级/私有备份、未知手改前置拒绝、ONNX 等三哨兵和 no-op 均通过，**不等于真实部署或发布完整资源**。
- **新候选 `4304397c`**：`AfCompatV130.IsExpectedRecordedPlayerMessageMethod` 精确对齐当前 `RecordPlayerMessage` 的 bool/六参数；StaticVerifier 旧 Stage 10/3 红例中的桥签名失败消失，新 Debug/Release Stage 各 11/2，剩余两项同属 TPAC 资源。原脚本新候选六构建均 0 warning/error、两 Stage 各 123 项；Native 55、旧 ABI 41+41、公开 API 158、四 DLL 元数据 1620、Release 双 API ONNX 各 33+5 再验通过；两新本地测试 ZIP 各 123 项/122 非 XML 字节匹配；当前完整 Release Stage 在新虚构根安装 123/123 SHA 相同、三哨兵保留、no-op 与坏配置前置拒绝通过，原合成部署契约的回滚/中断/旧默认升级也通过，均非真实部署或发布。详细红绿及候选 SHA 见主台账；编辑器仓内合成根被产品拒写门禁正确拦截，未放宽门禁、未碰真实玩家库。
- **O6 完整 Stage 补证**：全新仓内虚构目标对当前 Release Stage 123 项在第 61 项前注入同步故障，旧受管文件由私有备份恢复、其余 Stage 路径撤销，三未受管哨兵未变；之后成功安装 123/123 SHA 相同，no-op 无新 Recovery。另在独立合成根于第三项前让测试子进程突然退出，实见两项部分安装/`activating` 记录；再次部署写前拒绝，按 manifest/SHA 手工恢复旧状态后重试 123/123 通过。仅合成手工恢复，不是产品自动续传或真实游戏 L4，详见主台账。
- **O4 当前只读复核**：Bridge 绑定 16/12 wired/4 declared-only、fixture 10/6、存档身份 142/36 与配置 typed 168 均 PASS；Xihai Core/测试源码相对前候选未变，Runtime 桥由当前 StaticVerifier 另验。只读/fixture 结果不是运行时或实机签收，详见主台账。
- **HOLD/下一步**：F4-U 33 GUI/缺三 atlas、F4-A TPAC 历史 manifest 身份与来源/许可未闭；25 项图集源图片均在且 XML 尺寸/坐标不冲突，其中 4 个 `.png` 实为 JPEG。远端 `origin/main` 的 1.67 MB TPAC 与本仓字节相同，**实际包含 `nacisword1`**；将它仅加入全新合成 Release Stage 后 StaticVerifier **13/0**，纠正此前把正式 Stage `11/2` 归因于小包内容不足的推断。正式双 Stage/ZIP 仍未纳入 TPAC，远端没有 11.2 MB manifest 版本或三张 GUI sheet；不能以合成通过替代来源/分发权、游戏视觉或实际 Stage 签收。manifest 的 F: 原始来源及约定 G: 外仓当前均不存在。编辑器完整 smoke 待获准外部合成根，M9 两个实际版本和 L/S 新装/旧档未验。O7 当前只读清单 `tracked=19511 / unknown=0`、7 单测及代码地图 795 锚点双模式 PASS，但不代表资产来源签收。无真实 Deploy、游戏/玩家库/外仓写入、推送、发布或游戏启动；历史 Git 含私密资料，切勿推送。

## 以下为本轮之前的交接历史（相冲突状态以上段为准）

# 当前交接：J15 F4-M 模块 ONNX 契约代码与关联离线验证已完成（2026-09-27）

- **当前状态**：`F4-M_CODE_AND_OFFLINE_VERIFIED / F5_PARTIAL / J15_NOT_COMPLETE`。产品/测试 `df49d81a`、`7693bedc`、`43e8bcf2` 将门禁、embedding 与 reranker 统一到当前 `Modules/AnimusForge/ONNX`，退役模型外迁 CLI 的执行入口与 ModelsLock 运行资源；用户 Models/Recovery/仓内忽略副本保留但不再是运行来源。代码导航见[范围图](docs/architecture/af-framework-code-scope.md)与 792 锚点[地图](docs/architecture/af-framework-code-map.json)，一基坐标、证据和替代关系见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-onnx-f4m-implementation-20260927)，规格仍为[J15 第 9 节](docs/plans/j15-content-profile-plan.md#j15-onnx-install-contract)。
- **已验/边界**：Debug/Release 的 1.3、1.4、Bootstrap 六构建均 0 警告/错误；四实现 DLL 各 7 默认资源；DataPaths 66 checks、旧模型迁移拒绝、J15 内容/ManagedDeploy 合成契约；当前 Release 双版本 DLL 对安装模块现有 ONNX 作只读离线真实推理，各 28 断言。Debug/Release Stage 各 123 项、本地测试 ZIP 与 Stage 集合及非 XML 字节对齐，均无 ONNX。没有部署、游戏启动、外仓或用户库/Recovery 写入、推送；测试 ZIP 不可发布。
- **下一步**：F4-C/U/A/D 的手改配置、GUI/atlas、TPAC/设计源、旧资料与来源/许可分别闭合；F5 其余同候选矩阵后，另获具名游戏写入/实测授权才做 M9 双实际版本新战役、旧档、暂停/保存退出、补模重启和检索。LIVE/SAVE 均 `NOT-RUN`，不可把离线 DLL 推理当实机验收。保留玩家现有模型及历史副本，不自动搬回或清理。

## 以下为本次实施取代的仅文档交接（历史，不再作为当前状态）

- **唯一当前入口**：[新版计划第 9 节](docs/plans/j15-content-profile-plan.md#j15-af2-final-state)，先做 [F4-M](docs/plans/j15-content-profile-plan.md#j15-onnx-install-contract)；实际代码坐标、替代关系与验证在[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-onnx-contract-correction-20260927)。[代码范围图](docs/architecture/af-framework-code-scope.md)未变，产品仍为 `f4280eed`。
- **用户硬要求**：ONNX 放当前 `Modules/AnimusForge/ONNX`，门禁和实际加载使用这里，缺必需文件就暂停/退出。主 ZIP 不带模型，玩家另装；不迁 AppData、不依赖模型迁移凭证、不装 Python/SDK、不跑命令。
- **明确纠偏而非补迁移入口**：F2 的用户模型根/Recovery 前置条件与保留的模块门禁冲突；`fbd71c38`、`95159efd` 的模型迁移/初始化方案撤销。计划已按此重写分类、F2 状态、F4-M 和 F5，并同步单模块说明；其他个人数据保护、内容归位和 F3 产物链不整体回滚。
- **先做什么**：用户授权实施后，优先一次接通模块根/原候选、门禁、两个引擎、模型外迁入口退役及 M1–M9 验收；再完成其余 F4/F5。既有用户 Models、Recovery、忽略副本不删不搬回，游戏恢复另按具名范围确认。
- **本轮只有四份文档**：未改代码/测试/构建脚本，未搬模型、未写游戏/用户库、未安装软件、未构建/迁移/运行游戏/推送。F3 原离线证据不等于 ONNX 正常；F2 模型回归仍 OPEN，F4/F5、实机/旧档未完成。后文均为历史，不能用“无需重做 F1–F3”跳过本次纠偏。

## 以下为被替代的计划交接（非执行指令）

### J15 F4/F5 计划重构历史（模型方案已撤销）

- **本轮仅文档**：用户要求先完善可执行计划，未授权恢复产品实施。新的唯一入口为[计划第 9.7–9.11 节](docs/plans/j15-content-profile-plan.md#j15-scope-decisions)，实际定位和验证记录见[主台账当前项](docs/animusforge-refactoring-and-repository-reorganization-plan.md#j15-f45-executable-plan-20260927)；[代码范围图](docs/architecture/af-framework-code-scope.md)未改，产品基线仍为 `f4280eed`。
- **纠正正式版要求**：撤销 `fbd71c38` 中将开发 Python 迁移脚本作为玩家初始化入口的错误方案。玩家不装 Python/SDK、不取源码、不敲迁移命令、不额外捆绑解释器；初始化/升级/恢复由 AF 产品侧承担，开发脚本只供开发维护。F5-L1 必须在无开发环境下验收；这仍是待实现规格，未改产品或安装任何软件。
- **已定边界**：未来 AF 模块设置 UI 不挤进 J15 假迁移；当前 MCM 存储保留；FeatureBridges 按内部随版配置管理，不新增个人覆盖层，但旧手改冲突不能静默覆盖。F4 明确五组输入/动作/退出门，补模型无旧安装的产品侧初始化与恢复；F5 明确当前 8 资源、同候选离线矩阵、两版本实机/旧档矩阵和确认点。
- **下一步**：用户明确开始实施后先执行 F4 的逐项冻结与已确认仓内切片；批量移动/真实资料/游戏覆盖按具名清单确认。没有改产品/测试/脚本、没有迁移/启动游戏/推送；F4/F5 与完整 AF2.0 仍未完成。

## 以下为 F3 停点与历史切片（非新的执行授权）

- **F3 产品/测试 `a8f69b57`、`82d8ce53`**：同一干净 Stage 装配服务 Stage-only 与部署；部署只更新 Stage 列出的受管文件，目标旧字节先入私有 Recovery 并做 hash，同卷候选替换，失败回滚，未完成记录拒绝重试。旧 `/MIR` 和安装资料合并函数已移除；旧双模块只读警告的虚构重跑未改旧模块且无新 Recovery。虚构游戏根 123 项 SHA 对齐 Stage，PlayerExports 未变；故障注入回滚、恢复、重复、中断、7 个未受管/旧模块哨兵与完整 J15 内容 runner PASS。Debug/Release Stage 各 123 项无输入重组稳定，两 ZIP 各 123 项集合及 122 个非 XML SHA 对齐；代码地图 **789** 锚点两模式通过。既有 C# 六构建产物未变，本切片未重编；**未写 D: 游戏目录、未实机部署/启动、未改原始 `.sav`、未推送**。详证见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[范围图](docs/architecture/af-framework-code-scope.md)。
- **停点**：用户明确“F3做完就停下”。F3 为 **OFFLINE_VERIFIED**，不是 AF2.0 完成或可发布；33 项未映射 GUI/TPAC 及其权利、剩余旧资料属 F4，F5 同候选总验收与 1.3/1.4 实机/旧档均 **NOT-RUN**。当前 ZIP 仅装配/隐私工程候选。下方为早前修订历史，出现“部署失败关闭”不代表当前实现。

- **F3 新候选防陈旧打包 `8b3f3b51`**：打包对 Stage 九项程序产物逐字节比对当前 `single_module_artifacts`，旧 Stage 在创建 ZIP 目录前被拒；Stage 重建仍只要求旧目录满足隐私/内容白名单，不因程序版本旧而误清未知文件。合成文件漂移反例 PASS；Debug/Release 以当前模型代码重组 Stage 各 123 项并重复一次，文件集合/hash 稳定；两 ZIP 与各自 Stage 的 123 项集合及非 XML 字节独立一致，PlayerExports/ONNX/日志/未获准资产均为 0。代码地图 **785** 锚点两模式通过。此为装配与隐私工程证据，GUI/TPAC 等必需资源仍缺，**不可发布**。
- **产品 `eb03ce28`、锁换行固定 `2f589e6b`、旧模型退役 `0188367a`、完成记录门禁 `44e91672`**：两 ONNX 消费者只从 typed `%LOCALAPPDATA%/AnimusForge/Models/{embedding,reranker}` 加载；内嵌 `content/models.lock.json`、私有完成记录和整组文件元数据一次性验证，旧模块路径不再日常回退。显式迁移对 D: 安装源 10 项与仓内源 5 项分别做完整组 SHA-256、私有备份、同卷候选和目标优先激活；安装 10 项激活，仓内 0 项激活且一组字节冲突保留，两源重复执行均无再次激活。独立复验活动组 10 项全组 SHA；仓内五个已跟踪模型字节在验证两套私有备份后移至工作区忽略的 `local/` 并停止跟踪，D: 原件及用户活动库不动。合成迁移涵盖冲突、损坏、并发目标、中断、磁盘满与重复；DataPaths **58** 项、四实现各 **8** 内嵌资源、当前 Debug/Release × 1.3/1.4 + Bootstrap **六构建零警告错误**、代码地图 **784** 锚点两模式通过。模型活动库约 1.14 GiB、私有 Recovery 约 5.09 GiB；锁格式修正产生额外已验证备份集，未清理。详情见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[范围图](docs/architecture/af-framework-code-scope.md)。
- **仓内 PlayerExports 退役 `7f30aaa6`**：迁移完成记录、仓内原 3139 项、私有备份与用户活动副本逐项大小/SHA-256 一致；旧仓内目录同卷移入忽略的 `local/`，移后再逐项复验，当前跟踪数 0。编辑器用户根/拒写旧路径复测通过；D: 安装原件、用户活动副本及私有 Recovery 均保留。**Git 历史仍含私人数据，未改历史且严禁据此推送。**
- **仍未闭合**：模型运行时未在游戏中启动；reranker 本地文件的上游身份与转换仓再分发权未证实，故仍不进客户端 ZIP，也不能宣称发布许可通过。F3 程序文件部署仍失败关闭；GUI/TPAC/设计源及 `UnnamedNpcProfiles`/`VoiceMapping.json` 等旧资料退役、F5 同候选 Stage/ZIP 与总验收继续。无游戏覆盖/启动、原始 `.sav` 修改、推送或 G: 写入；实机旧档 **NOT-RUN**，**F1–F5 未完成**。

## 以下为 F3 Stage/ZIP 交接（历史）

# AF2.0 F3 干净 Stage/ZIP 已验，部署和资源门禁未闭（2026-09-27）

- **产品/测试 `d7c16c06`**：原一键打包入口现只接收项目内经清单验证的 Stage；Stage 仅从 107 项 `content` 映射、唯一 XML、双实现/Bootstrap 和六项 hash 锁定私有 DLL 组装，不复制 PlayerExports、ONNX、日志、安装覆盖或源码其他文件。ZIP 不再“扫安装目录再排除”，逐文件匹配 Stage，版本只在 ZIP 中生成，不回写源码/Stage。旧 Stage 中与来源不同的 9 个文件先私有备份并复验；Debug/Release 新 Stage 各 **123** 文件，两次无输入重组的文件集合/hash 稳定；两种 ZIP 与 Stage 集合/hash 独立核对通过，未知文件及旧打包参数失败关闭。代码地图 **780** 锚点两模式通过。详细见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **未闭门禁**：部署入口目前失败关闭，未实现/验收“仅程序文件”的事务部署；33 项未映射 GUI 和 TPAC 等必需资产还须 loader、来源与权利核对或功能等价替代，当前 Stage/ZIP 不可宣称功能完整或发布就绪。ONNX 用户模型定位/全组锁、其余旧资料退役及 F5 同候选总验收也未完成。未覆盖游戏、启动游戏、修改原始 `.sav`、推送或写 G:；实机旧档 **NOT-RUN**。**F1–F5 保持未完成。**

## 以下为 TerminalSettings 接线交接（历史）

# AF2.0 TerminalSettings 用户根与迁移契约已验，继续 F1–F5（2026-09-26）

- **本轮产品/测试 `7c39b200`**：TerminalSettings 活读写改为 typed `UserData/Settings`，损坏旧文件不被保存覆盖，候选校验后原子替换；写失败时 UI 内存值回退。显式迁移器对旧安装/仓内单文件按私有备份、hash、冲突保留与中断续作处理。两真实来源只读盘点均为 **0**，故无真实设置激活，也未读取旧模块作为日常回退。合成迁移及 DataPaths **50** 项通过，当前六构建均 0 warning/error；代码地图 775 锚点两模式通过。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **下一步**：ONNX 定位及依赖锁、F3 干净 Stage/ZIP 白名单、F4 其余资料与资产许可/退役，之后 F5 同候选离线总验收。无游戏覆盖/启动、原始 `.sav` 修改、Stage/ZIP、推送或 G: 外仓写入；实机旧档与发布验收 **NOT-RUN**。**F1–F5 不标完成。**

## 以下为 Prompt 迁移交接（历史）

# AF2.0 Prompt 分层与真实迁移已验，继续 F1–F5（2026-09-26）

- **本轮产品/测试 `528031c5`**：普通/Policy Prompt 读取改为用户覆盖→随包默认→owner 安全回退；编辑写入用户根且候选原子替换，损坏覆盖保留而不回写模块或源码。日志、模型下拉缓存改用 typed 用户根；30 份随包 Prompt 默认已加入 SHA 锁。D: 安装来源 30 份 Prompt 已分别私有备份并逐文件 hash 验证，14 份非基线文件无覆盖激活，重复执行激活 0；安装原件未改。合成迁移、Prompt/Policy 契约 9055 断言、DataPaths 44 项、内容映射静态核验及当前 Debug/Release × 1.3/1.4 + Bootstrap 六构建通过。详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **仍未完成**：TerminalSettings、ONNX 定位及其迁移，F3 干净 Stage/ZIP 白名单，F4 其余资料与资产权利/退役，F5 同候选离线总验收。完整 J15 runner 会清理未授权目录，因此本轮仅运行其静态映射检查；游戏启动、部署、原始 `.sav` 修改、实机旧档与发布验收均 **NOT-RUN**。没有推送或 G: 外仓写入。**F1–F5 不标完成。**

## 以下为 PlayerExports 迁移交接（历史）

# AF2.0 PlayerExports 双来源真实迁移可恢复完成，继续 F1–F5（2026-09-26）

- **状态**：产品/测试 **`38c5effd`** 将游戏与编辑器 PlayerExports 接同一 typed 用户根、未迁移默认根失败关闭；**`d8acf666`** 修复首次真实迁移发现的 Windows 临时路径过长，并有合成长路径回归。安装与仓内两来源各 3139 项已分别备份到私有 Recovery，安装激活 3139、仓内因相同字节激活 0；独立逐文件 hash/大小与原件一致，两来源重复运行均无再次激活。首次失败停在备份第 51 项之前、无活动数据/完成记录，修复后在同一恢复材料上续作；D: 与仓内原件、原始 `.sav` 未删除/修改。此前 DataPaths 44 项、编辑器路径正反例、编辑器 App 和六构建、762 锚点地图仍有效，但它们不证明剩余 F1–F5。详细见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **下一步**：接普通/Policy Prompt、可编辑配置、TerminalSettings、日志/缓存、ONNX 的真实消费者及合成迁移/回退；之后 Stage/ZIP 白名单、其他旧资料迁移与仓内旧树退役。BAAI 原模型 MIT 标签不能直接证明 ONNX 转换仓文件可再分发，F4 许可仍阻断发布。无 Stage、部署、打包、游戏启动、原始 `.sav` 修改、推送或 G: 外仓写入；编辑器完整编辑 smoke、实机旧档与发布许可 **NOT-RUN**。**F1–F5 仍未完成。**

## 以下为 AF2.0 设计交接（历史）

# AF2.0 完整资源/数据终态方案，待实际实施（2026-09-26）

- **最新请求**：用户拒绝“保护项原位排除、缩范围收尾”，要求完整最终方案；不再重复之前的二选一问题。
- **方案**：[J15 计划第 9 节](docs/plans/j15-content-profile-plan.md#j15-af2-final-state)定义独立用户数据、唯一默认源、可恢复迁移、取消源码回写、纯 Stage 与发布白名单，分 F1–F5 有限批次。意图 `093e4d3a`；实际 consumer 坐标与范围变化集中于[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)，[范围图](docs/architecture/af-framework-code-scope.md)仍反映已实现的旧候选。
- **当前事实**：这轮只交付设计，生产代码/脚本/真实数据未动；此前 107 项资源映射和离线结果保持，但不能用于证明新终态已实现。不是整个 AF2.0 或 J15 完成，真实旧档、发布许可和迁移仍须对应验收。
- **下一工程**：F1 的真实读写矩阵、路径/默认覆盖契约及反例，再做 F2 实际接线。F1–F3 可用合成数据推进，不让尚未闭合的素材许可阻塞不相关工程；真实批量迁移/外写/删除另用具名清单。无部署、推送、全局安装或 J16/J17。

## 以下为已被完整终态方案取代的过渡交接

# J15c 默认源已归位并离线验收，剩余数据/素材边界待闭合（2026-09-26）

目标状态：**blocked**。`0815fb6b` 后复核未发现新增变更或解除保护项门禁的决定；同一缺口连续三个目标轮次仍在，独立工作已完成。待下述处置决定/来源凭据到位后恢复，不重跑既有门禁制造进度。

- **状态**：`J15c_DEFAULTS_OFFLINE_VERIFIED / J15_PARTIAL_HOLD`。产品 `9db8fa8b`，测试消费者修复 `a3fa77af`；30 份默认 Prompt 原样 R100 归位，映射 107，安装覆盖/运行路径不变。完整目标未缩小；详见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)、[J15 计划](docs/plans/j15-content-profile-plan.md)。
- **已验**：完整 107 项合成投影、四种安装状态及重复组装、Bridge 24/隔离 12、Policy 216/1406/UI 387、六构建零警告错误、四实现七资源、旧 Native ABI、1620 DLL 元数据和当前 DLL Phase8 均通过；755 锚点两模式通过。额外重复 runner 清理被自动审查拒绝，未绕过，新增静态段单独通过，证据层级见台账。
- **最新补证 `2b61f1c5`**：PlayerExports 独立合成测试 45 断言及四个行为变异拒绝通过，没有读取真实导出或清理旧 fixture。ONNX 五文件已匹配固定上游提交，不再是来源未知；分发许可与真实数据迁移仍未闭合。hash、源码坐标、失败诊断只记主台账；新增子测试不冒充整体 runner 重跑。
- **下一步**：数据合成保护、模型来源和剩余 GUI/素材的可用 Git/loader 证据已查完，细节见主台账。已请求保护项处置决定：用户明确批准原位排除本次迁移，或维持全范围并补数据备份/迁移要求及素材来源凭据；未答复，不自动缩范围。之后再作 J15d 退出审计，不把 HOLD 算完成。不重做已迁 Prompt、已验六构建或被拒清理。无 Stage/Deploy/Package/push/外写；实机、旧档、provider、Gauntlet、音频、帧性能 NOT-RUN，`.dotnet-cli-home/` 保留。

## 以下为 FeatureBridges 最小片历史交接

# 当前交接：J15c FeatureBridges 已归位，完整验证待授权（2026-09-26）

- **状态**：`J15c_FEATUREBRIDGES_VERIFY / J15_PARTIAL_HOLD`，产品/测试 `7bc2ffbf`。FeatureBridges 默认源按 Foundation.Runtime 原样 R100 归位；映射 77、旧根 HOLD 40，运行路径/正文/七嵌入资源/C# 行为不变。完整 J15 目标未缩小，J15a/b 不重做；详见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)、[J15 计划](docs/plans/j15-content-profile-plan.md)。
- **已验**：五项 J15 无写入静态函数、15 项无临时文件 Bridge 测试、16/12 Bridge validator、inventory 7 和 unknown=0、755 锚点两模式通过。修复当前 Bridge 清单两处遗留 RuleBehavior 源路径，未改历史记录或 runtime。
- **下一步**：待确认具名构建/测试生成目录清理，再跑完整投影、Bridge 隔离、六构建和候选 DLL 门禁；这些当前 NOT-RUN，不能继承 J15b 结果。CustomPrompts/用户数据/模型/素材等继续按台账 HOLD 与授权条件推进，未到 J15d，更不是 J15 完成。无 Stage/Deploy/Package/push/外写，`.dotnet-cli-home/` 保留；实机/旧档/provider/Gauntlet/音频/帧性能均 NOT-RUN。

## 以下为 J15b 历史交接

# J15b 已确认静态内容有限离线完成（2026-09-26）

- **状态**：`J15a/b_OFFLINE_VERIFIED / J15_PARTIAL_HOLD`；J15b 产品/测试切片 `04056ce7`。在 J15a 7 项基础上，69 个已确认静态 JSON/GUI/XML/语言文件按 15 个 module/foundation owner 逐文件 `R100` 迁入 `content/`，映射现为 76 项；运行时 `ModuleData`/`GUI` 目标、7 个 EmbeddedResource、loader/LoadMovie/sprite/XML/Bootstrap/public API/保存身份均不变。实际责任、坐标与精确 HOLD 见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[J15 计划](docs/plans/j15-content-profile-plan.md)与[范围图](docs/architecture/af-framework-code-scope.md)。
- **验证**：J15 契约 `76 mappings / 69 J15b / 41 holds / 76 real projection / 8 invalid / 4 GCCZ fallback / 11 overlay aliases`；Prompt 36、两个 PromptLab、Xihai Core 88、inventory 7/11 与 unknown=0、当前 Debug 1.4 完整 Phase8 均 PASS。原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；四实现 DLL 各仍恰好 7 个资源且字节一致，卫星资源 0。代码地图仍绑定未改 J14 C# 产品 `e58f3558`，755 锚点两模式 PASS。
- **HOLD/停点**：旧 ModuleData/GUI 精确保留 41 项（异常/动态 profile、FeatureBridges、sprite 索引、临时/旧版/来源未确认图）；J15c 的 CustomPrompts、PlayerExports、VoiceMapping、ONNX、AssetPackages 等也未动。完整 Xihai StaticVerifier 为 10/13，未通过的是外部 TPAC `nacisword1`、其连带初始化和 AF bridge 签名漂移，不冒充通过。没有 Stage、Deploy、Package、push、游戏/外仓/玩家数据写入；两版本实机、旧档、安装覆盖、Gauntlet、provider、音频、帧性能均 NOT-RUN。`.dotnet-cli-home/` 保留。按用户要求停在 J15b；后续若明确继续，下一项是 J15c。

## 以下为 J15 计划就绪历史交接

# J15 content / profile 计划就绪，尚未施工（2026-09-26）

- **历史状态**：`J15_PLAN_READY / IMPLEMENTATION_NOT_STARTED`；已由上方 J15a 结果取代。本段其余规划边界仍作为后续 J15b/c/d 的历史输入。
- **历史下一动作**：原计划从 G0 开始逐文件分类和授权；J15a 已按实际授权完成，未由此授权后续批次。
- **历史验证边界**：当时只完成规划和 755 锚点定位，没有施工；现以主台账最新条目为准。

## 以下为前序 J14 产品交接

# J14 三渠道公共 API 离线完成（2026-09-26）

- **状态**：`J14_OFFLINE_VERIFIED`。Courier 公共票据/一次派出/权威阶段与失败回执已开放；三渠道共用限额、幂等和开始前取消。最终产品 `e58f3558`，测试修复止于 `a90e1b45`；责任、候选 SHA、失败修复与全部证据见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)及[V1 指南](docs/architecture/af-public-api-guide-v1.md)。
- **验收**：六构建、四 DLL 元数据/旧 Native ABI、三渠道及真实记忆写入/回读、保存/Bridge、显式当前 DLL Courier/Phase8 回放、755 锚点地图两模式和差异检查通过。仅离线完成；两版本实机、旧档、真实 provider、音频、独立子 MOD 游戏加载与帧性能分别 NOT-RUN。
- **停点**：J14 收口，不进入 J15，不推送、Stage、部署、打包、写游戏/外仓/玩家存档、安装工具或修改默认入口。`.dotnet-cli-home/` 保留；后续实机/发布或新阶段需新的明确请求。下方旧 ACTIVE 和继续指令均被本段取代，仅作历史证据。

## 以下为 J14b2 及更早交接（历史切片）

# 当前交接：J14b2 内部运输回执已接线，继续 b3/c（2026-09-26）

- **状态**：`J14b_ACTIVE / J14_ACTIVE`，产品/测试 `b2572623`。原运输各阶段、实际历史/信件接受、终态与精确 session/run 防陈旧回写已接线；179 项准入/生命周期、44 项后处理、552 项 Prompt、59 项 liveness、Native/Scene 55 与 Debug 双版本+Bootstrap 通过。详细身份与未覆盖范围见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[范围图](docs/architecture/af-framework-code-scope.md)。
- **继续执行**：b3 增加 V1 Courier 票据/提交与只读阶段投影，外部消费者/兼容验证后再开放能力；然后 c 完成当前候选 SHA 回放、真实记忆写入/回读、旧 ABI、六构建和代码地图两模式。Courier 仍 `NotSupported`，尚不能标 J14_OFFLINE_VERIFIED。仅本地；保留 `.dotnet-cli-home/`，不 push/Stage/部署/打包/外写/J15；实机、旧档、provider、音频、子 MOD 游戏加载、帧性能均 NOT-RUN。

## 以下为 J14b1 交接（历史切片）

# 当前交接：J14b1 准入完成，接续运输回执（2026-09-26）

- **状态**：`J14b1_ADMISSION_VERIFIED / J14b_ACTIVE / J14_ACTIVE`；本地产品切片 `4bc855cd`、`8fb16385`、`b6daf65f`，105 项准入检查、Native/Scene 55、Debug 双版本及 Bootstrap 通过。真实责任及证据见[主台账当前条目](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[代码范围图](docs/architecture/af-framework-code-scope.md)；JSON 地图仍是历史 J14a 基线，不声称本轮最终矩阵已过。
- **下一动作**：实施 b2 实际 session 阶段/动作与历史/回信交付/运输终止及失败回执；当前内部成功派出只保持 Running，Courier V1 仍未开放。再做 b3 和 c 最终离线矩阵及地图重绑，按[J14 计划](docs/plans/j14-public-api-plan.md)完成整体离线验收。保留 `.dotnet-cli-home/`；仅本地实施/测试/提交及本轮限定四个构建目录，无 push、Stage、部署、打包、游戏/外仓/存档写入或 J15。实机/旧档/provider/音频/帧性能仍 NOT-RUN。

## 以下为 J14a 交接（历史基线）

# J14a Scene 有限离线收口（2026-09-26）

- **状态**：`J14_G0_BASELINE_VERIFIED / J14a_OFFLINE_VERIFIED / J14_ACTIVE`。产品/测试 `1740b338`, `79fa7c48` 已开放真实 Scene 公共票据→原群组/接力→speech/后处理/记忆/AFEF 回执→终态，Native 原签名与 UI 入口不变；Courier/J14c 尚未完成。具体 owner、消费者和未覆盖责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)及[代码地图](docs/architecture/af-framework-code-map.json)。
- **离线证据与下一步**：Scene 群组 18、请求生命周期 34、后处理 37、独立外部消费者正常/枚举重排各 55、ChannelCutover 133、NativeCompletion 186、V1 142、四实际 DLL metadata 1252；原脚本在用户授权四个精确仓内产物目录、不带 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；730 锚点地图 recorded/working-tree 通过。旧 MemorySummary terminal runner 的既存抽取锚失效，此次未通过，详细限制与 SHA 见主台账。下一包按[计划](docs/plans/j14-public-api-plan.md)做 J14b Courier，随后 J14c 最终候选；不能称整体 J14、实机或发布完成。真实游戏、旧档、provider、音频、帧性能 **NOT-RUN**；不 push、Stage、部署、打包、写游戏/外仓/存档或启动 J15。原未跟踪 `.dotnet-cli-home/` 保留。

## 以下为 J13 交接（历史）

# 当前交接：主体 J13 最终离线收口（2026-09-25）

- **状态**：`J13_OFFLINE_VERIFIED / J13g_OFFLINE_VERIFIED`，仅按[原 J13 计划](docs/plans/j13-domain-owners-plan.md)完成 a–f owner 与 g 最终离线门禁，不是实机或发布验收；J14 未启动。最终产品 `39cf9d47` 修复 Onboarding Base URL 取消后、主线程消费前的迟到结果竞态；证据目录 `894acdfc`，详细责任、候选身份和风险见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[范围图](docs/architecture/af-framework-code-scope.md)。
- **最终证据**：原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4 + Bootstrap 六构建均 0 warning/error；当前 Debug 1.4 SHA256 `03D0FACFC1199B7BFA68B8BDEEABCFF8B69E5B9A97AE6BB279050C33509F0E55` 完整 Phase8 当前 DLL 回放通过。V1 API 119、四 DLL metadata 1148、Persistence Profile 142 key/168 binding、Identity 142 SyncData/36 CampaignBehavior、Bridge/readiness、Native/Scene/Courier/J12 定向回归与 722 锚点代码地图均通过；证据层级和命令见主台账。真实游戏、旧档、provider、UI 焦点、音频、帧性能仍 **NOT-RUN**，Phase8 目录仍 `REPRESENTATIVE`，不能称发布 READY。
- **交接边界**：本次只收口 J13；不 push、Stage、部署、打包、写游戏/外仓、改自动化或清理 `.dotnet-cli-home/`。J14 如需开工须另行授权。

## 以下为 f 交接（历史）

# 当前交接：主体 J13f UI/Overlay/Onboarding 有限离线收口（2026-09-25）

- **状态**：`J13f_OFFLINE_VERIFIED / J13_ACTIVE`，仅本包离线完成，下一步按[原计划](docs/plans/j13-domain-owners-plan.md)执行 **J13g** 最终候选门禁；J14 不开始。真实 owner 与保留 Campaign/Gauntlet/Native/百科边界见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)。产品/回放 `1e2bd80a`、`a2e9b8eb`、`26bc72fa`、`48bcc48a`，Native 测试入口修复 `61f8720a`。
- **离线证据**：Debug 1.3/1.4 + Bootstrap 0 warning/error；当前 Debug 1.4 `AnimusForge.dll` SHA256 `E07886118A440F3B462EE7C6A0182213B5C00BD42391AF76435E52EDD51156A5` 的完整 Phase8 包含五个 J13f owner 回放及 UI 宿主源码契约；Native admission 44/44、presentation 46/46，source inventory 7；[代码地图](docs/architecture/af-framework-code-map.json) 722 锚点 recorded/working-tree 通过（仅定位）。真实 Gauntlet/Campaign/provider/旧档/帧性能 **NOT-RUN**；Release 和最终 API/保存/Bridge 门禁留给 g，不冒充已验。
- **下一条具体动作**：以当前提交为候选，先核实 Release 两个仓内原脚本清理目标的绝对路径和 reparse，再运行原脚本 Debug/Release 六构建；随后四实现 DLL API/metadata、Persistence Profile/Chunk/Identity、Bridge/readiness/source inventory、当前 DLL Phase8、Native/Scene/Courier 与 J12 相关回归及地图两模式，最后主台账/交接收口。`.dotnet-cli-home/` 保留；无 push、Stage、部署、打包、游戏/外仓写入或自动化改动。

## 历史交接（以下各节按原记录保留）

# 当前交接：主体 J13e1 Duel 有限离线收口（2026-09-25）

- **状态**：`J13e1_OFFLINE_VERIFIED / J13_ACTIVE`。Duel 模块持有 exact 受理/运行转换与三种终局 typed 效果投影，原 Mission/Harmony/保存/Courier 薄适配保留。意图 `8cc8ef63`，生产/回放 `054781b1`、`88643619`、`ca49023f`、`d0d15a57`、`d89a13b5`；一基坐标、Patch 清单、覆盖和未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：原脚本无 Stage/Deploy 的 Debug/Release × 1.3/1.4+Bootstrap 六构建成功；Debug 1.4 SHA256 `64CBF30A1F436F1D05B8A0A8817DBA9A4A186C568F9484080E67B4427BC6A700` 的 Phase8 含当前 DLL exact owner 行为回放与 e1 聚合契约通过；DuelOutcome 20/20、DuelDispatch 16/16、ProductionDuel 35/35 × Debug/Release；V1 119/四 DLL metadata 1064、PersistenceIdentity 142/36、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 617 锚点两模式通过。实机 Mission/Harmony、旧档、MCM、经济副作用与帧性能均 **NOT-RUN**。
- **下一条具体动作**：按[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e2 Taunt**：先核对和平冲突 allowlist/原场景伤害上下文、`SceneTauntBehavior`/五个原 patch/生产消费者与关闭恢复路径，再逐责任切片迁真正 owner、做正反/失效回放和聚合契约；随后 e3–e5，不提前 J14。`.dotnet-cli-home/` 原未跟踪目录保留；无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 d4 交接（历史）

# 当前交接：主体 J13d4 WarStats 有限离线收口（2026-09-25）

- **状态**：`J13d4_OFFLINE_VERIFIED / J13_ACTIVE`。WarStats 活动/历史/旧账及近期序号、宣战/计数/死亡/归档/保存恢复归唯一 ledger；原 CampaignBehavior、v1–v5 保存键、事件装配和终端消费者保留。产品/行为 `6d5657e9`、`c8cc0efd`、`ab05a736`、`db1830bc`、`f722dd41`，聚合接线 `f0cc3ede`/`6b0c07f7`；一基坐标、覆盖/未覆盖责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **当前证据**：原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `656D4E9BF9E5894FEA899B6E3CA05BC264B1F725A16D32853E4874B6F051EC07` 的 Phase8 含 d4 状态反例及聚合契约通过；V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、迁移 fixture 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 600 锚点两模式通过。合成数据不是实机 Campaign/旧档/终端点击/帧性能验收，均 `NOT-RUN`。
- **下一条动作**：按[原计划](docs/plans/j13-domain-owners-plan.md)与场景伤害/军团会面案例先审 e1 Duel 的 host、typed outcome、三个现有 Duel 套件和生产消费者，开工意图后逐验证切片迁 owner；依序 e2–e5，不提前 J14。`.dotnet-cli-home/` 原未跟踪目录保留；未 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 d3 交接（历史）

# 当前交接：主体 J13d3 WorldEvents 有限离线收口（2026-09-25）

- **状态**：按[原 J13 计划](docs/plans/j13-domain-owners-plan.md)，`J13d3_OFFLINE_VERIFIED / J13_ACTIVE`；WorldEvents 收件箱 records/unread/stable-key/version 归唯一 owner，原 CampaignBehavior、v1 保存键和政策/UI/档案入口保留。产品/行为 `62ec9065`，聚合接线/政策 UI 契约 `b07898cb`；具体一基代码坐标、保留职责和风险见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **证据边界**：原脚本 Debug/Release × 1.3/1.4 + Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `C70D15E9B5251356F98FBDBA633E61205A45EB1B48D18FB0CE39C68E5BDE17B0` 的 Phase8 含 d3 行为/聚合通过，Policy UI 387、V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 586 锚点两模式通过。真实 Campaign、旧档、Gauntlet、provider/音频及帧性能均 `NOT-RUN`；离线数据不冒充实机。
- **下一步**：依计划先读 d4 WarStats 的事件→计数→归档→v5 保存/终端消费链，逐验证切片迁真正 owner、保持原 `AFWarStatsTerminal` 类型及事件注册；随后 e1–e5。`.dotnet-cli-home/` 未触碰；未 push、Stage、部署、打包、写游戏/外仓、改自动化或启动 J14。

## 以下为 d2 交接（历史）

# 当前交接：主体 J13d2 Proactive/Issue 有限离线收口（2026-09-25）

- **状态**：按[原计划](docs/plans/j13-domain-owners-plan.md)的 d2 范围，`J13d2_OFFLINE_VERIFIED / J13_ACTIVE`；Social 的资格/状态/pending opening 与 Issue 的 offer/in-progress/turn-in/完成回执已迁入各自 owner，Campaign/TW/反射/窗口保留原 host 适配。产品提交 `a59a0fc0`、`fe74d4fa`、`3f330c58`、`f739dbaf`、`4e4ad8bf`、`96d058ca`，聚合证据 `36037592`、`7fc6f680`，重复领取行为回放 `d34fd858`。
- **当前证据**：原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；Debug 1.4 SHA256 `0071A62D00D40E4113222F7A3E1FCDA641CABFA26EA4FC9371301025EF745544` 的 Phase8 包含 d2 聚合与行为反例通过，其中当前生产方法对合成的原版 Issue/Hero 验证未受理可 offer、已受理及错 owner 不可 offer；V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 575 锚点 recorded/working-tree 通过。真实 Quest/PartyScreen/旧档/provider/UI/音频/帧耗时未验，不以合成 fixture 冒充实机。
- **下一步**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)与[代码范围图](docs/architecture/af-framework-code-scope.md)进入 J13d3 WorldEvents，随后 d4 WarStats；J13e/f/g 未由 d2 替代。未 push、Stage、部署、打包、写游戏/外仓或 J14；`.dotnet-cli-home/` 不动。

## 以下为 d2 资格切片交接（历史）

# 当前交接：主体 J13d2 主动资格归 Social，继续 Issue（2026-09-25）

- **状态**：产品/回放 `a59a0fc0`，Proactive 的资格、状态、pending opening/消费均有有限离线证据；**J13d2/J13 仍 `ACTIVE`**，Issue 的 offer/in-progress/turn-in/完成回执未闭合。
- **本片证据**：完整主动候选/各需要资格算法迁入 `src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs`，原 host 留 Campaign/存档/会面适配；前驱源码字节级重组相等。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；Debug 1.4 SHA256 `DD81C07FD3F4146DC696A498A88467F703B886A8CBA69548BACABF1DCC1A85B5` Phase8 全通过，API 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 560 锚点两模式通过。真实游戏/Quest/旧档/provider/UI/音频/帧耗时未验。
- **下一步**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)核对三渠道、Quest 身份、offer/受理/交付与完成事实，迁入 Issue owner 并做 d2 聚合退出门。未 push、Stage、部署、打包、写游戏或外仓；`.dotnet-cli-home/` 不动。

## 以下为 d2 主会话切片交接（历史）

# 当前交接：主体 J13d2 主会话 owner 有限切片完成，继续资格/Issue（2026-09-25）

- **状态**：分支 `codex/af-main-refactor-continuation-20260831`，产品/回放 `f1ecb315`；J13a–c/d1 和 d2 opening、冷却、扫描、主会话/Issue 派遣 pending 均有有限离线证据，**J13d2/J13 仍 `ACTIVE`**，不提前 d3/J14。
- **本片证据**：Social 唯一 session owner 接管原保存 DTO 的读档规范化、重复启动、追逐探测/阶段/过期/取消与一次疲劳；原 Campaign/TW/AFEF 适配仍保留。原脚本 Debug/Release × 1.3/1.4+Bootstrap 六构建零警告错误；当前 Debug 1.4 SHA256 `D42E34E7105D35D9289D4FAD8DCEBC0D879E47E4159EE9B87D063EB17A835DEB` 的 Phase8 全通过，V1 119/四 DLL metadata 1060、PersistenceIdentity 142/36、source inventory 7、代码地图 554 锚点两模式通过。真实游戏/旧档/provider/UI/音频/帧耗时未验。
- **下一条具体动作**：依[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)先关闭主动候选资格的 Social 归属与聚合反例，再迁 Issue offer/in-progress/turn-in/完成回执并做 d2 聚合退出验收。未 push、Stage、部署、打包、游戏/外仓写入或改自动化；`.dotnet-cli-home/` 不动。

## 以下为 d2 增量扫描切片交接（历史）

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
# 当前交接：主体 J13e5 Exercise 有限离线收口（2026-09-25）

- **状态**：`J13e5_OFFLINE_VERIFIED / J13_ACTIVE`；仅此包完成离线门禁，J13f/g 与 J14 尚未开始。精确 MapEvent 排除真实战斗、两轮选择/延迟/迟到回调、活动及孤立事件一次 XP 回执分别归 Exercise owners，TaleWorlds PartyScreen/Mission/roster/角色/Hero/MapEvent/Encounter 实际副作用与原 Harmony/Terminal 适配保留。产品/回放 `ba645b29`、`def2b082`、`3d00d416`、`80e21be1`、`8654427a`；详见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)和[范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；最终 Debug 1.4 SHA256 `2F9D0E976A18CA4C28FDA20B4C6C728886EE770AF9769EBC825B5D8BEBFD02E8` 的完整 Phase8 含当前 DLL 身份/会话/结算回放，聚合接线另核对四 DLL 奖励参数。V1 119、四 DLL metadata 1128、PersistenceIdentity 142/36、迁移 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 708 锚点两模式通过。真实两版本 Mission/Harmony、旧档、MCM 实机、伤害/奖励/经济与帧性能 **NOT-RUN**；部分 XP 失败不重试以免双发，异常 TW 清理仍须实机观察。
- **下一步**：按[原计划](docs/plans/j13-domain-owners-plan.md)仅继续 **J13f UI/Overlay/Onboarding**，再 J13g；不提前 J14。`.dotnet-cli-home/` 保留，无 push、Stage、部署、打包、游戏/外仓写入或自动化改动。

## 以下为 e4 交接（历史）

# 主体 J13e4 Settlement/Inspection 有限离线收口（2026-09-25）

- **状态**：`J13e4_OFFLINE_VERIFIED / J13_ACTIVE`。入场 ticket、随行 Mission/Agent 身份和 Inspection 临时 session/清理分别归真实 owner，原 Campaign/Harmony/Terminal、存档与 GCCZ 薄接缝保留；产品/回放 `a946301a`、`cf162dd5`、`35daa7a6`，聚合 `ef6a83e1`。一基源码、保留/未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `877CE07F95D20F7DD82FEFAFA89D873CB3C831E0CC9240A5A5B5B39EAEA2BE39` 完整 Phase8 含三个当前 DLL owner 回放和 e4 聚合接线契约；V1 119/四 DLL metadata 1112、PersistenceIdentity 142/36、迁移 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 680 锚点两模式通过。实机入场/死亡/中止、旧档、GCCZ 副作用及帧性能 **NOT-RUN**。
- **下一步**：依[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e5 Exercise**；不提前 J14。`.dotnet-cli-home/` 保留，无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 e3 交接（历史）

# 当前交接：主体 J13e3 Encounter 有限离线收口（2026-09-25）

- **状态**：`J13e3_OFFLINE_VERIFIED / J13_ACTIVE`。目标/会话优先级、释放授权及 pending 返回四个真实 owner 与原 Campaign/tick/三个会话 patch 连接；产品/回放 `0b0b32c0`、`dc2bbe03`、`86c11889`，聚合 `73bef755`。一基位置、保留 host 和未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)、[范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：源码链接提取 53/53；Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `8545757A9A8880F1E5D40E74E7F0727D967DD86C6675442E571232293B803ACD` 完整 Phase8 含四个当前 DLL owner 回放和 e3 聚合源码契约；V1 119/四 DLL metadata 1100、PersistenceIdentity 142/36、迁移 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) 663 锚点两模式通过。原版事件顺序、实机会面/Harmony、旧档、native safe-passage 与帧性能 **NOT-RUN**。
- **下一步**：按[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e4 Settlement/Inspection**，随后 e5；不提前 J14。`.dotnet-cli-home/` 原未跟踪目录保留，无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 e2 交接（历史）

# 当前交接：主体 J13e2 Taunt 有限离线收口（2026-09-25）

- **状态**：`J13e2_OFFLINE_VERIFIED / J13_ACTIVE`。和平场景/物理 MCM、延迟犯罪与信任小数、冲突升格/结束状态分别由 Taunt 三个真实 owner 承接；原 Campaign/Mission、五个 Harmony patch、保存键与原生处罚/队伍恢复适配保留。产品/行为 `e62e2a82`、`9ec814eb`、`5873d594`，聚合 `bee366a8`；一基坐标、Patch target/条件、覆盖和未验责任见[主台账](docs/animusforge-refactoring-and-repository-reorganization-plan.md)及[代码范围图](docs/architecture/af-framework-code-scope.md)。
- **离线证据**：Debug/Release × 1.3/1.4 + Bootstrap 六构建 0 警告/错误；Debug 1.4 SHA256 `4C8FDC5AF6BD57AFDBB380B639FB4340792FA08237A0F2515C40EB2434DAE0A9` 的完整 Phase8 含三个当前 DLL owner 回放和 e2 聚合契约；context 22/22、penalty 16/16、lifecycle 14/14、V1 119/四 DLL metadata 1080、PersistenceIdentity 142/36、迁移 fixture 10、source inventory 7、[代码地图](docs/architecture/af-framework-code-map.json) recorded/working-tree。实际游戏 Mission/Harmony/旧档/MCM 点击/原生副作用和帧性能均 **NOT-RUN**。
- **下一条具体动作**：按[原计划](docs/plans/j13-domain-owners-plan.md)进入 **e3 Encounter**，先按军团会面目标案例审选中军团成员优先、合法 `_targetHero` 保留、释放授权/超时、pending 回调的换 party/Mission/save 失效和原 patch/注册，再逐切片迁 owner 与行为/聚合契约；随后 e4、e5，不提前 J14。`.dotnet-cli-home/` 保留；无 push、Stage、部署、打包、写游戏/外仓或改自动化。

## 以下为 e1 交接（历史）
