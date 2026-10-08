# Native memory continuity request replay

Run `python tests/modules/AF.Module.Memory/NativeMemoryContinuityReplayTests/run.py --run-root artifacts/<new-directory>`.

This runner generates its isolated prerequisite using the existing import-to-request replay, then executes the complete current Native `CapturePromptMessages` gate, real memory append/session/cache/raw projection, final message composition and API payload construction. It retains the original load/second-turn/reentry/reload/control/pending/failed scenarios with fixed expectations. Additional focused cases cover repeated pending input, occurrence counts, date/speaker/session identity, missing provenance, chronological shared budget, same-scene raw-only history and AFEF current scope. Production owners are linked or extracted directly; game, weekly trigger and HTTP sender leaves are controlled. No live game/provider claim.

Artifacts contain generated source, hashes, saved synthetic drafts, final request JSON, evidence and logs. The old failing reproduction remains at `artifacts/native-memory-reentry-audit-20261009/` and is not overwritten.
