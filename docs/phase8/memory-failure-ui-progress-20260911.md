# 记忆失败提示边界检查点（2026-09-11）

基线 `38488ed2`。沿深层记忆链确认：TryBuildMemoryRecallCandidates 和 TrySelectMemoryIdsWithPreprocess 最终直接调用 InformationManager.ShowInquiry；后台改变 _memorySummaryFailurePopupActive。日结记忆总结也使用同一出口。

本轮先完成此真实失败路径：原 OnEngineTick 消费有界提示，绑定原 MyBehavior owner 和检索/总结开始时 generation，主线程独占 UI/active 状态，过期和重复提示不显示，旧确认回调不得清除新弹窗；日志故障不改变展示状态。存读档和主动清理的现有临时状态重置点同步清理新提示。

不改召回/选择内容、参数/数值、错误文本或重试策略，不新增通用队列/网络管线，不推送/部署/存档访问。深层数据快照尚未落地，不得把本轮当成完整记忆线程安全。
