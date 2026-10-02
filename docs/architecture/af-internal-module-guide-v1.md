# 制作组内部模块接入说明 V1

## 使用者和位置

政策、宴会、GCCZ 等编进 `AnimusForge.dll` 的制作组代码使用 `AnimusForge.Refactor.Modules` 的 internal 契约。它不是独立子 MOD SDK，也不是从磁盘扫描插件的容器。

源码入口：

- `src/AF.Contracts/Internal/TeamModules/{IPolicy,IGathering,ISiege}ModulePort.cs`：三组专用 internal 契约。
- `src/bridges/{Policy,Gathering,Siege}/*ModuleAdapter.cs`：分别转接原业务 owner 的无状态薄实现。
- `src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs`：类型确定的无状态单例接线。
- `src/AF.Foundation.Runtime/ModuleDirectory/InternalModuleDirectory.cs`：登记、冻结、依赖/版本校验和状态查询。
- `src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs`：目录状态、加载/停止和冻结快照的唯一 owner。
- `src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs`：选择制作组目录工厂、转接 Campaign 注册与保留旧查询入口，不再持有目录状态。

J11 已按 [`docs/plans/j11-team-module-seams-plan.md`](../plans/j11-team-module-seams-plan.md) 完成必要离线验收；当前 owner/consumer/gate 见 [`af-team-module-seam-matrix.md`](af-team-module-seam-matrix.md)。

## 当前登记范围

| 模块 ID | 接缝能力 ID | 契约版本 | 目录门禁 |
|---|---|---|---|
| af.team.policy | af.team.policy.dialogue | 1 | 不新增门禁；仍由既有政策资格规则执行 |
| af.team.gathering | af.team.gathering.dialogue | 1 | 不新增门禁；仍由既有宴会资格规则执行 |
| af.team.siege | af.team.siege.dialogue | 1 | conversation-siege，原薄桥也保留同一实际门禁 |

`dialogue` 在这里指本次列明的方法组，不代表模块所有功能都已迁入新框架。没有为了凑齐表格给所有旧领域伪造 Ready 条目，也不把历史设计 fixture 当运行 manifest。

## 薄桥契约

1. 保留每个调用的目标、渠道、playerText、replyIsDirectPlayerResponse、AgentIndex 及 ref/out。
2. 后处理规则由原模块构建；正文不新塞动作标签；normalize 不变成 execute。
3. apply 返回 facts/notifications 后，仍由原调用方写历史/AFEF、展示通知。本层不二次提交。
4. 业务方法原有异常语义保持；不能 catch 后返回空字符串伪造正常。
5. 静态单例只保存无状态 adapter；不保存 Hero、Mission、会话、存档或请求结果。
6. 原有 NpcRulerPolicy 活动政策上下文兼容方法目前返回空正文；薄桥原样保留，不把包了一层说成恢复了新功能。

## 登记生命周期

```text
new Directory → TryRegister(definition) → CompleteRegistration()
              → owner 确认 adapter 已装配 → UpdateRuntimeState(Ready)
              → GetCapabilityStatus / GetSnapshot
```

- 登记并不自动 Ready。版本为精确匹配的正整数，不是未经验证的 SemVer。
- 空 ID/非法版本/重复模块或能力等拒绝；不覆盖既有 provider。调用方必须检查拒绝结果。
- 冻结时校验缺失依赖、版本和环；受影响模块及依赖者不可用，无关模块不被一概判死。
- 冻结后拒绝新增登记；首版不支持运行时替换 provider/热卸载/子 MOD 注册。
- 查询结果中的门禁状态不是实际请求授权；执行资格继续在原 owner 检查。

## 新增制作组接缝时的维护步骤

1. 先确定业务 owner，保持业务源码原位；写一个有实际调用者的 typed 方法，不加入未使用占位接口。
2. 给 adapter 做参数一一转发；在对应真实主体入口替换直接调用。跨渠道的同类接缝一起检查。
3. 在装配根绑定实例和唯一 ID；只有实际接线的方法组可以登记为已装配。
4. 选用确实支配该能力的既有 FeatureBridge；没有合适的不要擅自套更宽的功能开关。
5. 补参数/结果/ref/out 和原行为对照；不要只测“能找到模块”。
6. 更新能力矩阵和 HANDOFF。新增 public 能力是另一项工作，internal 方法不能自动外露。

## 性能与兼容

