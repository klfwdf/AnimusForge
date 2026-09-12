---
name: animusforge-maintainer
description: "Develop and maintain the specific Mount & Blade II: Bannerlord mod AnimusForge (AF/AFmod): features, gameplay, bug fixes, UI/content/configuration, integration APIs, performance, dual 1.3/1.4 compatibility, save and interaction safety, validation, packaging and handoff; also repository cleanup and refactoring when requested. Apply only to positively identified AnimusForge work; exclude other Bannerlord mods, generic C# projects, Minecraft, and unrelated AF abbreviations."
metadata:
  version: "0.1.1"
  local-adaptation: "af-core-framework-coordination-v1"
  short-description: "Develop and maintain AFmod with ownership, safety and compatibility constraints"
---

# AnimusForge Maintainer

Skill version: `0.1.1` — this versions the maintenance skill, not the mod, API or save schema. The authoritative value is `metadata.version`; keep this display synchronized.

Repository integration: the imported 0.1.1 rules have a documented local coordination adaptation, not an upstream release claim. When this repository also has `af-core-framework`, read [framework-coordination.md](references/framework-coordination.md) for task routing and the same-DLL internal/public boundary.

Treat AnimusForge as a long-lived, multi-author platform: one conservative foundation, independently owned gameplay modules, and explicitly co-owned bridge modules for cross-module behavior.

This is the **AF mod development, maintenance and constraints skill**, not a refactoring-only skill. It covers new features and gameplay, bug fixes, UI/assets/localization/configuration, module and sub-MOD integration, performance, compatibility, testing and release preparation. Repository cleanup and refactoring are specialized workflows within that scope, not the default purpose of every task.

This skill contains durable AF development and maintenance rules. It does **not** replace the live execution ledger, current repository state, the latest user instruction, or build/test evidence.

This directory is intentionally portable between Claude Code and Codex. Read [host-compatibility.md](references/host-compatibility.md) only when installing, updating, or validating skill discovery in either host; it does not change AF maintenance rules.

## 1. Prove this is AnimusForge before automatic routing

Automatically apply this skill only when at least one strong identity signal is present:

- the user explicitly names `AnimusForge`, `AFmod`, or `Mount-Blade-Bannerlord-AnimusForge-mod` in a Bannerlord-mod context;
- the working tree contains `AnimusForge.csproj` plus `AnimusForge/SubModule.xml` (or the legacy `AnimusForge/ModuleData/SubModule.xml` layout);
- the project contains `AnimusForge.Bootstrap` and selects versioned `AnimusForge.dll` implementations;
- characteristic AF sources such as `MyBehavior.cs`, `ShoutBehavior.cs`, `RewardSystemBehavior.cs`, and `DuelSettings.cs` occur together;
- the AF execution ledger `animusforge-refactoring-and-repository-reorganization-plan.md` identifies the workspace.

Do not auto-route from weak signals alone:

- a directory or variable named only `AF`;
- a generic Bannerlord mod request with no AnimusForge evidence;
- another project using `Animus`, `Forge`, `AF`, or an LLM;
- Minecraft Forge/NeoForge work;
- a request that merely links to DSH or asks about plugin architecture generally.

Manual invocation (`/animusforge-maintainer`, `$animusforge-maintainer`, or an explicit request to use the AF skill) overrides auto-detection, but still does not authorize editing an unidentified source copy.

If identity is uncertain, perform only bounded read-only inspection and ask for clarification only when repository evidence cannot decide.

Read [routing-and-identity.md](references/routing-and-identity.md) when routing, locating the canonical worktree, distinguishing backups/ZIPs, or reconciling multiple AF copies.

### Choose the actual task, not a permanent refactor agenda

| Request | Default workflow |
| --- | --- |
| New feature/gameplay or a bug fix | Identify the existing owner and approved behavior, implement the smallest complete change, validate its affected surfaces. |
| UI/content/Prompt/configuration | Check source of truth, runtime loading, user overrides and content ownership; test the actual consumer and packaging impact. |
| Performance/threading/compatibility | Reproduce or measure the affected path, preserve required behavior, apply the corresponding runtime/API rules. |
| Internal module or public sub-MOD integration | Define the supported contract, lifecycle and failure semantics; keep internal and external promises separate. |
| Build/test/package/handoff | Follow the existing workflow and explicit action authorization; report exactly what the evidence proves. |
| Refactoring/repository cleanup/original-plan audit | Additionally apply extraction/cleanup gates and the revision-bound refactor review checklist. |

