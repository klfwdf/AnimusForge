# AnimusForge 重构执行清单

> 本文件是 AF 重构的公共进度台账。它记录目标、阶段、当前状态、验证证据和交接信息；不替代 `.claude/skills/animusforge-maintainer/` 中的长期工作规范。

## 当前状态

- 项目：Mount & Blade II: Bannerlord AnimusForge mod
- canonical worktree：`E:\Mount-Blade-Bannerlord-AnimusForge-mod-main`
- 当前分支：`refactor/prepare-af-restructure`
- 基线 HEAD：`d4cb1467376c6e923f4295dcefc7878c11dbc7c1`
- 基线父提交：`96a1c60f1877813a9fb3440ddad068d6e92afa1e`（policy 功能基线）
- 当前工作 HEAD：`6f8ed40aca1832163366adbe1afa402a28c89fef`（准备文档/skill 已推送分支）
- 当前阶段：阶段 0，重构准备与基线
- 当前任务：阶段 0 收尾：构建依赖闭包与逐文件 owner matrix
- 当前负责人：待由后续 handoff 指定
- 物理程序集策略：暂不拆分为多个玩法 DLL；先在单一 `AnimusForge.dll` 内完成逻辑模块化
- 旧存档目标：必须兼容；至少保持现有程序集身份、序列化类型和 SyncData key，必要变更必须提供迁移与证据
- 游戏基线策略：保留可复现的测试记录，但不要求现在由用户立刻完成全量手测；优先记录关键功能和重构前后对比结果
- BannerlordRoot：`E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord`
- 已安装模块目录：`E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge`
- 主要游戏内测试版本：Bannerlord `v1.4.7.117484`
- 1.4 构建引用要求：按 `1.4.x` API 线管理；每个开发者可以使用自己的合法 1.4.x 安装，但构建记录必须写明精确 `BuildInfo`，共享验收使用固定代表性 overlay
- 最后更新：2026-08-30
- 状态：IN PROGRESS

## 重要工作区事实

- 工作区在准备开始时并非完全干净：`AnimusForge/SubModule.xml` 有用户已有修改（版本从 `v1.3.7` 变为 `v1.3.7.2`，且文件末尾换行发生变化）。本次准备不回滚、不覆盖该修改。
- 项目当前采用一个 `Modules/AnimusForge` 模块、Bootstrap 加载单一版本实现的发布契约。
- 当前主实现项目是 `AnimusForge.csproj`，Bootstrap 项目是 `AnimusForge.Bootstrap/AnimusForge.Bootstrap.csproj`。

## 阶段总览

### 阶段 0：准备与基线 — IN PROGRESS

- [x] 创建本地准备分支
- [x] 安装项目级 `animusforge-maintainer` skill（未提交）
- [x] 创建基线报告
- [x] 创建本公共重构台账
- [ ] 审阅 skill、基线报告和本台账
- [x] 完成当前仓库只读结构盘点
- [x] 确认代表性存档和游戏内基线方案
- [x] 形成第一版功能—owner—依赖—风险重构地图（见 `docs/animusforge-refactor-map.md`）
- [x] 完成第一版逐文件 owner matrix（见 `docs/animusforge-owner-matrix.md`）
- [ ] 记录可运行的 1.3.x、1.4.x、Bootstrap 构建结果

### 阶段 1：仓库边界与可重复性 — TODO

- [ ] 盘点源码、内容、测试、工具、脚本、文档、引用、依赖和产物平面
- [ ] 确认 `.gitignore` 与用户数据/生成产物边界
- [ ] 固化构建、stage、package、deploy 的现状说明
- [ ] 确认许可证、分发和第三方文件处理原则

### 阶段 2：模块目录与所有权地图 — TODO

- [ ] 建立现有功能 → 当前入口/文件 → 目标 owner 映射
- [ ] 标注存档、Prompt、标签、Harmony、Tick、UI、主线程和版本影响
- [ ] 标注跨模块行为和候选 Bridge
- [ ] 为每个目标模块定义非目标和回滚入口

### 阶段 3：Contracts 与基础运行时 — TODO

- [ ] 定义模块身份、能力、事件、DTO、契约版本
- [ ] 设计模块 manifest、profile、依赖和健康状态
- [ ] 整理 Foundation、主线程调度、后台任务、诊断和 SafeMode
- [ ] 整理 GameAdapter 与 1.3/1.4 API 边界
- [ ] 增加 no-op、缺依赖和 SafeMode 组合验证

