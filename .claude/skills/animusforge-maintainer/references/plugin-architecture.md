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
