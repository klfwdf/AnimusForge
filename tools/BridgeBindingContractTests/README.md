# Bridge Binding Contract Tests

`validate_bridge_bindings.py` 校验阶段 8 的 16 组 Bridge 绑定清单：

- `docs/phase8/bridge-binding-manifest.json` 与 `full-domain-readiness-catalog.json` 的 domain、owner、topology、实现状态和 required cases 必须闭合；
- 每个 entry path 必须是项目内真实文件，symbol 必须在声明文件中存在；
- 拒绝绝对路径、路径遍历、生成物/缓存目录和终端 UI 文件；
- `wired` 只能用于已经审阅的十二个安全入口，并要求源码出现对应的 `FeatureBridgeRuntime`/`FeatureBridgeIds` Gate；
- wired gate 通过 C# 注释/字符串屏蔽、真实方法声明和花括号配对提取方法体；同时固定 gate 必须早于凭据读取、网络、owner 回调、提交或玩法副作用。`conversation-siege` 还必须校验缓存字段初始化器中的 Bridge ID；
- `AnimusForge/ModuleData/FeatureBridges.json` 必须是严格的 schema/contract 配置，只能启用这十二个已审阅入口；空数组表示显式全部关闭；
- 其余四个 Bridge 必须是 `declared-only`，不允许把设计登记冒充运行时接线；
- 频率禁止使用 `tick`、`per-frame` 或 `full-scan`。

工具只读元数据和源码，不加载 Bannerlord 程序集、不启动游戏、不读取存档、不调用网络或执行 Bridge。

```powershell
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py
python -B .\tools\BridgeBindingContractTests\validate_bridge_bindings.py --json
python -B -m unittest discover -s .\tools\BridgeBindingContractTests -p 'test_*.py' -v
```

当前预期：`16` bindings、`12` wired、`4` declared-only、配置启用 `12`。

配置读取是运行时一次性、fail-closed 的 allow-list：文件缺失时使用代码内已审阅默认值；文件损坏、版本不符、
未知/重复/未接线 ID 会关闭全部 Bridge。此工具仍只读源码和元数据，不启动游戏、不读存档、不调用网络。

## Mandatory safety 与可选 Bridge 分开

`runtime-game-adapter` 保留在 canonical 16 组责任矩阵中，但没有已接线的可选调用入口，因此 runtimeBinding 为 `declared-only`，配置不启用，默认值为 false，未使用的便捷属性已移除。其它 12 个入口保持原配置和行为。

`InteractionComponentSafePatch` 是独立的 mandatory 安全补丁，仍由 `Patch_TriggerMassiveHook.EnsurePatchGroupIfDue` 安装，不读取 FeatureBridge 开关。本次清理无效开关**不是关闭安全补丁**。校验器验证该安装关系，并拒绝重新以可选 Gate 包裹安全补丁或删除安装调用的变异。

23 个元数据/源码测试保持原审计失败不变，仅对当前契约重新验证；额外覆盖无效配置、虚报 wired、可选开关误关安全补丁、漏安装。`BridgeRuntimeIsolationTests` 的 12 次隔离进程场景覆盖缺省、全部关闭、非法/profile 配置、无效 ID、默认不启用该记录，同时保留 16 definitions。此处的 PASS 不代表实机反射/Harmony 验收。
