# Memory failure UI boundary tests

```powershell
G:\Python310\python.exe tools/MemoryFailureUiBoundaryTests/run.py
G:\Python310\python.exe tools/MemoryFailureUiBoundaryTests/run.py --original
G:\Python310\python.exe tools/MemoryFailureUiBoundaryTests/run.py --mutate drop-revision
```

Current: 85 checks. Original `38488ed2`: 37 runtime assertion failures. Seven mutants must compile and fail: `off-main-ui`, `drop-generation`, `drop-owner`, `drop-campaign`, `drop-revision`, `keep-pending-on-reset`, `diagnostic-throws`.

The real notice implementation and SaveRuntimeGuard are linked, and the real EngineTick method and nine producer call statements are extracted. UI and main-thread identity are fixtures; network/ONNX and full producer methods are not executed. Their original algorithms, branches and error text are instead protected by exact source inverse checks for all affected declarations and the whole owner file.

Tests cover background versus direct calls, bounded concurrent publication, stale generations/owners, reset, failed or reentrant show, late acknowledgement, diagnostics and all producer statements. Keep these behavioral checks when extending the reviewed source baseline; do not delete the inverse proof to allow unrelated edits.

Generated fixtures/logs remain under ignored `.generated`. None of these results is real Bannerlord UI or save acceptance.
