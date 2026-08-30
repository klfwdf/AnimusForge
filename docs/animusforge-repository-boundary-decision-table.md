# AnimusForge 阶段 1：仓库边界与分发决策表

- 状态：初版运营决策；用户已决定先保持仓库现状
- 日期：2026-08-29
- 适用工作区：`F:\AnimusForge-main`
- 目的：在没有根级 LICENSE/NOTICE/第三方清单的情况下，先为仓库内容建立安全的保留、脱离源码、仅本地和可发布边界。
- 重要限制：本表不是法律意见，也不证明任何第三方内容已经取得分发授权；“可发布”必须同时满足来源、许可证、用户数据和构建闭包证据。

## 1. 决策原则

1. **先证据、后发布**：没有作者/来源/许可证/再分发证据的内容，默认不得进入公共源码发布物或客户端 ZIP。
2. **参考仓库保留**：用户确认的 Bannerlord 1.3.x/1.4.x 游戏源码参考仓库作为 tracked reference plane 保留；它们不是 AF 生产源码，也不进入 AF 客户端 ZIP。
3. **参考不等于生产源码**：参考仓库用于 API/构建审计，不作为 AnimusForge 生产源码或客户端发布内容。
4. **用户数据不等于默认资产**：`PlayerExports` 可能包含用户创作、角色资料、知识库和隐私内容；不自动合并、删除或发布。
5. **生成物不等于源文件**：构建输出、日志、缓存、浏览器 profile、工具发行包和临时审计结果不进入生产源码平面。
6. **不因分类而立即清理**：本切片只记录处置决策，不执行批量删除、移动、去跟踪或修改 `.gitignore`。
7. **统一客户端包沿用现有脚本约束**：只发布一个 `AnimusForge` 模块；Bootstrap 与两个实现按既有 marker/allowlist 验证；不包含游戏原版 DLL、Logs、ONNX 或未声明的根级实现 DLL。

## 2. 处置类别定义

| 类别 | 含义 | 是否进入公共源码提交 | 是否进入客户端 ZIP |
|---|---|---:|---:|
| 保留 | AF 自有源码、维护文档、必要内容或已确认的测试源码；继续由 Git 管理 | 可以，前提是来源明确 | 只有发布闭包中的内容可以 |
| 脱离源码 | 仍可保留为仓库外归档/独立内容包/发布附件，但不作为主源码树内容 | 默认不应继续作为普通源码 | 默认不进入 |
| 仅本地 | 仅供本机编译、反编译、诊断或测试；不可作为发布物 | 不应新增或继续扩大 | 不进入 |
| 可发布（条件性） | 已满足 AF 发布闭包，但仍须保留逐项来源/许可证证据 | 可以 | 可以，须通过 package 校验 |
| 参考保留 | 用户确认的参考源码树，继续作为 tracked API/reference plane；不视为生产源码 | 保留在参考仓库 | 不进入 AF 客户端 ZIP |
| 待确认 | 现有证据不足，暂不改变位置，先由 owner/许可证证据决定 | 不新增变更 | 不进入 |

## 3. 目录/文件决策

