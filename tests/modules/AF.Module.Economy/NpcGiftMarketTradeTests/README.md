# NPC Gift Market Trade Regression

Run `python -B tests/modules/AF.Module.Economy/NpcGiftMarketTradeTests/run.py` from the repository root.

The runner extracts the real reward record, normalization/migration, manifest merge, market transfer/bulk guards, party sale guard, market cleanup, caravan/villager stack isolation, economic pool, template/workshop exclusion and player roster capture/restore methods. Newtonsoft.Json is the local SDK reference. Item/roster/inventory/campaign/rendering, player-craft normalization, generation and logging boundaries are doubles.

Coverage includes old JSON without the new field, explicit permission across template repair, blocked special identities, equipped gift sale/buyback, mixed bulk transfer order/modifiers, open/close/daily/load market cleanup, continued random-economy isolation and sold gift absence during save/reload capture/restore.

`--mutate old-guard` and `--mutate old-cleanup` must fail. Gift-generation permission assignment before optional introduction generation has a source-wiring assertion, not a live NPC transfer test.

Legacy NPC gifts have no separate provenance field. Migration accepts their deterministic name + template identity or persisted NPC introduction source; unknown/custom identities remain blocked. This cannot reconstruct provenance that the old save never recorded.

Not covered: actual Harmony registration, native money/price settlement, Gauntlet inventory, real game save/load, or live NPC conversations. Those require in-game acceptance in both supported API lines.

## Daily world pass (2026-10-15)

The daily prefix before debt processing and actual OnEngineTick are extracted from production. The complete cleanup owner, market host partial and SaveRuntimeGuard compile directly. The 2,000-market / 600,001-slot case verifies zero item reads in the daily callback, then full world coverage. Engine/roster/game-state/clock-domain/UI/log boundaries are doubles; debt processing and live native callbacks are not replayed.

The approved replacement removes per-slot cursors, clean-cache stamps, global rule epochs and DailyTickSettlement admission. Their old slot-limit/cache-hit assertions are intentionally superseded by one-complete-market-per-frame, stable forward progress during quantity changes, coalesced whole-world rounds, fresh permission checks and next-round world membership assertions. Existing trade permission, gift preservation, modifier, economic-pool and roster persistence regressions remain.

Current cases cover per-market frame scheduling, 120 continuously changing markets completing in 120 ticks, 1,000 daily requests coalescing into one follow-up round, inventory replacement, new settlements, same/distinct trade rosters, saving/inventory UI/worker/retired owner/load gates, source/getter/removal errors, swap-last removal and changed candidate quantities/permissions. No repeat/delete-until-empty loop is used for reentrant additions; the next trade/daily pass handles newly added stacks.

`--mutate all-markets`, `--mutate restart-round`, `--mutate ignore-generation` and the existing `--mutate old-cleanup` must fail behavioral assertions after compiling. `--daily-source-ref 51da15d` remains a historical routing control using the current cleanup leaf, not a full old implementation replay.

Release timing probes run 7 samples per synthetic market (300/5,000/50,000 native slots and 5,000 slots including 1,000 prohibited stacks). Construction is outside measurement. These timings use the real production cleanup owner with managed engine doubles: native lookup, event dispatch, game frame cost and the player's actual largest market remain unmeasured. The one-market design intentionally has no hard millisecond cap. Production diagnostics emit one round summary with total work/max-market time and label, plus individual markets taking at least 8ms.
