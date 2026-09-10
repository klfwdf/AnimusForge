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
