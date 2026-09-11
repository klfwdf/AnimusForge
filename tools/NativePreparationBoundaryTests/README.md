# Native initial preparation tests

```powershell
G:\Python310\python.exe tools/NativePreparationBoundaryTests/run.py
G:\Python310\python.exe tools/NativePreparationBoundaryTests/run.py --original
G:\Python310\python.exe tools/NativePreparationBoundaryTests/run.py --mutate lose-rules
```

Current: 589 checks. Baseline `50f84818`: 52 runtime failures. Five mutants must compile and fail behavioral assertions: `drop-guard`, `move-capture-background`, `lose-culture`, `lose-rules`, `wrong-guide-offset`.

The fixture executes the actual extracted entry slice, private capture method and shared main-thread runner. It compares 48 healthy combinations against the original preparation executed on the physical main thread, checking payload and recording-helper call order/arguments. Direct and queued paths, stale-before-call, retired-before-consumption and queue timeout are included.

Game helpers and admission validity are recording stubs, not real Bannerlord behavior. Existing admission tests separately cover the real lifecycle guard. Preparation source-body inverse equality protects every unchanged surrounding line, and the copied builder statements must equal the original exactly. There is no real API call or save access.

The preparation container is private and still reuses existing LocationCharacter/Location-bearing target types; it is not a public thread-safe immutable DTO. Generated artifacts belong under ignored `.generated`.

Later persistent-history changes are normalized only through NativeHistorySnapshotTests/source_parity.py (exact SHA plus independent behavior proof); the original preparation and whole-owner assertions remain.
