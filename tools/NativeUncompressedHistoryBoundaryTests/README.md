# Native 未压缩历史线程边界

运行 `python tools/NativeUncompressedHistoryBoundaryTests/run.py`；`DOTNET_EXE` 可指定已安装 SDK 启动器。测试提取真实 Native 调用点、原 Hero/普通人物 bridge、主动开场去重和既有主线程调度，游戏 memory owner 用记录线程的 fixture。

69 项覆盖 Hero/普通人物 × 主动开场/玩家输入 × 有/无会话、GCCZ 记忆资格、Mission fallback、AFEF、去重、排队过期、generation/会话失效及暂存输入回滚。原 `53ddb7d4` 用 `--original` 执行相同测试，13 项失败。

`--mutate skip-admission`、`--mutate always-include`、`--mutate skip-rollback` 分别触发 4、4、3 项失败。`source_parity.py` 只接受这一处已验证调用点的 hash；定向恢复后必须与旧完整 Shout 源码一致，用于旧边界验证。

不声明所有 Native 输入已完成。角色上下文、persona、规则/lore、后处理准备和部分 TTS 调用仍需后续处理；真实游戏、实际 provider、存档和耗时未验证。
