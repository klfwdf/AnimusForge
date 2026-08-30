# Composition Matrix Contract Tests

这是阶段 3 的独立纯组合 runner，不属于 `AnimusForge.csproj`，不加载 Bannerlord DLL，不读取存档，不调用网络，也不执行真实模块/Bridge。

## 运行

```powershell
python F:\AnimusForge-main\tools\CompositionMatrixContractTests\validate_composition_matrix.py
python F:\AnimusForge-main\tools\CompositionMatrixContractTests\validate_composition_matrix.py --json
```

默认读取：

`F:\AnimusForge-main\docs\fixtures\phase3-composition-matrix\`

覆盖 18 个组合案例和 24 个不变量：

- no-op module；
- required dependency/provider missing；
- optional provider missing + fallback；
- incompatible contract；
- SafeMode；
- stale completion；
- partial start failure cleanup；
- Bridge failure/disabled；
- runtime-toggle/Harmony 冲突；
- bounded health/diagnostic output。

运行频率为 0，仅在开发验证时手动执行。通过只表示设计 fixture 的组合语义闭合，不表示生产 Module Host/Foundation 已实现。