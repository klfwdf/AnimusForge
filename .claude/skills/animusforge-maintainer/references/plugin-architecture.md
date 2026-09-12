# AF foundation, modules and bridges

This is AF's DSH-inspired plugin architecture adapted to Bannerlord/C# constraints.

## Adopted ideas

From DSH/Cordis, AF adopts:

- behavior contributes through modules rather than direct foundation edits;
- service/capability Definition, Provider and Consumer roles;
- declared required/optional dependencies instead of scan/load order;
- stable module identity, immutable package/build version, and distinct runtime generation;
- observable inventory and health/failure state;
- profile/bundle-like static compositions;
- explicit provider/default resolution;
- typed services for calls and typed events for observations;
- lifecycle-owned reversible registrations where the host can actually reverse them;
- generated dependency/capability/owner/profile catalogs and CI gates.

AF does not adopt:

- arbitrary JS/C# source execution in the game;
- network-downloaded runtime DLLs;
- an assumption that every contribution can be hot-reloaded/unloaded;
- browser Host/Client topology as a substitute for Bannerlord's real runtime domains;
- DSH's package granularity as a target assembly count.

## Foundation boundary

Foundation owns platform safety and composition:

```text
AF.Contracts
AF.ModuleRegistry
AF.Settings
AF.Diagnostics
AF.Scheduler
AF.Persistence facade
AF.GameAdapter ports
SafeMode/profile resolution
```

Foundation does not own gameplay rules, module-private prompts/action tags/save models/UI, or pair-specific integration.

An abstraction belongs in `AF.Contracts` only when current independent consumers need a stable seam. A hypothetical future use is not enough.

## Manifest

Each module/bridge requires a validated manifest resembling:

```yaml
id: af.module.example
kind: module
version: 1.0.0
contractVersion: 1
entryType: AnimusForge.Example.ExampleModule
owner:
  team: example
  maintainers: [account]
profiles: [single-player, developer]
requiredModules:
  - id: af.foundation.runtime
    version: ">=1.0.0 <2.0.0"
optionalModules: []
requiresCapabilities: [game-state.read]
providesCapabilities: [example.read]
persistence:
  namespace: example
  schemaVersion: 1
lifecycle:
  activation: save-load-boundary
  harmonyPatches: false
  runtimeUnload: unsupported
compatibility:
  bannerlord: ["1.3", "1.4"]
```

Validate before entry-point invocation:

- unique, stable ID and persistence namespace;
- version/contract/version-range syntax;
- owner and maintainers;
- required/optional module graph and cycles;
- capability provider availability and version compatibility;
- profile membership and conflicts;
- Bannerlord API support;
- lifecycle claims versus declared Harmony/save/UI/tick effects;
- DLL/content closure in the staged package.

## Capability seam

| Role | Contract |
| --- | --- |
| Definition | Stable interface, DTO and event in `AF.Contracts`; no private TaleWorlds object or module type. |
| Provider | Module/foundation implementation registered under one capability ID/version. |
| Consumer | Declares capability in manifest and resolves it through `ModuleContext`; never imports provider implementation. |
| Bridge | Consumer of participating modules' public capabilities; owner of cross-module behavior/state. |

Calls/queries use services. Notifications use typed events. Decisions that can be intercepted require an explicit arbitration contract defining order, short-circuiting, failure and ownership; do not create a generic middleware chain by default.

## Internal ports, external API and transitional adapters

Same-DLL collaboration may use typed `internal` ports; independent sub-MODs use a separately versioned public contract. “Public capability” means the provider's supported cross-owner surface, not that every internal interface must become C# `public`. Keep live game types in explicit main-thread adapters; do not expose them through background snapshots or the external API.

A small composition root and temporary adapter may reference an existing implementation to preserve behavior. Keep that knowledge at the adapter/composition boundary, record the remaining direct callers, and do not spread it back through the shared pipeline. A thin adapter is not automatically a jointly owned gameplay Bridge. New cross-domain behavior still requires the actual co-owner/capability/state/composition gates.

