---
name: animusforge-policy-effect-module
description: "Add, modify, or review AnimusForge PolicySystem source policy effect modules while preserving module contracts, MCM-controlled retrieval, runtime boundaries, persistence, performance, and Bannerlord 1.3/1.4 compatibility. Do not use for third-party DLL plugins or unrelated policy UI work."
---

# AnimusForge Policy Effect Module

## Purpose

Use this skill for a source-compiled policy effect under `PolicySystem/Effects/Modules/`.
Produce the smallest self-registering module that fits the existing compiler and runtime contracts, is selectable through the existing MCM module manager, and keeps existing policies stable.

This is not an independent DLL plugin protocol. Do not use it to redesign the policy lifecycle, rewrite unrelated policy UI, or restore archived policy implementations.

## Read First

1. Read the workspace `AGENTS.md`.
2. Read `docs/policy_effect_source_module_contract.md`.
3. When the change touches TaleWorlds APIs, campaign models, save behavior, UI, build output, or packaging, also read `docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md`.
4. Treat the root `PolicySystem/` tree as authoritative. `docs/custom_policy_lifecycle_v2.md` and `dist/` policy overlays are historical and must not be restored.
5. Inspect `git status --short` and preserve user changes.

Read the smallest matching implementation before proposing edits. Select an analogue by execution kind, hook, target kinds, target projection, funding behavior, and rollback semantics, not merely by a similar display name.

## Default Change Boundary

A normal new visible source module should change only:

- `PolicySystem/Effects/Modules/<moduleId>/<PascalName>EffectModule.cs`
- `AnimusForge/CustomPrompts/Policy/Effects/<moduleId>.json`
- focused cases and expected module lists in `tools/PolicyEffectModule.ContractTests/`

The module file owns its descriptor, typed payload, normalization, funding adaptation, and execution adapter. Assembly registration in that same file owns discovery.

Do not add a hardcoded module list to `PolicyEffectModuleCatalog`, `PolicyEffectModuleManagerUi`, or the project file. SDK source inclusion, assembly registration, Catalog discovery, scope filtering, and the dynamic manager already provide those functions.

Only widen the change boundary when the requested effect cannot be expressed through an existing execution interface, hook, target kind, projection, or game bridge operation. Before widening it:

1. identify the exact missing capability;
2. read the owning compiler, coordinator, save, runtime-index, and bridge paths;
3. explain why an existing module cannot express the behavior;
4. preserve both Bannerlord API builds and existing module behavior.

## Modularity Invariants

Treat each visible source module as one independently selectable policy capability with one canonical ID, one descriptor, one typed payload contract, and one prompt file.

- Keep public capability semantics in the visible source module. Put timing-specific or host-specific implementations in hidden runtime descendants only when a composite source genuinely needs them.
- Give every module one primary execution responsibility. Do not mix model contribution, daily mutation, one-shot mutation, scheduled mutation, or composite expansion in the same primary execution contract.
- Register through the module-local assembly attribute. Catalog, routing, MCM UI, prompt loading, compilation, persistence, and execution must consume the shared contracts rather than module-specific branches.
- Keep game interaction behind an existing model adapter or `PolicyEffectGameBridge` operation. A module may translate its typed payload into that operation; it must not absorb scheduler, target resolver, save codec, or UI responsibilities.
- Keep prompt text outside C# in the per-module JSON while retaining a semantically equivalent descriptor fallback.
- Keep module tests focused on the module contract and shared framework invariants. Do not make unrelated modules depend on its concrete class.
- Prefer adding a module over extending a central switch. Prefer extending a narrow shared contract only when at least one real capability cannot be represented otherwise.

## Isolation Invariants

Isolation is mandatory across configuration, authorization, targets, state, failures, and lifecycle:

- **Configuration isolation:** each visible module has its own prompt slot and per-context MCM state. A missing, corrupt, disabled, or edited module prompt/configuration must affect only that module and context.
- **Authorization isolation:** only modules frozen into the candidate and detailed allowlists may compile. Hidden runtime descendants are authorized only through the declared visible `SourceModuleId` lineage.
- **Target isolation:** execute only against the compiler-materialized canonical target set. Never fall back to a global hero, clan, party, kingdom, or settlement scan when a target is missing or invalid.
- **State isolation:** payload, runtime state, receipts, retry counters, and idempotency keys belong to one policy effect instance. Do not share mutable static execution state across modules, policies, campaigns, or saves. Shared caches must be immutable snapshots or explicitly keyed and invalidated.
- **Failure isolation:** validate and preflight before mutation. Return a scoped `Skipped` or `Failed` result instead of mutating unrelated targets or instances. Rollback and compensation may consume only receipts owned by that instance and must not mark another module complete or failed.
- **Lifecycle isolation:** activation, daily work, renewal, expiry, abolition, rollback, and compensation operate on the persisted instance selected by the coordinator. MCM changes future retrieval only and must not rewrite active instances.
- **Module-call isolation:** modules do not invoke other modules at runtime. Compile-time composite expansion through `IPolicyEffectCompositeModule` is the only allowed module-to-module composition, and its descendants must be declared and Catalog-validated.
- **Host isolation:** a new effect must not install its own global scheduler, duplicate an existing Harmony patch, or directly rewrite central policy collections. Such work is a framework boundary change and must be reclassified before editing.

