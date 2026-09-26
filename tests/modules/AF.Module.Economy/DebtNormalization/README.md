# Economy debt normalization production test

This focused executable extracts the private production `DebtRecord` schema from
`RewardSystemBehavior.cs`, compiles it with the real
`EconomyDebtNormalizationPolicy` and `EconomyDebtSchedulePolicy`, and exercises
legacy migration, invalid-line removal, clamps, aggregate rebuilding, unlimited
due handling, note limits, idempotence, due windows, reminder cadence, and
finite/unlimited overdue penalty arithmetic. It also verifies that the pending
quest state and ten quest lifecycle methods have moved to the Economy Debt owner
while load/import/create/resolve consumers remain wired. The same source guard
checks that the debt schema, ledger API/mutations, and real `DailyTickEvent`
handler are owned by the Economy Debt files rather than duplicated in the root.

`run.py` verifies the live wrapper/caller counts and compiles two mutations that
break unlimited-debt due normalization or weekly reminder cadence. Each mutation
must reach and fail its named runtime assertion; compile errors are not accepted
as negative evidence.

```powershell
python -B tests/modules/AF.Module.Economy/DebtNormalization/run.py `
  --dotnet G:/AFMOD/.dotnet-sdk/dotnet.exe
```
