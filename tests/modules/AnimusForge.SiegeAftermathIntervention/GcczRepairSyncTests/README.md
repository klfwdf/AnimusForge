# Reviewed GCCZ repair backport

This executable references the actual reusable core compiled into AF; it does not reproduce the algorithms in test code or simulate a running Bannerlord Mission.

```powershell
dotnet run --project tests/modules/AnimusForge.SiegeAftermathIntervention/GcczRepairSyncTests/GcczRepairSyncTests.csproj -c Release
```

- Capacity is reserved before staging a castle recruit group; unselected prisoners remain available.
- Exit morale scales recruitment unrest by actual joined recruits, while other unrest and earlier appeasement keep their existing rules.
- Colonization cannot overwrite another pending request or restart a sealed/committed operation.
- Switching an existing unlimited reply event to a finite setting immediately caps subsequent speakers.
- Native order deduplication and continuous-stall recovery cover command changes, slow movement, interrupted samples, native retry acknowledgement, cooldown, invalid geometry and agent removal.

`SiegeNativeMovementOrdersTests.cs` and `SiegeStuckRecoveryTests.cs` were taken from the committed standalone GCCZ test tree at `ede6529`, not its uncommitted test/UI work. AF integration adapts the older `AF-CULTURE-FIX` repairs `a815a25f`, `6a0256c1` and `2df565b3` onto current main instead of overwriting current files.

The local evidence under ignored `artifacts/gccz-reviewed-sync/` also builds isolated source mutations: restoring the full recruitment penalty fails `partial recruitment uses actual joined count`; reducing the recovery threshold to 0.9 seconds fails `recovery never triggers before seven continuous seconds`. Both negatives compile successfully before failing their named behavior assertions. Production source is never mutated by that evidence script.

Adjacent regression: run `tests/modules/AnimusForge.SiegeAftermathIntervention/TownRuleMemory.Tests/TownRuleMemory.Tests.csproj`. Main's newer bounded confirmed-event memory implementation is deliberately retained.

NOT_RUN: live roster changes, native Mission navigation/raycast/collision, old player saves, paid LLM/provider calls and real frame timings. Successful core tests or dual-version compilation do not establish those results.
