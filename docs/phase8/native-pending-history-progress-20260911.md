# Native 前置历史 / 清理续作（2026-09-11）

起点 `5847a195`，生产 `18f48678`；fetch 后 ahead 23 / behind 0。仅 AF-REFACTOR，本轮不推送/部署，两份用户草稿保持。

确认缺口：玩家显示名、tentative player line、pending AFEF 消费和 Native 历史消息在后台准备；五个后续拒绝分支直接在后台调用 rollback，重新解析当前历史 key，并使用 CurrentInstance 清理场景记录。读档实际会重置 event sequence，旧请求因此可能误删新记录；非 Hero party key 改变也会导致原 Native/Scene 记录不一致。

本轮范围：这段 prepare 变成原主线程队列的一次有界消费，固定 history key，并用原 formatter/窗口规则返回纯历史消息。所有五个拒绝分支及 action discard 使用同一固定 key + 原上下文主线程清理，只删 pending user/player，不删 AFEF。保留旧默认调用的行为，为私有 append/snapshot/formatter 增加明确可选 captured key，不另造历史格式。

新的 history 工作有明确 claim/expiry，未开始可超时，已开始须等真实结果，日志不决定 Task 结果；不能用现有 bool timeout helper 的 fallback 冒充完成，也不扩大改写所有 Scene 调用。原 LLM/动作/持久记忆 owner、公共 API、存档身份不改，更早的其他 prepare 与 TTS/Courier 仍待继续。

## 已落地（128e9842）

主线程一次准备固定 key、玩家名、event sequence、AFEF 和 Native 消息；五个拒绝分支与 action discard 统一原上下文清理。只删 player/user，不抹事实。queue claim 后不放弃结果；未开始超时明确失败，日志隔离。

111/12 新检查、原 184/15、88/9、44/7、46/6、ports 308/3、六项 Stage、532 实际 DLL 元数据与 16 组回归通过。原 helper 默认行为和 Action core 对照不变，168 存档绑定不变。

审计：`docs/audits/2026-09-11-native-pending-history-verification.md` / JSON；制作组版：`docs/handoffs/2026-09-11-native-pending-history-team-handoff.md`。

下一步：更早 Native prepare（人设/规则/持久记忆读取得分开游戏快照与后台工作），以及通用 main-thread func 的 bool timeout 竞态。不要把含网络的整个 builder 搬主线程；再处理 TTS 直接回调、Courier prepare。公共 Api.V1 仍只读，未推送/部署/实机，自动化继续。
