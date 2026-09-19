# Courier 双向最终 Prompt 组装线程边界（2026-09-16）

> J06 差分补强（2026-09-19）：`run.py` 现在还从 `77a3d234` 和当前 `CourierDeliveryBehavior.cs` 分别提取回信/主动来信最终消息构建器及其历史消息转换 helpers，先断言双方源码相同，再实际编译运行。76 场景比较完整 request JSON，并检查 system、上下文/历史 user、当前信件 user 的顺序；正常 550 checks PASS，Lore／实体／规则三项仅新侧文本丢失变异拒收。Hero/规则/共享上下文输入仍为 fixture，故这不满足 J06 的三类生产检索接入及 Native 全文门槛；J06 保持 `VERIFY / NOT_ACCEPTED`。

> 根整合状态（2026-09-16）：生产源码 `6e419f6d` 已本地提交，最终六Stage与4DLL1056元数据通过；整体收尾仍ACTIVE。当前边界以[总交接](../../docs/handoffs/2026-09-16-parallel-closeout-handoff.md)为准，以下包内记录保留原验证上下文。


## 范围与结论

本包封闭的是 **Courier 回信/主动来信在规则处理之后的最终 request/messages 组装**，不是整个 Prompt 线程重构完成，也不是三渠道 SDK 已交付。

- 旧真实路径：`Task.Run` → `PrepareAndGenerate*OffMainThreadAsync` → 名为 `Build*RequestOnMainThread` 的同步 builder。名字没有让实际调用回到游戏线程；角色、未压缩记忆消息、公开身份、亲属关系、位置、日期和近期事实仍在 worker 读取。
- 新真实路径：原 owner phase 捕获本次信件文字/意图/备用信/角色句柄/文化 → 后台调用原两个规则/上下文步骤 → 原 owner phase 检查 owner/generation/session/participant/存活/方向/文字源 → 原完整最终 builder。
- 在同一个 session 对象中编辑信件、意图或备用信，也会拒绝旧路由结果，不仅比较引用或 generation。
- 主体 Gateway、回信延迟提交、来信无动作后处理、状态机、默认开关、保存类型、公开 API 均不改变。未新增队列或第二条 LLM 管线。

## 已核实代码位置

基线：`154f7206`（生产背景 `51844800`）。本包修改尚待根代理联合验证后提交；当前差异绑定 `source-review.json`，不虚构提交号。