An error in one module must not disable Catalog discovery, MCM state, prompt fallback, persisted instances, or execution for unrelated valid modules. Fail closed for the affected module or instance and preserve the evidence needed for diagnosis.

## MCM Control Is Retrieval Control

The existing MCM button in `DuelSettings` opens `PolicyEffectModuleManagerUi`. A registered module with `PromptVisible = true` automatically appears there when its scope is supported.

The manager controls four retrieval contexts:

- `PlayerKingdom`
- `PlayerLocal`
- `NpcRulerKingdom`
- `PlayerVassal`

Apply these invariants:

- `AllowedScopes` determines which context toggles are available.
- New supported visible modules default to enabled.
- Saving updates `PolicyEffectModuleRetrievalSettings.json` atomically and replaces the cached snapshot.
- A disabled module is removed from routing for later policy requests in that context.
- A policy generation freezes its enabled module IDs. A later MCM save must not alter a pending or already-running generation.
- MCM retrieval settings are not a runtime kill switch. Never read them from module execution, model contribution, daily scheduling, persistence loading, rollback, or compensation paths.
- Existing active policies must continue from their compiled and persisted module instances after the source module is disabled for future retrieval.
- Hidden runtime descendants use `PromptVisible = false`, do not receive independent MCM rows, and remain controlled by the visible source module and its frozen `SourceModuleId` lineage.
- Unsupported scope/context combinations are normalized to disabled and must not be forced on in UI code.

If a request requires immediately stopping active effects, treat that as a separate lifecycle and migration feature. Do not overload the MCM retrieval toggle.

## Module Contracts

### Registration and descriptor

Every source module must carry:

```csharp
[assembly: global::AnimusForge.PolicyEffects.PolicyEffectModuleRegistration(
    typeof(global::AnimusForge.PolicyEffects.Modules.ExampleEffectModule))]
```

Use a globally unique canonical `Id` and `Order`. Declare legacy IDs only for real persisted aliases. Keep visible `CatalogSummary` and `PlayerDisplayName` concise and single-line.

Descriptor metadata must be internally consistent:

- `AllowedScopes`
- `AllowedSelectorKinds`
- `TargetKinds`
- `TargetProjection`
- `ExecutionKind`
- `Hook`
- `Aggregation`
- `ValueUnit`
- `FundingMode`
- `FundingStrategy`
- payload and runtime schema versions
- rollback and idempotency capabilities

Let Catalog validation reject inconsistent combinations. Do not weaken validation to make one module pass.

### Execution type

Implement exactly one primary execution interface matching `ExecutionKind`:

- `IModelModifierPolicyEffectModule`: build pure model contributions for the prepared canonical targets. Reuse `PolicyEffectModuleRuntimeAdapters` when the numeric pattern matches.
- `IDailyPolicyEffectModule`: perform one daily target-scoped mutation through the game bridge and return a receipt. Add `ICompensatingDailyPolicyEffectModule` when applied changes must be reversible.
- `IOneShotPolicyEffectModule`: apply transactionally and provide rollback with idempotency.
- `IScheduledOncePolicyEffectModule`: execute once through the scheduler and provide compensation with idempotency.
- `IPolicyEffectCompositeModule`: expose one prompt-facing capability and expand it at compile time into hidden runtime descendants.

Optional lifecycle or atomicity interfaces do not replace the single primary execution interface.

For a composite module:

- declare a non-empty unique `RuntimeModuleIds` lineage;
- keep descendants canonical, hidden, and non-composite;
- keep scope, selector kinds, target kinds, and projection compatible;
- preserve the visible source ID as `SourceModuleId`;
- never persist the compile-time composite as an executable instance.

### Payload, targets, funding, and persistence

