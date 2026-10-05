# Scene conflict judgment and display lifecycle regressions

Run from the repository root:

```powershell
python -B tests/modules/AF.Module.Conversation/SceneConflictJudgmentDisplayTests/run.py
```

Use `--source-revision <local-commit>` to verify an exact committed source slice without resetting the shared checkout. Only production source reads are pinned; test fixtures remain visible current files.

The fixture compiles the **full current ConversationHelper**, the current Overlay presentation partial and real main-thread queue/identity methods. It also executes source-extracted production SceneTaunt judgment handoff, saved-field clearing, menu/load callbacks and deferred diplomacy dispatch. No historical inverse/projection packet or refreshed review digest is used.

Coverage: stream/text/pending/typewriter state on different VM and real-end reuse; same-window refresh; scoped old-owner cleanup; genuine worker-to-main-thread queued callbacks; late finish/new request safety; incident matching by source/faction/settlement; native judgment handoff before world-map return; one-time clearing; unadjudicated conflict dispatch; saved judgment menu reopen/load; missing or mismatched identities. Production wiring and the existing five pending-diplomacy storage keys are asserted separately.

Negative controls (must compile and then fail a runtime assertion):

```powershell
python -B tests/modules/AF.Module.Conversation/SceneConflictJudgmentDisplayTests/run.py --mutate cross-vm-replay
python -B tests/modules/AF.Module.Conversation/SceneConflictJudgmentDisplayTests/run.py --mutate unowned-cleanup
python -B tests/modules/AF.Module.Conversation/SceneConflictJudgmentDisplayTests/run.py --mutate retain-judgment
```

The game objects, backend presentation scope and hostile action leaf are deterministic substitutes. Real Bannerlord event ordering, Gauntlet input/layout, actual currency/faction changes, TTS playback and player-save acceptance are **not** proven. An ambiguous old save already outside the judgment menu is deliberately not inferred to be paid, and an already expelled player is not automatically rejoined. The code transfers the matching same-faction conflict to native judgment; it does not waive native crime/trust penalties or unrelated incidents.

Performance: only event-time O(1) identity checks/cleanup; same-VM refresh reuses cached reflection. No new Tick polling or whole-world scan.
