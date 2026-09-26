# AF HANDOFF — 信使双向历史准备边界（2026-09-15）

## 1. 当前结论

- 生产/测试提交：`af754ab603dfcb7d2141862ce8d8506b21d89f21`；checkpoint：`de298e17`；此前生产：`73774a94`，此前文档：`f77fe5e4`。
- 工作目录：`G:/AFMOD/AF-REFACTOR`；本地分支：`codex/af-framework-skill-delivery-20260911`。
- 总体仍为**阶段 8 主体收尾 / B1 未整批验收**。本轮完成 Q1 的“双向历史捕获/检索”子责任离线验证，不是整个信使线程准备或 SDK 完成。
- main 主体对照固定 `437925b856fae76b4e9ee207e96ba048f35d5a67`。本轮整文件精确接线对照另使用 `73774a94`，不会把它冒充 main。
- 当前仅本地提交，未推送/部署/改存档/改默认入口，未开启自动化。上次实际核对的远端为 `f03557fbdfeabb921988fd177e6beb405cc0e614`；本轮没有重新查询远端，不能据此宣称 GitHub 仍然最新。以后获授权推送目标仅 `origin/codex/af-main-refactor-continuation-20260831`，先 fetch/核对分叉，不能强推。

## 2. 为什么改、改了什么

原 reply/inbound 的 Prepare 位于 Task.Run，历史直接读取 live Hero/历史状态。剩余 builder 还含同步 preprocess/lore 网络和人设，整段挪主线程会制造卡顿，因此本轮没有用这种方式伪装修复。

现在正常双向链为：

```text
原 persona 准备（尚未完成线程整改）
→ 原 Courier owner phase：主线程核对会话，捕获交付事实与既有 Memory 快照
→ 后台：复用快照的旧检索/选择/渲染，不另造历史算法
→ 原 owner phase：核对 Courier owner、generation、同一 Session、participant、方向/终态
→ 原 request builder 消费 prepared.ExtraFact/Text（其余 live 准备仍待迁）
→ 原 LLM / 原后处理 / 原运输到达与事实提交
```

- 删除两个 builder 中直接 live 历史读取及旧计时块。每次正常历史准备一次 capture/resolve，无新队列、公开类型、默认开关或重复记忆写入。
- 已准备结果非空但 Text 为空，含“历史可选读取失败”，都按空上下文继续，**不触发第二次读取**。失效/替换的会话不进入下游 builder；旧 owner/档代取消返回无结果，同 owner 的原超时仍进入现有失败处理。
- 出站 currentInput 为原信正文；入站为 null；交付事实 delivered 方向不变，不把 NPC 意图记录成玩家发言。当前 renderer 的 maxLines 参数未使用，这一点由源码守卫检查；以后实现该参数时必须重新审查本对照。
- `CourierHistoryWork` 本身不存 Hero/Session，但复用的 Func 闭包仍保留 memory owner 身份以验证失效，不能宣称整个闭包无 owner 引用。
- 两个旧同步公开 Capture API 有实际兼容责任，签名、参数名/可选 null 默认值、返回类型保持；它们仍要求主线程且可能同步阻塞。它们只使用私有 legacy capture helper，正常异步流程不回退此入口。

## 3. 核实的代码位置

以下均按 `af754ab603dfcb7d2141862ce8d8506b21d89f21` 核对，范围为一基行号；100 点代码地图仅导航，不表示整文件 DONE。

| 位置 | 符号 | 责任 / 未覆盖 |
|---|---|---|
| `CourierDeliveryBehavior.HistoryPreparation.cs:12-21` | `private sealed class CourierPreparedHistory` | 已准备结果与显式空文本 |
| `CourierDeliveryBehavior.HistoryPreparation.cs:36-46` | `private static CourierPreparedHistory CaptureCourierHistoryForLegacyEnvelope` | 仅两个旧同步公开Capture API调用；保留兼容 |
| `CourierDeliveryBehavior.HistoryPreparation.cs:48-57` | `private bool IsCourierHistoryOwnerCurrent` | capture/accept共同的主线程Session/participant验证 |
| `CourierDeliveryBehavior.HistoryPreparation.cs:59-116` | `private async Task<CourierPreparedHistory> PrepareCourierHistoryAsync` | 双向历史捕获、后台检索与晚结果接受 |
| `CourierDeliveryBehavior.cs:783-804` | `public static InteractionEnvelope CaptureCourierReplyRefactorEnvelopeForExternal` | 原同步出站Capture ABI；参数默认值不变 |
| `CourierDeliveryBehavior.cs:832-869` | `public static InteractionEnvelope CaptureCourierInboundRefactorEnvelopeForExternal` | 原同步入站Capture ABI；参数默认值不变 |
| `CourierDeliveryBehavior.cs:4672-4706` | `private CourierReplyGenerationRequest BuildCourierReplyGenerationRequestOnMainThread` | 回信builder消费prepared；其余live准备未迁 |
| `CourierDeliveryBehavior.cs:5094-5128` | `private InboundLetterGenerationRequest BuildInboundLetterGenerationRequestOnMainThread` | 来信builder消费prepared；其余live准备未迁 |
| `MyBehavior.HistoryPromptSnapshot.cs:27-33` | `internal static Func<string> CaptureHistoryContextWorkForHero` | 复用的既有memory快照入口，本轮未改 |
| `CourierDeliveryBehavior.DetachedPostprocess.cs:117-155` | `private async Task<T> RunCourierOwnerPhaseAsync<T>` | 复用既有主线程owner/generation/超时调度，本轮未改 |
| `CourierDeliveryBehavior.cs:4563-4565` | `PrepareCourierHistoryAsync` 调用 | NPC回信：persona后捕获，失效则不进入request builder |
| `CourierDeliveryBehavior.cs:5066-5068` | `PrepareCourierHistoryAsync` 调用 | 主动来信：persona后捕获，失效则不进入request builder |

