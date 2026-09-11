# Native 前置历史 / 清理验证（2026-09-11）

代码/测试：`128e9842`；检查点：`1547460a`；原反例：`5847a195`。

## 本轮结果

前置显示名、tentative 玩家输入、pending AFEF 消费和 Native 消息构造移到一次受准入约束的主线程队列消费。history key 固定一次，私有 append/snapshot/renderer 显式接收该 key。五个拒绝分支 await 原上下文清理，action discard 共用同一实现；不在后台重新解析 party key、不使用新的 CurrentInstance，只删除 player/user，不删事实。

history 队列采用 claim/expiry：未开始超时明确失败，不能成功返回空 fallback；已开始等待真实结果。失败发布/重复 callback/诊断异常不允许晚到补写或补删。

## 验证

| 检查 | 结果 |
|---|---|
| 新前置历史 / 真实五个拒绝分支 | 111 检查 / 12 行为变异 PASS |
| 原完整收尾 / strict memory | 184 / 15 变异 PASS |
| 原动作 / 准入 / 展示 | 88/9、44/7、46/6 PASS |
| 制作组 ports | 13 方法 / 31 调用 / 308 断言 / 3 变异 PASS |
| Debug/Release × 1.3/1.4/Bootstrap | 六构建、Stage 与 marker SHA PASS |
| 四实现 DLL PE / 公共 V1 | 532 元数据、119 API、256 并发、预期 CS0122 PASS |
| 16 组相关回归 | 全部符合预期；168 存档绑定不变，缺失证据仍 BLOCKED / exit 2 |

实际旧代码重现了后台历史访问、party key 改变时 Native/Scene 清理不一致、加载后 event sequence 复用导致误删新记录。测试执行实际 append、snapshot、clone、message renderer、AFEF consume 与两处集合清理；游戏 provider/场景 append bridge/窗口配置为明确 fixture。仅对 fixture capture 返回类型映射未准备空值，不改变实际 Native 判定。

三个私有 helper 去掉新增 capturedHistoryKey 参数/转发/默认表达式后，完整声明与旧版本相同；Action core 原样。没有新增历史格式/LLM/持久记忆实现，也没有改变默认渠道开关。

## 证据与清理

最终日志在 `.tmp/native-pending-history-20260911`。原反例、先前构建、最终构建分开保存；`build-*-before-explicit-timeout.log` 不是最终证据。诊断变异曾从测试 pump 直接逃逸，现 fixture 将整段消费包装为显式运行时 FAIL，未把编译错误或无标记崩溃算变异通过。最终 12 个反例全部按运行时断言被捕获。

删除旧后台准备块、五个旧 rollback 调用和重复 action-discard 包装，替换掉重新定位当前 key/owner 的清理逻辑。移除“全角色/事实同序号一起删”的宽清理；保留原默认 helper 行为。新 helper 均有实际调用，生成文件不提交。

## 未完成

此处不是整个 Native prepare 已完成：更早人设/规则/持久记忆游戏读取、通用 RunNativeConversationMainThreadFuncAsync 的 bool timeout 竞态、TTS 直接回调、Courier prepare 继续后续。新的 tentative snapshot 不等于持久化/恢复 receipt。

未实机/旧存档验收、未推送/部署、未修改其他工作树，两份用户草稿保留。公共 Api.V1 仍只读。源码/DLL/日志 SHA 与回归命令见同名 JSON；历史 .NET 10 工具没有运行。自动化继续。
