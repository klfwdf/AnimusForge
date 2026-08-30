# Bridge Fixture Contract Tests

这是一个独立、按需运行的纯 fixture runner，不属于 `AnimusForge.csproj`，不加载 Bannerlord DLL，不调用网络，不读取存档，也不执行 Bridge。

## 运行

```powershell
python F:\AnimusForge-main\tools\BridgeFixtureContractTests\validate_bridge_fixtures.py
python F:\AnimusForge-main\tools\BridgeFixtureContractTests\validate_bridge_fixtures.py --json
```

默认读取：

`F:\AnimusForge-main\docs\fixtures\phase2-settlement-policy-bridges\`

## 覆盖范围

- Settlement/Siege：A、B、A+B、A+B+Bridge、Bridge failure；
- Policy/Diplomacy：A、B、A+B、A+B+Bridge、Bridge failure；
- contract version、case ID 唯一性、canonical target plan、postprocess rule closure；
- 无 Bridge 时不得产生隐式跨域动作/通知；
- Bridge 成功时必须有主线程 apply、receipt 和 confirmed fact；
- stale generation / incompatible version 失败时不得产生跨域副作用；
- 结果输出有界，错误消息最多 240 字符。

该 runner 的运行频率为 0，仅在开发验证时人工调用。失败只表示 fixture/contract 不一致，不表示游戏运行时失败。