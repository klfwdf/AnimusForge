# J09 Actions / 事实提交实施计划

> 状态：`J09_OFFLINE_VERIFIED`（2026-09-21；LIVE/SAVE/provider 仍独立 NOT-RUN）
> 依赖：J07、J08 已完成必要离线验收。
> 启动基线：`5a2df9d64485cea03f4a069afda55c2f348968bc`；当前产品提交以 Git/HANDOFF 为准。
> 本文只授权 J09 的标签、ActionPlan、执行接缝和回执重构；不授权 J10 渠道状态机、J11 制作组玩法、J12/J13 领域玩法、J14 新公开提交能力、部署或默认切换。

## 0. 当前施工回执（2026-09-21）

| 包 | 状态 | 产品提交 / 证据 |
| --- | --- | --- |
| G0 / J09a | DONE | `20ba9527`：catalog/parser 原样归位 `AF.Module.Actions/Tags`；`21206ec6` 修复第 65 个 raw 动作绕过；`docs/architecture/af-action-protocol-owner-matrix.md` 完成 Prompt/parser/owner/渠道矩阵 |
| J09b | DONE | `fd01974b`：`ActionPlanIntegrityPolicy` 成为 raw/plan 有序一致性唯一 owner，executor 归位 `Execute`；Economy/Duel typed seam 保留 |
| J09c | DONE | `dbe87c4`：committer/cache 归位 `Receipts`；`65a14421` 提取动作终态 owner，成功/拒绝/partial/unknown 与历史提交解耦 |
| J09d shared core | DONE | `bb223aec`、`f61ec13e`：新增 `LegacyChannelActionCommitter`，三渠道 detached 生产提交均经同一 canonical request/action identity 与终态回执边界；无动作不调用 owner，disallowed/overflow fail closed |
| J09d default/compat | DONE | `f4f022a3` request-bound compatibility executor；`449227a9` 默认 Native/Scene/Courier 动作尾接线；`beb7dd38` minimal action identity capture，不重复分配 Prompt history；`2a1fc124` 修复 queued Scene action 未完成前过早发布 relay |
| J09e | DONE | `53071dca` 三渠道单执行/时序/无 memory 双写 guard；`d609e3f2` 334 锚点地图；重复 owner/旧路径清理、正常与有效变异、六构建、API/存档/Phase8 门禁通过 |

最终候选已通过：ActionProtocol 14 项及 5 个有效变异；InteractionPipeline、Economy、Duel；Scene 71/37 与 7 个可编译 Queue 变异；Courier 39/34 与 8 个可编译 owner 变异；Native action 91、admission 44、completion 184；默认三渠道 wiring 25；Debug/Release × 1.3/1.4/Bootstrap 六构建均 0 warning / 0 error；实际四 DLL API/metadata 1060；Persistence/Profile 142/168/13/44；Phase8、Bridge 及 334 锚点双模式通过。真实 Campaign/Mission、旧档、live Economy/外交/provider 仍为 `NOT-RUN`。

## 1. 目标与完成定义

J09 要把三渠道共用的动作协议收敛为一条清晰责任链：

```text
领域资格 / Prompt tag_rules
        ↓
本次请求的有限 allowlist
        ↓
唯一标签扫描与解析
        ↓
不可变 ActionPlan
        ↓
渠道主线程重新验证身份与上下文
        ↓
领域 typed execution port
        ↓
终态 receipt（成功 / 拒绝 / 部分成功 / unknown）
        ↓
可见历史 + 仅已确认的 AFEF 事实
```

只有以下四项同时满足，才可标记 `J09_OFFLINE_VERIFIED`：

1. **唯一协议 owner**：Native、Scene、Courier 使用同一标签目录、扫描器和 ActionPlan 规范；没有第二套缩水 parser。
2. **实际消费者接线**：三个生产渠道都经过同一提交/回执边界；渠道仍各自负责 live 目标解析、线程和会话身份。
3. **终态语义完整**：正文生成成功不能冒充动作成功；已执行、拒绝、部分成功、开始后未知必须可区分且不可误重试。
4. **兼容门禁通过**：相关正常用例、有效变异、双 API Debug/Release、Bootstrap、公共 API、存档契约和代码地图通过。

