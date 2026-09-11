# Native 成功收尾主线程续作（2026-09-11）

起点 `d9288faa`，生产 `9a5335be`；同名远端 ahead 17 / behind 0，两份用户草稿保留。

已确认源码：完整 Native 请求在 Task.Run 内运行，动作后先 await 目标检查，再在后台读取当前 SceneSessionId、调用 MyBehavior 历史存储与 Native 短期/场景记录。只检查 Agent 可用不等于原会话仍有效；最后世界地图关窗 callback 也未绑定原会话。

本轮范围：将成功动作与后续历史派发合在同一次主线程队列消费中，动作前捕获 scene session 与非 Hero 长期记忆身份，动作后按原 Campaign/generation 写原目标的持久历史；短期显示状态、TTS 与延迟关窗仍绑定原会话/revision。合法 owner 结束会话不能被当作未执行动作而抹掉此前效果。

明确限制：MyBehavior 的既有 void 历史 owner 吞错/可能无 owner，本轮不能把方法返回命名为可靠持久化 receipt，更不承诺 AFEF 原子事务。新增公共提交仍不开。先复现后台/会话切换反例，再测真实收尾、正常/主动、Hero/非Hero、合法关闭、读档、重复回调与关窗隔离；保持原 Action core 和业务模块不变。

## 已落地（d7ab9610）

动作前捕获原 scene/party identity，主线程一次消费执行原 Action core 和收尾。持久历史派发与临时会话状态分开守卫；关窗使用出队 context/revision，不要求已结束 Task 仍占用后台 slot。动作 discard 的 pending 清理同步主线程并限制原上下文。删除旧后台 tail、两次存在间隙的目标检查和无用途 stopwatch，Action core 声明不变。

最终 102 检查 / 9 变异、原 88/9、44/7、46/6、ports 308/3、六项 Stage 和 16 组相关回归通过。审计：`docs/audits/2026-09-11-native-completion-verification.md` 及 JSON；简明版：`docs/handoffs/2026-09-11-native-completion-team-handoff.md`。

下一步：优先复用 `MyBehavior.CommitExternalDialogueHistory` / `MemoryCommitResult`，补支持原 scene session 的 Native 接线与接受语义；不能直接改成 detached 默认场景 -1，更不能把运行期接受当磁盘保存。之后继续更早 Native prepare 和失败分支 pending 清理、TTS 直接回调、Courier prepare。公共 V1 只读，未推送/部署/实机，自动化继续。