| 路径/对象 | 当前证据 | 角色 | 初步处置 | 发布结论 | 后续责任 |
|---|---|---|---|---|---|
| `AnimusForge.csproj`、根级 AF C#、`PolicySystem/`、`AnimusForge.Bootstrap/` | AF 工作树、项目引用和构建 marker 可核验 | AF 生产源码与组合边界 | **保留** | 仅构建产物进入客户端包；源码是否公开按仓库发布策略处理 | 对应 owner matrix；不得在本阶段大搬家 |
| `AnimusForge.SiegeAftermathIntervention/` | AF/GCCZ 融合边界已有文档约束 | 可复用领域源码/适配器 | **保留** | 仅被构建闭包实际引用的结果进入包；不发布参考依赖 | Siege/Settlement owner；保持 AF 薄适配器 |
| `extensions/AnimusForge.XihaiAction/src/` | 扩展源码被主项目 source-link 编译 | SceneActions/BattleSpeech 扩展源码 | **保留** | 仅 AF unified 包实际需要的内容进入包；第三方许可未确认前不发布依赖 | External/Scene owner |
| `AnimusForge/ModuleData/`、`AnimusForge/GUI/` | 38 个 ModuleData 文件、74 个 GUI 文件 | AF 模块内容与 UI 资源 | **保留** | 逐项确认资源来源后进入包；不得把用户导出和模型混入 | Content/UI owner |
| `AnimusForge/AssetPackages/pack0.tpac` | tracked 运行时资产，来源/许可证未在仓库说明 | 运行时资产 | **待确认** | 暂不单独判定可发布 | 记录生成来源、依赖和发布用途 |
| `extensions/AnimusForge.XihaiAction/AssetPackages/pack0.tpac` | tracked 扩展资产，来源/许可证未在仓库说明 | 扩展运行时资产 | **待确认** | 暂不单独判定可发布 | 扩展 owner 提供 provenance |
| `原版游戏本体代码1.3.x/` | 9,399 个 tracked 参考文件，含 Bannerlord/Harmony/MCM 等源码 | 1.3.x 游戏源码参考仓库 | **参考保留** | 禁止进入 AF 源码发行包或客户端 ZIP | 用户已确认保留在仓库参考平面；仅补充来源/许可证说明，不执行清理 |
| `原版游戏本体代码1.4.5/` | 7,113 个 tracked 参考文件 | 1.4.x 游戏源码参考仓库 | **参考保留** | 禁止进入 AF 源码发行包或客户端 ZIP | 用户已确认保留在仓库参考平面；仅补充来源/许可证说明，不执行清理 |
| `.tmp/build_check/{1.3,1.4}/` | 174 个 tracked 文件，含 181 个 tracked DLL 中的大部分引用 overlay | 构建证据/依赖 overlay | **仅本地** | 禁止直接复制进入客户端 ZIP | 由可复现构建记录和 hash 代替；不批量删除 |
| `.tmp/{duelsettings_decomp,force-speech-stage,mcm*_decomp,option_screen_decomp}/` | 反编译、临时 stage、审计结果，磁盘内容多于 tracked 内容 | 临时审计/迁移材料 | **脱离源码** | 不进入客户端 ZIP | 先审计消费者，再归档或去跟踪 |
| `.codex_tmp/` | 252 个 tracked 文件，含浏览器 profile、DB、缓存、日志 | Codex/本机 scratch | **仅本地** | 禁止发布 | 不再新增；后续从 Git 索引脱离需单独切片 |
| `AnimusForge/PlayerExports/` | 3,139 个 tracked JSON，6 个编年史目录；已被 `.gitignore` 忽略但历史仍 tracked | 用户导出、知识库、角色/世界数据 | **脱离源码** | 默认不进入客户端 ZIP；不得未经确认公开用户内容 | 建立内容包/样例/脱敏/迁移策略；本切片不删除 |
| `AnimusForge/ONNX/` | 5 个 tracked 文件，含 model/tokenizer/config，总量约 95 MB | 检索模型与 tokenizer 资源 | **仅本地** | 现有 unified client package 明确排除 ONNX | 许可证、模型来源和单独资源分发另行确认 |
| `tools/PlayerExportsEditor/src/`、其他 tools 源码 | 工具源码和 smoke/contract tests 可识别 | 外部工具/验证工具源码 | **保留** | 不编译进主 runtime；是否单独发布按工具 owner 决定 | 保持源码与运行时分离 |
| `tools/PlayerExportsEditor/dist/` | 9 个 tracked EXE/RAR/数据条目，约 304 MB | 工具发行物 | **脱离源码** | 不进入 AF 客户端 ZIP | 仅作为明确 release artifact；补充版本/来源记录 |
| `Phase0_Local_Archive/` | 26 个 tracked 文件，含 baseline/reports/archive outline | 阶段归档与交接材料 | **脱离源码** | 不进入客户端 ZIP | 保留到阶段验收完成；之后移到仓库外归档或独立文档包 |
| `*.log`、`Logs/`、`Logs (10).zip`、`Logs (11).zip` | 未来日志已被 ignore；历史压缩包 tracked | 运行日志/调试归档 | **仅本地** | 禁止发布 | 不新增；保留必要诊断证据，敏感内容不得外传 |
| `*.dll`、`*.exe`、`*.rar`、`*.zip` 等历史二进制/归档 | tracked 二进制分散在 overlay、工具 dist、归档和资产目录 | 依赖证据或发行物 | **待确认/仅本地** | 默认禁止发布 | 按来源、调用者和许可证逐项登记 |
| `.claude/settings.local.json` | 被用户全局 ignore 命中且仍 tracked | 本机设置 | **仅本地** | 禁止发布 | 不把个人设置当公共 skill/config；去跟踪需单独授权 |
| `README.md`、`README_BUILD.md`、`docs/`、维护 handoff | AF 维护与构建说明可读 | 项目文档/证据 | **保留** | 文档可随源码发布；不得包含私密路径、凭据或用户数据 | 持续同步实际命令和 NOT-RUN 证据 |

## 4. 当前可执行边界

在许可证和来源证据补齐前，允许：

- 继续编译 1.3/1.4/Bootstrap；
- 使用本地参考树和 build overlay 做构建验证；
- 维护 AF 生产源码、文档、测试源码和必要 ModuleData/GUI；
- 生成项目本地 stage 作为验证产物；
- 为用户数据建立备份、迁移和脱敏方案，但不覆盖原数据。

在许可证和来源证据补齐前，禁止：

- 将 Bannerlord/第三方反编译源码或 DLL 放入公共 ZIP；
- 将 `.tmp` overlay、ONNX、用户 PlayerExports、浏览器 profile、日志或工具 dist 混入 AF 客户端包；
- 批量删除或移动 tracked 内容；
- 修改 `.gitignore` 以掩盖未决的 tracked 内容；
- 把“当前能编译”解释为“可以合法分发”。

## 5. 未决确认项

1. AF 自有源文件、ModuleData、GUI、TPAC 的作者/来源和公开分发政策。
2. 原版参考树已确认继续 tracked 保留在游戏源码参考仓库中；未决部分仅是许可证、来源说明和是否随公共仓库分发。
3. 第三方依赖（Harmony、MCM、UIExtender、MBOptionScreen、ONNX/Tokenizer、工具发行包）的许可证、版本和再分发范围。
4. `PlayerExports` 是否全部为用户/项目自有内容，哪些目录需要样例化、脱敏或迁移到独立内容包。
5. `Phase0_Local_Archive`、历史 ZIP/RAR 和 tracked 诊断资料的长期归档位置。
6. 是否建立根级 `LICENSE`、`NOTICE`、`THIRD_PARTY_NOTICES` 和依赖 provenance manifest。

## 6. 状态

- 决策表：已建立，属于阶段 1 的初版运营边界。
- 用户决策：先保持现状；所有目录和文件暂不删除、移动或取消 Git 跟踪。
- 许可证/第三方政策：`BLOCKED`，本表不替代法律确认。
- 批量清理/移动/去跟踪：`HOLD / NOT-RUN`，等待用户明确授权和逐项来源证据。
- `.gitignore`：本次不修改。
- 客户端 package/deploy：`NOT-RUN`。
- 下一项：在用户改变决定或提供许可证/provenance 证据前，维持现状；可继续进行不改变仓库内容的基线和只读审计。
