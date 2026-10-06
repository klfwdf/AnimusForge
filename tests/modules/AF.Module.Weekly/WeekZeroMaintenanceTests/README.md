# Week-zero daily maintenance regression

Run from the repository root:

```powershell
python tests/modules/AF.Module.Weekly/WeekZeroMaintenanceTests/run.py --out artifacts/week-zero-proof
```

Optional `--source-ref <commit>` runs the same assertions against historical source. The pre-fix checkpoint fails because opening maintenance calls the full archive sanitizer.

Optional `--records <decoded-event-records.json> --materials <decoded-source-materials.json> --opening <raw-opening.txt>` replays local decoded player data. These files are read-only inputs and must remain outside tracked test fixtures. `--opening` uses the exact original source text; when omitted the large-archive check derives an equivalent scenario-name variant from the saved opening.

The runner extracts the production opening upsert, synchronous and deferred maintenance, source hash, publication revision, async summary commit, DTOs, and sanitizers. It compiles them in a repository-local Release harness. Game kingdom enumeration, diagnostics and network admission are stubbed; no game/API/save writes occur.

Checks cover raw/normalized legacy bodies, successful old LLM hash markers, 30 repeated days, unchanged history object/material identity, true edits, late async responses, kingdom titles/labels, empty inputs, and key-only query parity including fallback keys. Timing excludes JSON loading and is offline .NET 8 evidence, not Bannerlord frame timing.
