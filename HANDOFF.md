# AF 总 HANDOFF — 当前入口（2026-09-11）

## 最新续作：Native 准入（生产/测试 77d4a940）

- 普通输入/主动开场统一主线程捕获、后端 busy 与会话票据；结束会话也拒绝尚未开始的旧排队请求。
- 动作实际执行前复查作用域；旧请求/旧 Overlay 完成不能清新请求 busy 或结束新展示。
- 44 个定向检查、7 个行为变异、最终六项构建和相关回归通过；公共 V1 仍只读，未部署、未推送。
- 最新短版：`docs/handoffs/2026-09-11-native-admission-handoff.md`；验证：`docs/audits/2026-09-11-native-admission-verification.md`；续作台账：`docs/phase8/native-admission-progress-20260911.md`。
- 下一轮：继续 Native prepare/动作后事实回执/流式回调的完整线程与会话归属，不能把本轮准入当成整个 Native 公共服务完成。Courier 双向 prepare 仍待处理。


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

给制作组直接看的短版：`docs/handoffs/2026-09-11-framework-v1-team-handoff.md`。

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

1. Native：主线程准入、排队 epoch、共享后端 busy 和旧 UI finally 隔离已完成；继续完整 prepare/commit/stream 展示作用域与结果回执，之后才开放有限公共普通文本提交。
2. Courier 双向更早的 prepare：拆开游戏读取、网络/人设/记忆准备和主线程完成，避免把整段含网络的 builder 搬主线程。
3. 保持 Scene 主体的接力、旁听、后处理、记忆/AFEF 和 TTS 回归；新接缝必须有原功能对照。
4. 在稳定请求与事实回执上再扩充公共结果/生命周期通知、内部贡献协议、经过批准的制作组能力转接或子 MOD 扩展。
5. 真实 1.3/1.4 Host、旧存档、新外部 DLL 加载/升级验收完成后，再单独确认默认迁移及有证据的旧路径删除。

不要把旧 MOOD fallback 差异、同步 Action 网络不能真正取消、Courier 前置线程缺口写成“本轮已修”。

## 6. 构建 / 回滚 / 协作边界

使用既有 `一键编译覆盖推送/build_single_module.ps1 -Stage`，一套源码构建 1.3、1.4、Bootstrap；不改脚本、不拆 Contracts DLL、不改程序集/存档身份。准确本机构建参数保存在验证日志及实施台账中。

当前每小时自动化 af-7-8 已由用户明确授权启用；只本地推进、验证和提交，不推送、不部署、不安装 SDK、不操作存档。原两份 2026-09-06 用户草稿改动保留，不能 stage 进本轮提交。

回滚采用本轮实现提交的定向 `git revert <commit>` 并保留用户改动，不 hard reset，不 force-push。`6e0de826` 是开始写生产之前的明确检查点。

不要推原共享 `refactor/prepare-af-restructure` 或恢复其已改写历史；远端交付要使用经用户确认的专门重构分支。最新 fetch 时同名远端为 `a58c2191`，本地文档领先；新修改尚未推送。
