# Module Catalog Contract Tests

这是阶段 3 的独立、按需运行的纯 metadata runner。它不属于 `AnimusForge.csproj`，不加载 Bannerlord DLL，不启动游戏，不读取存档，不调用网络。

## 运行

```powershell
python F:\AnimusForge-main\tools\ModuleCatalogContractTests\validate_module_catalog.py
python F:\AnimusForge-main\tools\ModuleCatalogContractTests\validate_module_catalog.py --json
```

默认读取：

`F:\AnimusForge-main\docs\fixtures\phase3-module-catalog\`

## 验证内容

- module ID、persistence namespace、contract version、owner 和 maintainer 唯一/完整性；
- required dependency 存在性和依赖环；
- profile include/exclude 和 required dependency closure；
- safe-mode 只保留 foundation 与 GameAdapter；
- bridge peer 数量和独立 persistence namespace；
- 1.3/1.4 API line closure；
- lifecycle 声明边界；
- health state、issue 数量和消息长度上限；
- 16 个无效输入场景的预期状态，包括 `Blocked`、`Degraded`、`Failed` 和 `RestartRequired`。

运行频率为 0，仅手动验证 catalog；runner 通过不代表生产模块已经实现或游戏已验收。