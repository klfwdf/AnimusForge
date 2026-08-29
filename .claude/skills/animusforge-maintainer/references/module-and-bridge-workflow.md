# Module and bridge workflow

Use this reference to create, extract, change, integrate or review an AF module or bridge.

## Choose the owner first

Classify every change as one of:

| Owner | Use when |
| --- | --- |
| Foundation | Composition/safety capability required across independent modules. |
| GameAdapter | TaleWorlds/Harmony/version/main-thread mechanism with no gameplay policy. |
| One module | Behavior, data, prompt, action, UI or policy belongs to one domain/team. |
| One bridge | Behavior exists only from the interaction of two or more modules. |

Do not move code while ownership is unresolved. Record conflicts in the module catalog/ledger.

## New/extracted module checklist

Before substantial gameplay code:

1. Stable `af.module.<name>` ID.
2. Owner/team/maintainers.
3. Purpose and explicit non-goals.
4. Required and optional module/capability dependencies.
5. Provided capabilities/events with contract versions.
6. Profiles and Bannerlord 1.3/1.4 support.
7. Persistence namespace/schema and legacy keys/types it owns.
8. Lifecycle class and Harmony/UI/tick/background contributions.
9. Health check and structured failure/degradation behavior.
10. README, manifest, focused tests and real profile composition test.
11. Content mapping and package closure.
12. Known limitations and rollback/facade path.

Start with an adapter/facade around existing behavior. Move one vertical slice and keep old entry points delegating. Delete old code only after call-site, save, dual-version, channel, profile and composition evidence.

## Bridge qualification

A bridge is justified only if:

- A and B remain independently coherent;
- the behavior cannot be owned honestly by only A or only B;
- both owners agree on the public capabilities and outcome;
- the bridge can fail/disable without making A/B crash;
- its state has a clear owner and persistence namespace.

If several consumers need a generic capability, consider a small contract/provider instead. Do not create a bridge as a dumping ground for copied module logic.

## Bridge contract

A bridge must declare:

```text
Participating module IDs and compatible versions
Required/optional capabilities and event versions
Joint maintainers/review owners
Cross-module behavior and non-goals
Data owner and persistence namespace/schema
Activation/profile/lifecycle class
Conflict/arbitration behavior
Missing/incompatible/failed bridge degradation
A/B/bridge composition test matrix
```

It may call public services or subscribe to public typed events. It may not:

- import participating modules' implementation assemblies;
- reflect private fields/methods;
- read/write another module's raw save keys;
- patch around the other module without declared conflict ownership;
- duplicate the other module's domain algorithm;
- put its state under A or B's namespace;
- rely on Harmony registration order.

## Required composition matrix

| Composition | Expected result |
| --- | --- |
| Foundation + A | A works independently. |
| Foundation + B | B works independently. |
| Foundation + A + B, no bridge | Both work independently; no hidden integration. |
| Foundation + A + B + bridge | Declared integration works. |
| Bridge without A or B | Manifest resolves to `Blocked`; entry point not invoked. |
| Incompatible A/B version | `Blocked` with exact version reason. |
| Bridge start/runtime failure | A/B remain usable; inventory reports bridge failure and trace. |
| Bridge disabled | No cross-state writes; existing saved bridge data preserved. |
| SafeMode | Bridge absent; data preserved and inventory explains it. |

## Module review questions

- Does this change force an unrelated module author to edit code?
- Is any private module type exposed through a contract?
- Did a one-consumer helper get promoted prematurely to foundation?
- Does the manifest reflect actual, not aspirational, dependencies?
- Are optional dependencies truly optional under test?
- Can failure leave registrations, tasks or patches active?
- Does save ownership remain unique?
- Does the module support both declared API lines?
- Is a cross-module behavior hidden in one module instead of a bridge?
- Are module owner and bridge co-owners recorded in the ledger/catalog?

## Minimal directory

```text
AF.Module.<Name>/ or AF.Bridge.<A><B>/
  *.csproj
  module.yaml
  README.md
  src/
  tests/
  content/    # only when this owner ships content
```

The README covers responsibility, public APIs/capabilities/events, configuration, persistence, lifecycle, Harmony/tick/UI effects, extension rules, validation and limitations.
