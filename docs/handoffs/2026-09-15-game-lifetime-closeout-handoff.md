# AF 全范围收尾续点：真实 Game 生命周期与最终提交退役

源码/测试 **29448d1b7ec7bc7a92820c8b510aaad56c21ff37**；起点807bc5b9，实施checkpoint e44f74b2。唯一工作区 G:/AFMOD/AF-REFACTOR，本地分支 `codex/af-framework-skill-delivery-20260911`。**当前仍是阶段8连续收尾，整体任务ACTIVE，不是整个重构项目DONE。**

## 1. 这次实际解决了什么

从玩家视角，本包处理退出当前局、换局、切场景时的旧任务残留，以及“已经开始执行却被超时误报成没执行”的问题：

- 真正接入 `InitializeGameStarter`、`OnGameEnd`、卸载三个引擎入口。按实际Game引用识别开始/替换/结束，旧Game晚到的结束事件不会清掉新Game。
- 从实际CampaignGameStarter保存本局主体实例，包括构造后注册失败的My/Courier singleton；结束时不依赖已经为空的Campaign.Current。先让旧generation失效，再逐owner清理；单个清理抛错不阻断其余owner。
- Native准备和最终动作、Courier准备/后处理owner phase和最终commit都登记到原owner的待办退役表。它不是另一条队列或LLM链。
- 读档/Mission reset：旧未开始待办立即结算失效，下一会话仍可提交。GameEnd：旧owner永久关闭新准入。清队列窗口也不接收会被随手清掉的新任务。
- 已claim的动作/commit必须等待真实结果；定时器、取消或退役不能撤销它，也不能将它误报为可重试的未执行。若真实执行永不返回，本包不承诺安全强停。
- Courier原最终commit旧实现没有原子claim；同一个回调重复执行会二次提交，已由固定旧声明复现并修复。原会话、送达和入站无回执清理仍由原owner执行，业务时机不提前。
- MyBehavior另补清理窗口竞态：旧singleton还存在时，worker可能捕获新generation并在队列清理后再提交。现在清理第一步就关闭准入，连尚未懒创建dispatcher的路径也拒绝；退出时解除静态地图通知订阅，不清用户存档。

Courier大类净减154行，最终commit调度集中到 `CourierDeliveryBehavior.CommitDispatch.cs`。这次新增的是实际生命周期/退役责任，不把partial拆文件冒充全部业务owner拆完。Native大类仍有大量未拆责任；必要旧Saveable、公开兼容接口与活跃业务保留。

## 2. 已核实代码位置

以下均为 `29448d1b7ec7bc7a92820c8b510aaad56c21ff37` 的一基行号；完整当前地图共124点。后续用符号+提交定位，不凭旧行号修改。

| 位置 | 符号 | 责任 |
|---|---|---|
| `SubModule.cs:655-675` | `protected override void InitializeGameStarter` | 注册前失效，成功/失败后捕获真实实例 |
| `SubModule.cs:98-103` | `public override void OnGameEnd` | 真实 GameEnd 入口 |
| `SubModule.cs:105-112` | `protected override void OnSubModuleUnloaded` | 卸载先退役活动 Game |
| `AfCampaignRuntimeLifecycle.cs:23-36` | `internal static void CaptureOwners` | 捕获实际注册和半途构造的主体实例 |
| `AfCampaignRuntimeLifecycle.cs:41-59` | `private static void RetireOwners` | 各主体故障隔离、释放持有列表 |
| `Refactor/Runtime/GameLifetimeCoordinator.cs:7-39` | `internal sealed class GameLifetimeCoordinator` | 引用身份区分旧/新 Game；纯 owner，不持久化 |
| `Refactor/Runtime/PendingOperationRegistry.cs:9-88` | `internal sealed class PendingOperationRegistry` | 已有队列退役登记，不新增第二队列 |
| `MyBehavior.CampaignLifetime.cs:10-27` | `internal void RetireCampaignRuntime` | 先停止准入，再退役待办/人设/订阅 |
| `MyBehavior.MemorySummaryMainThread.cs:33-46` | `private sealed class MemorySummaryDispatchHost` | 旧 singleton 清理窗口的新 generation 也不能准入 |
| `ShoutBehavior.CampaignLifetime.cs:19-25` | `internal void RetireCampaignRuntime` | 准备/动作登记 seal 与原 Scene/Native 清理 |
| `CourierDeliveryBehavior.CampaignLifetime.cs:19-31` | `internal void RetireCampaignRuntime` | owner phase/final commit seal 与原 Courier 清理 |
| `CourierDeliveryBehavior.CommitDispatch.cs:14-93` | `private Task<InteractionCommitResult> DispatchCourierRefactorCommitAsync` | 最终提交原子 claim/待办退役/真实回执 |

真实消费者：Shout的 `RunNativeConversationMainThreadFuncAsync` 与 `ApplyNativeConversationGameActionsOnMainThreadAsync` 共用 `_pendingMainThreadFunctions`；Courier的 `RunCourierOwnerPhaseAsync` 与 `DispatchCourierRefactorCommitAsync` 共用 `_pendingOwnerPhases`。My原 `MemorySummaryDispatchHost` 检查 `_campaignRuntimeRetired`，不另建dispatcher。Scene四个原清队列位置改为先退役待办再清原队列；原Scene epoch/session/播放/后处理gate清理继续执行。

