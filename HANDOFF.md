# AF 总 HANDOFF — 当前入口（2026-09-11）

## 最新续作：Native 前置历史（生产/测试 128e9842）

- 玩家显示名、tentative 输入、pending AFEF 与 Native 历史消息改在同一次主线程消费里准备；原 history key 只解析一次，私有 helper 默认行为不变。
- 五个拒绝分支与 action discard 共用固定 key + 原 owner/generation/会话/revision 的清理，改为只删 player/user，不误删事实或新存档重用序号。未开始队列超时明确失败，晚到不补做；原 Action core 未改。
- 111 检查 / 12 变异，原 184/15、88/9、44/7、46/6、ports 308/3，六项 Stage、四 DLL 532 元数据和 16 组相关回归通过；不是实机。
- 简明版：`docs/handoffs/2026-09-11-native-pending-history-team-handoff.md`；技术说明：`docs/architecture/af-native-pending-history-boundary.md`；审计：`docs/audits/2026-09-11-native-pending-history-verification.md`；台账：`docs/phase8/native-pending-history-progress-20260911.md`。
- 检查点 `1547460a`。未推送、未部署、未操作真实存档；公共 Api.V1 仍只读。
- 下一项：更早 Native 人设/规则/持久记忆 prepare，以及通用 main-thread func 的 bool timeout 问题；保持网络在后台，随后继续 TTS 直接回调、Courier prepare。不要把本段完成当成全 Native 或最终阶段 8 DONE。

## 前序续作：Native 记忆接受结果（生产/测试 18f48678）

- Native 已从 void 历史外壳接到一个支持 sceneSessionId 的 internal strict owner，检查运行期接受结果；原 public 六参接口保留 -1 loose 与 ABI。原 Action core、底层 Append/AFEF/数值未改。
- owner 缺失/false/失败不再按正常完成处理，提示记忆未确认，不重放动作或删除部分记录。必要关窗在记忆失败后也保留且仍绑定原会话；非持久 NPC/空 payload 不伪造写入请求。
- 184 检查 / 15 变异，原 88/9、44/7、46/6、ports 308/3，六项 Stage 和实际四 DLL 532 元数据通过。存档契约仅修正两处 -34 的源码行号，168 绑定身份不变，复验 PASS。
- 简明版：`docs/handoffs/2026-09-11-native-memory-acceptance-team-handoff.md`；技术说明沿用更新后的 `docs/architecture/af-native-completion-boundary.md`；审计：`docs/audits/2026-09-11-native-memory-acceptance-verification.md`；台账：`docs/phase8/native-memory-acceptance-progress-20260911.md`。
- 检查点 `c5de2186`。Applied 仅为运行期接受，不是磁盘/SyncData/跨动作事务或恢复 receipt；新 Api.V1 仍只读。未推送、未部署、未实机验收。
- 下一项：更早 Native prepare/失败 pending 清理，再做 TTS 直接回调、Courier prepare 和完整生命周期/恢复证据；不改制作组业务或直接开放新公共提交。

## 前序续作：Native 主线程收尾（生产/测试 d7ab9610）

- 动作后的历史派发、短期记录/显示标记和最终 TTS 改到同一次主线程消费；不再先检查目标再返回后台写游戏状态。原 Action core 未改。
- 动作前捕获 scene session 与非 Hero party memory identity；动作合法结束会话时保留原目标历史派发，临时状态/延迟关窗仍绑定原 context/revision。动作 discard 清理也限定原上下文主线程。
- 新 102 检查 / 9 变异，原动作 88 / 9、准入 44 / 7、展示 46 / 6、ports 308 / 3，最终六项 Stage 和 16 组回归通过；不是实机/旧存档验收。
- 最新简明版：`docs/handoffs/2026-09-11-native-completion-team-handoff.md`；技术说明：`docs/architecture/af-native-completion-boundary.md`；审计：`docs/audits/2026-09-11-native-completion-verification.md`；台账：`docs/phase8/native-completion-progress-20260911.md`。
- 本轮检查点 `e49aabbd`。未推送、未部署，公共 Api.V1 仍只读；旧 ForExternal 兼容入口不等于新公开 SDK。
- 下一项：复用已有 MemoryCommitResult 严格接受边界并保留 Native scene session；随后处理更早 prepare/失败 pending 清理、TTS 直接回调、Courier prepare。旧 void 历史 owner 仍可能吞错/无 owner，不能宣称已实现可靠持久化或完整原子 AFEF receipt。

## 前序续作：Native 动作派发边界（生产/测试 9a5335be）

