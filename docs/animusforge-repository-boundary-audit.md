# AnimusForge 阶段 1：仓库边界与可重复性审计

- 审计阶段：阶段 1：仓库边界与可重复性
- 审计日期：2026-08-29
- canonical worktree：`F:\AnimusForge-main`
- 分支：`refactor/prepare-af-restructure`
- HEAD：`7567c59fd9a8883097b66a5dd007fcdc9ef9f495`
- 目的：记录仓库平面、生成物/用户数据边界、构建输出契约和分发风险；本报告不是删除或迁移授权。

## 1. 结论摘要

1. 仓库是 AF 生产源码、Bannerlord 1.3/1.4 游戏源码参考仓库、工具、扩展、构建依赖、用户导出内容和临时审计数据的混合树；其中游戏源码参考树是用户确认的有意保留 reference plane，不应按生成物清理。
2. 当前 unified build/stage/package/deploy 设计已经形成清晰的单模块发布契约，但仓库历史 tracked 内容没有完全遵守当前 `.gitignore` 边界。
3. 生成物、用户数据和参考依赖必须先分类、确认消费者和保留/迁移策略，再分批清理；不能用一次递归删除解决。
4. 仓库没有发现根级 `LICENSE`、`NOTICE` 或第三方清单，因此许可证与分发处理原则仍是阶段 1 的未完成项和当前阻塞。

## 2. Git 与文件平面统计

命令口径：`git -c core.quotePath=false ls-files`，截至本次审计。

| 平面 | tracked 数量 | 当前分类 |
|---|---:|---|
| 全部 tracked 文件 | 21,425 | 混合仓库索引 |
| C# | 17,039 | AF 生产/扩展、工具、原版参考源码混合 |
| JSON | 3,331 | ModuleData、PlayerExports、工具数据、配置/快照混合 |
| DLL | 181 | 主要集中在 `.tmp/build_check` 参考 overlay，也有其他二进制依赖 |
| PNG | 131 | UI/素材/历史工件候选，需按消费者确认 |
| Markdown | 97 | 项目文档、handoff、工具说明和参考资料 |
| XML | 71 | 模块内容、配置、参考源码项目文件/数据 |
| tracked ignored | 3,568 | 已被 ignore 规则命中但仍留在 Git 索引中 |

### C# 来源分布

| 路径平面 | C# 数量 | 处理原则 |
|---|---:|---|
| `原版游戏本体代码1.3.x/` | 9,322 | 用户确认保留的 1.3.x 游戏源码参考仓库；不作为 AF 生产源码或发布物处理 |
| `原版游戏本体代码1.4.5/` | 7,043 | 用户确认保留的 1.4.x 游戏源码参考仓库；不作为 AF 生产源码或发布物处理 |
| 仓库根级 C# | 289 | AF 主实现热点和组合根候选，未经授权不移动 |
| `PolicySystem/` | 77 | AF Policy 领域候选 owner |
| `AnimusForge.SiegeAftermathIntervention/` | 183 | GCCZ 可复用规则/适配边界，按现有 owner 维护 |
| `extensions/` | 74 | SceneActions/BattleSpeech 等扩展源码，需保持 AF 薄适配器边界 |
| 其他 AF/工具/项目路径 | 48 | 按 owner matrix 逐项归类 |

### 关键内容与工具平面

- `AnimusForge/ModuleData/`：38 个 tracked 文件，属于发布内容/Prompt/XML 平面。
- `AnimusForge/GUI/`：74 个 tracked 文件，属于模块 UI 内容平面。
- `AnimusForge/PlayerExports/`：3,139 个 tracked JSON，属于用户可编辑导出内容；当前被 `.gitignore` 忽略但历史文件仍 tracked，不能直接删除或覆盖。
- `AnimusForge/AssetPackages/pack0.tpac`：tracked 运行时资产，需由内容 owner 确认是否属于发布闭包。
- `extensions/AnimusForge.XihaiAction/AssetPackages/pack0.tpac`：tracked 扩展资产，需与扩展发布边界一起确认。
- `tools/PlayerExportsEditor/dist/`：9 个 tracked 发行物/压缩包条目，不能与工具源码等同处理。
- `Phase0_Local_Archive/`：26 个 tracked 本地阶段归档条目，应保持审计/交接用途，后续确认是否应脱离源码发布面。