[代码范围图](../architecture/af-framework-code-scope.md) / [提交绑定坐标JSON](../architecture/af-framework-code-map.json) / [本候选审计JSON](../audits/2026-09-15-game-lifetime-verification.json)。

## 3. 验证：分清层级

| 检查层 | 当前结果 | 证明不到的部分 |
|---|---|---|
| 实际生命周期协调器/登记表/引擎回调 | 36检查、12有效故障反例 | Game/业务owner主体为fixture |
| 实际Native/Courier派发+退役partial | 15检查 | 低层原reset与游戏为fixture |
| My真实宿主/dispatcher/退役竞态 | 7检查、1故障反例，覆盖初次与已创建dispatcher | 非实际Campaign退出 |
| Courier最终commit边界+实际DTO | 19检查、3故障反例 | Session业务为fixture；不代表资产全验收 |
| Native最终动作/记忆/待录历史 | 91/184/111检查，5个队列故障反例 | 实际游戏副作用/展示未执行 |
| 记忆、三渠道、内部接缝 | 历史852+Native27、writer238、dispatcher37、人设169、渠道132、Courier后处理39/历史122/owner phase16、内部ports308+3反例、Campaign注册42+5反例通过 | 不等于完整main主体逐功能等价 |
| API与实际DLL | 外部只读API119、快照32、4实现DLL元数据728通过 | SDK三渠道提交仍未开放；无真实子MOD加载 |
| 构建/回放 | Debug/Release × Bootstrap/1.3/1.4六Stage，实际1.4 Courier Host回放通过 | Host回放端口仍为fixture；Stage不覆盖游戏 |
| 保存身份 | 固定main437925b8对照146键/36行为相同 | 未加载/另存真实旧档 |

旧红分别使用807bc5b9真实Game回调、Native动作派发、Courier最终提交声明，暴露缺少退役、等待悬挂和重复提交；不是声称全部旧功能有错。测试中的编译错误、首次SDK定位错误、一次旧红进程超时及错误故障注入已经修正并留在原始日志，不计作有效反例。

最终Stage使用的是提交前同一份C#（当时HEAD为e44f74b2），已将归一化源码hash逐文件与29448d1b核对。只采用 `stage-debug-final.log`、`stage-release-final.log`、`api-metadata-final.log` 和 `actual-host-replay-final.log`，不拿早期产物冒充最终My竞态修复。审计记录6DLL hash与41份冻结日志，位于 `.tmp/game-lifetime-20260915/final-evidence`；测试命令见 `tools/GameLifetimeTests/README.md`。

## 4. 整体清单仍有这些必交项

| 责任 | 当前判断 | 后续要求 |
|---|---|---|
| 主体Game生命周期 | 本包核心入口及待办退役离线通过 | 实机结束/重进/读档、完整TTS/所有订阅继续核对 |
| 三渠道准备/Prompt | 人设与历史边界已进展；其余仍不合格 | 共用770行Prompt构造混有live对象和规则/lore网络，必须拆捕获→后台路由→主线程接受；不能把同步网络整体搬主线程 |
| 角色/库存/消息、人设升格 | 仍有旧混合路径 | Native与Courier消息构造、Scene相应消费者一并对齐，保持原失败降级与玩家语义 |
| B1记忆性能 | writer/失效/部分owner已验证 | 首次深复制、全积压/终步真实记录成本、预算与背压，不用每帧两个callback冒充有界 |
| 内部接口 | AF→制作组typed贡献ports已稳定验证 | 制作组→主体的双向服务契约仍需完成，不动政策/宴会/GCCZ业务 |
| 外部子MOD | Api.V1仍只有真实只读目录 | **Native/Scene/Courier都必交**完整提交、结果、关联/取消；不得改Available假装开放或另建缩水链 |
| 全主体main复现/删旧 | 未完成 | 逐功能、逐符号核销：迁新owner/薄适配/必要兼容/外域保留；活跃旧代码不能直接清仓 |
| 最终验收 | 未完成 | 当前候选真实游戏/旧档/live Economy/AFEF/独立子MOD通过后才结项 |

继续依据 [main主体矩阵](../phase8/af-core-main-closeout-matrix-20260915.md) 和 [14职责计划](../phase8/af-core-responsibility-decomposition-plan-20260915.md)，不另立“新阶段完成”的说法。

## 5. 交付与回滚

- 本包仅本地提交。最后确认已推送GitHub的是10defeb4，本包不自动继承那次已完成的推送授权。
- 未部署游戏、未操作存档、未切默认入口、未恢复自动化；政策/宴会/GCCZ业务未改。
- 三份保护文档hash保持；两份9月6日用户草稿原本有未提交改动，不能提交或覆盖。9月11日本地专用简明版继续不上传。
- 本地新简明稿：`.tmp/game-lifetime-20260915/team-handoff.md`。本详细HANDOFF和总HANDOFF可随将来获准的重构分支发布。
- 回退应做针对29448d1b的逆向提交，连同新接线/partial/对应证明一致回退。前生产基线807bc5b9，仅用于定位；不得hard reset、强推或覆盖其他制作组的工作。
