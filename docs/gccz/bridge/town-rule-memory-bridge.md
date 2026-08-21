# Town Rule Memory AF Bridge

## Scope

This bridge persists local governance history for each occupied town and exposes it only while the GCCZ town intervention stage is active. It does not add a management loop, copy the player biography, or alter settlement outcome values.

## Core ownership

The reusable project owns all persistent value semantics:

- `SettlementRuleMemoryRecord` is the immutable save-neutral snapshot.
- `SettlementRuleMemoryStore` detects ruler and culture transitions and keeps one predecessor summary.
- `SettlementRuleMemoryCodec` converts snapshots to versioned primitive strings.
- `TownPromptComposer.BuildSettlementRuleMemoryContext` formats the localized prompt fragment.

The AF adapter `GcczTownRuleMemoryRuntimeBridge` only reads Bannerlord objects, obtains the AF ruler personality, synchronizes primitive save data, and calls the core.

## Runtime flow

1. A town prompt request must already be inside the active GCCZ intervention stage.
2. On first observation, the previous owner is recorded before the new owner when capture provenance is available.
3. If no history is available, the current ruler receives the two-year minimum duration fallback.
4. The current settlement ruler, settlement culture, and AF personality are observed before both main dialogue generation and semantic postprocessing.
5. Player-ruler personality is left empty so this feature cannot duplicate the existing AF player biography.
6. A successful culture replacement refreshes the same settlement record immediately.
7. Final settlement aftermath observes the definitive post-capture owner before the GCCZ stage closes.
8. Castle, village, world-map, and ordinary AF dialogue calls return no town rule context.

## Save compatibility

AF stores `Dictionary<string, string>` under `_gcczTownRuleMemoryRecordsBySettlement_v1` and an initialization marker under `_gcczTownRuleMemoryStorageInitialized_v1`. Each value uses the `v1` codec. Missing storage is treated as an old save and initializes lazily. Invalid entries are rejected independently; load or formatting failures return an empty context and cannot block town entry.

## Compatibility boundary

This slice changes prompt memory only. It does not modify eligibility, costs, thresholds, rewards, penalties, or completion requirements for any positive or negative settlement outcome.

## Verification

Standalone tests cover lazy initialization, the two-year fallback, same-ruler refresh, ruler and culture transitions, predecessor retention, personality isolation, codec round trips, corrupt data rejection, restore filtering, and localized prompt composition. The fused project must also pass its reusable-core build and pinned Bannerlord 1.3/1.4 reference builds.
