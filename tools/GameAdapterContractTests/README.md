# GameAdapter Contract Tests

阶段 3 的独立、按需运行的 GameAdapter metadata runner。不属于 `AnimusForge.csproj`，不加载 Bannerlord DLL，不执行反射，不调用网络或存档。

## 运行

```powershell
python F:\AnimusForge-main\tools\GameAdapterContractTests\validate_game_adapter.py
python F:\AnimusForge-main\tools\GameAdapterContractTests\validate_game_adapter.py --json
```

验证 1.3/1.4 helper、参数/签名差异、missing member、unknown version、Bootstrap marker、反射缓存、主线程 apply、SubModule.xml 单 Bootstrap 和双实现选择边界。

通过只表示设计 fixture 闭合，不表示生产 GameAdapter 已实现或双版本运行时已验收。