## 3. 生成物、用户数据与参考依赖边界

### 当前 `.gitignore` 已覆盖的未来生成物

- `bin/`、`obj/`、`.vs/`；
- `*.pdb`、`*.tmp`、`*.log`、`Logs/`；
- `packages/`、`*.nupkg`；
- `artifacts/`、`.codex_tmp/`、`FUYONG/`；
- `AnimusForge/PlayerExports/`；
- `tools/ActionPostprocessPromptLab/{runs,dist}/`；
- `tools/PreprocessTopicPromptLab/{runs,dist}/`；
- `AnimusForge/ONNX/reranker/`。

### 当前边界缺口或历史残留

- `.tmp/` 没有单独的目录级 ignore 规则，但当前 `*.tmp` 规则会命中名为 `.tmp` 的目录；本仓库目前有 174 个 `.tmp/**` tracked 文件，因此这些历史文件仍需单独审计。
- `.codex_tmp/` 已被忽略，但仍有 252 个历史 tracked 文件；内容包含浏览器 profile、数据库、缓存和日志，明确不应进入源码发布面。
- `AnimusForge/PlayerExports/` 已被忽略，但仍有 3,139 个历史 tracked 文件；它们是用户导出/知识包候选，必须先建立保留、示例化、脱敏和迁移策略。
- `AnimusForge/ONNX/` 只有 `reranker/` 被忽略，父目录仍存在 5 个 tracked 条目；模型、tokenizer 和运行时依赖应分开登记。
- `tools/PlayerExportsEditor/dist/` 没有明确的仓库级忽略规则，当前有 9 个 tracked 发行物/压缩包条目。
- `.claude/settings.local.json` 被用户全局 Git ignore 规则命中且已 tracked；它不是项目发布内容，应避免把本机设置当作公共配置。
- 当前没有 `.gitattributes`，也没有发现 Git LFS 跟踪记录。
- `*.dll` 当前有 181 个 tracked 条目，主要是 `.tmp/build_check/1.3` 和 `.tmp/build_check/1.4` 的引用/依赖 overlay；这些文件必须按“构建引用证据”与“可分发运行时”区分，不能直接复制进 unified 客户端包。
- 当前没有 tracked `.log` 文件，但仓库工作区可生成 `build-1.3.log`、`build-single-release.log`、`core-tests.log`、`static-verifier.log` 等 ignored 日志。

## 4. 构建、stage、package、deploy 现状

### Build

入口：`一键编译覆盖推送/build_single_module.ps1`。

- `-Stage` 与 `-Deploy` 互斥。
- 顺序构建 1.3 实现、1.4 实现和 Bootstrap；使用隔离的 output/intermediate 目录。
- 1.3 依赖必须通过 BuildInfo 验证的 pinned overlay；1.4 依赖必须通过 1.4.x BuildInfo 验证。
- 写入 build marker、程序集名、flavor、引用版本和 SHA-256；拒绝 1.3/1.4 DLL hash 相同的情况。
- 本次已验证 Debug stage 成功，但当前 1.4 使用的是 `v1.4.6.115628` overlay；本机游戏根目录实际是 `v1.4.8.119303`，两者不可混称。

### Stage

- 输出：`bin\Debug\single_module_stage\AnimusForge`。
- 从 `AnimusForge/` 复制模块内容，排除 `Logs`、`PlayerExports` 和源 `bin`，再复制 PlayerExports 到 stage。
- 生成一个 `AnimusForge` 模块，包含 Bootstrap、`versions/1.3/AnimusForge.dll`、`versions/1.4/AnimusForge.dll` 和允许的私有运行时 DLL。
- stage 不修改游戏目录；stage 失败时清理项目内 staging 目录。

