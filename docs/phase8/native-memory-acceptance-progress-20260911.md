# Native 记忆接受结果续作（2026-09-11）

起点 `29ca75c9`，生产 `d7ab9610`；fetch 后同名远端 ahead 20 / behind 0。两份用户草稿、其他工作树保持。

问题：Native 主线程收尾仍用 MyBehavior void 外壳；owner 缺失、返回 false 或内部吞错时仍按正常回复结束。已有 `CommitExternalDialogueHistory` 能报告运行期接受结果，但只有 loose session (-1) 入口，不能直接拿来替代 Native 场景历史。

范围：将已有严格提交逻辑收敛为一个支持 sceneSessionId 的 internal owner 入口；保留原 public 六参签名和 -1 语义。Native 复用此入口并检查 MemoryCommitResult，非持久 NPC/真正空 payload 不伪造写入请求。失败阻止正常完成，不重放动作，不删除部分记录；动作已要求的绑定原会话关窗仍须保留。

Applied 仅表示原日记和最近历史 owner 在运行期接受；不代表 SyncData、磁盘、跨动作/记忆原子事务或新子 MOD API 已完成。Hero/非 Hero 资格、AFEF 格式、原底层 Append 实现、制作组业务和存档键不改。先用实际 void 外壳和 Native 完整 tail 复现，再运行 strict owner 与 Native 消费连通测试及原回归。

## 已落地（18f48678）

- 同一 internal scene-aware strict owner 复用原判断/写入算法；原 public 六参 facade 保留 -1 与 ABI。Native 检查明确运行期接受结果，失败使用原受保护 UI 通道提示，不自动重试/回滚部分记录。
- 必要关窗在记忆失败分支也保留，仍检查原 context/revision。非持久 NPC/真正空 payload 不构造虚假成功 receipt。
- 184/15 新验证、88/9、44/7、46/6、ports 308/3、六项 Stage、四 DLL 532 元数据及 MemoryCommitRecovery PASS。相关回归的两处 Patience 源码行号 fixture 漂移已逐项确认并复验，168 个绑定身份不变。
- 审计：`docs/audits/2026-09-11-native-memory-acceptance-verification.md` / JSON；短版：`docs/handoffs/2026-09-11-native-memory-acceptance-team-handoff.md`。

下一步：更早 Native prepare 和失败分支 pending 历史清理，然后 TTS 直接回调与 Courier 双向 prepare。单次运行期接受已接入，但仍无完整稳定请求/恢复与实机证据，不能开放新 Api.V1 写入、默认切换或扩大到制作组业务。未推送/部署，自动化继续。
