# Native 主线程收尾验证（2026-09-11）

生产/测试：`d7ab9610`；检查点：`e49aabbd`；旧反例基线：`d9288faa`（生产 `9a5335be`）。

## 结果

- 新 **102 检查 / 9 行为变异 PASS**，执行真实 Native 动作调用到返回的完整 tail、真实 dispatch/上下文检查/收尾 partial。
- 原动作派发 **88 / 9 变异**、准入 **44 / 7**、展示 **46 / 6**、制作组 ports **308 / 3** 均通过。
- 最终 Debug/Release × 1.3/1.4/Bootstrap **六构建与 Stage PASS**；六 DLL 与 marker SHA 一致，四实现 DLL 的 V1 元数据 **472 断言 PASS**。
- 最终 16 组相关回归符合预期：管线/桥接隔离、四组生产程序集回放、绑定、存档身份、入口清单、Scene 后处理/队列、Channel、Courier owner、Native TTS fallback、公共 API 和缺失证据门禁。
- 168 个存档绑定不变。缺失证据示例仍按预期 BLOCKED / exit 2，不提升为实机验收。

## 实际复现和修改

旧源码 tail 在隔离队列中复现：主线程校验后，后台访问场景和写历史；在校验与使用之间改 generation/session，旧回复进入新 session。新链把 capture→原 Action core→历史派发/临时显示收尾放在一次主线程 claim 内，不再进行该后台写入。

动作前固定原 scene session、非 Hero party memory ID；动作后只在原 Campaign/generation 派发持久历史。owner 合法结束或改变场景时，原目标的已完成对话仍可交给历史 owner；TTS/短期显示与延迟关窗则要求原 context/revision。动作 discard 的 pending 玩家记录只在原上下文主线程上清理；旧 save/revision 不盲删新历史。

原 `ApplyNativeConversationGameActionsCore` 与基线完整声明相同，未改规则、数值、默认渠道开关或制作组业务。未新增第二套 LLM/动作/记忆实现。

## 验证方法与证据保留

游戏对象、availability provider、业务 Action core、MyBehavior 历史 owner、TTS 和场景记录器为明确 fixture；源码运行/物理线程检查、程序集回放、元数据及构建都不等于实机。

最后的输出在 `.tmp/native-completion-20260911/final`。之前的构建保留为 `build-*-before-discard-guard.log`，不拿旧产物证明最终源码。原缺陷反例独立保留在 `original.log`。动作套件的 return-null 变异曾暴露 fixture 直接解引用的 NRE，已恢复原 nullable fallback 后用“应有类型化失败”断言拒绝；未把异常编译/工具崩溃当 PASS。

同名 JSON 记录最终源码/产物/日志 SHA 与回归命令；工具 README 记录源码提取、stub 与变异边界。核心声明无改动，实际 Native post-dispatch tail 不再包含历史写入、TTS 或关窗入队。

## 清理与剩余

删除动作后两次“主线程检查→后台使用”的跳转、后台历史块及吞错 catch、未绑定关窗与无用途 stopwatch；保留原核心业务和既有兼容入口。旧套件接线只补“不传 completion payload”的明确 stub；完整 payload 由新套件执行，ports 反向全文对照不放宽。

**本轮没有可靠持久化 receipt。** MyBehavior 的旧 void 外壳可能吞错或无 owner，方法返回不是日记/AFEF/磁盘提交确认。下一步复用已有严格 MemoryCommitResult 语义，保留 Native scene session，再处理更早 prepare/失败清理。TTS 直接回调、Courier prepare、实机/旧存档仍待后续。历史 .NET 10 工具未运行。

只本地修改、验证、提交；未推送、未部署、未操作真实存档，用户两份草稿和其他工作树保留。自动化继续。
