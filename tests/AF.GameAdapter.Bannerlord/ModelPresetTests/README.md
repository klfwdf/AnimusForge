# 模型预设回归

在仓库根目录运行：

```powershell
python tests/AF.GameAdapter.Bannerlord/ModelPresetTests/run.py
python tests/AF.GameAdapter.Bannerlord/ModelPresetTests/run.py --mcm .tmp/build_check/1.3/MCMv5.dll
python tests/AF.GameAdapter.Bannerlord/ModelPresetTests/run.py --baseline d3fde95d4
```

使用当前 DuelSettings / TownAmbientSettings 的原始模型属性、缓存和解析方法，以及 McmDropdownRuntimeRefresh 的真实前缀；不修改这些方法实现。路径和日志为隔离替身，属性读写顺序重现 MCM BaseSettingsJsonConverter。调用引用快照中的真实 DropdownJsonConverter，并在 .NET Framework 4.7.2 中给真实 SetSelectedIndexAction 构造器安装 Harmony 前缀，执行 DoAction / UndoAction。

前置条件：原统一构建已经准备引用快照、Coup.RuntimeProbe 的 project.assets.json 及 net472 引用程序集包；引用文件只读，不下载或修改游戏配置。输出全部位于新建的 `artifacts/tests/model-presets/run-*`。net8 不适用于本机游戏 Harmony 的动态 IL 实现。

覆盖：五模型文本与原地选择一致、保存/加载、缓存列表重排、默认与手动模式、共享缓存只读、重复 getter、终端等价选择、跨列表预设复制、实际 MCM 动作与撤销、失效索引。基线模式保留修前代码并使用空前缀，应以行为断言失败。

不覆盖：完整 MCM provider/UI、游戏渲染、真实玩家配置、API 请求。已经保存为错误模型名的旧预设无法可靠反推原选择，需要玩家重新选择并保存一次。
