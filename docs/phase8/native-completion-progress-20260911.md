# Native 成功收尾主线程续作（2026-09-11）

起点 `d9288faa`，生产 `9a5335be`；同名远端 ahead 17 / behind 0，两份用户草稿保留。

已确认源码：完整 Native 请求在 Task.Run 内运行，动作后先 await 目标检查，再在后台读取当前 SceneSessionId、调用 MyBehavior 历史存储与 Native 短期/场景记录。只检查 Agent 可用不等于原会话仍有效；最后世界地图关窗 callback 也未绑定原会话。

本轮范围：将成功动作与后续历史派发合在同一次主线程队列消费中，动作前捕获 scene session 与非 Hero 长期记忆身份，动作后按原 Campaign/generation 写原目标的持久历史；短期显示状态、TTS 与延迟关窗仍绑定原会话/revision。合法 owner 结束会话不能被当作未执行动作而抹掉此前效果。

明确限制：MyBehavior 的既有 void 历史 owner 吞错/可能无 owner，本轮不能把方法返回命名为可靠持久化 receipt，更不承诺 AFEF 原子事务。新增公共提交仍不开。先复现后台/会话切换反例，再测真实收尾、正常/主动、Hero/非Hero、合法关闭、读档、重复回调与关窗隔离；保持原 Action core 和业务模块不变。
