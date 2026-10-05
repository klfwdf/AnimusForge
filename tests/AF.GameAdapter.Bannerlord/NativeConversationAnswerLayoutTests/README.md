# 原版选项占位与新 UI 隔离回归

`dotnet run --project tests/AF.GameAdapter.Bannerlord/NativeConversationAnswerLayoutTests/NativeConversationAnswerLayoutTests.csproj`

直接链接完整生产 `NativeConversationAnswerAreaController.cs`；Gauntlet Widget/Harmony/日志为明确托管替身，不运行游戏或 GPU。50项检查覆盖原版场景/地图的透明占位、交互禁用、NPC状态不动、迟到选项、常规恢复/强制恢复，以及新UI场景/地图保持原隐藏容器行为；原版输入定位/失焦回复另运行 NativeConversationInterruptionTests/run.py。

`-- --new-ui-only`只运行新UI场景/地图。可用`-p:ControllerSource=<历史真实源码路径>`编译同一夹具，对比修正前后STATE行；新UI必须完全一致。历史原版控制器的完整检查应在`native.slot-kept`失败，不能把编译失败视为负控通过。

回放只证明生产状态操作与隔离，不证明实际Gauntlet布局、Alpha继承或点击命中。实机仍须复测，不能再从局部HorizontalAlignment推出屏幕位置。