For ordinary production work, read [mod-development.md](references/mod-development.md). A routine fix does not require a new module, manifest, whole-project refactor or historical R01–R07 audit. Conversely, calling broad decomposition a feature task does not bypass its gates. A historical “core-only” project scope is not this skill's permanent limit: current authorization and the actual domain owner determine whether gameplay/module work is in scope.

## 2. Follow the non-skippable start protocol

Before planning or editing AF:

1. Locate the AF execution ledger. Preferred name:
   `animusforge-refactoring-and-repository-reorganization-plan.md`.
2. Read its current status, canonical-worktree decision, active task, phase gate, change record, and handoff snapshot.
3. Verify those claims against the latest user request, current paths, Git root/branch/HEAD/status, build outputs, and tests.
4. Treat the latest user instruction and repository evidence as newer than stale ledger text, but record every resolved mismatch back into the ledger.
5. Do not edit a ZIP, archive, backup, release directory, decompiled reference tree, temporary audit clone, or ambiguous copy.
6. If the canonical worktree is still unknown, perform only the ledger's canonical-worktree discovery task. Do not start source, Git-index, asset, build, package, or migration changes.
7. Before the first write, mark the selected ledger task active and add an intent row with scope, paths, risks, and validation.
8. At each meaningful checkpoint, blocker, rollback, completion, or handoff, update the ledger before claiming progress.

Resolve the current status entry from the latest user request, actual Git state and explicit supersession links before choosing the applicable ledger. The selected ledger records execution state; this skill provides durable method and architecture constraints, not a replacement for approved project scope. Do not copy the full live task table into this skill. A revision-bound review checklist may preserve findings as reference evidence, but it must never silently become the current ledger.

For a read-only review, do not write the ledger, check out another branch over existing work, or implement findings. Bind conclusions to the requested remote/branch/commit and distinguish source inspection from executed tests. If the user requests only this standalone skill to be updated, edit only that selected skill; do not require an AF source checkout or update/install other copies. Record the scope and validation without creating a second project execution ledger.

Read [ledger-and-handoff.md](references/ledger-and-handoff.md) for the exact write-back protocol.

### Reconcile scope before judging completion or deviation

- Compare the original plan with the latest explicit scope decision and actual code. A repository handoff can document a past decision; it cannot grant this session new implementation, deletion, deployment or publication authority.
- Classify findings separately: confirmed defect/constraint gap, explicitly unfinished work, authorized scope adjustment/HOLD, and stale status/documentation. Do not label all four as unauthorized architectural drift.
- A narrower task does not complete the excluded original goals. Map each original requirement to implemented, partial, deferred/HOLD, transferred to an owner, or explicitly out of scope with its rationale. A transferred task is not implemented merely because another owner exists.
- Keep repository, lifecycle, compatibility and LIVE/SAVE gates independent. A permitted small adapter or static slice does not authorize broad decomposition or final/default cutover.
- For reviews against the original plan, use [refactor-review-checklist.md](references/refactor-review-checklist.md); recheck any dated finding against the actual target revision.

## 3. Enforce repository cleanup before broad decomposition

This section governs repository cleanup and broad architectural decomposition, **not a blanket freeze on normal mod development**. An authorized feature, bug fix, content/configuration update or compatibility patch may proceed within its established owner without completing unrelated cleanup first. Preserve user data, old identities and the existing build flow; apply any directly relevant gate.

For cleanup and broad refactoring, AF's cleanup gate remains binding. Until the ledger's repository gate is complete:

- allow inventory, ownership mapping, data classification, license review, `.gitignore`/artifact-plane design, reproducible-build preparation, and ledger/docs work;
- do not broadly move production C#;
- do not introduce the final module assembly graph;
- do not change save type identity or existing `SyncData` keys;
- do not replace the three-channel interaction pipeline;
- do not delete tracked assets merely because they look generated;
- do not edit or package against an unconfirmed worktree.

Separate source, content, tests, tools, scripts, documentation, references, local dependencies, and artifacts. Treat PlayerExports, logs, game DLLs, decompiled sources, ONNX assets, tool distributions, generated images, caches, archives, and user-writable module data according to ownership, license, reproducibility, and data-loss risk—not file extension alone.

Read [repository-structure.md](references/repository-structure.md) for the target planes and cleanup sequence.