- 新测试：`tools/CourierHistoryPreparationTests/README.md`。主体 helper 的真实代码与原 owner-phase 源码编译执行；Hero/session/history provider 使用替身。
- `tools/TeamModulePortParityTests/run.py` 接入精确逆变换，保留原完整 owner 文件比较；未通过刷新旧文件哈希掩盖额外变更。
- `tools/ModuleFrameworkApiTests/ArtifactMetadata.cs` 额外检查两项公开同步 Capture ABI 与新类型私有性。无新 Api.V1 能力。

## 4. 验证结果及证据层级

| 检查 | 结果 | 能证明 / 不能证明 |
|---|---|---|
| 实际新 helper + 原 owner-phase | 122 断言，双向 30 场景 + 2 同步兼容用例 | 主/后台物理线程、空值/异常、替换/终态/档代/超时；引擎为替身 |
| 有效行为故障注入 | 4/4 被拒绝 | worker capture、main resolve、跳过 accept、错 currentInput；均成功编译后行为失败 |
| 精确整文件逆变换 | 4 守卫通过 | 六个获审查接线变化以外的业务差异、依赖漂移被拒绝 |
| 既有历史与 Native | 852 / 27 通过 | 原 snapshot/渲染与 Native 消费回归；不代表大存档帧耗时 |
| 渠道切换 / Courier 后处理 | 132 / 39 通过 | 既有提交/退回边界保持，不代表实机运输验收 |
| 制作组 typed ports | 308 断言、13 方法/31 调用点、3 行为故障通过 | AF 侧接缝，不验证政策/宴会/GCCZ 玩法 |
| V1 与内部快照 | 119 API/256 并发、32 快照/128 并发、3 故障，CoreOnly 与 CS0122 隔离通过 | 仍为只读 API，不代表提交 SDK |
| 实际 DLL | Courier Host replay PASS；4 个实现 DLL 共 648 元数据断言 | 实际程序集宿主合同/ABI；不等于游戏加载与真实子 MOD |
| 双版本构建 | Debug/Release × Bootstrap/1.3/1.4 共 6 项 Stage 通过 | 项目内组装，未改游戏目录 |
| main 存档身份 | SyncData 146、CampaignBehavior 36，无增删 | 身份不变，不等于旧档加载成功 |

证据索引：[候选验证 JSON](../audits/2026-09-15-courier-history-capture-verification.json)，含源码/6 个 DLL 与冻结日志 SHA256。原始日志仅本地 `.tmp/courier-history-20260915/final-evidence/`；外部成员可按测试 README/原 Stage 脚本重跑。

初次构建暴露两个同步消费者漏传参数（CS7036），已补兼容路径；`stage-debug.log` 是失败历史，不是最终候选证据。只认 `stage-debug-final.log` / `stage-release.log`。构建时 HEAD 为 checkpoint，工作树 C# 与本生产提交相同；后续只改文档，不用旧 DLL 冒充新源码。

## 5. 工程师审查与玩家视角

- 确认正常两向 builder 都传非空 prepared Text，空历史不会重新发起检索；未改变主链路正文、规则资格、ActionPlan 或到达时机。
- 玩家场景推演：没有历史仍应能收到信；检索失败不让整封信消失；生成期间换档/替换目标/结束会话应丢弃旧工作，不向新目标误交。以上有对应局部离线测试，**不是实际游戏通关验收**。
- 保留旧同步入口是 ABI/调用责任，不是新建备用 LLM 链。两个旧 private 消息 formatter 的 null-history fallback 本轮未改；现有两个 builder 均显式传非空值，正常 prepared 流程不走 fallback。后续收口消息职责时需一起去掉无消费者的可选回退，不据此宣称旧代码已删干净。
- 快照复制仍随历史量增长，尚无主线程耗时上界；虽然网络检索已在后台，不能标记 B1/性能完成。

## 6. 后续继续顺序

1. **Q1 剩余 prepare**：定位 persona、规则资格、lore 和消息上下文真实 live 读取；拆成主线程捕获 → 后台网络/计算 → 主线程接受。不能把整个同步 preprocess/lore 搬主线程。清理私有 builder 的误导性 OnMainThread 命名与无消费者 fallback 时保留整文件对照。
2. **生命周期与 B1**：补完整 Campaign/Mission/Memory owner 失效；首次深复制、writer、尾步提交和单 job 实际预算，不能只限每帧回调数。
3. **稳定双层接口**：内部双向主体服务接缝；外部 Native、Scene、Courier 全部提交/结果/取消契约，复用现有完整 owner 与唯一动作/记忆提交。Api.V1 当前只读尚未达标，不把 NotSupported 改成假成功。
4. **全主体复现与清理**：按 [main 验收矩阵](../phase8/af-core-main-closeout-matrix-20260915.md) 逐职责比对、迁移、删除/保留说明；制作组业务不重写。真实游戏/旧档/live Economy/AFEF 与独立子 MOD 加载验证后，才可标整体收尾。

## 7. 接续与回滚边界

生产回滚点为 `73774a94`；使用针对 `af754ab6` 的审查后逆向提交，不 hard reset，不把其他本地成果一起回退。三份保护文件哈希保持；两份用户草稿仍未提交，本地专用 Native 简明 HANDOFF 不上传。新的制作组简明版在 `.tmp/courier-history-20260915/team-handoff.md`，也不入库。

下一位先读本文件 → 根 HANDOFF 当前段 → 总台账和 main 矩阵 → 双 Skill → 当前 Git/代码。以实际源码和新指示为准，不把历史 PASS 当作新候选实机验收，不自动恢复自动化或推送/覆盖游戏。
