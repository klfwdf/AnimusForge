# AF 重构全面检测报告（35524b04）

## 结论

**当前版本不能按“功能完全复现、可发布、可以继续广泛删旧”验收。** 本轮确认 4 个 P1 功能问题、2 个 P2 功能问题，另有 Bridge/目录不一致、测试契约滞后与未闭合的时序风险。六项构建通过不等于这些问题不存在。

用户要求“关闭自动化，全面检测”。`af-7-8` 已通过应用工具设为 **PAUSED**，旧 `af` 也确认 PAUSED；不恢复自动化。本轮只检测、生成证据和报告，**未修生产代码、未改正式测试断言、未推送、未部署、未读写真实存档或调用真实 LLM/TTS 服务**。

## 检测范围和基线

- 工作区：`G:\AFMOD\AF-REFACTOR`；分支 `codex/af-main-refactor-continuation-20260831`。
- 源码基线：`35524b043c5daabca6e51a02caf084131e87e16e`。fetch 后相对共享远端 ahead 11 / behind 0；没有合并或切换源码。
- 全范围盘点现有工具、20 领域目录、默认入口和验收状态；对 Scene/BattleSpeech、Courier/Native、Gateway、Memory/Economy、TTS 等高风险调用链深入审查。其他领域为指定入口抽查，**不是所有源码逐行审计或真实游戏全功能穷举**。
- 发现的历史集成问题不全部归咎于最近一次 Scene 修复；本报告记录的是当前仍存在的问题。
- 两份 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-06-integrated-phase8-handoff.md`、`G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-06-team-brief.md` 原有占位草稿保持未提交，不覆盖。

## 已确认功能问题

### F1 · P1：指定物品 ALL 会转移 Hero 的其他库存

位置：`G:\AFMOD\AF-REFACTOR\RewardSystemBehavior.EconomyReplay.cs:233-267`。

`TryReplayGiveAsset` 对 `quantity=ALL` 遍历全部可用物品，没有按 `assetToken` 筛选。原样提取方法执行：库存 `grain=5, iron=7`，请求 `grain:ALL`，实际输出转移 `grain=5, iron=7`，共 12 件；应只转移 5 件 grain。

旧实现 `G:\AFMOD\AF-REFACTOR\RewardSystemBehavior.cs:20131-20183` 与 Party owner `G:\AFMOD\AF-REFACTOR\RewardSystemBehavior.EconomyPartyReplay.cs:284-300` 都限定指定资产。当前 Courier detached Hero executor 可进入此 owner；公开 opt-in executor 也受影响，不能泛称所有默认渠道都走它。

**验证边界：**真实方法＋库存/转移替身；未转移真实游戏物品。建议先修资产解析与数量语义，再用双物品、不同 modifier、有限数量、GOLD 与未知资产作正反例。

### F2 · P1：Courier 默认回信丢失人设、来信和完整上下文

位置：`G:\AFMOD\AF-REFACTOR\CourierDeliveryBehavior.cs:787-790`；同文件 inbound opt-in `835-838` 同形；根因在 `G:\AFMOD\AF-REFACTOR\Refactor\Adapters\LegacyPromptPackageAdapter.cs:24-27,52-68`。

实际 `CreateCourierChatMessage` 返回匿名 `{role,content}`，adapter 只认字典，静默跳过消息。当前生产 DLL 回放：两条匿名消息 **2→0**，字典对照 **2→2**。

进一步执行真实 capture→composer→transport-tail：`appendCurrentPlayerInput:false`，composer 仍为 0 条，`ShoutNetwork.EnsureFinalUserTurn` 仅补一句“请继续完成当前请求，只输出最终结果。”。所以不是 HTTP `messages=[]`，而是只剩通用续写句，原 `LETTER SENTINEL` 不存在。这个请求仍可正常收到非空回复，不会必然触发 fallback。

默认可达链：已送达会话→`PrepareAndGenerateCourierReplyOffMainThreadAsync`→Host capture。Native 当前仍是完整旧默认；同一个 adapter 对 Native parity 诊断的影响，不能写成 Native 默认也已全部丢 Prompt。

### F3 · P1：Courier detached 跳过权威领域归一化

位置：`G:\AFMOD\AF-REFACTOR\CourierDeliveryBehavior.cs:976-980,895-896`。

构建 WorkItem 后仅保存两段 Prompt，丢掉 `CompleteOnMainThread`；原始模型标签直接交给全量 tag parser，未恢复本轮规则资格、候选序号翻译、互斥与领域归一化。

生产 DLL 反例：本轮 `selectedRules=[courier_reply]`，没有 reward，输入 `[ACTION:GIVE_ASSET:GOLD:100]`，仍生成 1 个 ActionRequest 并投影为 1 个 Economy action。相同输入交给真实 `NormalizeRewardPostprocessTagsForScene(allowAssetTransfer:false)`，只剩 MOOD。

**验证边界：**解析/投影资格旁路已复现，非真实扣款。真正执行仍需模型返回该标签、有效会话/owner、主线程提交成功。F2 不会必然阻断此路径。其他领域的序号映射后果需要逐项补验，不能用这个反例声称所有领域均已出错。

### F4 · P1：旧玩家输入等待后可继续作用于新场景

位置：`G:\AFMOD\AF-REFACTOR\ShoutBehavior.cs:26959-26968`。

`ProcessShoutConfirmedInternal` 等待 gate 后没有核对请求原 generation/session。gate 自己的 waiter 守卫只保护其内部状态，不保护调用者。执行真实 Mission-end reset 完成旧 gate 后，旧调用继续 `BeginNewPlayerDrivenSceneConversationEpoch`，清新场景 speech，并可接受 index 相同的新 Agent。

离线反例执行真实 reset、Task/续体和入口连续代码：旧文本被新对象接受、新 epoch 递增、新 speech 被清除。只切 Mission/session 即可复现，不必换存档 generation。

建议将整个玩家提交绑定到首次 await 前的上下文，在任何 epoch/reset/目标/UI 副作用之前拒绝过期请求；不能只修后面的回复发布。

### F5 · P2：BattleSpeech 普通回退丢失原框选目标

位置：`G:\AFMOD\AF-REFACTOR\extensions\AnimusForge.XihaiAction\src\Runtime\AfCompatV130.cs:1390-1397`。

classifier 接管时 `ResumeAfShoutUi→ResumeGame→EndShoutProcessing` 清除 `_activeShoutTargetingContext`。NONE/ordinary/错误/超时回退只反射传文字、事实和 index，没有传冻结的框选范围。

真实反射和目标选择回放：原 20m 框选目标直接入口可接受，回退改为默认 4m 扫描后拒绝；另一近距离例中原 framed=1 变成 2，未框选旁听者被当成参与者。未运行真实 classifier 网络或 Harmony 安装。

建议使用携带冻结目标且验证 Mission/session/Agent 身份的专用桥；不要临时恢复一个全局可变上下文覆盖新输入窗口。

### F6 · P2：TTS 旧任务在取消后仍可干扰新轮

位置：`G:\AFMOD\AF-REFACTOR\TtsEngine.cs:392-426,567-575`；`G:\AFMOD\AF-REFACTOR\ShoutBehavior.cs:3455-3499,13246-13273`。

两组独立原方法回放：

1. worker 已 dequeue，StopPlayback 设置取消后，worker 又将 `_cancelCurrent=false`，继续旧 job。
2. 旧合成阻塞时 Stop，再建立新 Native wait token `222`；旧 HTTP 失败返回 null，没有取消校验就发送失败事件，按 AgentIndex 完成了新 token。

job/事件缺少请求级身份；当前成功分支的取消检查不能保护失败分支。网络/音频/游戏 owner 为替身，未实际播放或执行带路等游戏动作。后续应补 request identity 与取消后的所有终态发布检查。

## 门禁、测试与额外风险

- **P2 Bridge 模型矛盾仍在：**`G:\AFMOD\AF-REFACTOR\docs\phase8\bridge-binding-manifest.json:160-166` 声称 disabled 不装补丁，而 `G:\AFMOD\AF-REFACTOR\InteractionComponentSafePatch.cs:12` 已不依赖 optional gate。当前 validator FAIL 正确；FeatureBridges 仍暴露一个无效开关。应区分 mandatory safety 与 optional bridge，不恢复不安全行为或降低 validator 换绿灯。
- **P3 默认状态目录滞后：**`G:\AFMOD\AF-REFACTOR\docs\phase8\full-domain-readiness-catalog.json:258,270` 仍写对话完全 OPT_IN，实际是 Native 旧默认、Scene/Courier 部分新默认的混合状态。目录不能据此授权删旧。
- **方法级已复现、完整集成尚未复现：**两个 waiter 同时等一个 gate，后来的 waiter 覆盖 owner 且捕获 processing=false，最终无法恢复第一个借出的 true。普通 UI 有 busy 检查，BattleSpeech 反射旁路是否构成完整重入尚需实测。位置 `G:\AFMOD\AF-REFACTOR\ShoutBehavior.cs:26712-26717,26776-26783`。
- **线程风险：**Courier 在 Task.Run 中调用名为 OnMainThread 的 request builder，Host capture 又重复准备。实际后台游戏对象读取及重复计算已从调用链确认；未把真实崩溃/档案污染当作已发生。
- **错误结果风险：**Gateway 把非空 transport 错误提示当成功正文的路径仍需端到端故障回放，本轮未列为第四个 Courier P1。
- **清理未完成：**`G:\AFMOD\AF-REFACTOR\ShoutBehavior.cs:27162` 固定启用 per-Hero 分支后仍保留不可达旧群组路径。本轮不删除；先解决功能问题，再按实际调用/存档/外部接口职责清理。

## 全套检测结果

| 项目 | 结果 |
|---|---|
| 官方 Debug/Release × 1.3/1.4/Bootstrap | 六项 PASS，均 0 warning / 0 error；仅项目内 Stage |
| C# 测试项目盘点 | 41 个；37 个执行，最终 34 PASS / 3 FAIL；另 1 个仅部分安全子集通过，3 个 SDK 阻塞 |
| 三个首次失败的测试 | DuelDispatch / EconomyAwareActionPlanExecutor / ProductionValidationProvider 正常 restore 后重跑 PASS；原因是旧 assets 或旧 package 路径，不列为生产缺陷 |
| Python 检查与回放 | 27 个命令：24 PASS、2 FAIL（同一 Bridge 清单问题）、1 个缺失证据示例按预期 BLOCKED/exit 2 |
| Scene 既有回归 | Channel 132 + 提取14；方法差分71 + completion2；Queue37；Gate6及相应变异/旧版红测均通过，但未覆盖本次发现的玩家提交入口缺口 |
| PolicyEffect net472 | 编译 PASS；仅四个无外部存储风险的 flags：log164、target-plan764、jurisdiction11、normalizer117 断言通过；默认全套/ONNX未运行 |
| SDK 10 工具 | 3 个 PromptLab/PlayerExportsEditor smoke tests 未运行；实际 NETSDK1045。机器有 .NET 10 runtime，但没有 SDK 10，runtime 不能代替编译器 |
| 20 领域验收目录 | LIVE/SAVE 全 NOT_RUN，Release 全 BLOCKED；目录 17 域标 COMPLETE 不是实机完成证据 |
| 源码稳定性 | 检测前后清单 SHA-256 一致，生产/配置/正式测试无改动；原两份草稿保留 |

### 三个仍失败的 C# 测试如何解释

1. **GiveAssetTagCodec.StressTests**：`G:\AFMOD\AF-REFACTOR\tools\GiveAssetTagCodec.StressTests\Program.cs:162` 强制要求 `MyBehavior.cs` 含 `GiveAssetTagCodec.ReplaceTags`。历史 `fea254c3` 已删除旧 `TranslateRewardItemIndexes`，当前共享归一化位于 `G:\AFMOD\AF-REFACTOR\ShoutBehavior.cs:24491-24657`。这是测试对旧文件布局的绑定失效，不可据此恢复 dead helper；它也不能排除独立 F1/F3 缺陷。该断言之后的测试没有执行，不能宣称整套 PASS。
2. **PhaseEightParityReplayTests**：`G:\AFMOD\AF-REFACTOR\tools\PhaseEightParityReplayTests\Program.cs:104` 将导航和顶距 50 合并断言。保留原红断言的诊断回放确认导航栈 `0→1→1→0`、退出 tag browser 正常；系统页隐藏搜索栏时顶距实际应为 6（`9e17ef1d`，`G:\AFMOD\AF-REFACTOR\AnimusForgeTerminalUiModels.cs:300-302`）。是旧布局断言过时，不是重复压栈；后续周报等该 runner 未到达部分仍未完成。
3. **WorldDiplomacyCompression.SmokeTests**：`G:\AFMOD\AF-REFACTOR\tools\WorldDiplomacyCompression.SmokeTests\Program.cs:290` 禁止 DECLARE schema 有 commitment；`b16cb47c8` 的外交修复在 `G:\AFMOD\AF-REFACTOR\WorldDiplomacyBehavior.cs:11500` 加入了它。当前 Prompt 与旧测试合同冲突；本轮没有修改任一方，也未据此断言实际外交执行已失败。需要复核新协议、版本和测试共同演进。

保留原始 FAIL，不修改断言换绿灯。NuGet NU1900、部分 net6 的 NETSDK1138 分别是漏洞元数据获取警告与旧目标框架警告；未隐藏。官方六项构建无警告。

## 证据和重放

所有命令、stdout、源码/产物指纹和临时复现都在 `G:\AFMOD\AF-REFACTOR\.tmp\full-audit-35524b04`，不是生产模块或安装包。关键文件：

- `baseline.json`、`source-manifest.json`、`artifact-hashes.json`：源码与本次构建绑定。
- `dotnet-results-final.json`、`python-results.json`、`build-Debug.log`、`build-Release.log`、`retry-*.log`：全量检查明细。Python 的缺失证据示例 exit 2 表示门禁阻塞，不是发布 PASS。
- `channels/audit-findings.md`、`channels/witnesses.log`、`channels/evidence-manifest.json`：F1/F2/F3 的四个 witness。
- `scene/report.md`、`scene/reproduce.py`、`scene/run.log`：F4/F5 与双 waiter 风险的四个 fixture。
- `domains/domain-audit.md`、`domains/tts-cancel`、`domains/tts-late-failure`、`domains/parity-browser`：TTS 两个竞态和布局诊断；各目录附真实提取方法/产物来源。
- `project-inventory.json`：50 个 C# 项目总盘点，其中 41 个测试项目。其余是工具应用/核心/导出器，不计为已运行测试。

本轮 Debug 1.4 Stage SHA-256：`96059a3d6a2ed4927698f4dac9f8702677f01146803588ff72d454ab97369d06`；Release 1.4：`39eade43451a5fe9ac3dfbe040ab71272134b676f4cc4b0f23eccadba16841fb`。不是沿用上一份 HANDOFF 的旧 DLL。重放均使用明确的本机 SDK/引用目录与项目内 Stage；需要真实游戏验收时另行安排，不直接部署这些测试产物。

**缺陷 witness 的 exit 0 表示坏状态成功重现，绝不是修复通过。** 复现方法原样执行，游戏实体/网络/音频等边界按各报告明确 stub；没有用它们替代实机证据。

## 后续建议顺序（本轮未实施）

1. 先修 F1 的资产范围与 F2/F3 的 Courier 完整主回复/权威后处理，形成同一功能闭环回归；不只让 Prompt 变非空。
2. 修 F4 整个玩家请求身份，再闭合 F5 frozen target 回放、双 waiter 与 F6 TTS 请求身份；覆盖失败、取消、旧 callback 与新 session 同 index。
3. 同步 mandatory safety/Bridge 与渠道默认目录；将三个过时/冲突测试改为当前行为验收，保留反例能力，不弱化断言。
4. 再跑全部离线/双版本构建，并按准确提交与游戏版本验收 Campaign/Mission、旧档、live Economy、AFEF、TTS、第三方模组及安装/回滚。完成前不恢复自动化、不广泛删旧、不标阶段八 DONE。
