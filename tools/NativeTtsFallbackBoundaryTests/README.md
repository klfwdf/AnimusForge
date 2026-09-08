# Native TTS fallback 主线程与请求边界

直接提取当前生产 `ScheduleNativeConversationTypewriterPlaybackFallback`、TTS owner/lifetime、Native wait 注册/判定以及真实 `TtsEngine.PlaybackRequest`。游戏环境仅提供 generation、Mission、受控主线程队列和 typewriter 边界 fixture，不加载 Bannerlord 或作者原 TTS 测试框架。

## 确认问题与修复边界

旧延迟任务在后台先检查 A 的 wait token，再调用全局 typewriter。在两步之间注册同 AgentIndex（包含 -1）的 B，A 仍能释放 B。独立精确方法重演得到 `old=A(token 1), current=B(token 2), releasedByOldCallback=true`。

生产修复仅变更一个 fallback 方法：冻结 owner/request，保留原等待时长；到时只排主线程，消费时核对当前 owner、完整 generation/Mission/scene-session/epoch 和 wait token。锁顺序是 owner bubble → native wait，最终检查与释放属于同一临界区。正常 fallback 不完成 Native wait，仍只启动文字显示。

## 运行

```powershell
python -B tools/NativeTtsFallbackBoundaryTests/run.py
python -B tools/NativeTtsFallbackBoundaryTests/run.py --schedule-source-ref 5ce8767a --output-name red-5ce8767a
python -B tools/NativeTtsFallbackBoundaryTests/run.py --mutate drop-lifetime --output-name mutant-drop-lifetime
python -B tools/NativeTtsFallbackBoundaryTests/run.py --mutate drop-instance --output-name mutant-drop-instance
python -B tools/NativeTtsFallbackBoundaryTests/run.py --mutate drop-atomic-wait --output-name mutant-drop-atomic-wait
```

- 绿色：14 PASS / 0 FAIL。
- 红色：旧 **schedule 方法**配合当前 request-owner/wait 依赖，3 PASS / 11 FAIL；这是单方法历史比较，不冒充整个旧版本实机回放。
- 变异拒绝：删除 lifetime、删除当前 owner、删除原子 wait 锁，分别 4 / 2 / 1 个断言失败，必须 exit 1，不能将编译失败算检测成功。
- 14 场景：正常、重复、同 index B、取消、读档 generation、Mission、scene-session、epoch、替换/null owner、错误 token、缺 wait、typewriter 异常、检查/执行间竞争注册 B。

最后一例在真实 getter/check 边界启动竞争注册任务：当前方法的 native wait 锁必须让 B 等待 A 释放后注册；去锁变异将得到 A 释放 B。fixture 用 1ms delay 与受控队列缩短等待，不改生产 delay 公式。测试中的竞争等待只存在 fixture，不在生产方法中增加同步等待。

结果与生成物在 `.generated/<output-name>/run.log`，带被测 schedule 的 SHA256。`TtsPlaybackOwner.Prepared` 在本窄测试未赋值产生 CS0649；它来自原生产类型提取，不影响行为断言。仍未验证真实音频、Rhubarb、winmm、实际 UI 焦点与 Bannerlord mission 生命周期。