### Deploy

入口：`一键编译覆盖推送/deploy_module.ps1`，由统一覆盖入口调用。

- 只允许目标为 Bannerlord `Modules/AnimusForge`，并拒绝源目录与目标目录相同、重解析点和越界路径。
- 先在同一 Modules volume 创建 staging/backup，再验证模块布局和 DLL marker，之后替换目标模块。
- 保留已安装模块的 `Logs`、`PlayerExports`、`ONNX`；首次统一部署可只读合并 legacy 1.3/1.4 PlayerExports。
- 替换失败时尝试从完整 backup 恢复；部署成功后再以非删除方式同步 PlayerExports 回源码。
- 本阶段未运行 deploy，也没有写入游戏目录。

### Package

入口：`一键编译覆盖推送/package_mod.ps1`，默认输出到脚本目录下的 `packages/`。

- 生成一个 ZIP，包含一个 `AnimusForge/` 模块和两套实现路径。
- 校验模块 Id/Name/Version、Bootstrap 唯一声明、实现程序集名、Build marker、版本 flavor 和 1.3/1.4 hash 差异。
- 拒绝游戏拥有的 TaleWorlds/SandBox/StoryMode/Native/CustomBattle DLL、根级实现 DLL、Logs 和 ONNX；可按参数排除 `CustomPrompts`。
- 包装失败时恢复被临时修改的 SubModule.xml 版本内容并清理临时 ZIP。
- 本阶段未运行 package。

## 5. 许可证、第三方和分发边界

本次在仓库根目录、`docs/` 和非临时源码树中没有发现根级 `LICENSE`、`COPYING`、`NOTICE` 或第三方清单文件。

需要明确政策的对象包括：

- `原版游戏本体代码1.3.x/` 与 `原版游戏本体代码1.4.5/` 的 decompiled/reference source；用户已确认它们属于仓库内游戏源码参考平面，保留但不进入 AF 客户端发布物；
- `.tmp/build_check/{1.3,1.4}/` 中的 TaleWorlds、Harmony、MCM、UIExtender、MBOptionScreen 等 DLL；
- tracked 的 Harmony/MCM/工具源码和发行包；
- `AnimusForge/ONNX/`、`.tpac`、模型/tokenizer 和工具发行物；
- `PlayerExports` 中可能包含用户创建内容、知识库、角色资料或其他可分发数据。

在取得明确的许可证与分发策略前：

- 不将原版参考源码、游戏 DLL 或第三方二进制加入统一客户端 ZIP；
- 不把 `.tmp` overlay 当作正式依赖发布物；
- 不删除 tracked 用户/参考/归档数据；
- 不把本机用户设置、日志、存档或运行时缓存加入提交；
- 将当前阶段 1 状态保持为 `IN PROGRESS`，而不是标记 `DONE`。

## 6. 阶段 1 结果与下一步

已完成：

- 源码、内容、测试、工具、脚本、文档、引用、依赖和产物平面统计；
- `.gitignore`、tracked ignored 和用户数据/生成物边界审计；
- build/stage/package/deploy 现状读取和记录。

未完成/阻塞：

- 第三方依赖和参考源码的公开分发/许可证说明；参考仓库保留本身已确认，但不等于获得公共分发授权；
- 历史 tracked 生成物、用户导出内容和依赖 overlay 的最终处置决策；游戏源码参考仓库不属于这项待清理对象；
- 旧存档备份与最小游戏基线（属于阶段 0 收尾，仍未完成）；
- 精确 `v1.4.8.119303` overlay 构建验证。

下一项准确任务：

> 确认第三方/参考源码公开分发原则，并为初版决策表中的非参考仓库未决对象补齐 owner、来源和许可证证据；游戏源码参考仓库保持 tracked，不执行清理或移动。

已建立初版决策表：`docs/animusforge-repository-boundary-decision-table.md`。它只提供保守的运营边界，不替代许可证确认。