### 阶段 4：Persistence / Profile / Config — TODO

- [ ] 盘点 SyncData、JSON、PlayerExports、AFEF 和旧数据
- [ ] 建立持久化 namespace 与迁移目录
- [ ] 保留现有程序集/类型/key 兼容性
- [ ] 建立配置快照、模块开关和 profile 解析边界

### 阶段 5：Conversation 统一交互管线 — TODO

- [ ] 统一场景喊话、自由对话、信使的快照/资格/Prompt/历史结构
- [ ] 统一后处理标签、动作执行入口和 AFEF 事实写入
- [ ] 保留旧入口作为 facade
- [ ] 验证三渠道规则和记忆一致性

### 阶段 6：Memory / Prompt / Action — TODO

- [ ] 提取 Memory 与事实服务
- [ ] 提取 Prompt/Rule 与前后处理规则
- [ ] 建立统一动作解析、授权、当前状态验证、主线程执行和结果记录
- [ ] 先迁移低风险动作垂直切片

### 阶段 7：领域模块渐进迁移 — TODO

建议顺序（以实际依赖盘点为准）：

1. Economy / Trade / Debt / Reward
2. Policy
3. Courier
4. Duel
5. WorldMap
6. Scene
7. Diplomacy
8. Siege / Battle
9. Knowledge / UI

每个领域都必须保持旧入口可用，完成调用方、存档、双版本、渠道、profile 和组合验证后才删除旧实现。

### 阶段 8：Bridge、旧结构清理与最终验收 — TODO

- [ ] 仅为确有跨模块所有权的行为建立 Bridge
- [ ] 验证 A、B、A+B、A+B+Bridge、Bridge 故障矩阵
- [ ] 清理 God Object、重复注册、旧 facade 和临时代码
- [ ] 验证 1.3、1.4、Bootstrap、stage、package、存档和游戏内场景
- [ ] 记录所有 NOT-RUN 与剩余风险

## 目标逻辑模块（第一版，非最终物理 DLL 方案）

- `AF.Contracts`
- `AF.Foundation.Runtime`
- `AF.GameAdapter`
- `AF.Persistence`
- `AF.Profile` / `AF.Config`
- `AF.Module.Conversation`
- `AF.Module.Memory`
- `AF.Module.Prompt`
- `AF.Module.Action`
- `AF.Module.Policy`
- `AF.Module.Economy`
- `AF.Module.Courier`
- `AF.Module.Duel`
- `AF.Module.WorldMap`
- `AF.Module.Scene`
- `AF.Module.Diplomacy`
- `AF.Module.Siege`
- `AF.Module.Knowledge`
- `AF.Module.UI`
- `AF.Bridge.*`

第一阶段优先建立逻辑边界和公共契约，不为了目录图强行拆成许多 DLL。发布契约仍是一个 Bootstrap 加载一个版本化 `AnimusForge.dll` 实现。

## 每个重构切片的必填记录

- owner：Foundation / GameAdapter / 单一 Module / 联合 Bridge
- 改动文件与公共契约
- 影响的渠道、profile、Bannerlord API 线、Harmony/Tick/UI
- 存档 namespace、key/type 和用户数据影响
- 运行频率、缓存、队列上限、主线程边界
- 验证命令和实际结果
- 回滚 commit 或旧 facade
- 下一步和阻塞项

## 状态规则

- `TODO`：尚未开始
- `IN PROGRESS`：正在处理
- `VERIFY`：实现完成但验收未完成
- `DONE`：验收证据完整
- `BLOCKED`：有明确阻塞原因
- `NOT-RUN`：检查未运行，必须写原因；不能当作通过

## 变更意图记录

| 时间 | 任务 | 范围 | 风险 | 验证 | 状态 |
|---|---|---|---|---|---|
| 2026-08-30 | 第一版重构地图完成 | `docs/animusforge-refactor-map.md`：运行链、owner、持久化、交互、风险、顺序 | 不移动源码；目标仍为单一 `AnimusForge.dll`、旧存档兼容 | 3 个只读审计结果合并；构建仍被依赖闭包阻塞 | VERIFY |

- 最新 handoff：`docs/handoffs/2026-08-30-refactor-preparation.md`
- 下一位接手者先读取：`CLAUDE.md`、`.claude/skills/animusforge-maintainer/SKILL.md`、本文件、baseline 和最新 handoff。
