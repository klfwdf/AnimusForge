# AF.Contracts Contract Tests

这是阶段 3 的独立 metadata runner，不属于 `AnimusForge.csproj`，不加载 Bannerlord DLL，不读取存档，不调用网络，也不执行 capability/event。

## 运行

```powershell
python F:\AnimusForge-main\tools\AFContractsContractTests\validate_af_contracts.py
python F:\AnimusForge-main\tools\AFContractsContractTests\validate_af_contracts.py --json
```

默认读取：

`F:\AnimusForge-main\docs\fixtures\phase3-af-contracts\`

## 覆盖内容

- 9 个设计-only contract、3 个 typed event、6 个 capability；
- contract/event/capability ID 唯一性；
- event payload contract 绑定和 immutable；
- 1.3/1.4 API line closure；
- live Bannerlord object、raw dictionary、dynamic/object、delegate/MethodInfo 字段拒绝；
- optional/required version、stale event、SafeMode gameplay event 等 18 个无效场景；
- bounded field/health 输出和 runtime frequency=0。

runner 通过只代表 catalog/fixture 设计一致，不代表 `AF.Contracts` 生产类型已实现。