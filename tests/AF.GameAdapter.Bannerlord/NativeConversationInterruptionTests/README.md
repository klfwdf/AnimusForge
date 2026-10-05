# 原版输入位置与失焦暂停显示回写回归

运行：`python -B tests/AF.GameAdapter.Bannerlord/NativeConversationInterruptionTests/run.py`。

测试编译当前生产的 `AnimusForgeNativeConversationOverlay.Interruption.cs`、`Presentation.cs`、完整 `ConversationHelper.cs`；从当前 overlay 提取真实 ApplicationTick、临时界面判定/隐藏/恢复、128预算队列、恢复窗口、焦点、代次和关闭方法。屏幕/暂停菜单、NPC/token/save状态及原生layer为明确替身，不运行真实Game/Campaign/provider，也不制造新的网络链路。

覆盖30项：工作线程仅排队，暂停时显示完成/停止等待点/清busy，暂停不抢焦点或弹窗，恢复和晚一帧VM重绑重绘，重复焦点转场，部分流式继续，换NPC/代次/关闭/普通模式不串写，TTS打字机保留，Inquiry焦点，128预算及延后重试失效。原版输入XML定位/绑定及5种宽高比只做参数投影，不能称为原生渲染或真实鼠标命中验收。

四个编译成功的负控：`--mutation skip-paused-drain`、`skip-resume-repaint`、`steal-focus`、`allow-stale-text`；每个须在对应运行断言失败，不把编译/路径错误当作红例。运行输出与源SHA manifest保存在每次独立 `artifacts/tests/native-focus-pause*/run-*/`；不删除其他测试产物，不写游戏目录。

设计边界：失焦暂停设置保持；网络请求仍走原前/主/后链路，所有UI与游戏对象处理仍归主线程。只重放当前NPC/token/save/模式/请求代次内的UI文字，不重放动作或记忆。重绘复用原8帧恢复窗口，正文相同不触发NPC查询；交互只在恢复时一次执行。实机Alt-Tab、原生渲染、真实LLM/TTS/玩家旧档仍须另验。
