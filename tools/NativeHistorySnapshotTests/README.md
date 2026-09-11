# Native history input snapshot tests

```powershell
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --native
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --original
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --native --original
G:\Python310\python.exe tools/NativeHistorySnapshotTests/run.py --mutate reuse-afef-list
```

Memory: 852 checks / 120 healthy combinations, including duplicated IDs, missing titles, null blocks and AFEF lists. Native: 27 checks, actual capture/start/accept statements. The historical `659bb998` methods are compiled alongside the candidate for exact rendered text, query, API-prompt and branch-count comparison; owner mutation follows capture.

Eight memory mutants: `reuse-owner-blocks`, `reuse-afef-list`, `lose-summary`, `ignore-generation`, `ignore-owner`, `live-scene`, `live-date`, `live-query`. Two Native mutants (add `--native`): `drop-capture-guard`, `drop-accept-guard`. Require runtime FAIL and nonzero exit, never compiler failure.

Real production render/recall/select methods are extracted; owner data, game APIs, ONNX vectors, API responses and the unchanged strict-JSON boundary are fixture seams. No real campaign, save or provider request is exercised. The copy is a recall projection, not a full persistent record.

`source-review.json` freezes exact declarations and the mechanical inverse of nullable-snapshot defaults. `source_parity.py` verifies and restores only this independently tested change for older whole-file suites; unrelated edits still fail. A private helper rename has an explicit old signature, not a broad waiver. Keep existing assertions/mutations when the next reviewed change extends this proof.

Generated code remains under ignored `.generated`. SDK 8 is used; unused-field warnings from copied fixture data models are not hidden.