真实 Campaign/Mission、旧存档、真实物品/金币/外交状态和 provider 仍分别标记 `NOT-RUN`，不能用离线通过替代。

## 2. 当前真实边界

| 责任 | 当前生产位置 | 当前状态 / J09 动作 |
| --- | --- | --- |
| 运行期允许目录 | `src/modules/AF.Module.Actions/Tags/LegacyActionTagCatalog.cs` | 有限 family/template 列表已归位并完成 Prompt/owner/UI 对账矩阵 |
| 标签扫描和解析 | `src/modules/AF.Module.Actions/Tags/LegacyActionTagParser.cs` | 唯一 detached parser；保留 balanced bracket、嵌套 RichText、坏标签恢复、顺序、重复、64 上限和显式 wildcard 规则 |
| 动作 DTO / 端口 | `Refactor/Contracts/InteractionContracts.cs:277-553` | `PostprocessContext`、`ActionRequest`、`ActionPlan`、执行/回执接口；属于稳定内部契约，不因整理物理目录改变签名 |
| 通用执行适配 | `src/modules/AF.Module.Actions/{Plan,Execute}` | raw/plan integrity、channel commit 与 Native/Economy/Duel adapter 已分层；领域玩法没有搬入通用 Actions |
| 唯一提交边界 | `src/modules/AF.Module.Actions/Receipts/{ActionExecutionCommitter,InteractionResultCommitter}.cs` | 动作终态和可见历史/confirmed facts 已分开；owner-started 异常为 unknown 且不可重试 |
| 幂等票据 | `src/modules/AF.Module.Actions/Receipts/InteractionCommitReceiptCache.cs` | reservation 只能由自己完成；冲突 fingerprint fail closed；终态缓存有界 |
| Native 接缝 | `ShoutBehavior.CreateNativeConversationActionPlanExecutorForExternal`、`ShoutBehavior.NativeActionDispatch.cs` | 保留主线程、当前目标复核、异常 owner-started 语义 |
| Scene 接缝 | `ShoutBehavior.ScenePostprocess.cs`、`CreateSceneShoutActionPlanExecutorForExternal` | 保留统一后处理 work item、先正文后动作、直接场景/GCCZ 特例和会话代际；J10 的接力/旁听/距离不在本包改写 |
| Courier 接缝 | `CourierDeliveryBehavior.DetachedPostprocess.cs`、`CreateCourierReplyActionPlanExecutorForExternal` | 已使用 detached parser；提交仍必须发生在正确到达/回复阶段，不能提前到预生成；完整运输状态机留 J10 |
| 领域回执 | `Refactor/Runtime/*OutcomeReceipt.cs` 及 Economy/Duel/Weekly/Notoriety 接口 | J09 只统一终态词汇和通用接缝；带存档/领域生命周期的 receipt 暂留原 owner，后续 J12/J13 再物理归位 |

### 2.1 必须先解决的目录差异

当前至少存在三类“标签目录”，不能因为名字相近就机械合并：

- Prompt 中输出给模型的 `tag_rules`：由本次规则资格和领域上下文决定。
- `LegacyActionTagCatalog.DefaultAllowedTagFamilies`：detached 管线的运行期兼容 allowlist。
- `AnimusForgeTagCatalog`：终端/UI 的标签说明与浏览目录，还包含 AFEF、CONTENT、场景 session 等非 ActionPlan 协议。

J09 必须产出逐 family 矩阵，证明“可提示、可解析、可执行、由谁执行、哪些渠道允许”。`AFEF`、`CONTENT`、`AF_SCENE_SESSION` 等不是游戏动作，不得为了目录统一塞进 `ActionPlan`。

## 3. 不变量

