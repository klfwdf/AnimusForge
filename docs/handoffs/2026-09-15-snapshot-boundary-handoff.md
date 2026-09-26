# AF 框架内外快照边界与对照测试 HANDOFF（2026-09-15）

## 结论

**本轮实际实现与离线验证完成；整体阶段 8/B1 尚未完成，不宣称“完美复现/零 BUG”。**

生产/测试提交 `f07cb2a24d72446f547949ffb1c518f14b573532`，对照 `955a6be3`，意图检查点 `aa3f3c07`。工作区仍为 `G:/AFMOD/AF-REFACTOR`，分支 `codex/af-framework-skill-delivery-20260911`。仅本地提交，未推送/部署/操作存档，自动化保持暂停。

## 这轮真正完成了什么

| 项目 | 原状 → 本轮结果 |
|---|---|
| 内部框架与公开 API 依赖 | Runtime 原先引用 Api.V1 枚举/DTO → 使用纯内部状态/冻结快照；无 API 源码/引用的 CoreOnly 实际编译通过 |
| 公开快照转换 | 原来混在 Runtime → `Api/Internal/AfV1SnapshotProjection` 独立负责；V1 类型、方法、枚举与原因码保持 |
| 作用域/并发 | 原锁内直接生成 public DTO → 原锁内捕获不可变内部结果、锁外投影；不暴露活 Directory，不在投影时重新求 gate |
| 历史完整制作组对照 | 旧工具找不到已迁移记忆方法 → 先使用既有 B1 严格逆变换，再做原 Native/receiver/全文件对照；完整入口恢复通过 |
| 旧代码清理 | Runtime 删除 public DTO 构造及状态映射，净减 19 行；不保留第二份映射或旧 GetSnapshot 入口 |

真实链路：

```text
Api.V1.AfApi.GetSnapshot（对外原签名）
→ ModuleFrameworkRuntime.CaptureSnapshot（原锁内，原 Directory）
→ ModuleFrameworkSnapshot / ModuleBindingSnapshot（纯内部、冻结）
→ Api.Internal.AfV1SnapshotProjection.Create（锁外、显式 V1 映射）
→ 原 public AfFrameworkSnapshot
```

这不是新注册器、队列、ServiceLocator 或可写 API。内部模块与子 MOD 的承诺继续分开；政策/宴会/GCCZ 的玩法、数值、提示词、默认路径和存档没有修改。

## 代码位置（f07cb2a2，一基行号）

| 位置 | 符号 / 责任 |
|---|---|
| `Api/V1/AfApi.cs:34–37` | `GetSnapshot`：唯一公开消费者，原签名不变 |
| `Refactor/Modules/ModuleFrameworkRuntime.cs:82–108` | `CaptureSnapshot`：捕获原目录/能力状态；Stopped 不求 gate |
| `Refactor/Modules/ModuleFrameworkSnapshot.cs:8–14` | `ModuleFrameworkLifecycleState`：内部装配状态，不是 Campaign 可执行状态 |
| `Refactor/Modules/ModuleFrameworkSnapshot.cs:20–47` | 两个内部快照类型：复制容器、复用原不可变声明/能力结果，不持有游戏 owner |
| `Api/Internal/AfV1SnapshotProjection.cs:12–57` | `Create` / `MapState` / `MapStatus`：API 版本映射，不直接强转内部枚举 |
| `tools/TeamModulePortParityTests/run.py:27–55` | `restore_reviewed_nonport_deltas`：新增调用原 `source_parity.restore_memory_summary_source`，随后继续严格旧对照 |
| `tools/ModuleFrameworkApiTests/source_boundary.py:15–43` | 精确还原本次边界提取，并验证原生命周期/注册/公开入口未出现额外差异 |
| `tools/ModuleFrameworkApiTests/SnapshotBoundaryChecks.cs:11–76` | 冻结、并发、旧新完整 DTO、映射对照 |
| `tools/ModuleFrameworkApiTests/run.py:52–70` | 三种成功编译、实际行为断言拒绝的快照故障 |

