# AF 首次 capture/copy 预算检查点（2026-09-14）

**阶段8 / B1继续VERIFY。** 本检查点在生产改动前。前生产`4d6994bc`，文档`b7248250`。不进入B2；不重做深line/trigger。

意图：把`CaptureMemorySummaryInput`里三类来源的首次整图复制改为共享metadata计费（有限窗口128条）。同步无预算入口保持完整捕获oracle；`ExecuteCapturedMemorySummaryJobAsync`按窗口续跑。列表引用/count变化失效；最终仍走`IsMemorySummaryInputCurrent`。Sanitize/Prompt/raw指纹仍可原子，本刀不切。
