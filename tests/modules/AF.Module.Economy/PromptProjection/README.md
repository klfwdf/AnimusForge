# Economy prompt projection tests

This package executes the production detached formatter and verifies that
`RewardSystemBehavior` captures live debt/trust state before delegating to it.
It does not initialize Bannerlord, mutate a save, or prove live prices.

```powershell
python -X utf8 -B tests/modules/AF.Module.Economy/PromptProjection/run.py --dotnet G:/AFMOD/.dotnet-sdk/dotnet.exe
```

The default run also compiles two mutations: one changes the merchant debt
confirmation marker and one changes trust level rounding. Each mutation must
reach its named runtime assertion and fail; compilation failure is not accepted
as negative evidence.