## 4. Use the AF foundation/module/bridge architecture

### Foundation

`AF.Foundation.Runtime` describes a logical responsibility, not a mandatory additional DLL or a claim that a complete module host exists. It provides only capabilities that every module needs or that protect the host:

- module manifest validation, profile resolution, dependency graph, inventory, health, and failure states;
- stable `AF.Contracts` capability/event/DTO definitions and contract-version checks;
- Bannerlord main-thread dispatch, scheduler budgets, cancellation and stale-generation guards;
- settings snapshots, module enablement, diagnostics and trace IDs;
- persistence namespaces, migration catalog, save-size/chunk protection;
- controlled GameAdapter ports for TaleWorlds/Harmony/1.3-1.4 differences;
- SafeMode and explicit fallback selection.

The foundation must not own module gameplay rules, module-private prompts/tags/data, or module-pair-specific behavior. “AF core/body” is not a synonym for Foundation: Conversation, Prompt, Action and Memory responsibilities still have their domain owners even inside the same DLL.

### Modules

Each `AF.Module.*` has one clear owner and can evolve without unrelated authors editing it. It declares:

- stable module ID, version, contract version, owner and maintainers;
- required/optional modules and capabilities;
- provided capabilities and typed events;
- supported profiles and Bannerlord API lines;
- persistence namespace/schema;
- lifecycle class (`boot-only`, `save-load-boundary`, or `runtime-toggle-safe`);
- Harmony/UI/tick/background effects;
- health check, focused tests, composition test, content ownership, limitations.

Modules depend on `AF.Contracts` and foundation ports, not on another module's private implementation.

### Bridges

Cross-module gameplay belongs in `AF.Bridge.<A><B>`, not in the foundation and not hidden inside A or B.

A bridge must:

- be co-owned/reviewed by the participating module maintainers;
- consume only supported cross-owner capabilities/events; same-DLL contracts may be C# `internal`, while independent sub-MOD contracts are separately `public` and versioned;
- own its cross-module state in a separate persistence namespace;
- document behavior with A alone, B alone, A+B without the bridge, A+B+bridge, and bridge failure;
- leave A and B independently usable when absent, disabled, incompatible, or failed.

No owner means no bridge implementation. Do not make the foundation inherit abandoned gameplay.

Read [plugin-architecture.md](references/plugin-architecture.md) for manifests, capability seams, lifecycle, inventory, SafeMode, and DSH-inspired constraints. Read [module-and-bridge-workflow.md](references/module-and-bridge-workflow.md) before adding, extracting, or integrating a module or bridge.

## 5. Route only the AF references needed for the task

Keep this file loaded, then read only relevant references:

- ordinary feature/gameplay, bug fix, UI/content/configuration and development workflow: [mod-development.md](references/mod-development.md)
- repository identity, multiple copies, canonical worktree, audit clone: [routing-and-identity.md](references/routing-and-identity.md)
- execution-ledger write-back or cross-window continuation: [ledger-and-handoff.md](references/ledger-and-handoff.md)
- repository inventory, cleanup, directory migration, large assets, artifacts: [repository-structure.md](references/repository-structure.md)
- foundation, module manifests, profiles, capabilities, lifecycle, failure isolation: [plugin-architecture.md](references/plugin-architecture.md)
- new module, module extraction, bridge ownership or composition: [module-and-bridge-workflow.md](references/module-and-bridge-workflow.md)
- Bootstrap, unified single module, dual 1.3/1.4 implementations, Harmony/API compatibility: [bannerlord-compatibility.md](references/bannerlord-compatibility.md)
- scene shout/native conversation/courier, preprocess/prompt/postprocess/action plan/history: [interaction-pipeline.md](references/interaction-pipeline.md)
- save types, `SyncData`, schemas, migration, PlayerExports, user data: [persistence-and-user-data.md](references/persistence-and-user-data.md)
- Harmony/reflection/tick/async/main-thread boundaries, health/fallback diagnostics: [runtime-safety.md](references/runtime-safety.md)
- tests, build matrix, package/profile closure, in-game acceptance: [validation.md](references/validation.md)
- current hotspots, strangler order, God Objects and deferred debt: [known-debt.md](references/known-debt.md)
- original-plan comparison, scope changes, real module-host versus catalog evidence, and dated review follow-ups: [refactor-review-checklist.md](references/refactor-review-checklist.md)

