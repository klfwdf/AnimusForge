# 记忆失败提示闭环（2026-09-11）

基线 `38488ed2`；检查点 `88777e45`；生产/测试 `6f0bac67`。

- 已完成：9 个原失败出口传入操作 generation；有界待提示；现有 EngineTick 消费；静态 owner、实际 Campaign owner、generation、展示 revision 隔离；旧确认/失败重入/日志故障防护；现有两处临时重置点清理提示。
- 已清理：后台直接 ShowInquiry 与无身份 bool 清理。保留原错误文字、重试、算法和存档类型。
- 验证：85/7；原 Native 589/132/44/46/88/184/111、ports 308/3；最终六项 Stage、16 组相关回归、四 DLL 532 元数据通过。存档绑定只更新两处 -14 行号。
- 未推送/部署/实机/存档访问；两份用户草稿保留；Api.V1 仍只读。

## 下一步：真正的数据快照，不再重复本轮提示检查

已确认 `BuildCompressedMemoryContextById` 仍读取 owner 的可变块/草稿；`BuildMemoryRecallQueryText` 读当前场景、近期 6 条非 AFEF 文本并截取末尾 1200 字符；筛选与正文使用当前场景/日期。不能删掉这些输入，也不能整个搬主线程。

1. 主线程捕获原 identity/owner、场景与日期、总览、相关设置以及必要的记忆输入副本；身份解析不能漂到后台再从新 Campaign 查 owner。
2. 后台保留原 ONNX 召回、最新块保留、API 筛选、排序、AFEF 与正文格式；与原健康输出逐项对照。
3. 本次追读发现 TryBuildMemoryRecallCandidates 没有写回块内 embedding；引擎按现有锁维护文本 embedding 缓存。不要凭空新增存档缓存回写。总览 sanitize 与其他 helper 仍要继续核对。
4. 复用本轮显式 generation 的错误出口，再补完整请求起点和结果接纳边界。当前 generation 只从记忆操作开始捕获，不等于 Native admission 起点。
5. 之后继续 persona/独立周报绑定、TTS 直接回调及 Courier prepare；真实游戏/旧存档仍单独验收。

技术说明：`docs/architecture/af-memory-failure-presentation-boundary.md`。不要移除已有 whole-file inverse/变异测试来容纳后续改动；提供新的独立审查证据。
