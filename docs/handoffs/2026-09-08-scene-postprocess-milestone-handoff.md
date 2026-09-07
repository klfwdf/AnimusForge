# AF Scene 完整后处理里程碑交接（2026-09-08）

## 结论与边界

本轮按完整 Scene 调用链实施，不止提取一个 helper。用户已明确批准：Scene 默认新管线只生成正文，再由完整后处理执行动作、多人接力和原有记忆 owner；只改项目源码，不切 Native、不改开关、不部署游戏。

本项为 **Scene 项目内修复及定向离线 VERIFY**，不是阶段八 DONE、真实游戏通过或全部代码零 BUG。全局 Bridge 校验仍有下述集成失败；真实 Campaign/Mission、旧存档、live Economy、AFEF、TTS 与安装目录运行均 `NOT_RUN`。

- 工作区：`G:\AFMOD\AF-REFACTOR`；分支：`codex/af-main-refactor-continuation-20260831`。
- 本项原始基线 `d40808b3`，意图提交 `17151d6b`；开始时 fetch 的共享远端为 `aefa02ad`，不把历史 `a096c1b1` 当最新提交。
- 开发期间其他作者加入 `77772637`、`4a239d95`、`cec3877a`、`af2b4c0b`。已保留；最终组合构建以 `af2b4c0b` 加本轮 Scene 修改为输入，构建前后源码指纹一致。
- 本文与 Scene 代码/测试同批本地提交。准确修复提交可用 `git log -1 --format=%H -- ShoutBehavior.ScenePostprocess.cs` 定位。
- 两份 `2026-09-06-integrated-phase8-handoff.md` / `2026-09-06-team-brief.md` 原有占位草稿不改、不暂存。未推送、未部署、未操作存档、未跨工作区写入。

## 实际修复

1. **取消默认入口提前提交。** 默认 Scene 使用完整 prepared messages 和 5000-token 预算，仅生成正文：无 postprocess composer、空 ActionPlan、不调用 Commit、不写早期记忆。异常、过期、终态失败及矛盾的非空 ActionPlan 不得重新启动旧请求；只保留提交前明确可重试的内部一次回退。
2. **恢复完整后处理。** 原 `TryRunSceneUnifiedActionPostprocess` 移到同类型 partial，保留完整签名、参数默认值、规则资格、动态目标/资产/债务、归一化顺序及周报调用。原 Queue 改为主线程 prepare → 后台字符串网络请求 → 主线程 complete/dispatch。不是用缩减的 Courier builder 替代 Scene。
3. **恢复历史与旁听 owner。** NPC 正文只通过原 Scene/shared-history 与长期听众 owner 顺序记录；玩家发言不再由 detached Host 追加。正文和 action-only speech 均 `commitHistory:false`。保留原 AFEF flush 顺序，未把原 void owner 伪装成原子事务。
4. **恢复接力及阶段语义。** 使用真实 `firstTurn`、END、battle suppression、relay/summon/guide 候选。relay 等待期间借用既有输入 gate，让 speech worker 可以运行，避免双方互等。
5. **封堵正文动作旁路。** 主回复播放前用既有标签 codec 清除可执行领域标签，只恢复已由 Scene 状态确认的 `[STP]` / `[END]`。后处理仍是交易、Duel 等领域动作的权威来源；原 public opt-in factory/Host 保留。
6. **隔离读档、晚回调和超时。** 正文排队、听众历史、后处理三阶段、speech 发布与五处会话保持/清理回调检查 generation/session/epoch。目标失效返回专用结果。Queue 自有 180 秒 deadline，speech 等待 30 秒；超时只退休该请求，晚回调不再次执行动作/relay。
7. **修复 gate 跨请求串扰。** task continuation 绑定原 gate，不能扣新 gate 的计数；waiter owner 防止旧 finally、timeout、提示覆盖新场景。ExecutionContext 按请求捕获/复制/释放，保存 mentions 与六项 runtime target，主线程原上下文得到恢复。

