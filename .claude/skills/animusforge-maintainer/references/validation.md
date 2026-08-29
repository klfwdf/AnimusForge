# AF validation strategy

Choose evidence according to the changed surface. A single successful compile is never proof of save, Harmony, interaction, profile or package correctness.

## Validation layers

| Layer | Environment | Proves |
| --- | --- | --- |
| Contract/unit | Standard .NET | Manifest/schema, dependency graph, capability DTO, tag parser, action/prompt/config/persistence pure behavior. |
| Foundation composition | Standard .NET where possible | No-op module lifecycle, state/inventory, dependency blocking, failure isolation, profile resolution, disposer behavior. |
| Module/bridge composition | Standard .NET + fakes | Module-alone/dependency/optional capability and bridge matrix. |
| Compatibility/build | Windows + legitimate pinned game refs | 1.3, 1.4, Bootstrap, GameAdapter/patch compile targets. |
| Stage/package | Windows/build artifacts | Unified module layout, profile DLL/content closure, allowlist, marker/hash, forbidden entries. |
| Save migration | Fixtures + in-game | Old key/type/schema reads, current writes, missing/corrupt behavior, user-data preservation. |
| In-game | Supported Bannerlord 1.3/1.4 | Campaign/Mission/Encounter/Harmony/Gauntlet/thread/lifecycle behavior. |
| Interaction | Keyless fixtures + optional real API/in-game | Rule/prompt/postprocess/action/history alignment and visible output. |

## Manifest and profile tests

Test:

- invalid/duplicate IDs and persistence namespaces;
- missing owner, entry type or contract version;
- required/optional dependency behavior;
- dependency and capability cycles;
- incompatible module/capability/Bannerlord versions;
- conflicting Harmony/hook declarations;
- profile closure and undeclared DLL/content;
- invalid runtime-toggle claim for Harmony/save/persistent module;
- SafeMode excludes optional gameplay and preserves data metadata.

## Lifecycle and failure isolation

At least one real Loader/host composition should prove:

```text
start succeeds → contributions visible → stop/dispose removes reversible contributions
start partly fails → reversible contributions removed → module Failed
required dependency fails → dependent Blocked
unrelated module remains Active
optional provider missing → explicit Degraded or documented behavior
stale generation completion → ignored/rejected
```

Hand-constructing objects without the actual module host is insufficient for product-visible lifecycle behavior.

## Bridge matrix

Every bridge tests:

- A only;
- B only;
- A+B, bridge absent;
- A+B+bridge;
- bridge dependency version mismatch;
- bridge start/runtime failure;
- bridge disabled and saved bridge data preserved;
- SafeMode;
- both supported Bannerlord API lines where game behavior is involved.

## Three-channel interaction

For each applicable action:

```text
preprocess/eligibility
prompt block order and role semantics
history/AFEF inputs
postprocess rules/capabilities
RAW and normalized FINAL
ActionPlan
main-thread execution/rejection
visible tag removal
AFEF/history readback
```

Compare scene shout, native/free conversation and courier. Non-applicability requires explicit exclusion reason and test.

## Persistence

Cover:

- representative old saves/schema fixtures;
- existing `SyncData` key/type compatibility;
- UTF-8 chunk exact boundaries, multibyte text, missing/corrupt chunks and count limits;
- module/bridge namespace collision;
- absent/disabled/failed module data preservation;
- migration idempotency/commit point;
- PlayerExports merge and deployment rollback;
- save size and timing diagnostics.

## Build/package matrix

Record exact commands and versions for:

1. 1.3 reference provenance verification;
2. 1.3 implementation build;
3. 1.4 game root/reference verification;
4. 1.4 implementation build;
5. Bootstrap build;
6. Foundation/module/bridge profile closure;
7. staged module validation;
8. ZIP allowlist/manifest/hash/marker;
9. absence of direct implementation declaration, TaleWorlds DLLs, forbidden ONNX and accidental logs/artifacts.

Do not deploy as a side effect of build validation.

## In-game scenario record

Record:

```text
Game/API version
AF/module/profile versions and inventory
Save identity or new campaign
Scene/menu/mission
Steps
Expected result
Observed result
Relevant trace/log path
Cleanup/rollback
```

A screenshot is useful for UI but does not replace state/action/save evidence.

## Reporting unavailable checks

Use:

```text
NOT-RUN: <exact check>
Reason: <missing Windows/game/ref/API credential/etc.>
Risk: <what remains unproven>
Required next environment/action: <specific>
```

If acceptance depends on the check, keep ledger state `VERIFY` or `BLOCKED`, not `DONE`.
