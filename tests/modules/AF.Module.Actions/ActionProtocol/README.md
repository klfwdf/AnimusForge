# J09 Action protocol owner contract

使用真实 `LegacyActionTagCatalog`、`LegacyActionTagParser` 和
`ActionPlanIntegrityPolicy`，在纯 .NET 8 fixture 中验证三渠道共享的 detached
协议。网络、Bannerlord、游戏对象、存档和真实领域执行均不参与。

```powershell
python -B tests/modules/AF.Module.Actions/ActionProtocol/run.py
python -B tests/modules/AF.Module.Actions/ActionProtocol/run_mutations.py
```

覆盖有限 allowlist、平衡括号/RichText、raw/plan 顺序和参数一致性、未授权
标签、AFEF/CONTENT 非动作边界，以及超过 64 个标签时执行前整体拒绝。变异必须
编译成功并命中具名断言；路径或编译失败不算有效红例。
