# 日结压缩输入和来源接受边界

本片接续 `9040d184` 的主线程写回边界。原三个 Execute 在后台 continuation 读取 Hero、草稿、历史状态、settings 和信任；请求期间来源更新后，旧结果仍可能删除当前草稿或把当前任务标为三次失败。

## 当前调用路径

1. `ProcessMemorySummaryQueueAsync` 在既有主线程队列中准备任务，捕获的原 save generation 贯穿所有波次和额外总览。
2. 每个实际启动的任务在主线程捕获完整私有数据副本及字符串 prompt。日结额外冻结姓名替换规则、信任、日期/小时、AFEF 的场景 fallback 和完整周报回执。重大履历和总览分别冻结 cursor 素材、已有状态和新增块。
3. 后台只消费这些输入进行 provider 请求及解析，不再读取 Hero/草稿或调用游戏相关 renderer。重试仍为最多三次，保留 Retry-After/1500ms 和 60 秒波次节流。
4. 请求前/重试前及最终成功或失败接受前，在主线程复核原 owner、Campaign、generation、queue entry、source/state 对象身份和完整内容。相同 ID/cursor 下的文本或回执变化也拒收。被替换的旧任务不能消费新来源、覆盖新状态或把新来源标记为 API 失败。

来源变化属于过期工作；保留当前来源供原调度器后续处理。原始网络请求若已经开始仍会完成，不能声称真的取消了网络。没有新增持久字段、公开能力或存档键，原 Apply/Mark 实现及 prompt builders 的业务正文保持原样。

## 成本和边界

`MemorySummarySourceStamp` 仅序列化当前任务的私有数据来源、任务和相关摘要状态；不序列化整个 owner 或游戏对象。Json.NET 使用独立设置和缓存的 contract resolver，禁用类型名；不修改进程默认设置。快照每个任务启动一次，完整来源比较只发生在请求/重试/接受检查。空间与当前任务的来源规模有关，未测实机峰值。

复用 `MyBehavior.MemorySummaryMainThread.cs`，EngineTick 仍最多消费两个排队动作，没有新增轮询队列。该上限限制动作数，不能当成单次动作耗时的实测保证；批量结果接受和原资格检查仍有与素材/队列大小相关的成本。维护入口既有 Hero 查找与预算前扫描仍是待测性能点。

此边界覆盖三种日结压缩任务，不代表所有 memory writer、Native、Scene、Courier 或 TTS 已迁移完成。来源 stamp 是本次任务的瞬态接受凭据，不用于持久化、AFEF 动作回放或公共 ABI。

## 证据

真实源码回归及变异见 `tools/MemorySummaryInputBoundaryTests/`；旧签名的全文件定向反转检查见同目录 `source_parity.py`。本轮构建/合同/存档身份记录见 `docs/audits/2026-09-12-memory-summary-input-verification.md`；准确源码提交和坐标在根 HANDOFF 与代码范围图记录。

LIVE、真实 provider、旧存档读档及真实帧耗时未验证，离线 fixture 不替代这些验收。
