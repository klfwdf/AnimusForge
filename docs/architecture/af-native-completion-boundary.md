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
  ├─ 在原 Campaign/generation 向原历史 owner 派发完整 payload
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

现有 `MyBehavior.AppendExternal*History` 是 void 外壳，可能吞错或没有可用 owner；本轮返回值只确认收尾代码已返回，**不是可靠日记/AFEF/磁盘提交 receipt**。原日记资格、AFEF 规范和存档写入没有重写，新 Api.V1 的完整公开提交仍为 NotSupported；原 ForExternal 兼容入口仍保留原 owner 准入，不等于新 SDK 契约。

这次迁移的是动作后的收尾。更早 Native prepare 与几个前置失败分支的 pending history 清理仍需继续查；TTS 引擎自身直接回调、真实读档、owner 切场景时的运行细节也仍待实测。

下一步优先复用已有 `MyBehavior.CommitExternalDialogueHistory` / `MemoryCommitResult` 的严格接受语义，设计不丢 scene session 的内部 Native 接线；不能把 Native 改成较短 detached 路径，也不能把运行期接受说成磁盘保存成功。

验证工具：`tools/NativeCompletionBoundaryTests`；结果与源/DLL SHA 见本轮 audit，总接续顺序见根 `HANDOFF.md`。
