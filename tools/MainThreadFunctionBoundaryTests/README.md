# Shared main-thread function boundary tests

This suite extracts both production declarations from `ShoutBehavior.cs` and compiles them with the real `PreprocessFormatException`. Only the queue, thread identity and optional diagnostics are fixtures; no Bannerlord gameplay is emulated.

```powershell
G:\Python310\python.exe tools/MainThreadFunctionBoundaryTests/run.py
G:\Python310\python.exe tools/MainThreadFunctionBoundaryTests/run.py --original
G:\Python310\python.exe tools/MainThreadFunctionBoundaryTests/run.py --mutate expire-started
```

- Current: 132 checks. Baseline `613ac245`: exit 1 with actual runtime failures, not compilation failures.
- Production deadline stays 30,000 ms. Fixture deadline is 120 ms; a physical queue consumer is held beyond it for the claimed-operation case.
- Seven mutations: `drop-claim`, `expire-started`, `keep-expired-live`, `failed-publication-live`, `swallow-format`, `diagnostic-throws`, `forget-queued-result`. Every mutation must compile, exit 1 and print `FAIL`.
- Success/fallback/format-error identity, late/duplicate consumption, failed publication before/after claim, and diagnostic failures are tested. Ordinary operation exceptions intentionally retain the existing fallback policy.
- The scheduler inverse still proves the normalized whole host equals `613ac245`. The separately tested Native preparation declaration is first restored using its exact reviewed SHA from TeamModulePortParityTests; NativePreparationBoundaryTests independently proves that extraction against `50f84818`. Future unrelated changes still fail, and all 132 scheduler checks / 7 mutations remain.
- Generated fixtures/logs remain under ignored `.generated`. Test failures never count as live-game acceptance, and the fake queue's publication faults test defensive ownership, not a claim that `ConcurrentQueue` normally throws after publishing.
