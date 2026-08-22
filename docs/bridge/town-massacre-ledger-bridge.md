# Town Massacre Ledger Bridge

## Runtime boundary

The reusable core owns victim identity, snapshot sealing, weighted progress, stop/resume transitions, and incremental consequence math. `SiegeAiInterventionBehavior` remains the thin Bannerlord bridge that discovers live agents, applies teams and movement, records removals, and invokes the native aftermath.

The bridge is active only during a guarded GCCZ captured-town mission. It rejects castles, missing settlements, missing mission agents, and completed ledgers.

## Captured victim snapshot

- Starting a massacre transitions the existing town operation ledger from plunder when necessary.
- The bridge captures every currently eligible ordinary civilian and settlement notable exactly once, then seals the snapshot.
- Ordinary civilians have weight `1`; notables have weight `3`.
- An empty sealed snapshot never satisfies massacre completion.
- Soldier hunting selects only living, captured, unrecorded victims. Later spawns cannot expand the operation denominator.

## Death accounting

- Ordinary civilians count only on `Killed`.
- Notables count on `Killed` or the existing GCCZ forced-unconscious path because that path queues a deterministic campaign death at settlement resolution.
- The ledger rejects duplicate removal events by stable target identity.
- Loot claims use the same target record, so a previously robbed target cannot pay a second reward when killed.

## Consequence anchors

- The first committed massacre progress fills any unapplied portion of the legacy full-plunder relationship and trust baseline.
- Further consequences interpolate from that baseline to the legacy full-massacre relationship and trust anchors by weighted victim progress.
- A full non-empty victim outcome is forced to exactly `10000` basis points before settlement resolution.
- The legacy full massacre still uses native `Devastate`, the existing extra prosperity penalty, and the existing recruitment slowdown. The bridge skips only the legacy relationship effects already supplied incrementally by the ledger.
- If an active massacre is upgraded to colonization, final resolution applies only the delta needed to reach the legacy colonization relationship and trust anchors. Previously committed plunder or massacre deltas therefore cannot stack on top of the full colonization result.

## Stop, resume, and exit

- `[ACTION:11]` is eligible only for a direct reply from an allied ordinary soldier while a massacre is active.
- Stopping changes the pending native aftermath to `Pillage`, stops future hunting, clears soldier assignments, and keeps living captured victims frightened and fleeing on the player team.
- Existing deaths, queued notable deaths, loot, and applied consequences remain.
- An in-flight fatal removal can still enter the stopped ledger, but stopped targets cannot create a new loot claim.
- Starting massacre again resumes the same ledger and cannot duplicate recorded victims or rewards.
- Leaving an incomplete ordinary massacre performs the same partial stop without showing a scene message.
- Pending colonization is downgraded to partial massacre when stopped. Colonization-specific commit and persistence remain a separate state-machine slice.

## Verification expectations

Standalone tests lock the plunder and massacre anchors, weighted progress, empty-snapshot guard, duplicate rejection, stop/resume behavior, and colonization cancellation transition. The fused project must compile against both pinned Bannerlord 1.3 and 1.4 reference overlays before this bridge is accepted.