性能：去掉默认入口第二次缩减 Prompt 前处理；规则/资产准备每轮一次，不新增 Tick 全量扫描。网络阶段只接收 prompt 字符串。WorkItem 持有 request-local Hero/Character 引用，不是可持久化或跨存档 DTO；仍需继续审查更早的全量 Scene/Courier capture，不能宣称所有旧游戏读取均已移出后台。

## 清理与保留

- 删除默认提前 Host commit 路径及 `sceneShoutDetachedCommitted` 屏蔽条件；原主文件中约 770 行后处理/Queue 实现迁出，没有保留重复版本。
- 去掉默认复 capture 所触发的缩减规则/Prompt 重建；删除正文直接无保护排队及允许泄漏动作标签的旧路径。
- 保留 `ShoutBehavior` 原类型/程序集身份、原同步 full-postprocess wrapper 和公开 opt-in factory：Native、Courier/外部回放仍有真实调用，不属于可盲删的 dead facade。
- 新 partial 登记到原领域/Bridge 入口清单；不提高任何 LIVE/SAVE 状态。不修改 GCCZ 核心规则、JSON Prompt、存档 key/type、数值、官方构建脚本或开关。

## 验证证据

| 验证 | 本轮结果与真实边界 |
|---|---|
| ChannelCutoverBoundary | 132 PASS；14 个提取/接线检查 PASS；含原 Courier 24 项。真实生产 helper/连续 block，依赖为 stub |
| 后处理全方法差分 | 71 fixtures + 2 completion guard PASS；5 个生成副本变异正确拒绝；8 个提取自检 PASS |
| Queue 编排 | 37 场景 PASS；7 个生成副本变异正确拒绝；物理主/后台线程、晚网络、目标失效、mentions/runtime context |
| Gate 竞态 | 原 `d40808b3` 三种真实竞态红测复现；修后 6 组 PASS；真实 Task continuation/await，不只看源码字符串 |
| Interaction 契约 | Pipeline 40、Host 69、receipt 39、Native failure 4 PASS |
| 生产 DLL 回放 | Detached / Configured / Courier / OptIn / EconomyOwner / EconomyAwareCommit / DuelOutcome 七套 PASS；DuelOutcome 双 API 35 项 |
| 官方构建 | Debug/Release × 1.3/1.4/Bootstrap 六项均 0 warning / 0 error；仅项目内 Stage；构建前后源码一致 |
| 入口/持久化 | entry inventory 与其 10 个自检 PASS；Persistence/Profile 142 keys / 168 bindings PASS |
| 全局 Bridge | **FAIL**；20 个自检中 1 个仓库基准检查 error，同一原因如下，未绕过 |

红测：`d40808b3` 的原 staging 缺陷为 50 PASS / 32 FAIL；新增五个生命周期回调在 `cec3877a` 为 5 PASS / 15 FAIL。含本轮此前未提交修复的红基线，不将所有失败混称为某一个新增补丁的独立效果。

差分测试执行原完整方法与新完整 prepare/network/complete，领域 helper/最终游戏效果仍为 stub。生产 DLL 回放另行覆盖实际 owner/normalizer/契约，不等同真正的 Campaign mutation。历史测试输出中的 `noDefaultCutover=1` 是旧结构测试标签，不代表目前三渠道默认状态。Interaction runner 的 NU1900 是 NuGet 漏洞元数据服务不可达警告，未关闭审计掩盖；官方六项构建本身无警告。

### 全局 Bridge 失败归因

`4a239d95` 有意移除 `InteractionComponentSafePatch.EnsurePatched` 的 FeatureBridgeRuntime gate，使 FocusTick 安全补丁不再依赖可选开关。该作者交接已说明此意图，但 `bridge-binding-manifest.json` 的 `runtime-game-adapter` 仍声明 wired 且 disabled 应不安装 patch，故当前验证报：

```text
wired entry lacks an explicit FeatureBridgeRuntime gate: runtime-game-adapter
```

本轮保留安全修复，不为验绿恢复 gate，也不降低 validator 或伪造 PASS。下一项需明确 mandatory safety patch 与 optional bridge 的边界，再同步真实入口/矩阵/fixture。这不是本轮 Scene 源码编译失败。

