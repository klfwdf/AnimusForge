# Economy debt normalization production test

This focused executable extracts the private production `DebtRecord` schema from
`RewardSystemBehavior.cs`, compiles it with the real
`EconomyDebtNormalizationPolicy`, and exercises legacy migration, invalid-line
removal, clamps, aggregate rebuilding, unlimited due handling, note limits, and
idempotence.

`run.py` also verifies the three live wrapper calls and compiles a mutation that
breaks unlimited-debt due normalization. The mutation must reach and fail the
named runtime assertion; compile errors are not accepted as negative evidence.

```powershell
python -B tests/modules/AF.Module.Economy/DebtNormalization/run.py `
  --dotnet G:/AFMOD/.dotnet-sdk/dotnet.exe
```