| 文件 / 行号 | 符号 | 责任 |
| --- | --- | --- |
| `CourierDeliveryBehavior.cs:4284-4300` | `两个 Start / Begin / Prepare 传递 run` | 同一 session 每次真实 Start 建立新的 runtime-only reservation；旧排队开始与旧错误回调不得改新run。 |
| `CourierDeliveryBehavior.cs:4378-4505` | `PrepareAndGenerateCourierReplyOffMainThreadAsync` | 回信真实worker沿原准备与后续完整生成路径接线。 |
| `CourierDeliveryBehavior.cs:4829-4877` | `PrepareAndGenerateInboundLetterOffMainThreadAsync` | 来信真实worker沿原准备与后续完整生成路径接线。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:26-34` | `BeginCourierPromptRun / IsCourierPromptRunCurrent` | 弱表按实际Start保留当前run身份；无保存字段，无新队列。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:46-67` | `CompleteCourierPromptSourceChanged` | 仅当前run+同live session/participant源失效时调用原失败owner；回信先封住旧tags，来信用当前fallback。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:72-111` | `CourierPromptInput` | 冻结请求级路由值；Hero/Character句柄仍由未迁移共享规则builder消费，不冒称纯快照。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:121-127` | `CaptureCourierPromptInput / IsCourierPromptInputCurrent` | 主线程捕获并验证源文字；区分源失效与owner/目标退休。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:139-154` | `BuildCourierPreparedPrompt` | 原两个同步规则/lore步骤保持顺序，正常async调用留后台。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:156-191` | `PrepareCourierPromptRequestAsync` | 原owner phase capture/accept，过期源有明确generation终结，旧run只丢弃。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:195-199` | `两个同步兼容 builder` | 原public envelope capture仍有真实消费者；保留其main-thread同步约定。 |
| `CourierDeliveryBehavior.PromptPreparation.cs:207-243` | `两套 Build*RequestFromPreparedPrompt` | 原最终业务组装整体迁移，固定main业务尾部和完整Message builders保持。 |

路径均相对仓库根。联合代码再次变动后需重新核实行号；符号及 source-review 固定差异是权威追踪点。

## 对照与测试

```powershell
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/source_review.py
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py --old-worker
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py --mutate worker_assembly
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py --mutate main_preprocess
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py --mutate skip_accept
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py --mutate wrong_direction
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run.py --mutate skip_source
```

- 当前 **58 场景 / 252 断言 PASS**：两方向、正常/空上下文/空话题/命令任务/空历史、三种种子来源、同步兼容、调用者原在主线程、提前/路由期间 owner/generation/session/角色/方向/终态/死亡变化、文字被编辑、规则/组装错误和原owner超时。
- 实际旧 request 方法在 worker 运行时触发 `fixture game read on worker`（旧红，非编译失败）。
- 5 个生产故障注入均返回行为失败：把组装搬后台、把规则网络搬主线程、漏最终验证、错方向输入、漏文字源验证。
- 固定 main `437925b856fae76b4e9ee207e96ba048f35d5a67` 中两个最终 request 方法从 `selectedRuleHits` 开始的完整业务尾部，以及两套完整 `Build*Messages` 声明，与新路径逐字相同。
- `source_review.py` 精确逆变换完整 Courier 文件的 20 个已批准 hunk（含 Start/Begin/Prepare 接线与失败回调）；保留 live 文件/依赖 hash 守卫，拒绝其他未审查差异。根代理应在旧 Courier source-parity 链之前调用其 `restore(source)`，不能刷新旧证明 hash 消除失败。
- 测试编译真实新 partial、原生产 owner-phase、实际旧 request bodies；游戏/规则网络/底层消息生产者是有物理线程断言的 fixture。不是 LIVE、不是实际网络、不是当前候选双版本 Stage 的替代。
- 日志位于本目录 `.generated/<variant>/run.log`；生成目录不提交。

## 清理、频率与尚未完成

- Courier 大类删除两段旧混合 builder 实现（初版主文件净减 68 行；本次追加必要的 Start/Begin/Prepare run 传递后需按最终diff统计）；同步兼容入口留在新 partial，因两个 public capture 仍调用它们，不能删除。
- 每封信增加两次原owner队列阶段及一个后台计算任务；无Tick轮询或新全局缓存。最终历史消息/世界地图任务组装的成本仍随实际记录数变化，本包**没有**证明单回调硬帧预算。
- 尚未完成的共享边界：`MyBehavior.BuildShoutPromptContextForExternalInternal` 混合 live 资格/粘连话题状态/规则网络/lore/世界实体；`RunCourierRulePreprocessForExternal` 自身也混合此责任。本包没有把这部分 Hero/Character 读取变成纯快照。
- `AIConfigHandler` 现有 guardrail context 是 `AsyncLocal`，不是 `ThreadStatic`；后续拆分必须显式传播其调用级上下文，不能只换线程原语。
- Native 的实际 `RunNativeConversationBackgroundPreprocessAsync` 消费者，以及 Scene 单人/多候选接力/旁听/被动反应的消息准备，仍需独立完成；不允许把 Scene 简化成一个单目标 façade 来冒充等价。
- 共享 MyBuild 内库存/资源/实体等资产上下文 **仍未** 完整主线程化；本包只迁移最终消息阶段中确实执行的角色/事实/未压缩记忆等读取。
- 后续建议：先把 shared builder 拆为 runtime eligibility snapshot + 后台规则/lore selection + owner revalidation 后的 runtime/粘连状态/最终上下文；再逐一迁移全部实际 Native/Scene/Courier 调用点，保留多人轮次及唯一后处理/记忆提交。

## 收口补充：同源失效不能卡住信使（独立复审发现）

初版在 source 变化时返回 null，但真实 Start 已设 `ReplyGenerationStarted=true`，两个真实 caller 直接结束，tick 不会再启动。因此初版“旧结果丢弃”断言不等于信使流程可继续。本次已修复：

1. 每次真实 Start 在同 owner 的弱表中建立 reservation，沿原 Begin → Prepare 传递；无新的存档字段。
2. 同一活跃 run/source 失效时使用原 Failure owner 完成 generation、调用原 ProcessSession 推进与释放暂停。不是把 Started 裸改为 false 自动重试。
3. 已被新 Start 取代（即使新run还在人设等待）或 owner/session/participant/generation 失效的旧run只丢弃；旧准备异常回调也检查reservation。
4. 回信先清理 `ReplyText` / `ReplyPostprocessedText` 并设置 `PostprocessConsumed=true`，避免原失败tick执行遗留preflight动作标签；不伪称撤回已经发生的动作。来信使用当前备用信，空备用信仍走原Fail的当前LetterText回退。

`run_liveness.py`：**16 场景 / 59 断言 PASS**。编译真实 Start/Begin/两个完整Prepare caller/原Failure，抽取原回信tick等待分支、完整入站tick方法、入站等待/解除暂停前缀与原commit最前端消费guard；在业务游戏/网络seam使用fixture，不宣称完成完整运输/动作实机测试。覆盖新Start在人设等待时旧结果/旧异常、源变化/空fallback、替换/退休及预置旧reply/tags不能执行。

```powershell
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run_liveness.py
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run_liveness.py --old
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run_liveness.py --mutate drop-failure
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run_liveness.py --mutate ignore-run
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run_liveness.py --mutate old-fallback
G:\Python310\python.exe -X utf8 -B tools/CourierPromptPreparationTests/run_liveness.py --mutate keep-stale-tags
```

旧红是**本包未修的中间候选**，不是固定 main 的缺陷声明。`liveness_review.py` + `liveness-review.json` 从当前源精确反变换重建旧方法/partial并验证前后hash，fresh clone不依赖 `.tmp` 或既有 `.generated`，没有提交整份旧Courier源码。4个liveness故障注入均为实际行为失败，不以编译错误计数。

正常旧retry UI只在主正文Generate返回API错误之后开放，其prepare已结束且Started未释放，原两个Start门禁不允许再开prepare。本 reservation 仅封闭实际Start/准备范围，**不是**任意外部强制reset后遗留retry按钮、最终动作提交或完整Courier生命周期的总授权。旧retry按钮按sessionId直接使用旧request的责任仍需后续独立迁移。

没有 commit / push / 部署游戏 / 存档操作 / 默认切换 / 自动化改动。