- 前半批 `8da4fbd7` 修复动作异常被当成功、日志异常让回复提前结束；本次 `9a5335be` 继续补齐未消费动作队列的等待期限。原业务 Core 未改。
- 仅尚未 claim 的动作可在 30 秒后过期，晚到不补做；已开始动作等待真实结果，不按超时伪装取消或自动重试。两个 Overlay 失败分支仍在原展示 scope 内，目前共 16 个受保护异步 UI 消费点。
- 88 检查 / 9 变异、原准入 44 / 7、展示 46 / 6、ports 308 / 3、六项 Stage 构建及 16 组相关回归通过；不是实机验收。
- 简明交接：`docs/handoffs/2026-09-11-native-action-outcome-handoff.md`；技术边界：`docs/architecture/af-native-action-dispatch.md`；最终审计：`docs/audits/2026-09-11-native-action-timeout-verification.md`；台账：`docs/phase8/native-action-outcome-progress-20260911.md`。
- 前半批审计保留在 `docs/audits/2026-09-11-native-action-outcome-verification.md`；检查点分别为 `861dd7a7`、`841e8751`。
- 未推送、未部署、公共 API 仍只读。下一项：Native 成功路径的主线程事实/记忆收尾（须区分旧会话晚返回和 owner 合法结束会话），然后更早 prepare/TTS、Courier prepare。不要把本次派发取消当整个回合回滚。

## 前序续作：Native 展示观察（生产/测试 32230a64）

- 两个 Overlay 提交入口复用同一内部观察桥和完整旧 Native 流程；14 个 UI 异步消费位置在出队时核对捕获会话，而不是只看当前 NPC 可用。
- 后端已释放时，合法最终结果仍能显示；换会话/读档/新 revision 后旧结果失效，并只释放本地旧 busy，不操作新显示。
- 新 46 检查 / 6 变异、原准入 44 / 7、六项构建和相关回归通过。公共 V1 仍只读；没有推送或部署，实机未验收。
- 最新短版：`docs/handoffs/2026-09-11-native-presentation-handoff.md`；技术边界：`docs/architecture/af-native-presentation-lifetime.md`；验证：`docs/audits/2026-09-11-native-presentation-verification.md`。
- 前序准入生产 `77d4a940`，记录保留在 `docs/phase8/native-admission-progress-20260911.md`；本轮台账为 `docs/phase8/native-presentation-progress-20260911.md`。
- 下一轮：继续 Native prepare/动作后事实回执及剩余 TTS 直接回调边界，然后处理 Courier 双向 prepare；不能把 Overlay 观察票据当成完整公共请求服务。

## 1. 当前结论

**已进入确认架构的初版实施；本轮新接口不是整个阶段 8 或完整 SDK 的最终完成。**

```text
AnimusForge.dll
├─ AF 主体：对话、LLM、Prompt、标签、记忆、调度
├─ internal 模块接口与薄桥 → 政策 / 宴会 / GCCZ
└─ public Api.V1（首版只读） ← 独立子 MOD DLL
```

当前工作树：`G:\AFMOD\AF-REFACTOR`。
分支：`codex/af-main-refactor-continuation-20260831`。
初版实施前：`df6ab928`；本轮意图/回滚检查点：`6e0de826`。
框架初版生产与测试提交：`a616958c`；最新生产见上方续作段。
精确最终提交请运行 `git log -3 --oneline`；本文与本轮源码一起提交，不编造包含自身的未来 commit hash。

给制作组直接看的最新短版见上方；框架初版说明保留在 `docs/handoffs/2026-09-11-framework-v1-team-handoff.md`。

## 2. 框架初版真实变更（a616958c）

- `Refactor/Modules/InternalModuleDirectory.cs`：内部定义、依赖/版本校验、冻结与只读目录。未初始化不报告可用，冲突不覆盖 provider。
- `TeamModulePorts.cs / TeamModuleAdapters.cs / TeamModuleServices.cs`：3 组 internal 接口、13 个原样转接方法、单例薄桥。没有改额外模块业务实现。
- `ShoutBehavior.cs / ShoutBehavior.ScenePostprocess.cs / MyBehavior.cs / CourierDeliveryBehavior.cs`：共 31 处 receiver 接入；既有默认流程、参数和权威提交顺序保留。
- `ModuleFrameworkRuntime.cs / SubModule.cs`：加载时显式装配，卸载时发布停止状态；不是 Campaign/game ready 事件，也没有新增 Tick。
- `Api/V1/AfApi.cs / AfApiContracts.cs`：稳定英文 ID、V1 能力查询、框架只读快照。内部能力 `IsExternallyCallable=false`。
- 新增契约/外部编译/薄桥回归，更新受真实receiver迁移影响的旧测试接线。
- 存档对照表只刷新因新增 using 引起的源码行号；168 个 key/ref/type/source 身份保持不变。