1. 不新增、删除或改名任何 `SyncData` key，不改变保存类型、程序集名或公开 V1 API。
2. 不把领域玩法写进通用 tag switch；通用层只做协议、计划、路由和回执，领域 owner 通过 typed port 执行。
3. 不用 `ACTION:*` 放开未知动作；wildcard 仅保留当前受审计的有限前缀语义。
4. raw 输出和 ActionPlan 必须严格有序一致；raw 中出现已识别但未授权的协议标签必须 fail closed，不能静默忽略后继续执行其余动作。
5. `ActionPlan` 只含 detached 字符串/参数，不携带 Hero、Agent、Settlement、Party、Mission 等 live 对象。
6. 游戏对象读取、资格复核和状态修改只在所属主线程；跨 await 后重新验证 generation、session、channel、subject、target 和 request fingerprint。
7. 一个动作计划只经过一个权威执行边界；禁止“新执行器 + 旧直接执行”双写。
8. owner 抛异常时可能已经修改游戏状态，终态必须是 `UnknownAfterStart`，不可自动重试或伪造 confirmed fact。
9. 部分成功只记录实际确认的 action count / facts；未执行或 unknown 动作不能进入 AFEF。
10. 可见对话历史与动作事实分开：正文成功可以写可见 exchange，但不能据此把动作标成功。
11. reservation/ticket 只能由创建它的请求完成；迟到旧请求不得覆盖或释放新请求。
12. Scene 的正文先展示、共享后处理、唯一动作/记忆提交顺序不变；Courier 到达提交时点不变。
13. 保留现有 Duel/Economy/Weekly sidecar 的 request-bound fingerprint 与 terminal receipt；不靠重放 ActionPlan 恢复未知状态。
14. 不为减少行数制造转发壳、复制 parser、删除活跃兼容入口或压缩断言。

## 4. 执行包

### G0：盘点与基线冻结

一次性完成以下清单，之后按失败信号定向回归，不反复重开全仓调查：

- 导出所有 Prompt `tag_rules` family/template、`DefaultAllowedTagFamilies`、parser `SupportedFamilies`、三渠道实际 executor 和领域执行入口。
- 为每个 family 记录：Prompt 资格来源、参数语法、目标解析、允许渠道、typed owner、成功事实、失败/部分/unknown 语义、是否有持久化 receipt。
- 记录三渠道同一 raw 输出在当前代码中的 ActionPlan 与执行结果；保留一个正常、一个不允许、一个部分成功、一个 owner-started 异常基线。
- 核对反射/Harmony/外部引用、namespace、public/internal 可见性和 `.csproj` 实际 Compile 集合。

**退出门**：矩阵中没有 `UNASSIGNED` 的已支持动作；无法确定 owner 的 tag 保持拒绝，不猜测执行器。

### J09a：Tags 唯一 owner

目标目录：

```text
src/modules/AF.Module.Actions/
└─ Tags/
   ├─ LegacyActionTagCatalog.cs
   ├─ LegacyActionTagParser.cs
   └─ （仅在确有两个以上消费者时）ActionTagAuthorization.cs
```

实施要求：

1. 先做物理归位，namespace、公开/内部可见性和构造/方法签名不变；真实 Compile 输入只保留一份。
2. Native、Scene、Courier 都从同一 parser 实例/工厂语义建立本次请求的计划；不复制 `ExtractCandidateSpans`、normalize 或 allowlist 判断。
3. 保留 `GIVE_ASSET` 中 `[ROT]`、坏 outer tag 遇到下一个 protocol opener 的恢复、重复标签顺序、参数 target/quantity 规则和 maxActions 上限。
4. 增加 Prompt `tag_rules` → request allowlist 的核对门，不让未注入规则的领域动作仅因全局默认目录而获权。
5. `RemoveProtocolTags` 与 `Parse` 必须共享同一识别语义；移除可见标签不能改变 ActionPlan 顺序或授权结果。

**退出门**：三渠道同一 raw/context 得到逐字段相同 ActionPlan；旧 parser 路径无实际编译/消费者；正常与恶意/坏格式 fixture、可编译变异均通过。

