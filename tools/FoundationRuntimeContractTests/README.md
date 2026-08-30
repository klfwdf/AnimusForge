# Foundation Runtime Contract Tests

独立、按需运行的阶段 3 Foundation metadata runner。不属于 `AnimusForge.csproj`，不加载 Bannerlord DLL，不读取存档，不调用网络，也不执行主线程任务。

## 运行

```powershell
python F:\AnimusForge-main\tools\FoundationRuntimeContractTests\validate_foundation_runtime.py
python F:\AnimusForge-main\tools\FoundationRuntimeContractTests\validate_foundation_runtime.py --json
```

默认读取：

`F:\AnimusForge-main\docs\fixtures\phase3-foundation-runtime\`

覆盖：

- 主线程 dispatch 与后台 snapshot DTO；
- generation、stale、cancel、timeout、failure 状态边界；
- SafeMode 数据保留和 gameplay 排除；
- diagnostics/health 有界输出；
- live object、delegate、raw dictionary、无界 payload 拒绝；
- 1.3/1.4 API line closure。

通过只表示 contract fixture 一致，不表示 Foundation 生产实现已创建。