### 不要夸大

- 当前只登记 `af.team.policy/gathering/siege` 的选定 `dialogue` 接缝，不是所有模块功能完成迁移。
- 目录的可用状态不是执行授权，也不拦截全部历史 ForExternal 调用；每个真实请求仍由原 owner 检查。
- 新公共 API 只有 `CatalogRead`；Native/Scene/Courier 提交、动作、记忆和扩展注册明确 `NotSupported`。
- 旧 `NpcRulerPolicyBehavior.BuildActivePolicyDialogueContextForExternal` 目前本就返回空上下文；新桥没有恢复/更改业务。

## 3. 文档导航

- 总体图及责任：`docs/architecture/af-framework-v1-overview.md`
- 制作组接入步骤：`docs/architecture/af-internal-module-guide-v1.md`
- 子 MOD 使用示例/兼容边界：`docs/architecture/af-public-api-guide-v1.md`
- 本轮实施与验证台账：`docs/phase8/framework-v1-execution-20260911.md`
- 原逐项清单：`docs/phase8/af-core-review-checklist-20260910.md`（历史审批快照；用户随后已授权本轮初版）
- 上一生产修复：`docs/handoffs/2026-09-09-recovery-fixes-handoff.md`
- Courier 深层线程缺口：`docs/audits/2026-09-09-courier-thread-boundary-plan.md`

## 4. 框架初版验证（最新 Native 验证见上方）

已完成：Debug/Release × 1.3/1.4/Bootstrap 六项构建全部通过；新目录 44、公共 API 119、薄桥 308 个断言通过，四份实际实现 DLL 的 472 个元数据断言通过；原 Scene/Courier/管线与所选生产回放通过。原始命令/日志在 `.tmp/framework-v1-20260911/`，可提交的摘要在 `docs/audits/2026-09-11-framework-v1-verification.md`。

**离线回归/构建不能替代实机验收。** 前次制作组对旧候选的测试反馈，不会自动成为本轮新接口的 LIVE/SAVE 证据。

## 5. 后续工作（按顺序，不重写额外模块业务）

1. Native：准入、排队 epoch、共享后端 busy、Overlay 队列观察与动作派发失败/未开始超时和主线程收尾边界已落地；单次运行期记忆接受结果也已接入；前置历史与五个拒绝清理也已收敛；继续其他 prepare/通用队列、完整请求/恢复证据及 TTS 引擎直接回调边界，再评估有限公共普通文本提交。
2. Courier 双向更早的 prepare：拆开游戏读取、网络/人设/记忆准备和主线程完成，避免把整段含网络的 builder 搬主线程。
3. 保持 Scene 主体的接力、旁听、后处理、记忆/AFEF 和 TTS 回归；新接缝必须有原功能对照。
4. 在稳定请求与事实回执上再扩充公共结果/生命周期通知、内部贡献协议、经过批准的制作组能力转接或子 MOD 扩展。
5. 真实 1.3/1.4 Host、旧存档、新外部 DLL 加载/升级验收完成后，再单独确认默认迁移及有证据的旧路径删除。

不要把旧 MOOD fallback 差异、同步 Action 网络不能真正取消、Courier 前置线程缺口写成“本轮已修”。

## 6. 构建 / 回滚 / 协作边界

使用既有 `一键编译覆盖推送/build_single_module.ps1 -Stage`，一套源码构建 1.3、1.4、Bootstrap；不改脚本、不拆 Contracts DLL、不改程序集/存档身份。准确本机构建参数保存在验证日志及实施台账中。

当前每小时自动化 af-7-8 已由用户明确授权启用；只本地推进、验证和提交，不推送、不部署、不安装 SDK、不操作存档。原两份 2026-09-06 用户草稿改动保留，不能 stage 进本轮提交。

回滚采用本轮实现提交的定向 `git revert <commit>` 并保留用户改动，不 hard reset，不 force-push。`6e0de826` 是框架初版检查点；最新两批检查点见顶部，需回滚时定向反转对应实现提交并保留用户改动。

不要推原共享 `refactor/prepare-af-restructure` 或恢复其已改写历史；远端交付要使用经用户确认的专门重构分支。最新 fetch 时同名远端为 `a58c2191`，本地已有源码/测试/文档领先；新修改尚未推送。