### J09b：Plan 与 typed execution ports

目标目录：

```text
src/modules/AF.Module.Actions/
├─ Plan/        # canonical plan/fingerprint/raw-plan parity
└─ Execute/     # 通用路由与 typed port 组合，不含领域玩法
```

实施要求：

1. 从 `LegacyNativeActionPlanExecutor` 提取与 Bannerlord 对象无关的 raw/plan 一致性、canonical fingerprint、重复/顺序检查。
2. 现有 `ActionRequest` / `ActionPlan` 契约优先原样复用；若物理归位会触发 ABI/namespace 变化，则留在契约文件，只新增内部实现，不为目录美观破坏边界。
3. 建立稳定的“family → typed domain port”映射；通用执行协调器只能组合结果，不直接操作金币、物品、债务、外交、决斗、场景或 GCCZ 状态。
4. 复用现有 Economy/Duel request-bound 能力和渠道 delegate；没有完整 typed port 的领域仍走已审计 legacy delegate，不伪装成已模块化。
5. 执行前再次检查 channel/session/subject/target/generation/fingerprint；校验失败不得调用任何领域 port。

**退出门**：计划校验只有一个 owner；各领域动作只进入自己的 typed port 或明确保留的 legacy adapter；同一计划不能被执行两次。

### J09c：Receipts 与事实提交

目标目录：

```text
src/modules/AF.Module.Actions/
└─ Receipts/
   ├─ InteractionResultCommitter.cs
   └─ InteractionCommitReceiptCache.cs
```

物理迁移时保持 `AnimusForge.Refactor.Runtime` namespace 和现有可见性，除非先有完整消费者/ABI 证据支持后续命名空间变更。

终态表：

| 状态 | 可自动重试 | `actionsExecuted` | confirmed facts / AFEF | 可见 exchange |
| --- | --- | --- | --- | --- |
| 无动作且正文成功 | 否 | false | 无动作事实 | 按原规则写 |
| 全部执行且 receipt 精确匹配 | 否 | true | 仅 owner 确认事实 | 写 |
| 执行前拒绝 | 仅由上层新请求决定，不在提交器自动重试 | false | 无 | 保留原可见历史语义 |
| 部分成功 | 否 | true（仅表示至少一项确认） | 仅 applied 子集 | 写并标部分终态 |
| owner 开始后异常 / 不可判定 | **禁止** | false | 无 confirmed fact | 写可见 exchange，终态 unknown |
| 重复相同 fingerprint | 否 | 复用原终态 | 不重复写 | 不重复写 |
| 同 requestId 不同 fingerprint | 否 | false | 无 | fail closed |

实施要求：

- 保留 action 先执行、再依据 receipt 写 confirmed facts 的真实顺序；不能先写 AFEF 再执行。
- `InteractionCommitReceiptCache.Reservation` 的 owner 身份不能弱化；终态条目才可按上限淘汰。
- 四类领域 OutcomeReceipt 不做无差别搬家：通用接口/读取接缝可归 Actions，带存档 key、领域状态机和恢复语义的类留领域 owner，并在 J12/J13 归位。
- Weekly sidecar 准备失败不阻断核心提交；核心开始后 sidecar 异常不能触发动作重放。

**退出门**：成功、拒绝、partial、unknown、duplicate、fingerprint conflict 的结果/历史/事实逐字段覆盖；抛异常变异必须命中 unknown 断言。

### J09d：三渠道实际接线

1. **Native**：继续通过已验的 Native 四阶段，在主线程构造 executor 并提交；保留提前观察/TTS/显示和 owner-started 异常包装。
2. **Scene**：`Prepare → Request → Complete → Execute/Receipt` 只走一个后处理/执行链；保留直接场景命令与制作组桥的既有相对顺序，不改 group/relay/passive/reaction 状态机。
3. **Courier**：预生成只产生文本/计划候选；实际到达/回复 commit 才执行。旧 retry、运输、来信 session 身份留 J10，本包只替换解析/提交接缝。
4. 三渠道各自 live target resolver 保留在 GameAdapter/宿主；共享 Actions 模块不读取 Bannerlord 对象。