## 重放与产物

在 `G:\AFMOD\AF-REFACTOR` 运行，使用 `G:\AFMOD\.dotnet-sdk\dotnet.exe`；Python 工具均只写项目内忽略目录。

```powershell
python -B tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --output-name scene-main-staging-current
python -B tools/ChannelCutoverBoundaryTests/test_extraction.py
python -B tools/ScenePostprocessParityTests/run.py
python -B tools/ScenePostprocessParityTests/run_mutations.py
python -B tools/ScenePostprocessParityTests/run_queue.py
python -B tools/ScenePostprocessParityTests/run_queue_mutations.py
python -B tools/ScenePostprocessParityTests/run_gate_red.py
python -B tools/ScenePostprocessParityTests/run_gate.py
python -B -m unittest discover -s tools/ScenePostprocessParityTests -p test_extraction.py
```

官方构建沿用 `2026-09-08-cutover-terminal-safety-handoff.md` 的本机命令，**仅 `-Stage`，不 `-Deploy`**。固定引用：1.3 `v1.3.15.110062`；1.4 `v1.4.6.115628`。

- 最终构建/生产回放日志：`.tmp/scene-postprocess-milestone-20260908/verified-build-*.log` 与同目录 `Production*ReplayTests.log`。
- 源码/产物绑定：同目录 `verified-source-manifest.json`、`verified-artifact-hashes.json`。前者为源码及参考输入的超集，不是编译文件数量统计。
- 方法/Queue/Gate 回放：`tools/ScenePostprocessParityTests/.generated/`；Channel：`.tmp/channel-cutover-boundary/scene-main-staging-current/`。
- Stage：`bin/Debug/single_module_stage/AnimusForge` 与 `bin/Release/single_module_stage/AnimusForge`，**没有覆盖游戏**。
- 回滚只对本项 Scene 提交做审查后的定向 `git revert`，保留其他作者的遭遇修复及两份草稿；不 reset、不重写历史。

最终源码 SHA-256（工作文件字节，Git 换行规范化后的 blob hash 另计）：

```text
ShoutBehavior.cs                  2cfa4c930513c543b1e7eb3a1f58911d1275a11fe5077321d61d96717d1e3755
ShoutBehavior.ScenePostprocess.cs b9f611f87714762a4fb08ad9230164fe82fab759e96607530ab7bbdd07286117
Debug 1.4 AnimusForge.dll         9ec58eb54c348d0d59a5ddd9614ce539475623f7d7d93f26d6f5980f4d1dbb1f
Release 1.4 AnimusForge.dll       5c06221f51985b9fa5aa65ce5d65075cf249295accc403718d96fa10f936e0d5
```

## 后续整模块顺序与玩家验收

1. 先解决上述 mandatory safety / optional Bridge 清单矛盾，恢复真实全局门禁，不回退安全补丁。
2. 继续 Scene/Courier 前段游戏对象 capture 与网络阶段分离；Native 完整 Prompt/匿名消息 parity、流式、主动开场和回调等价前不切默认。
3. BattleSpeech 异步 classifier fallback 可经 `AfCompatV130.TryReplayOriginalPlayerShout` 反射旁路进入喊话；普通 UI 已挡 busy，但同 gate 重入的完整 BattleSpeech 时序尚未复现。作为有具体入口的待验证风险，不宣称已修。
4. 玩家清单：单人/多人听众共享记忆，连续接力且只有第一轮携带玩家命令；给钱/物品/还债/Duel 只执行一次；正文泄漏标签不扣款；跟随/传唤结束保持；生成中读档、退出场景、目标离场、API超时后不影响新场景；普通战斗与 GCCZ 活动场景各自隔离；旧档/AFEF/TTS 分别记录版本与提交证据。
5. 所有领域的真实证据齐备才做最终默认切换、广泛删旧和发布。自动化按既定整模块顺序接续，本轮不把阶段七/八标 DONE。
