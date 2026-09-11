# 共用主线程函数闭环（2026-09-11）

- 基线 `613ac245`；检查点 `84d7097b`；生产/测试 `5bf830f3`。
- 已完成：queued/claimed/retired CAS；仅未开始过期；日志/错误格式化隔离；direct/queued 格式异常一致；普通异常 fallback 保留。
- 已清理：旧 bool、旧 wait catch-all、重复两套执行处理。25 个调用点保持原样；无新队列或 Tick。
- 验证：132 / 7 变异；Native 44/46/88/184/111、ports 308/3；六项最终 Stage、16 组相关回归、实际四 DLL 532 元数据。详见 `docs/audits/2026-09-11-mainthread-function-verification.md`。
- 当前只做本地提交，不推送/部署/真实存档访问；两份用户草稿未纳入。

## 下一条可执行路径

1. 审查 `SubmitNativeConversationTextInternalAsync` 更早的 prepare：`persistedHeroHistoryTask` 仍用 Task.Run 调实际历史构造，`BuildShoutPromptContextForExternal` 同时涉及人设/规则/检索。先区分游戏读取与纯计算/网络，不能整段搬上主线程，也不能删减 prompt。
2. 需要修改其他 ShoutBehavior 声明时，为本轮 whole-host inverse 测试提供新的独立审查证据，不直接删除断言；Team ports 精确非 receiver 白名单也需更新。
3. 继续 Native TTS 引擎直接回调、Courier 双向 prepare 和完整生命周期证据后，再评估有限公共普通文本提交。

## 不能宣称

普通 fallback 不代表跨动作原子性；started 不代表可取消；Applied 不代表磁盘存档；离线 PASS 不代表实机 PASS。公共 Api.V1 继续只读，整个阶段 8 未完成。
