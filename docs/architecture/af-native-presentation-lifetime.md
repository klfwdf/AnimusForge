# Native 内部展示观察票据

用于 `G:\AFMOD\AF-REFACTOR` 当前完整 Native 流程，不是对外 API，不授予动作或记忆写入权限。

## 两种责任必须分开

| 责任 | 生命周期 | 检查入口 |
|---|---|---|
| 后端请求票据 | 从准入到完整后端 Task 结束 | `IsNativeConversationAdmissionCurrent`，仍要求当前后端 slot |
| 展示观察票据 | 起始界面上下文 → 绑定已准入请求 → 后端结束后的排队结果/TTS 后显示 | `NativeConversationPresentationScope`，不依赖仍占用后端 slot |
| Overlay 本地提交 | 本对象与 `_submitGeneration` | 控制本地 busy、等待动画、待显示提示 |

后端 Task 释放不能使合法最终 UI 回调全部失效。反过来，观察票据有效不代表可以执行动作、重放请求或宣称 TTS 已播放完毕。

## 接线顺序

1. Overlay 在主线程捕获 origin scope；它只读取当前上下文，不抢占后端或消费 NPC 开场。
2. 两个提交入口都走 `SubmitNativeConversationForOverlayAsync`，再调用原 `SubmitNativeConversationAdmittedAsync` 与完整 Native 业务体。
3. 真正准入后递增本实例的展示 revision，并将 scope 绑定至本请求捕获值。每个 scope 只允许提交一次；重试应重新捕获。
4. 后台的 stream/main/postprocess/final/error 等回调仍使用原 UI 队列；`RunNativePresentationCallback` 在出队时才验证捕获值。
5. 后端完成后，scope 仍可验证排队的最终显示；发生新准入、读档、真正 ConversationEnded、manager/token/Mission/目标变化时失效。
6. 失效时只退休本 Overlay 旧 generation 的本地状态，不恢复旧 NPC 原文、不写旧回复、不结束其他请求的全局流式。正常完成才结束它自己的流式并显示“可以回复”。

## 性能与线程

- origin/snapshot 仅每次提交捕获；不扫描 DLL，不创建新轮询任务或每帧 DTO。
- 既有 Tick 的 `HasCurrentContext` 仅比较 owner、generation、epoch、revision、manager/token/Mission；不枚举 Agent 或重新解析人物。
- 完整目标/Agent 校验在已有显示消费边界执行，替代旧“当前对象是否可用”检查，而非再叠一套逐片段扫描。
- 等待点动画只有到刷新间隔时才做完整检查；后处理通知在排入和实际显示时分别检查。
- 这些观察方法要求主线程，后台探测返回 false。网络不能因此被转到主线程；也不能把 false 解释成物理网络已取消。
- `_submitPresentationScope` 在关闭、退休和完成时清除；长期实例只保留数字 revision，不额外永久保留旧 Campaign/Hero。

## 验证边界

真实旧 stream 回调已在隔离队列重现串显示。新测试链接真实 scope 和 UI helpers，执行两条真实 stream 回调，覆盖后端先完成、入队后换人/读档、同上下文新请求、旧最终完成、通知、动画及本地 busy。

完整 Native prepare、动作后事实/记忆回执、TTS 引擎直接回调和独立子 MOD 公共提交仍需要后续工作与真实 Host 验收。不能把仅用于 Overlay 的观察票据推广为全局动作授权或对外 SDK。
