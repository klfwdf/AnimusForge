# Native 请求准入续作（2026-09-11）

本轮自动化起点：14dec2d7；生产基线 a616958c。同名远端 ahead 5 / behind 0，未合并或推送。保留两份用户草稿。

## 已确认的问题与范围

- 普通输入与 NPC 主动开场入口先 Task.Run 再解析目标/ConversationManager/ActiveToken，存在调用至后台开始间换对象的窗口。
- 只有 Overlay._isSubmitting，没有覆盖旧公开入口与普通/主动两入口的统一后端排他。
- CanSubmitNativeConversationForExternal 被 OnApplicationTick 用来关闭 UI，因此不能直接加入 busy 判断。
- 现有通用主线程 helper 超时会放弃返回值，不适合直接托管具有保留/释放责任的准入票据。

本轮只做主线程准入、捕获会话/目标与既有后处理边界的作用域复查、普通/主动入口共用后端排他、准入失败保留输入及最小行为反例。后台 Task 真正结束后才释放后端票据；不宣称已取消物理网络，不把后端结束当 TTS 播放结束。

仍沿用完整旧业务链；公共 V1 提交保持 NotSupported。Native 全部前置游戏读取、完整原子事实回执及展示生命周期没有因为新增准入就自动完成。Courier 后续继续，不改额外模块业务。

## 验证与交付

先记录原入口的换目标/并发反例，再跑生产接线提取/源链接行为测试和相关既有回归；Stage 双版本/Bootstrap，不部署、不推送。重大编辑前提交本页为检查点。完成后更新本页、总 HANDOFF 和简明制作组 HANDOFF，并保留未验证项。

## 本轮已落地

- 两个旧公开入口改用共同准入包装，仍进入原完整 Native 方法，不切换到 detached/opt-in。
- 准入在主线程捕获 generation、ConversationManager/ActiveToken、Mission、目标和 AgentIndex；普通/主动入口共用一个后端票据。
- 主动开场只在成功取得准入后消费。排队未开始可超时；已开始的捕获必须接收结果，避免泄漏票据。
- 复用已注册的 ConversationEnded 事件失效旧票据，覆盖同 Hero/同 token 再次对话；旧请求最终只能清自己的票据。
- 六个既有动作前校验和实际动作队列执行处复查票据。动作后可能主动退出对话，原后续记账语义暂保留，不能据此宣称完整原子回执已完成。
- busy 与 CanSubmit/UI 存在性分开；准入拒绝不当作 NPC 回复。旧/关闭 Overlay 的 finally 不再结束新 UI 流式或清其 busy，也不恢复旧输入状态。

## 结果与下一项

原入口两个并发/换目标反例已复现；44 个定向检查与 7 个变异控制用于本轮交付。最终构建与回归结果以本轮审计记录为准。

继续 Native 的完整 prepare/commit/展示回执边界（含仍在后台的实体读取与历史操作），之后才评估有限公共提交；Courier 双向 prepare 仍在队列中。不要把这次后端准入当成完整 Native 服务或物理取消已经完成。当前每小时自动化继续有效，不因本轮一个批次完成而标记整个项目 DONE。

最终本地生产/测试提交：`77d4a940`。最终 44 检查 / 7 变异、六项 Stage 构建、相关回归通过。精确证据见 `docs/audits/2026-09-11-native-admission-verification.json`。新增会话 epoch 专门覆盖“请求还在队列里，旧会话已结束、同 token 新会话已有主动开场”的场景；不会消费新开场。
