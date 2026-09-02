# Bridge Binding Contract Tests

`validate_bridge_bindings.py` 校验阶段 8 的 16 组 Bridge 绑定清单：

- `docs/phase8/bridge-binding-manifest.json` 与 `full-domain-readiness-catalog.json` 的 domain、owner、topology、实现状态和 required cases 必须闭合；
- 每个 entry path 必须是项目内真实文件，symbol 必须在声明文件中存在；
- 拒绝绝对路径、路径遍历、生成物/缓存目录和终端 UI 文件；
- `wired` 只能用于已经审阅的三个安全入口，并要求源码出现明确的 `FeatureBridgeRuntime`/`FeatureBridgeIds` Gate；
- 其余 Bridge 必须是 `declared-only`，不允许把设计登记冒充运行时接线；
- 频率禁止使用 `tick`、`per-frame` 或 `full-scan`。

工具只读元数据和源码，不加载 Bannerlord 程序集、不启动游戏、不读取存档、不调用网络或执行 Bridge。

```powershell
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py --json
```

当前预期：`16` bindings、`3` wired、`13` declared-only。
