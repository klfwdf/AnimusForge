# Native 持久记忆输入快照

## 接线与责任

完整 Native 入口现在先排到原主线程队列，验证 admission，再解析原 Hero/普通人物的记忆身份、NPC 次级输入和当前 MyBehavior owner。得到后台 work 后才启动原并行历史任务；任务结果用于 prompt 前，再通过原主线程队列验证同一 admission。

MyBehavior 的 internal capture factory 在主线程固定：
- owner、请求 generation、记忆 ID；
- 原总览文本、场景、游戏日期、相关 MCM 数值；
- 原召回查询（玩家/NPC 输入、最近 6 条非 AFEF 草稿文本、末尾 1200 字符限制）；
- 当前召回/选择/呈现实际需要的块字段，以及独立的 AFEF 列表。

后台复用原 BuildHistoryContextById / BuildCompressedMemoryContextById / TryBuildMemoryRecallCandidates / TrySelectMemoryIdsWithPreprocess；没有新 LLM、排序器或记忆系统。generation 由 Native admission 传入，错误继续走上一轮有保护的提示出口。

## 保留的语义

原最新块保留、候选上限、ONNX 富标题召回、两种筛选模式、筛选填充、顺序、日期/时间标题、Summary 和 AFEF 格式保持。普通人物继续由原 wilderness identity helper 解析；无持久记忆资格保持旧空历史结果。原 history-only 异常空串 fallback 也保留，不将它冒充严格读取 receipt。

原生对话 UI 文本仍不混进 AF 历史；主动开场仍不伪造玩家输入。已有的公开历史接口和其他渠道默认调用不改变签名，未传 snapshot 时走原完整逻辑。

## 不可越界使用

这是 private 召回用途投影，不是全量存档记录。它不复制本路径不读取的周报材料、publicity 等字段，不能写回存档或交给其他业务当完整 CompressedMemoryBlock 使用。返回给 Native 的只是内部 work / 字符串，不向 Api.V1 暴露游戏对象或新增写能力。

主线程捕获不等于全局记忆事务。其他异步维护/压缩写入者的线程与锁责任尚未全面核对；当前测试证明捕获后的原集合修改不影响工作，不证明与所有后台写入者并发捕获时具有原子一致性。

## 性能

每请求一次有界阶段捕获，而非每 Tick 全量扫描。复制成本与已有块/AFEF 条目数量线性相关，字符串复用不可变引用。少量记忆不可能进入 embedding 的分支跳过草稿查询构造；不额外运行 ONNX/API，也不新增截断或减少记忆块来换性能。主线程实际耗时仍需游戏测试。

## 验证

- Memory suite：852 检查，120 组数据/模式/身份/形状组合；把真实历史版本的 renderer/召回/筛选方法编译在候选旁边比较，不用候选自身充当旧实现。
- Native suite：27 检查，执行实际捕获、后台工作和结果接纳片段；覆盖普通/主动开场、Hero/普通人物、失效和排队超时。
- 10 个新行为变异必须编译后被断言拒绝。UI/游戏数据/ONNX 引擎/API与严格 JSON 外部边界是 fixture，不能当实机。
- 6 个 MyBehavior 默认方法做逐项逆变换；完整 MyBehavior/ShoutBehavior 只允许审核并固定 SHA 的新差异。旧套件通过 source_parity 还原这一已独立验证的差异，原断言和反例继续执行。

## 仍未完成

此改动仅将 Native 的这条持久历史路径接到快照。Scene/Courier 旧调用仍可走原默认逻辑；persona/规则构造、独立周报绑定、后续游戏对象引用、TTS 直接回调、其他记忆维护写入者和真实旧存档/游戏验收仍待接续。过期结果可丢弃，已开始的网络不会因此被真正取消。

用户已要求本轮完成后暂停自动化，后续必须等用户指示。不能把本次交付写成整个阶段 8 DONE。
