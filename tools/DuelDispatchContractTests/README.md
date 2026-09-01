# Duel Dispatch Contract Tests

Pure `.NET 8` contract runner for `LOCAL-7-M2`. It does not load Bannerlord,
start a Mission, read a save, or execute a live Economy owner.

```powershell
G:\AFMOD\.dotnet-sdk\dotnet.exe run `
  --project .\tools\DuelDispatchContractTests\DuelDispatchContractTests.csproj `
  -c Release
```

The runner verifies that the committer's canonical request/action identity is
bound after request reservation, exact Duel Queue precedes Economy and legacy
dispatch, queued/started/rejected/unknown receipts stay non-successful,
duplicate and conflicting requests cannot redispatch, bogus request/action
bindings fail before every owner, Economy failures/exceptions terminalize an
unstarted Duel, Duel+Mood remains exact without hiding an uncertain Mood
effect, Courier is explicitly unsupported, and the public legacy constructor
retains the M1 `legacy-unbound` behavior.

Passing this runner is not live Campaign/Mission, old-save, Fourberie, Economy,
Memory/AFEF, death, stake, cancellation, or default-route evidence.
