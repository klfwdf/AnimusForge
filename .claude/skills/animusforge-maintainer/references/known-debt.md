# AF known debt and strangler order

This reference summarizes durable debt categories from the 2026-08-29 read-only audit. Re-verify counts and paths in the canonical worktree before using them as current facts.

## Audit snapshot

Approximate maintained-code hotspots in the audited remote revision:

| File | Lines | Mixed responsibilities |
| --- | ---: | --- |
| `MyBehavior.cs` | 57,864 | Events, memory, save, prompt/context, import/export, LLM support. |
| `ShoutBehavior.cs` | 38,156 | Three-channel runtime, scene/native UI, postprocess, TTS, threading. |
| `RewardSystemBehavior.cs` | 22,091 | Items/gold/debt/join/asset/action tags/reflection. |
| `WorldDiplomacyBehavior.cs` | 18,967 | Diplomacy state, LLM, history compression, legality, UI, execution. |
| `SiegeAiInterventionBehavior.cs` | 16,958 | Siege runtime, AI, reflection, action and persistence. |
| `KnowledgeLibraryBehavior.cs` | 13,682 | Retrieval, storage/context and HTTP/LLM. |
| `CustomPolicyBehavior.cs` | 12,688 | Policy generation, lifecycle, effects, persistence and UI. |
| `SceneTauntBehavior.cs` | 10,228 | Peace-scene combat/taunt/damage/Mission/conversation blocking. |
| `WorldMapPartyCommandBehavior.cs` | 9,993 | Tags, commands, task state, execution and memory. |
| `CourierDeliveryBehavior.cs` | 9,855 | Courier state machine, parties, LLM, UI and persistence. |

Other systemic debt:

- root-level production sources and mixed repository planes;
- original/decompiled game references, DLLs, logs, temporary caches, tool distributions and archives tracked together;
- broad static singleton/global state and many public static APIs;
- reflection/Harmony/version adaptation scattered through business code;
- numerous broad/silent exception catches;
- weak automated CI/review/release evidence relative to project size;
- source-text smoke tests standing in for behavioral tests;
- three-channel consistency maintained partly by documentation/manual discipline;
- oversized shared postprocess signatures with many boolean rule flags;
- save logic and business state tightly coupled in Behavior classes.

## Freeze rule

Until ownership and strangler paths are established, avoid adding new business behavior directly to:

```text
MyBehavior.cs
ShoutBehavior.cs
RewardSystemBehavior.cs
AIConfigHandler.cs
DuelSettings.cs
```

Allowed changes are focused bug/compatibility fixes, tests, diagnostics, facades and implementation extraction. Record justified exceptions in the ledger.

## Strangler order

1. Repository identity/cleanup/reproducibility gate.
2. Module catalog and owner map.
3. `AF.Contracts`, manifest/profile/registry and no-op composition tests.
4. Foundation/GameAdapter/SafeMode and dual-version package closure.
5. `AF.Module.Conversation` shared interaction seam.
6. Domain manifests before code movement.
7. Low-risk action handlers (`GIVE_GOLD`, `GIVE_ITEM`, debt).
8. `AF.Module.Memory` with legacy save facade.
9. Courier/RewardsDebt/WorldMap/Duel/Policy/Diplomacy/Siege one owner at a time.
10. Co-owned bridges after participating public capabilities stabilize.
11. Patch/tick registration and God Object facade cleanup.
12. Root source removal and optional Git-history maintenance.

## Common false refactors

Avoid:

- moving a 50k-line class unchanged into `modules/` and calling it modular;
- creating `AF.Contracts` that exports every existing private type;
- putting all behavior into Foundation services;
- letting modules discover one another via reflection/static singleton;
- adding bridge behavior as `if (OtherModule.Instance != null)`;
- splitting every helper into a DLL without independent ownership/lifecycle value;
- renaming save types/keys during directory cleanup;
- replacing visible bool parameters with an untyped dictionary/service locator;
- claiming failure isolation while module exceptions still escape application tick or save load;
- marking repository cleanup complete while old artifacts remain required for builds.

## Technical-debt record format

For durable debt record:

```text
Owner module/foundation/bridge
Current evidence and impact
Why it is deferred
Safe extension/avoidance rule
Prerequisite task/decision
Validation needed before removal
```

Put live scheduling/status in the execution ledger. Put module-specific limitations in its README. Put irreversible decisions in ADRs.