A read-only external API is a valid limited release when unsupported submission/write/registration capabilities report that fact explicitly. It does not complete a planned request/action API or prove external MOD loading and binary compatibility. Internal and external callers must ultimately use the same authoritative execution/fact owners, not a second shortened pipeline.

## Catalog readiness is not module-host readiness

Check these layers separately:

| Layer | Required evidence |
| --- | --- |
| Directory/registry | Unique declarations, dependency/version/cycle checks and truthful query results. |
| Adapter wiring | Actual callers reach typed providers with preserved arguments/results and explicit legacy coverage. |
| Runtime ModuleHost | Real module activation, owned registrations/tasks/resources, stop or restart policy, partial-start cleanup, failure reporting and dependency propagation. |
| Composition | Failure/absence/disablement of A leaves unrelated B usable; profiles and save data remain valid. |

`Ready` may mean only adapter construction if documented that narrowly. Do not infer runtime health from `provider != null`, a declaration existing, an offline fixture passing or a manually assigned state. Likewise, a registry's dependency algorithm is real progress, but unused production dependency declarations do not prove real profile/module closure.

A shutdown that only changes a status string is not resource disposal. A directory's `Failed` state is not fault isolation unless actual failures update that state, block affected dependents and allow unrelated contributions to continue. Review real registration/Tick/dispatch call paths, not just status enums. Preserve existing behavior during a scoped adapter slice, but leave missing host guarantees explicitly unfinished.

## Lifecycle states

```text
Discovered → Disabled | Blocked | Starting → Active | Degraded | Failed | RestartRequired
```

- `Disabled`: profile/settings intent.
- `Blocked`: required dependency/capability missing, incompatible version, cycle or conflict.
- `Degraded`: optional capability absent and an explicit fallback was selected.
- `Failed`: load/runtime/health failure; dependents become blocked; unrelated modules continue.
- `RestartRequired`: unsafe to apply the configuration in the current process/campaign.

Activation classes:

| Class | Examples | Rule |
| --- | --- | --- |
| `boot-only` | Bootstrap, global compatibility/save-type owners | Decide before process startup; change requires restart. |
| `save-load-boundary` | CampaignBehavior/gameplay/persistent/Harmony modules | Decide before new/load campaign; change requires campaign exit/reload or restart. |
| `runtime-toggle-safe` | Pure UI/diagnostic contributions with no patch/save/thread residue | May toggle only with disposer and composition test. |

A module start is transactional for reversible contributions. `ModuleHandle` owns service/event/UI/timer/task registrations. On failure it disposes what is safely reversible. It must not claim to reverse engine state it cannot restore.

## Inventory

Expose at least:

```text
ModuleId, kind, version, contractVersion
owner/maintainers, profile membership, enabled intent
required/optional modules and capabilities
provided capabilities
activation class and Bannerlord lines
state, run generation, start time, health
failure stage/message/trace ID
persistence namespace/schema
```

Read the registry's live authoritative state; do not build a second stale cache without a clear reason.

## Profiles

- `single-player`: foundation + supported normal modules/bridges.
- `safe-mode`: foundation, GameAdapter, persistence and diagnostics; only explicit recovery modules.
- `developer`: adds inventory, trace, contract checker and test hooks.
- `server`: only explicitly server-safe components; do not infer current modules are compatible.

Profiles are validated static compositions included in a release. They do not download or execute unknown plugins.

## SafeMode

SafeMode must load enough foundation/persistence metadata to diagnose optional-module failures and protect saves. It must not:

- delete unknown module data;
- pretend disabling a module preserves identical gameplay state;
- auto-migrate a module's data without its migration owner;
- silently activate replacement gameplay.

## Provider and fallback resolution

One owner resolves provider selection explicitly:

```text
configured compatible provider
→ registered profile default
→ explicit unavailable/degraded result
```

Record provider/fallback identity and reason in diagnostics. Consumers must not hide defaults inside execution methods.

## Assembly granularity

A logical module may begin as a project/namespace inside the existing implementation assembly. Split a physical DLL only when it has real independent ownership plus at least one of:

- independent release/load;
- dependency closure;
- lifecycle/replacement;
- permission/isolation;
- focused tests/maintenance.

Do not create dozens of tiny assemblies merely to mirror DSH packages.
