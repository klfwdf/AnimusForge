# Campaign composition extraction checks

Production responsibility: `SubModule.InitializeGameStarter` → `ModuleFrameworkRuntime.RegisterCampaign` → `CampaignComposition.Register` → `CampaignModelComposition.Register`, followed by the original ordered behavior constructors. Team directory registration uses the existing `InternalModuleDirectory` via `TeamModuleRegistration`.

Run from the repository root:

```powershell
python -X utf8 -B tools/CampaignCompositionTests/run.py --dotnet <dotnet-sdk-executable>
python -X utf8 -B tools/CampaignCompositionTests/run.py --source-only
```

- Pinned pre-extraction source: `61d578926329ace61bf6b6ae43e12bf7d89b4696`.
- Whole-file inverse guards preserve all unrelated `SubModule` and runtime code, not just selected hashes. Four model helper bodies, model/behavior ordering and bridge binding policy are compared to old source.
- The executable compiles real composition/runtime/catalog/API source. Only engine types, model implementations, behavior constructors, team services and feature gates are test doubles. The actual old and new engine entry methods are extracted; the old model methods remain in generated test code only.
- Checks cover 36 ordered behavior registrations, four models before behaviors, default/last non-AF inner selection, fresh instances per Campaign and repeated callback, null/other starter, constructor/registration failures, and directory states not becoming a new gameplay gate.
- Five mutations must compile and fail behavioral assertions: missing behavior, reordered models, discarded custom inner, added directory gate, abort on model error. Compilation failure is not a valid mutation result.
- Existing V1/API tests independently prove public surface/lifecycle/parallel reads, with `--artifact-root` checking real dual-version DLL metadata. No new public execution capability is declared.
- **Not live-game validation:** engine constructor side effects, real save loading, Campaign/Mission cleanup and player outcomes remain outside this harness.

`TeamModulePortParityTests` retains its original full-owner gate and now composes this verified inverse before its historical load/unload proof. Its older Native/Memory declaration table currently fails on `ProcessMemorySummaryQueueAsync` already absent at the pinned baseline; this extraction does not waive that failure. Direct port-execution and signature checks can pass independently but are not a full-suite PASS.

Generated fixtures/logs stay under ignored `.generated/`; no reference implementation enters production compilation.