The repository's own current docs and ledger outrank bundled snapshots in this skill when evidence conflicts. Report and reconcile conflicts instead of silently choosing one.

## 6. Preserve AF's non-negotiable runtime contracts

- Publish one `Modules/AnimusForge` launcher module.
- `SubModule.xml` loads only `AnimusForge.Bootstrap.dll`.
- Bootstrap selects exactly one versioned implementation:
  `versions/1.3/AnimusForge.dll` or `versions/1.4/AnimusForge.dll`.
- Never load both implementations and never recreate retired version-specific launcher modules.
- Preserve the `AnimusForge` assembly/save identity and existing serialized type/key compatibility until a tested migration explicitly replaces it.
- Keep TaleWorlds access and game-state mutation on the main thread.
- Background work receives immutable snapshots and returns results that are revalidated on the main thread.
- Keep scene shout, native conversation, and courier aligned in rule eligibility, prompt/history semantics, postprocess capabilities, action execution, and AFEF memory facts unless a documented exclusion explicitly applies.
- Internal action tags never leak into visible NPC text.
- Do not trust an LLM-produced action as game truth; parse, authorize, validate current targets, execute once, and record the result.
- Do not silently swallow failures at module, save, action, compatibility, or interaction boundaries. Report a bounded structured failure and explicit degradation.

## 7. Implement by an owned, reversible slice

For every non-trivial change:

1. Identify the owner: foundation, GameAdapter, one module, or a jointly owned bridge.
2. Identify affected contracts, profiles, module manifests, persistent namespaces, channels, Bannerlord API lines, Harmony/tick/UI contributions, and user data.
3. Keep public capability changes smaller than feature changes. Do not expose private Behavior classes, static fields, Harmony targets, UI VMs, or raw save dictionaries.
4. Keep old entry points as facades while strangling implementation into modules; remove them only after call-site, save, dual-version, channel, profile, and composition evidence exists.
5. One module extraction or bridge is one reviewable slice. Do not move hundreds of files to satisfy a directory diagram.
6. Do not promise runtime DLL hot loading/unloading. Modules with Harmony, save types, CampaignBehavior, or persistent state activate at boot or save-load boundaries.
7. Do not download arbitrary DLLs or run generated/untrusted C# in the Bannerlord process.
8. Stop if a third workaround is forming around the same seam; re-evaluate the contract or owner before another patch.

## 8. Validate according to the changed surface

Never infer success from static reasoning alone. Run the narrow strongest checks available and record exact commands/results in the ledger.

At minimum consider:

- manifest schema, unique ID, dependency cycle, contract version, owner and profile-closure checks;
- foundation no-op module and failure-isolation composition tests;
- module-alone, dependency-missing, optional-provider-missing, incompatible-version and SafeMode cases;
- bridge matrix: A, B, A+B, A+B+bridge, bridge failure;
- pure parser/action/prompt/config/persistence tests;
- Bannerlord 1.3 implementation build;
- Bannerlord 1.4 implementation build;
- Bootstrap build and implementation-selection metadata;
- staged module and ZIP allowlist, hashes, both implementations, no forbidden ONNX/game DLLs;
- representative old-save load and migration evidence;
- focused in-game scenarios for Campaign, Mission, Encounter, Gauntlet, Harmony and three interaction channels;
- thread, cancellation, stale generation, tick budget, queue bound and diagnostic evidence. Budget the actual jobs/records and elapsed work inside a callback, not only the number of callbacks dequeued.

If an environment cannot run a check, record `NOT-RUN` with the concrete reason and keep the task out of `DONE` when that check is an acceptance requirement.

Read [validation.md](references/validation.md).

## 9. Finish with ledger, module docs, and evidence

Before reporting completion:

- update the live ledger task state, actual paths, validation, evidence, risks, rollback and next step;
- update affected existing feature/module/bridge docs and declarations; require new manifests or ADRs only when the approved module/bridge/architecture change needs them, not as placeholders for routine fixes or Skill-only updates;
- update durable skill references only when a stable AF method or architecture rule changed;
- report only checks actually run;
- list unavailable checks and remaining risk;
- ensure no ledger item remains falsely active after work stops.

The final report should identify:

1. owning module/foundation/bridge;
2. concrete files and contracts changed;
3. save, profile, channel, 1.3/1.4 and user-data impact;
4. commands/tests and results;
5. module/bridge/ledger documentation updated;
6. remaining blockers and the next exact ledger task.
