# Imported Memory Prompt Replay

Scope: execute the current single-NPC import method, production JSON reader and
sanitizers, authoritative state projections, Hero lookup, history snapshot, recall,
Native history capture, persisted-section split, Native prefix handoff, strict
message assembly and API caller in one offline executable.

Game objects, settings, UI choices, auxiliary model responses and the final network
gateway are deterministic substitutes. This does not verify the player's installed
DLL, save, actual HTTP provider or the model's response. No private memory fixture
or message body is checked in or printed.

Run `python -X utf8 -B tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/run.py --memory-file <absolute-json-path>`.
The input is read in place. Only counts, hashes and assertion results are retained.
The default run generates synthetic memory inside its isolated artifact directory.

Acceptance: correct NPC receives the overview and recalled summaries/AFEF in the
gateway messages; default recall selects four blocks; all-block recall preserves
every summary; overwrite refreshes the next turn; skip preserves existing memory;
wrong target, invalid format, disabled Hero and stale generation are distinguished.
