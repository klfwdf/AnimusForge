# AF Mod development and maintenance

Use this reference for normal AnimusForge feature/gameplay development, bug fixes, UI/content/configuration work, integrations and maintenance. Refactoring is one possible technique, not the objective of every task. The skill is not permanently restricted to the scope of a historical core-framework project.

## 1. Scope the requested change

- Confirm the actual AF source worktree and current task/owner before writes; preserve other authors' work.
- State the observable current behavior, desired behavior, affected users/channels and explicit non-goals. For a bug, use a concrete reproduction; for a feature, define success and rejection/failure cases.
- Determine whether the change belongs to an existing gameplay owner, shared platform safety, GameAdapter, UI/content or a genuine jointly owned Bridge. Do not infer responsibility from a folder, the name DuelSettings, or being in the main DLL.
- Current user authorization and owner responsibility select the scope. An older task's “do not rewrite Policy/GCCZ” limit does not permanently prohibit authorized work on those domains; it also does not authorize touching a separately maintained source copy.
- Ordinary changes in an established owner do not require finishing the whole refactor plan, moving files, creating a module/DLL or introducing a manifest merely to make a small fix. New modules, cross-domain behavior and broad extraction still require their specific gates.

## 2. Implement complete features, not just new entry points

Trace the actual consumer path: trigger/input → qualification/context → behavior → authoritative result → persistence/notification/UI as applicable.

- Reuse existing owner methods, typed contracts, configuration and error handling before inventing parallel systems.
- Preserve unaffected behavior. Approved gameplay/parameter changes are allowed; document intentional differences and keep regression checks for what must remain unchanged. A prior characterization test is not a permanent ban on product improvement.
- Do not add new domain behavior indiscriminately to large shared Behavior/settings classes. Prefer the responsible domain service or a small real seam; when a focused existing entry must change, preserve its established ordering and compatibility.
- Define scope, target, quantities, preconditions, revalidation, partial outcome and duplicate handling for side effects. A request accepted or queued is not an executed action or confirmed fact.
- Do not add speculative abstractions, unconsumed interfaces, blanket configuration knobs or a generic service locator to a simple feature. Changes should read like the surrounding code.

## 3. Feature-specific surfaces

| Surface | Required checks |
| --- | --- |
| Gameplay / Campaign / Mission | Real target and state owner, allowed scene/campaign contexts, native fallback, event order, repeat/late execution, save/load effect. |
| LLM topics / Prompt / action tags | Eligibility → actual injected postprocess rules → RAW/FINAL → parse/authorize/execute → visible cleanup → confirmed history/AFEF; compare each applicable channel. |
| UI / Gauntlet / input | Actual prefab/datasource binding, main-thread updates, focus/shortcut handling, open/close/dispose and late callback behavior; consult the repository's matching UI case. |
| Content / localization / assets | Author/source/license, actual loader/resource path and identifiers, fallback/localization behavior, package inclusion; retain user-edited content and merge precedence. |
| Settings / JSON / MCM | Existing configuration owner, defaults and validation, unknown/null/legacy handling, capture/reload timing and restart semantics; do not duplicate a competing settings source. |
| Internal or sub-MOD API | Real consumer, version/shape, immutable external DTOs, lifecycle/capability availability, unsupported/failure results and old-consumer compatibility. |
| Performance / threading | Reproduce or measure the hot path, frequency and actual work budget; capture game state on its owner thread, background pure/network work, stale/source guards on completion. |
| Persistence | Existing type/assembly/key/enum identities, disabled/unknown data retention, migration/failure/rollback and representative old-save evidence when affected. |

Read the relevant existing project case before changing its mechanism; examples include encyclopedia injection, scene damage contexts, army-member meeting targets, scene Agent movement, action tags and three-channel alignment. Do not activate unrelated product skills or treat readable reference source as automatic reverse-engineering work.

## 4. Validate proportional to the changed surface

- First reproduce the bug or define the new feature's positive, negative and lifecycle cases. Run the narrow strongest existing tests; add focused tests for behavior the change introduces.
- Use actual production source where practical and label fakes/projections. A text assertion is not runtime behavior, a Compile item list is not a full build, and a fixture is not a real Campaign/Mission.
- For production code/API compatibility changes, validate both supported implementation lines and Bootstrap as applicable through the existing official workflow. Pure docs/skill work does not need unrelated game builds.
- UI/resources require real loader/binding/package checks and visual or game acceptance when relevant; feature-specific evidence cannot be replaced by an unrelated module catalog passing.
- A missing game environment must be recorded precisely, but should not halt independent local analysis, pure logic tests or authorized source work. Do not claim release/runtime acceptance without its required evidence.
- Preserve official build/cover commands. Build-only, Stage/package, installation, game deployment and publication are separate actions; execute only the authorized ones, never deploy as an incidental test step.

See [validation.md](validation.md), [runtime-safety.md](runtime-safety.md), [interaction-pipeline.md](interaction-pipeline.md) and [persistence-and-user-data.md](persistence-and-user-data.md).

## 5. Finish at the right scope

Record changed behavior, owner, files, configuration/content/save/API impact, checks actually run, missing acceptance and rollback. Update the affected feature/module docs and the project's current task entry, not every historical refactor document.

A feature can be locally implemented and tested while broader refactoring remains unfinished. Conversely, directory cleanup or passing architecture fixtures cannot make an unimplemented feature complete. Only use [refactor-review-checklist.md](refactor-review-checklist.md) when the actual task is original-plan comparison or refactoring review; its historical issue list is not a standing prerequisite for making AF mods.
