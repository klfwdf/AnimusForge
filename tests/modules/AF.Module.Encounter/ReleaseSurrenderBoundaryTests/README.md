# Release and NPC surrender scope regression

Run `python tests/modules/AF.Module.Encounter/ReleaseSurrenderBoundaryTests/run.py`.
The runner extracts twelve production host declarations verbatim and links the real
EncounterReleaseOwner and EncounterPendingReturnOwner. No production body is rewritten.
Native Campaign/Mission objects, release eligibility and mutation endpoints are fixtures.

Checks cover EndConversation callbacks replacing encounter, party, mission, manager or
save generation; ordinary release; wrong party rejected before close; pending surrender
cancellation, missing encounter (no resurrection), manual dialog exit, non-Hero parties,
mission teardown, session reset and exactly-once successful completion.

Engine-only replays call the actual `OnEngineTick` without Campaign Tick to cover
paused encounter menus, both conversation-active flags, mission teardown, stale scope,
initial delay, shared 250ms retry throttling, failure retention and exactly-once drain.
Other engine-tick owners are no-op fixtures; the surrender consumer is extracted intact.

`--source-file` accepts a historical LordEncounterBehavior.cs for negative controls.
The separate EncounterLifecycleBoundaryTests covers the ten-second map-dialog delay,
manual early exit and the toolbar preserving an encounter awaiting NPC surrender.
These tests do not create a real battle, award loot or modify a player save.

Native farewell replays set LeaveEncounter before EndConversation, invoke the one-shot
end callbacks before native teardown, and simulate Update/Finish before the next engine
tick. Accepted surrender must retain the captured encounter and settle once after teardown.
Changed scope, player surrender, ordinary farewell, session reset, repeated tags and manager
replacement must preserve native behavior and detach stale hooks. The optional end-hook
extraction keeps historical sources usable for a behavioral negative control.
