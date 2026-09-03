# Bridge Binding Contract Tests

`validate_bridge_bindings.py` 校验阶段 8 的 16 组 Bridge 绑定清单：

- `docs/phase8/bridge-binding-manifest.json` 与 `full-domain-readiness-catalog.json` 的 domain、owner、topology、实现状态和 required cases 必须闭合；
- 每个 entry path 必须是项目内真实文件，symbol 必须在声明文件中存在；
- 拒绝绝对路径、路径遍历、生成物/缓存目录和终端 UI 文件；
- `wired` 只能用于已经审阅的十个安全入口，并要求源码出现对应的 `FeatureBridgeRuntime`/`FeatureBridgeIds` Gate；
- wired gate 通过 C# 注释/字符串屏蔽、真实方法声明和花括号配对提取方法体；同时固定 gate 必须早于凭据读取、网络、owner 回调、提交或玩法副作用。`conversation-siege` 还必须校验缓存字段初始化器中的 Bridge ID；
- `AnimusForge/ModuleData/FeatureBridges.json` 必须是严格的 schema/contract 配置，只能启用这十个已审阅入口；空数组表示显式全部关闭；
- 其余六个 Bridge 必须是 `declared-only`，不允许把设计登记冒充运行时接线；
- 频率禁止使用 `tick`、`per-frame` 或 `full-scan`。

工具只读元数据和源码，不加载 Bannerlord 程序集、不启动游戏、不读取存档、不调用网络或执行 Bridge。

```powershell
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py --json
python -B -m unittest discover -s .\tools\BridgeBindingContractTests -p 'test_*.py' -v
```

当前预期：`16` bindings、`10` wired、`6` declared-only、配置启用 `10`。

配置读取是运行时一次性、fail-closed 的 allow-list：文件缺失时使用代码内已审阅默认值；文件损坏、版本不符、
未知/重复/未接线 ID 会关闭全部 Bridge。此工具仍只读源码和元数据，不启动游戏、不读存档、不调用网络。
