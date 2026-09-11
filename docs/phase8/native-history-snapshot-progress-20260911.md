# Native 持久记忆输入快照检查点（2026-09-11）

基线 `659bb998`。本轮要把 Native 当前唯一的后台身份/owner 查找与可变持久记忆读取改为主线程捕获，后台保留原召回/筛选/格式。不是改写所有渠道或新增记忆系统。

主线程固定 identity、owner、请求 generation、总览、场景/日期/设置、召回查询与块的只读用途副本；后台不再读取这些 owner/game 状态。保持原最新块保留、候选/筛选规则、AFEF、原错误出口及 legacy 默认调用。结果使用前再检查原 Native admission。

先对原 renderer/召回/筛选输入建立会失败的测试，再实现真实 Native 接线。现有全文逆变换保留，后续独立快照验证用精确 SHA 证明新增差异，不删除旧断言。只本地提交与 Stage，不推送/部署/真实存档访问，用户草稿保留。

## 完成与暂停

- 本地源码/测试：`8f1cd479`；检查点：`e1a09954`。未推送/部署，草稿和其他工作树保持不动。
- Native 主线程 capture → 原后台 recall/select/render → 主线程 admission accept 已接线。原 public/default 历史入口仍保留其他渠道与 ABI 责任；新投影不能当完整存档记录写回。
- 测试：852 memory / 120 组合、27 Native；原实现分别 305 / 12 个断言失败，候选通过；10 个新变异均 runtime 拒绝。旧边界 9 组与 26 个反例复验通过。
- 最终小历史优化后重跑 Debug/Release × 1.3/1.4/Bootstrap、16 组相关回归及实际四 DLL 532 元数据检查；六项 DLL 与 `.build.json` 哈希一致。168 个存档 binding 不变，本轮未调整 fixture。
- 清理：旧 Native history helper 无生产引用；新接线、旧默认逆变换和既有门禁未弱化。自己的 diff 检查通过；两份 2026-09-06 用户草稿保持原状。
- 自动化 `af-7-8` 已按用户要求设为 PAUSED 并回读验证；名称、提示词、周期、目标任务保留。交接完成后不开始下一项。

## 交付入口

- 总 HANDOFF：`HANDOFF.md`
- 给制作组（仅本地，不上传）：`docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md`
- 技术说明：`docs/architecture/af-native-history-snapshot-boundary.md`
- 验证证据：`docs/audits/2026-09-11-native-history-snapshot-verification.md` 及同名 JSON

## 剩余边界

其他后台 writer 并发捕获尚未全面审查；persona/规则/周报绑定、TTS、Courier prepare 和 Scene/Courier 的快照接入仍需接续。主线程耗时、真实游戏/旧存档/provider 未验证。任何下一项均等用户新指示；本轮完成不代表阶段 8 或完整公共 SDK 完成。
