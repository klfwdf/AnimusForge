# Town Operation Ledger Bridge

## Scope

`TownOperationLedger` is the reusable, scene-scoped source of truth for the current GCCZ town operation. The first runtime integration covers plunder. The operation kind and state model are intentionally reusable by the later massacre and colonization slices, but those two integrations are not complete yet.

The AF bridge owns only live Bannerlord reads and side effects. It must be guarded by the active GCCZ town stage and must reset the ledger when the scene session ends.

## Plunder snapshot

The bridge initializes one bounded snapshot when plunder first starts:

- current town treasury gold;
- actual market inventory values reported by the existing reward system;
- the estimated personal gold value of the eligible civilians and named Heroes already present in the scene;
- one unique target key for every eligible visible person;
- bounded synthetic keys for the town treasury and market inventory.

The initial total value is frozen as the percentage denominator. Resuming a stopped operation reuses that denominator and the same target records. A defensive clamp expands the denominator only if the runtime awards more value than the estimate, so progress cannot exceed one hundred percent before a complete outcome is explicitly selected.

## Target categories

The bridge classifies targets in this order:

1. merchant occupations;
2. settlement notables and headmen;
3. ordinary civilians.

The category controls source attribution and dialogue context. It does not create separate consequence tables. A merchant action semantically includes the allowed market-goods attempt; no player-dialogue keyword matcher decides whether goods are taken.

## Accounting and interruption

Every target must be claimed before loot is moved. A successful action records actual gold, actual item value, item count, acquisition source, and target category, then permanently completes that target. A failed action releases the claim. This prevents direct robbery, soldier plunder, and the exit sweep from rewarding the same target twice.

Stopping changes the ledger to `Stopped`, releases in-flight claims, and prevents new claims. Acquired loot and committed consequences remain. Starting plunder again resumes the same ledger rather than opening a parallel operation.

## Consequence compatibility

Partial consequences use only cumulative acquired value:

`progress = (acquired gold + acquired item value) / initial available value`

The result is clamped to zero through ten thousand basis points. Each settlement submission applies only the difference from the previously committed cumulative basis points. `TownPlunderConsequenceDelta` scales the legacy complete plunder anchors and guarantees:

- zero percent applies no GCCZ plunder consequences;
- repeated submission without new value applies nothing;
- one hundred percent exactly equals every legacy complete plunder anchor;
- intermediate submissions sum to the same deterministic cumulative result regardless of batching.

Selecting the complete Pillage outcome still uses the existing native Bannerlord aftermath call and requirements. The ledger is forced to exactly one hundred percent only after the existing final market-loot path runs. If the guarded ledger could not initialize, the AF bridge falls back to the original complete GCCZ outcome adapter.

## Runtime cost and reset

The bridge performs the broad scene and market valuation scan only when the ledger begins. Normal operations use unique dictionary target lookup and cached acquired totals. Prompt context consumes an immutable snapshot. Scene reset clears all operation targets, values, claims, and committed progress.

## Deferred work

This slice does not persist an interrupted ledger into a save made outside the active scene. It also does not yet replace massacre victim accounting or colonization completion state. Those integrations must extend the same operation model without reintroducing parallel target or reward sets.
