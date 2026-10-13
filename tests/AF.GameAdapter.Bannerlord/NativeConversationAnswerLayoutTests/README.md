# 原版选项占位与新 UI 隔离回归

`dotnet run --project tests/AF.GameAdapter.Bannerlord/NativeConversationAnswerLayoutTests/NativeConversationAnswerLayoutTests.csproj`

直接链接完整生产 `NativeConversationAnswerAreaController.cs`；Gauntlet Widget/Harmony/日志为明确托管替身，不运行游戏或 GPU。61项检查覆盖原版场景/地图的透明占位、交互禁用、NPC状态不动、迟到选项、常规恢复/强制恢复，以及新UI场景/地图保持原隐藏容器行为。新增卸载生命周期检查：精确movie身份、重复/外国卸载、保持其他活界面、整层ClearContext绕过ReleaseMovie，以及不再访问/持有已销毁树。Harmony接线不是替身测试的证据，须由双API构建和实机补足。

`-- --new-ui-only`运行新UI场景/地图加生命周期，共35项。可用`-p:ControllerSource=<历史真实源码路径>`编译同一夹具，对比修正前后STATE行；新UI必须完全一致。未修占位的历史控制器应在`native.slot-kept`失败；已修占位但未修卸载的版本应在`lifecycle.never-restore-unloaded-tree`失败，不能把编译失败视为负控通过。

原版输入定位/失焦回复另运行 NativeConversationInterruptionTests/run.py；本文件不覆盖该独立夹具。实际运行状态及未通过项登记唯一主台账。

回放只证明生产状态操作与隔离，不证明实际Gauntlet布局、Alpha继承或点击命中。实机仍须复测，不能再从局部HorizontalAlignment推出屏幕位置。