**退出门**：三渠道同一协议和 fake typed owner 得到相同 ActionPlan/终态；各渠道取消、stale、目标变化和重复回执都不执行；不存在旧直接执行与新执行并行。

### J09e：清理与整包验收

- 只在替代实现接真实消费者、Compile 输入唯一、无反射/Harmony/存档/外部 ABI 责任后删除旧路径。
- 保留项逐项写理由；`Legacy*` 名称本身不是删除理由。
- 全仓检查重复 scanner、重复 allowlist、直接 ActionPlan 执行、直接 confirmed fact 写入和旧路径引用。
- 更新代码地图、范围图、owner matrix、主台账和根 HANDOFF；制作组简版仍留 `.tmp`，不入 Git。

## 5. 验证矩阵

### 5.1 必跑正常用例

- `InteractionPipelineContractTests`
- `ScenePostprocessParityTests` 及其正常 suite
- `CourierPostprocessOwnerRegressionTests`
- `CourierCommitOutcomeTests`
- `NativeActionDispatchOutcomeTests`
- `EconomyAwareActionPlanExecutorContractTests`
- `DuelOutcomeContractTests`
- `WeeklyMemoryMaterialOutcomeContractTests`
- `NotorietyConversationOutcomeContractTests`
- 新增三渠道 ActionPlan/receipt 等价契约

### 5.2 必须有效变红的故障

至少覆盖：

1. 放宽为 `ACTION:*`；
2. 跳过 raw/plan 一致性；
3. 打乱重复标签顺序；
4. 忽略 caller cancellation/stale；
5. 同 requestId 接受不同 fingerprint；
6. executor 抛异常后标成可重试失败；
7. partial 把全部动作写成 confirmed facts；
8. Courier 在预生成阶段执行；
9. Scene 同时走旧直接执行和新提交器；
10. reservation 由非 owner 完成。

每个变异必须编译成功、命中具名场景并因预期断言失败；编译错误、路径错误或未触发代码不算红例。

### 5.3 最终兼容门禁

- Debug/Release × Bannerlord 1.3/1.4 + Bootstrap 六构建，0 warning / 0 error。
- 实际四个 implementation DLL 的 public API/metadata 检查。
- Persistence/Profile/Config、chunk/identity 相关门禁；无新 key/type。
- 代码地图 recorded + working-tree；只证明定位，不冒充玩法验收。
- 不访问真实付费 provider，不 Stage/Deploy/Package，不操作游戏或存档。

## 6. 有限责任包与停止钻牛角尖规则

每个 J09a–J09e 都按“实现 → 自审 → 受影响测试 → 有效变异 → 必要构建”完成一个整包。满足本节退出门后立即进入下一包，不因以下原因滞留：

- 主类仍然很长；
- 还能再加一个相似 fixture；
- 历史文档仍有旧路径；
- Legacy 命名不好看；
- 可以为了目录整齐再搬一个领域类。

只有新的可复现失败、相关源码变化或退出门未满足，才重开已闭合包。不得通过降低断言、跳过真实消费者、复制实现或删兼容功能来“加速”。

## 7. 提交、回滚与交付

建议提交序列：

1. `chore: checkpoint before J09 action protocol convergence`
2. `refactor(actions): centralize tag catalog and parser`
3. `refactor(actions): separate plan validation and typed execution ports`
4. `refactor(actions): centralize commit receipts and fact outcomes`
5. `refactor(actions): wire native scene and courier action commits`
6. `docs: accept J09 offline package and hand off to J10`

每个产品提交都可定向 revert；不用 reset/rebase/force push。J09 完成前不删除领域 receipt 或旧执行入口；若某渠道未接通，状态必须写 `J09_IN_PROGRESS`，不得因标签 parser 已搬迁而提前标 DONE。