Use typed payloads and strict normalization. Reject malformed shapes, non-finite values, invalid integer ranges, incompatible scopes, and unsupported schema versions. Never enable JSON type metadata such as `$type`.

Use the compiler-provided canonical target set. Do not resolve targets by free-form names inside an execution module and do not scan all heroes, clans, parties, or settlements on a hot path. If target projection is needed, use an existing projection or target-plan mechanism.

Funding transformation belongs in `TryApplyTypedFunding`. Do not reapply funding inside execution. Use `InheritPolicy` plus the appropriate strategy for policy-scaled numeric effects, and `Unscaled` only when the effect semantics genuinely require it.

Persisted identity and authorization belong to the framework:

- the compiler freezes candidate and detailed-module authorization;
- `SourceModuleId` records prompt-facing provenance;
- the save codec validates source-to-runtime lineage;
- unknown or invalid modules become inert rather than executing;
- the execution coordinator owns lifecycle state, retries, receipts, idempotency, rollback, and compensation orchestration.

A module must not bypass or rewrite those responsibilities. When increasing a payload or runtime schema version, implement and test the declared migration chain.

## Prompt Contract

For each visible source module, add:

`AnimusForge/CustomPrompts/Policy/Effects/<moduleId>.json`

with matching `Version`, `ModuleId`, `UnderstandingPrompt`, and `EvaluationPrompt`.

The prompt must state direct causal meaning, target unit, sign, frequency, scale, and exclusions. Keep the descriptor fallback prompts semantically aligned with the JSON. Hidden runtime descendants do not get prompt files.

A filename alone does not register or authorize a module. Unknown files remain inert. Do not change prompt loading into directory enumeration.

## Performance Contract

Before implementation, state:

- trigger and expected frequency;
- maximum targets processed per invocation;
- whether execution is model-time, activation-time, daily, scheduled-once, or lifecycle-event driven;
- cache, index, or batching strategy.

Required defaults:

- keep descriptors static and allocation-free after initialization;
- keep reflection discovery at Catalog initialization only;
- use cached Catalog, routing, prompt, and runtime-index snapshots;
- use dictionary or indexed lookup for hot paths;
- process only compiler-materialized targets;
- avoid repeated reflection, full-world scans, prompt-directory scans, per-tick file I/O, pointless allocations, locks, and polling;
- do not trade away validation, authorization, idempotency, compensation, or existing behavior for speed.

## Implementation Workflow

1. Translate the request into an effect contract: cause, scope, selector, final target, frequency, unit, funding, duration, reversibility, and MCM contexts.
2. Choose and read the closest existing module and its host model or game bridge adapter.
3. Confirm that current hooks and target planning can express the effect. Keep core files untouched when they can.
4. Implement the module, assembly registration, descriptor, typed payload, strict validation, and only the matching execution method.
5. Add the prompt JSON for a visible source module.
6. Confirm automatic MCM coverage through `PromptVisible` and `AllowedScopes`; do not add per-module MCM properties.
7. Extend focused contract tests for discovery, unique IDs/orders, scope/context visibility, routing disablement, payloads, compile authorization, execution, persistence, idempotency, and rollback or compensation as applicable.
8. Verify both Bannerlord implementations, Bootstrap, and unified single-module stage through the established repository workflow. Do not modify that workflow.

## Verification

Use the smallest relevant checks first, then the required compatibility matrix for a completed module change.

At minimum verify:

- the new visible ID is present exactly once in Catalog and expected test lists;
- every supported MCM context can include it and every unsupported context cannot;
- disabling it removes it from new routing while an existing prepared or persisted instance remains executable;
- payload round-trip and invalid payload rejection;
- canonical target and source lineage authorization;
- execution frequency and value unit;
- funding scaling;
- receipt identity, retry behavior, rollback, or compensation where applicable;
- prompt JSON ID/version and fallback alignment;
- no full scan or file I/O was introduced into a hot path.

Run the focused `PolicyEffectModule.ContractTests` mode that covers the module, normally including `--policy-all-modules-contract-only`. Then use the repository unified build script described in the compatibility documentation to verify `BannerlordApi=1.3`, `BannerlordApi=1.4`, Bootstrap, and Stage.

Never claim a build, test, or in-game check passed unless it was actually run and its output confirms success.

## Stop and Reclassify

Do not treat the work as a simple module addition if it requires any of the following:

- a new global execution scheduler;
- a new target language or cross-module target resolver;
- a new save envelope;
- runtime hot-unload or immediate cancellation of active effects;
- changes to unified build or module output;
- third-party assembly discovery.

Report the boundary change and get an explicit decision before expanding into those areas.
