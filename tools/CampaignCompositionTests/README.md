# Campaign composition extraction checks

Production responsibility: `SubModule.InitializeGameStarter` → `ModuleFrameworkRuntime.RegisterCampaign` → `CampaignComposition.Register` → `CampaignModelComposition.Register`, followed by the original ordered behavior constructors. Team directory registration uses the existing `InternalModuleDirectory` via `TeamModuleRegistration`.

Run from the repository root:

```powershell
python -X utf8 -B tools/CampaignCompositionTests/run.py --dotnet <dotnet-sdk-executable>
python -X utf8 -B tools/CampaignCompositionTests/run.py --source-only
```

- Pinned pre-extraction source: `61d578926329ace61bf6b6ae43e12bf7d89b4696`.
- The `SubModule` check is scoped to the live Campaign delegate, partial-start cleanup and failure propagation so unrelated lifecycle fixture hashes cannot block composition. The runtime owner still uses an exact inverse; four model helper bodies, model/behavior ordering and bridge binding policy are compared to old source.
- The executable compiles real composition/runtime/catalog/API source. Only engine types, model implementations, behavior constructors, team services and feature gates are test doubles. The actual old and new engine entry methods are extracted; the old model methods remain in generated test code only.
- Checks cover 36 ordered behavior registrations, four models before behaviors, default/last non-AF inner selection, fresh instances per Campaign and repeated callback, null/other starter, constructor/registration failures, and directory states not becoming a new gameplay gate.
- Five mutations must compile and fail behavioral assertions: missing behavior, reordered models, discarded custom inner, added directory gate, abort on model error. Compilation failure is not a valid mutation result.
- Existing V1/API tests independently prove public surface/lifecycle/parallel reads, with `--artifact-root` checking real dual-version DLL metadata. No new public execution capability is declared.
- **Not live-game validation:** engine constructor side effects, real save loading, Campaign/Mission cleanup and player outcomes remain outside this harness.

`TeamModulePortParityTests` independently verifies every current typed-port call expression and the module framework lifecycle. Snapshot boundary evolution keeps its own exact runtime inverse; no suite refreshes unrelated fixture hashes to hide drift.

Generated fixtures/logs stay under ignored `.generated/`; no reference implementation enters production compilation.
