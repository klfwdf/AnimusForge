# NPC Gift Market Trade Regression

Run `python -B tests/modules/AF.Module.Economy/NpcGiftMarketTradeTests/run.py` from the repository root.

The runner extracts the real reward record, normalization/migration, manifest merge, market transfer/bulk guards, party sale guard, market cleanup, caravan/villager stack isolation, economic pool, template/workshop exclusion and player roster capture/restore methods. Newtonsoft.Json is the local SDK reference. Item/roster/inventory/campaign/rendering, player-craft normalization, generation and logging boundaries are doubles.

Coverage includes old JSON without the new field, explicit permission across template repair, blocked special identities, equipped gift sale/buyback, mixed bulk transfer order/modifiers, open/close/daily/load market cleanup, continued random-economy isolation and sold gift absence during save/reload capture/restore.

`--mutate old-guard` and `--mutate old-cleanup` must fail. Gift-generation permission assignment before optional introduction generation has a source-wiring assertion, not a live NPC transfer test.

Legacy NPC gifts have no separate provenance field. Migration accepts their deterministic name + template identity or persisted NPC introduction source; unknown/custom identities remain blocked. This cannot reconstruct provenance that the old save never recorded.

Not covered: actual Harmony registration, native money/price settlement, Gauntlet inventory, real game save/load, or live NPC conversations. Those require in-game acceptance in both supported API lines.

The daily entry prefix before debt processing is extracted from the production partial as well. The 2,000-market / 600,001-slot case asserts no global daily scan, then runs the current queued settlement/load cleanup through the actual `OnEngineTick` entry. Equipment restoration, conversation-close, watchdog and UI text are controlled leaves; this does not replay the debt loop or measure native game frame time. `--daily-source-ref 51da15d` supplies the historical daily prefix/global routing helpers; its roster cleanup leaf uses the current owner, so this is a negative routing control, not a full historical implementation replay.

The complete production `GeneratedRewardMarketCleanupOwner`, reward market-cleanup partial and `SaveRuntimeGuard` compile directly into the fixture. The 52 cases cover identical/distinct trade rosters, version/rule invalidation, mutable allowed gifts, per-slot/time limits, loaded backlog, deduplication across inventory replacement, swap-last deletion, source mutation/reentrant callbacks, changed world enumeration, owner/worker/save/load admission, failed getters/deletions, logging/disposal failure and unstable producers. The engine doubles model `VersionNo` and native swap-last removal; they do not prove native callback timing or actual Harmony registration.

`--mutate unbounded-slots`, `--mutate ignore-deadline`, `--mutate ignore-generation` and `--mutate cache-mutable-gifts` compile and must fail their behavioral checks. Synchronous trade cleanup is still proportional to the selected roster; its unstable-source guard records failure and leaves an invalid cache stamp. The 2ms background deadline is soft: one native deletion/read remains atomic. World admission and empty/stale jobs also consume the 128-unit allowance; no whole-market callback hides behind a queue-job counter.

Daily diplomacy order can be checked with `dotnet run --project tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests -c Release -- --campaign-daily`; this selects the existing campaign application replay without bypassing any of its assertions.