[91 点机器索引](../architecture/af-framework-code-map.json)绑定此提交；历史行号需按历史提交查询。生成的旧 Runtime 仅用于测试，不进入生产编译。

## 验证与真实失败记录

| 验证 | 最终结果 |
|---|---|
| 完整制作组对照 | 四个 owner 整文件、SubModule 历史逆变换、13 签名/31 调用点、308 断言、3 个故障反例通过 |
| Memory 逆变换守卫 | 原 15 项通过，未刷新规则 hash 或豁免未审查声明 |
| 内外编译边界 | CoreOnly 不包含任何 API 文件/引用，编译通过 |
| 快照 | 32 项、128 并发捕获/投影、与 955a6be3 原 Runtime 的完整 public DTO 对照通过 |
| API | 原 119 项/256 并发、外部访问 internal 的 CS0122 拒绝通过 |
| 快照故障反例 | Ready 错映射、共享可变输入列表、投影重读活目录，3 类均编译成功且被断言拒绝 |
| Campaign 装配回归 | 原 42 项/5 类故障通过，36 行为/4 模型原顺序与失败处理保持 |
| 构建/实际 DLL | Debug/Release × 1.3/1.4/Bootstrap 六 Stage 通过；4 实现 DLL 元数据 584 项通过 |
| 存档身份 | SyncData 146、CampaignBehavior 36，无新增/移除，Bootstrap 模块身份不变 |

所有构建仅使用既有脚本的 Stage；没有覆盖游戏。实际产物、源码和日志哈希见[验证清单](../audits/2026-09-15-snapshot-boundary-verification.json)。

过程中不是“全程绿”：测试接线初次漏编新检查类、旧故障注入仍使用旧内部枚举均曾失败，已修复重跑。预期异常还留下一个测试进程锁住生成 EXE；已核实其路径仅在本项目测试 `.generated` 下后停止该进程，测试入口改为打印原异常并非零退出。所有断言和故障拒绝要求保留，未用编译失败、超时或文件占用冒充故障反例 PASS。

## 性能、玩家视角及未验证边界

- 仅显式查询分配快照，按原目录上限有界（当前仅 3 组接缝）；无 Tick/轮询/跨查询能力缓存。容器防御复制有额外小型分配，本轮未做游戏帧时间性能测量。
- 旧 public 快照在停止/重载后保持内容，查询不会提交对话/金币/记忆；Ready 仍是“adapter 已装配”。从主菜单查询不得误显示为“Campaign 已可接单”。这些只读 API 行为已离线执行。
- 原 `MyBehavior` 新档/读档 generation、`ShoutBehavior` Mission epoch/队列清理未改；本轮没有新造 GameEnd 清理或完成统一 Campaign/Mission 生命周期。
- **未进行游戏内实测**：真实构造副作用、旧存档、退出/重进、Mission 切换、live Economy/AFEF、原 AF 全功能对照仍需实机；六构建不能代替这些。

## 接下来与收尾门槛

1. 下一优先项仍是 **真实 Campaign/Mission owner 的生命周期接缝**：从原新档/读档/结束/晚回包路径逐项验证，再提取；不能仅改目录状态宣称释放完成。
2. 按 [14 类职责清单](../phase8/af-core-responsibility-decomposition-plan-20260915.md)继续 Conversation/Prompt/Actions 与三渠道真实 owner 拆分，保留 Scene 接力/旁听/去重和唯一权威提交。
3. B1 首次 capture/copy、深来源、完整 writer/预算继续待验；本次快照是小型模块目录快照，**不是**记忆整图深复制问题的修复。
4. 对照工具阻断现已消除；后续变更继续走严格逆变换/行为回归，不拿新 hash 消掉失败。全部候选的实机/存档门槛通过前不删仍承担责任的旧 owner、不默认切换、不最终发布。

回滚采用对 `f07cb2a2` 的受审查逆向提交，原状态见 `955a6be3` / `aa3f3c07`；不 hard-reset 或改历史。三份受保护文件未改变。简明版仅在本地 `.tmp/snapshot-boundary-20260915/team-handoff.md`，本次不上传。
