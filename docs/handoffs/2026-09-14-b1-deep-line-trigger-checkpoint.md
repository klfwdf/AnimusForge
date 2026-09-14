# B1 checkpoint: deep line/trigger budget (2026-09-14)

Intent only. Production is still `86805518` / docs HEAD `16adcc02` until the slice lands.

Next production slice (do not redo index / Campaign window / sort / typed raw / per-draft owner caps):

- Keep `SanitizeDailyMemoryDraftEntry` as the synchronous save/read oracle consumer.
- Extract the exact line filter+normalize body and the per-trigger MemoryId/day/date bind so cooperative sealing cannot grow a second rule set.
- Charge each line and each trigger bind as shared metadata (`<=128` per window). Draft identity stays one expensive grant.
- Private line/trigger lists until that draft finishes; list ref/count changes invalidate and reseal.
- `SanitizeWeeklyMemoryMaterialTriggers` remains one atomic list call after the bind pass (large trigger lists are still not hard-sliced).
- Do not enter B2.

Rollback: revert this checkpoint docs commit; no production change yet.
