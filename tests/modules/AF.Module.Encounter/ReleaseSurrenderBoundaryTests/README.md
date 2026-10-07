# Release and NPC surrender scope regression

Run `python tests/modules/AF.Module.Encounter/ReleaseSurrenderBoundaryTests/run.py`.
The runner extracts ten production host declarations verbatim and links the real
EncounterReleaseOwner and EncounterPendingReturnOwner. No production body is rewritten.
Native Campaign/Mission objects, release eligibility and mutation endpoints are fixtures.

Checks cover EndConversation callbacks replacing encounter, party, mission, manager or
save generation; ordinary release; wrong party rejected before close; pending surrender
cancellation, missing encounter (no resurrection), manual dialog exit, non-Hero parties,
mission teardown, session reset and exactly-once successful completion.

`--source-file` accepts a historical LordEncounterBehavior.cs for negative controls.
The separate EncounterLifecycleBoundaryTests covers the ten-second map-dialog delay,
manual early exit and the toolbar preserving an encounter awaiting NPC surrender.
These tests do not create a real battle, award loot or modify a player save.