登记/依赖校验在冷启动；实际主体调用通过 typed 单例直达原 adapter，不逐次查字典。公共查询按需构造少量只读快照，不新增每帧扫描或网络请求。框架目录不持久化，不改变任何 SaveableTypeDefiner/SyncData 键。


## 制作组 → AF 的三渠道服务

同 DLL 制作组代码可通过 `CoreDialogueServices.CreateClient()` 取得 **internal** `CoreDialogueClient`，调用 `SubmitNative(requestId, playerText)`、`SubmitScene(contextTicket, requestId, playerText)` 或 `SubmitCourier(contextTicket, requestId, playerText)`；operation 提供 `Completion` / `Snapshot` / `Cancel()`。这不是独立 DLL 可引用的 public API。

- Native 复用 `ShoutBehavior.SubmitModuleNativeDialogue` 和原主线程准入；仅提交当前对话玩家文本，不接受指定 Hero、任意 handler、Prompt 或动作委托。
- Scene 先在游戏主线程、有效场景喊话框选期间调用 `CaptureSceneContextTicket()`；Courier 先在原 UI 完成收件人、模式和附件草稿，再在游戏主线程调用 `CaptureCourierContextTicket()`。两者返回 `null` 表示当前无法签发；票据不透明，提交可从任意线程调用，由原 owner 一次 claim 和重验，不绕过资格/效果/历史/AFEF。
- 每个 client 最多保留 128 个不同请求 ID，完全相同 ID、渠道、票据和文本重试返回同一 operation；冲突/容量拒绝，不淘汰终态 ID。`Cancel()` 只保证主线程开始前取消；开始后的部分/未知效果不能当作回滚，也不能自动换 ID 重发。`Dispose()` 撤销本 client 待领票据和未开始任务，已开始结果仍可读取。
- 最小调用：`var client = CoreDialogueServices.CreateClient(); var ticket = client.CaptureSceneContextTicket(); if (ticket != null) { var operation = client.SubmitScene(ticket, "scene-turn-1", "请说说你的看法"); var result = await operation.Completion; } client.Dispose();`；Courier 对应改用 `CaptureCourierContextTicket()`/`SubmitCourier(...)`。更新自有 UI 时由调用方回游戏主线程；不要从后台持有或操作 Hero/Agent/Mission。
- 公共 `Api.V1` 投影同一内部服务，但内外枚举/DTO 分开；政策、宴会、GCCZ 的主体→模块 13 方法 typed ports 仍另行保留。三渠道服务可用不等于全部模块已改为双向调用，更不等于实机通过。

## 四扩展的宿主目录接入（2026-10-02）

这是已有四扩展的真实生命周期元数据，不新增玩法 port 或动态注册。三组制作组接缝与四扩展共七个目录模块；内战端口仍未在本目录登记，本包不改变该范围。

| 模块 ID | 能力 ID / V1 | Ready/Available 的确切边界 |
|---|---|---|
| af.extension.illustrator | af.extension.illustrator.host / 1 | 原初始化入口完成；不代表生图开关/provider或所有补丁可用 |
| af.extension.dialogue_ui | af.extension.dialogue_ui.host / 1 | 资源初始化后仍等待延迟Presentation安装；主呈现安装成功才就绪，可选轮盘回退保留 |
| af.extension.coup | af.extension.coup.host / 1 | 原Start完成且Mission保护、入城、叛乱三个既有兼容探针可用；不代表当前可发动政变 |
| af.extension.vengeance | af.extension.vengeance.host / 1 | 真实embedded认领/宿主挂接成功；原战役准备/注册失败可见，不代表当前可处决 |

四项初始为 NotInitialized；启动/安装返回失败或抛出时 unavailable/failed（V1合并为Unavailable）。注册失败只影响对应宿主；不能由注册成功覆盖启动失败。停止/重载按原目录生命周期处理，卸载期间状态报告不能复活stopped目录。状态只在上述同步生命周期事件更新，不是实时健康检测或MCM状态镜像；实际 gameplay 请求仍由原owner重验。

独立消费者通过 `AfApi.GetSnapshot().Modules` 读取这些条目，`IsExternallyCallable` 始终 false；不能把 `.host` 当 `AfApi.GetCapability(...)` 的公开调用能力。公共ActionExecute/MemoryWrite/ExtensionRegister仍NotSupported。宿主Start/RegisterCampaign/Tick/Shutdown执行路径保持，Tick没有新查询/扫描/锁/反射。

[本包证据/源码/未验/回滚](../animusforge-refactoring-and-repository-reorganization-plan.md#four-hosted-extension-catalog-20261002)。
