# Native 记忆接受结果续作（2026-09-11）

起点 `29ca75c9`，生产 `d7ab9610`；fetch 后同名远端 ahead 20 / behind 0。两份用户草稿、其他工作树保持。

问题：Native 主线程收尾仍用 MyBehavior void 外壳；owner 缺失、返回 false 或内部吞错时仍按正常回复结束。已有 `CommitExternalDialogueHistory` 能报告运行期接受结果，但只有 loose session (-1) 入口，不能直接拿来替代 Native 场景历史。

范围：将已有严格提交逻辑收敛为一个支持 sceneSessionId 的 internal owner 入口；保留原 public 六参签名和 -1 语义。Native 复用此入口并检查 MemoryCommitResult，非持久 NPC/真正空 payload 不伪造写入请求。失败阻止正常完成，不重放动作，不删除部分记录；动作已要求的绑定原会话关窗仍须保留。

Applied 仅表示原日记和最近历史 owner 在运行期接受；不代表 SyncData、磁盘、跨动作/记忆原子事务或新子 MOD API 已完成。Hero/非 Hero 资格、AFEF 格式、原底层 Append 实现、制作组业务和存档键不改。先用实际 void 外壳和 Native 完整 tail 复现，再运行 strict owner 与 Native 消费连通测试及原回归。
