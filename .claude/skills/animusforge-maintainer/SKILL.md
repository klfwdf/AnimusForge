---
name: animusforge-maintainer
description: "Maintain the specific Mount & Blade II: Bannerlord mod AnimusForge (AF/AFmod): repository cleanup, module/bridge architecture, dual 1.3/1.4 Bootstrap packaging, save and interaction safety, validation, and execution-ledger handoff. Apply only to positively identified AnimusForge work; exclude other Bannerlord mods, generic C# projects, Minecraft, and unrelated AF abbreviations."
metadata:
  short-description: "Maintain verified AnimusForge/AFmod safely"
---

# AnimusForge Maintainer

Treat AnimusForge as a long-lived, multi-author platform: one conservative foundation, independently owned gameplay modules, and explicitly co-owned bridge modules for cross-module behavior.

This skill contains durable AF maintenance rules. It does **not** replace the live execution ledger, current repository state, the latest user instruction, or build/test evidence.

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

The ledger is the live execution-state authority. This skill is the durable method and architecture authority. Do not copy the full task table into this skill.

Read [ledger-and-handoff.md](references/ledger-and-handoff.md) for the exact write-back protocol.

## 3. Enforce repository cleanup before broad decomposition

AF's cleanup gate remains binding. Until the ledger's repository gate is complete:

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

`AF.Foundation.Runtime` provides only capabilities that every module needs or that protect the host:

- module manifest validation, profile resolution, dependency graph, inventory, health, and failure states;
- stable `AF.Contracts` capability/event/DTO definitions and contract-version checks;
- Bannerlord main-thread dispatch, scheduler budgets, cancellation and stale-generation guards;
- settings snapshots, module enablement, diagnostics and trace IDs;
- persistence namespaces, migration catalog, save-size/chunk protection;
- controlled GameAdapter ports for TaleWorlds/Harmony/1.3-1.4 differences;
- SafeMode and explicit fallback selection.

The foundation must not own module gameplay rules, module-private prompts/tags/data, or module-pair-specific behavior.

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
- consume only public capabilities/events;
- own its cross-module state in a separate persistence namespace;
- document behavior with A alone, B alone, A+B without the bridge, A+B+bridge, and bridge failure;
- leave A and B independently usable when absent, disabled, incompatible, or failed.

No owner means no bridge implementation. Do not make the foundation inherit abandoned gameplay.

Read [plugin-architecture.md](references/plugin-architecture.md) for manifests, capability seams, lifecycle, inventory, SafeMode, and DSH-inspired constraints. Read [module-and-bridge-workflow.md](references/module-and-bridge-workflow.md) before adding, extracting, or integrating a module or bridge.

## 5. Route only the AF references needed for the task

Keep this file loaded, then read only relevant references:

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
- thread, cancellation, stale generation, tick budget, queue bound and diagnostic evidence.

If an environment cannot run a check, record `NOT-RUN` with the concrete reason and keep the task out of `DONE` when that check is an acceptance requirement.

Read [validation.md](references/validation.md).

## 9. Finish with ledger, module docs, and evidence

Before reporting completion:

- update the live ledger task state, actual paths, validation, evidence, risks, rollback and next step;
- update affected module/bridge `README.md`, `module.yaml`, owner/capability/profile/persistence catalogs, and architecture ADRs;
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
