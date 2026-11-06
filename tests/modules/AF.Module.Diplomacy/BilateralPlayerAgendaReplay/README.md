# Bilateral player agenda replay

Run from the repository with a new output directory:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/modules/AF.Module.Diplomacy/BilateralPlayerAgendaReplay/run.ps1 -OutputDir artifacts/bilateral-player-agenda-replay -BaselineRevision 31c3eee34
```

The launcher copies the actual queue, timing, participant detection, snapshot,
notification eligibility, auto-vote eligibility, agenda-list and ballot-opening method bodies
from the production sources without rewriting their logic. Engine entities,
campaign clock, reflection wrapper, event delivery, and UI leaf objects are fakes.
The optional baseline uses the same scenarios against the requested Git revision
and must fail behaviorally on the player-vassal 21-day deadline assertion.

Coverage includes player vassal, player ruler, NPC counterpart, synchronous listener
visibility, pending map notice, source-derived list refresh, notification exceptions,
queue rejection, retries, actual player-vassal ballot-opening admission, and the pre/post-deadline auto-vote rules. Queueing does
not run an election or a peace action. This harness does not validate Harmony
registration, native rendering/input, saved games, or end-to-end war termination.

All generated sources, build outputs, temporary CLI home, logs and receipt stay
inside the new `OutputDir`. Preserve logs needed for task evidence; remove only
that explicitly created directory after verification when no longer needed.
