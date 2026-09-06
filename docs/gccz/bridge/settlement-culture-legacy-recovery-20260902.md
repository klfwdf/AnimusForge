# Settlement Culture Legacy Recovery Bridge

Date: 2026-09-02

## Scope

This slice audits the external `AF_CultureFix` report against the current AnimusForge main branch. It does not import the legacy submodule or its Harmony/reflection implementation.

Current AnimusForge already owns these paths:

- `GcczSettlementCulturePersistenceBehavior` persists explicit GCCZ culture overrides under the existing `_gcczSettlementCultureOverrides_v1` save key.
- `SiegeAiInterventionBehavior` registers GCCZ menu options on `OnSessionLaunchedEvent`.
- settlement-intervention completion uses the existing guarded `PlayerEncounter.Finish(true)` flow.
- current town and village notable deaths use `KillCharacterAction`, which clears `StayingInSettlement` through the native Hero/Settlement cache path.

Those paths were retained. No parallel culture store, menu patch, encounter-exit patch, freeze watcher, or standalone log system was added.

## Remaining compatibility gap

Very old polluted saves can contain a dead notable that still has `StayingInSettlement` and therefore remains in `Settlement.Notables`. Such saves can also contain replacement notables from one culture while the non-saveable `Settlement.Culture` reverted to another culture before the AF override ledger existed.

The AF bridge now:

1. performs one bounded all-settlement repair after load finishes;
2. uses `LeaveSettlementAction.ApplyForCharacterOnly` for dead cache entries instead of reflection;
3. performs per-settlement daily cleanup, which is normally a no-op because current native kills already clear the cache;
4. infers a legacy town culture only when the same load repair actually cleaned a dead notable, no explicit override exists, at least three living notables have cultures, every living notable culture agrees, and that culture differs from the town;
5. records the inferred culture through the existing override ledger so the repair is persistent and idempotent.

Villages, mixed notable cultures, missing culture evidence, small samples, explicit overrides, and ordinary culture differences never qualify for inference.

## Additional current-path hardening

The follow-up audit found three silent partial-success paths in the current implementation:

- culture mutation continued even when the persistence behavior could not record an override;
- town colonization marked its state committed before all town and bound-village culture overrides were durable, while a bound-village exception was swallowed;
- gradual village education removed its pending schedule even when the durable culture mutation was rejected.

The AF adapter now fails closed before applying an untracked culture mutation, records the town and every bound village before committing colonization state, removes the empty exception path, and retains a due gradual-education entry for retry when persistence is temporarily unavailable. Existing hearth, relation, notable replacement, reward, penalty, and eligibility values are unchanged.

## Ownership

- Reusable decision policy: `AnimusForge.SiegeAftermathIntervention/SettlementCultureLegacyRecoveryPolicy.cs`
- Bannerlord event/cache/save adapter: `GcczSettlementCulturePersistenceBehavior.cs` in the AF main worktree
- Existing save key and assembly/type identities: unchanged

## Validation

- Full GCCZ standalone core suite: PASS, including eight focused recovery-policy cases.
- Follow-up durable-commit audit: PASS; GCCZ boundary verifier confirmed effect isolation and the current AF adapter compiled after the fail-closed changes.
- Unified Debug Stage: Bannerlord 1.3 implementation, Bannerlord 1.4 implementation, and Bootstrap all built with zero warnings and zero errors.
- Game directory and saves were not modified.
- Representative polluted-save runtime validation remains required.
