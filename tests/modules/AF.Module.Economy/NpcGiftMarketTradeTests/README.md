# NPC Gift Market Trade Regression

Run `python -B tests/modules/AF.Module.Economy/NpcGiftMarketTradeTests/run.py` from the repository root.

The runner extracts the real reward record, normalization/migration, manifest merge, market transfer/bulk guards, party sale guard, market cleanup, caravan/villager stack isolation, economic pool, template/workshop exclusion and player roster capture/restore methods. Newtonsoft.Json is the local SDK reference. Item/roster/inventory/campaign/rendering, player-craft normalization, generation and logging boundaries are doubles.

Coverage includes old JSON without the new field, explicit permission across template repair, blocked special identities, equipped gift sale/buyback, mixed bulk transfer order/modifiers, open/close/daily/load market cleanup, continued random-economy isolation and sold gift absence during save/reload capture/restore.

`--mutate old-guard` and `--mutate old-cleanup` must fail. Gift-generation permission assignment before optional introduction generation has a source-wiring assertion, not a live NPC transfer test.

Legacy NPC gifts have no separate provenance field. Migration accepts their deterministic name + template identity or persisted NPC introduction source; unknown/custom identities remain blocked. This cannot reconstruct provenance that the old save never recorded.

Not covered: actual Harmony registration, native money/price settlement, Gauntlet inventory, real game save/load, or live NPC conversations. Those require in-game acceptance in both supported API lines.

The daily entry prefix before debt processing is extracted from the production partial as well. The 2,000-market / 600,001-slot case asserts no global daily scan, then executes the actual settlement and load cleanup to verify special-item removal and permitted gift preservation. Equipment restoration, watchdog and UI text are controlled leaves; this does not replay the debt loop or measure native game frame time. `--daily-source-ref 51da15d` replays the old prefix and must fail that case after 600,001 reads.

Daily diplomacy order can be checked with `dotnet run --project tests/modules/AF.Module.Diplomacy/WorldDiplomacyRoundLifecycle.SmokeTests -c Release -- --campaign-daily`; this selects the existing campaign application replay without bypassing any of its assertions.
