# Native 动作后收尾边界

该接缝属于 AF 主体内部；不增加 DLL、不改存档身份、不开放子 MOD 写 API。使用原完整 Native 管线及同一个 Action core。

## 本轮实际顺序

```text
后台：正文 / 后处理完成，整理 playerText、openingFact、TTS 状态等已有值
  ↓
原主线程队列（一次 claim）
  ├─ 原请求准入/上下文校验
  ├─ Capture completion：固定原 scene session 与非 Hero 记忆身份
  ├─ 原 Action core（规则和数值不改）
  ├─ 在原 Campaign/generation 向原历史 owner 提交 payload 并检查运行期接受结果
  ├─ 原会话/revision 仍匹配时：短期记录、显示标记、未提前派发的 TTS
  └─ 需要关窗时排入带原 context/revision 的 callback
  ↓
后台只返回最终文本与诊断；不再做这段历史写入
```

动作前捕获异常仍属于 `NoConfirmedEffect`；进入 Action core 或后续收尾后抛出的异常为 `UnknownAfterStart`，保留已发生效果且不自动重试。动作队列仍沿用 30 秒“仅未开始可超时”边界。

## 两类状态分开

- **历史身份**：原 Campaign/generation、原 Hero 或已解析的非 Hero party memory ID、原 scene session。动作本身可能合法结束会话或改变场景，因此不能以“当前对话已结束”直接否认动作已发生或改写到新 party/session。
- **临时界面/场景状态**：要求原 owner、generation、epoch、manager/token/Mission、目标与 presentation revision 仍匹配。结束的会话不重新建立短期显示状态；新请求不被旧回调关窗。
- 延迟关窗不依赖仍占用 backend busy，因为原 Task 可先释放。但出队必须再验证 context/revision。
- 动作派发被丢弃时，pending 玩家记录只在原上下文的主线程上清理；旧 save/scene/revision 的清理被跳过，不对新 owner 按旧 event sequence 盲删。

## 性能与保留

每次真实 Native 提交仅新增一个小文本 payload 和一个主线程 scope；scene/非 Hero 身份捕获一次，沿用原历史 owner 的整理/存储逻辑。没有新 Tick、轮询、第二套 LLM、后台游戏写入服务或全仓扫描。删除原动作后的两次“主线程检查后回到后台使用”跳转、对应后台历史块、未绑定会话的关窗和无用途 stopwatch。

正常/主动开场的 user/assistant/fact 参数、无发言占位策略、Hero 与非 Hero 持久化资格保持。历史派发先于最终 TTS/展示收尾，避免展示阶段越过间隙后再决定历史归属。已流式显示/已播放的正文不因此回滚。

## 严格不夸大

当前 Native 不再依赖 `MyBehavior.AppendExternal*History` 的 void 返回。`MyBehavior.DialogueHistoryCommit.cs` 提供一个支持 sceneSessionId 的 internal 严格入口，复用原日记/最近历史 owner 和 MemoryCommitResult；旧 public 六参 CommitExternalDialogueHistory 仍以 -1 调它，保留 ABI 与 loose 语义。

对适用且非空的 payload，只有 HistoryWritten（Applied/Duplicate 的既有语义）允许正常收尾。Rejected/Failed/无结果会成为 `native.memory.commit_unconfirmed`，保留原结果原因和已发生的动作/部分记录，不自动重试。动作已要求的关窗仍按原 context/revision 排队；它不能因记忆失败被吞掉，也不能关闭后来新请求。非持久 NPC 或真正空 payload 没有提出持久历史请求，不能伪称有 Applied receipt。

**Applied 只确认运行期日记与最近历史 owner 的接受，不确认 SyncData/磁盘，不是动作+记忆原子事务，也不是跨重试恢复 receipt。** 新 Api.V1 的公开提交仍为 NotSupported；原 ForExternal 兼容入口不等于新 SDK 契约。旧 void 外壳还有其他实际调用者，本轮未删除它们。
这次迁移的是动作后的收尾。更早 Native prepare 与几个前置失败分支的 pending history 清理仍需继续查；TTS 引擎自身直接回调、真实读档、owner 切场景时的运行细节也仍待实测。

下一步处理更早 Native prepare/失败清理，以及 TTS 直接回调、Courier 双向 prepare；需要进一步的稳定请求身份/恢复协议时复用既有 recovery 边界，不另造存档键或擅自打开公共提交。

验证工具：`tools/NativeCompletionBoundaryTests`；结果与源/DLL SHA 见本轮 audit，总接续顺序见根 `HANDOFF.md`。
