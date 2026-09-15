# Memory summary run ownership proof

Production path: `MyBehavior.ProcessMemorySummaryQueueAsync` obtains one `MemorySummaryRunOwner.Lease`; the same token crosses planning, capture, retry/waves, parse, each Apply/Mark and notification. Reset removes authority; disposal compares the lease, so an old run cannot release its replacement. No persisted identity, HTTP cancellation, extra queue, Prompt or algorithm is introduced.

## Reproduce

Set `DOTNET_EXE` to an installed SDK runtime; use Python 3.10+ from the repository root.

```text
python tools/MemorySummaryRunOwnerTests/run.py
python tools/MemorySummaryMainThreadBoundaryTests/run_business.py --run-scope-cases
python tools/MemorySummaryMainThreadBoundaryTests/run_business.py --run-scope-cases --run-owner-baseline
python tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py --run-scope-cases
python tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py --run-scope-cases --run-mutate release-replacement
python tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py --run-scope-cases --run-mutate ignore-run-authority
python tools/MemorySummaryRunOwnerTests/source_parity.py
```

- Owner: 47 checks including concurrent admission. Three primitive faults execute and fail.
- Business: original 36 cases plus three same-generation replacement cases. Reviewed `155f1b7a` baseline keeps 36 original passes but fails all three new cases. Current 39 pass.
- Terminal: 85 existing writer/capture/parse/Apply/Mark/recovery scenarios plus 10 same-generation retirement scenarios for daily, major and overview, failed requests, retries and a result parsed before reset. Current 95 pass. Both run-owner mutations compile and fail the new cases (the release-replacement fault has direct authority failures, not a tool timeout).
- Test-only old busy assertions are explicitly conditional: retired old instances may now release their own runtime-only lease; they may not release a replacement. Existing data, notification and write checks remain. The old `worker-release` mutation is retired because lease disposal is intentionally engine-free and safe on workers; actual write dispatch mutants remain.
- `source-review.json` contains precise reviewed hunks against `155f1b7a`. Its inverse validates the live source and preserves previous proof hashes instead of blindly refreshing their baselines. Windows separators are normalized before lookup.

The business fixture scripts provider completion; terminal/captured tests execute actual request/source/parser/writer methods with controlled game/provider/clock seams. These are not Bannerlord LIVE/SAVE or hard memory/time budget acceptance. Character copying, raw fingerprint and final atomic validation remain separately tracked performance work.
