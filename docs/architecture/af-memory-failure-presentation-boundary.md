# 记忆失败提示的主线程边界

## 已确认的问题与实际接线

记忆召回、记忆筛选和日结总结的 9 处失败出口共用 ShowCompressedMemoryBlockingPopup。旧出口直接调用 InformationManager.ShowInquiry 并写弹窗 active 状态；召回/网络任务在后台执行时，这些 UI 操作也会在后台发生。

现在出口只发布 MemoryFailureNotice，由已有 MyBehavior.OnEngineTick 首段消费；主线程调用仍可立即消费，不新增 Tick、timer 或通用队列。

- 每个 MyBehavior owner 至多一个待展示消息；同代际首条有效错误优先，已有弹窗时抑制重复。旧代际占位不能挡住新代际错误。
- BuildCompressedMemoryContextById 在操作开始捕获 generation，同一值传给召回和筛选；日结总结使用原先已捕获的 generation。9 处错误文字和错误分支保持原样。
- 发布与消费均核对原 owner / generation；消费和确认还在主线程核对实际 Campaign owner，单有静态 Instance 不代表游戏仍有效。UI、active 修改和确认均在主线程。故障注入的非主线程 UI 确认会被拒绝，不可冒充正常引擎回调。
- 确认回调还核对一次性 revision。旧窗口确认、显示失败后迟到的确认、重入显示的外层失败，都不能清除新窗口状态。
- 原读档与“清理当前存档数据”的临时状态重置点调用同一提示清理函数，丢弃现有待提示并使旧确认失效。此函数本身不读写存档、不执行清理操作，也不取消已发出的网络。
- Logger 只是观察者；日志不可写不应隐藏需要用户处理的记忆失败。

## 不变的内容

保留原错误标题、正文、“知道了”按钮和暂停行为；只把 UI 发起移到正确线程。召回候选、ONNX 评分、富标题、保留最新块、筛选 API/解析、正文/AFEF 组装及重试/阻塞策略未改。没有自动重放 LLM 或游戏动作。

旧 bool active 继续表示是否有弹窗；revision 是区分每次展示/确认的身份，不是第二套业务状态。新增消息槽是瞬态字段，不是存档键或恢复 receipt，公共 Api.V1 不开放新能力。

## 验证边界

测试执行真实提示 owner、原 EngineTick 入口、SaveRuntimeGuard 与 9 个生产调用语句；InformationManager / TWParallel 是 fixture，不是真实游戏 UI。它测试后台、并发合并、过期、owner 替换、重置、迟到确认、日志错误、显示失败/重入。

对受影响的 8 个 MyBehavior 声明做精确逆变换；除了提示实现外，算法/生产分支逆变换逐字等于基线，整个 MyBehavior 也恢复为基线。原工具的 Team ports 全文逆变换与断言没有删掉，只新增精确 SHA 审核条目。

## 仍需后续解决

这不是完整记忆线程安全：可变 blocks/drafts、Hero/部队身份、当前场景/日期读取仍待数据捕获。当前代际绑定是每次记忆构造/总结操作的起点，不替代完整 Native/Courier 请求的起点与会话绑定。当前存档内的主动清理也不等于取消所有先前的异步任务；只有已经发布的提示和确认会立即失效。

本轮追到 TryBuildMemoryRecallCandidates 只新建候选并读取块；未发现它写回块内 embedding。embedding 缓存留在 OnnxEmbeddingEngine 内、按现有锁管理，不应在后续快照改造中额外写入存档。MyBehavior 的总览 sanitize 与其他构造读取仍需在下一步区分。

仍需真实 1.3/1.4 UI、暂停、旧存档及慢网络返回验收。不得因为本提示边界通过就开放公共写 API或标阶段 8 DONE。
