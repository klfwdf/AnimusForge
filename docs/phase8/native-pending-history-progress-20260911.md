# Native 前置历史 / 清理续作（2026-09-11）

起点 `5847a195`，生产 `18f48678`；fetch 后 ahead 23 / behind 0。仅 AF-REFACTOR，本轮不推送/部署，两份用户草稿保持。

确认缺口：玩家显示名、tentative player line、pending AFEF 消费和 Native 历史消息在后台准备；五个后续拒绝分支直接在后台调用 rollback，重新解析当前历史 key，并使用 CurrentInstance 清理场景记录。读档实际会重置 event sequence，旧请求因此可能误删新记录；非 Hero party key 改变也会导致原 Native/Scene 记录不一致。

本轮范围：这段 prepare 变成原主线程队列的一次有界消费，固定 history key，并用原 formatter/窗口规则返回纯历史消息。所有五个拒绝分支及 action discard 使用同一固定 key + 原上下文主线程清理，只删 pending user/player，不删 AFEF。保留旧默认调用的行为，为私有 append/snapshot/formatter 增加明确可选 captured key，不另造历史格式。

新的 history 工作有明确 claim/expiry，未开始可超时，已开始须等真实结果，日志不决定 Task 结果；不能用现有 bool timeout helper 的 fallback 冒充完成，也不扩大改写所有 Scene 调用。原 LLM/动作/持久记忆 owner、公共 API、存档身份不改，更早的其他 prepare 与 TTS/Courier 仍待继续。
