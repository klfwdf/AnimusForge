# Routing and AnimusForge identity

Use this reference to decide whether the AF skill should apply and to locate the one worktree that may be changed.

## Positive identity signals

Treat these as strong evidence when they occur in a coherent Bannerlord mod tree:

| Signal | Strength | Notes |
| --- | --- | --- |
| User explicitly says AnimusForge or links `Mount-Blade-Bannerlord-AnimusForge-mod` | Strong | Confirm the requested path is not only a ZIP/archive copy. |
| `AnimusForge.csproj` and `AnimusForge/SubModule.xml` | Strong pair | The current AF layout; inspect both rather than routing from a single filename. |
| `AnimusForge.csproj` and `AnimusForge/ModuleData/SubModule.xml` | Legacy strong pair | Accept only for an older known layout; verify the current module structure before writes. |
| `AnimusForge.Bootstrap` plus `versions/1.3` / `versions/1.4` implementation contract | Strong | Distinctive unified-module topology. |
| `MyBehavior.cs`, `ShoutBehavior.cs`, `RewardSystemBehavior.cs`, `DuelSettings.cs` together | Strong combination | Do not route from one generic class name alone. |
| AF execution ledger names the workspace | Strong | Verify ledger claims against current disk/Git state. |
| Module manifest IDs beginning `af.foundation.`, `af.module.`, or `af.bridge.` | Strong after migration begins | Validate against `AF.Contracts` schema, not prefix alone. |

## Negative and weak signals

Do not auto-route solely because:

- the path contains `AF`, `afmod`, `forge`, `animus`, or `mod` without coherent AF project evidence;
- the project is another Mount & Blade II: Bannerlord mod;
- the task concerns Minecraft Forge/NeoForge;
- a variable/class happens to be abbreviated `AF`;
- DSH/plugin architecture is being discussed outside AnimusForge;
- the user asks generically about C#, Harmony, LLMs, NPCs, prompts, or save systems.

Manual invocation is valid but does not establish which copy may be edited.

## Canonical-worktree protocol

Before writes:

1. Find Git roots under the user-selected AF area without treating ZIPs or extracted release folders as repositories.
2. For each candidate record:
   - absolute path;
   - repository remote;
   - branch and HEAD;
   - dirty status;
   - whether it is source, release, backup, audit clone, decompiled reference, staging, or test instance;
   - relationship to the latest user request.
3. Read the AF execution ledger's canonical-worktree task/status.
4. Compare repository identity and revision with the ledger's audit/reference baseline.
5. Select exactly one write target. Mark all other copies read-only, backup, archive, reference, or unresolved.
6. Write the decision and evidence back to the ledger before source/Git-index/build/asset changes.

If no Git root exists, do not initialize one or choose an extracted folder without an explicit project decision. Keep work read-only and report the blocker in the ledger.

## Routing examples

| Request/workspace | Auto-route? | Reason |
| --- | --- | --- |
| “修 AnimusForge 的信使后处理” in verified AF tree | Yes | Product name and characteristic subsystem. |
| `/Volumes/.../AFmod/...` with AF ledger and characteristic sources | Yes after identity check | Path alone is weak; combined evidence is strong. |
| “分析一个普通骑砍 2 Mod 的 Harmony 崩溃” | No | Generic Bannerlord task. |
| “Minecraft Forge 1.20.1 的 AF 模组” | No | Minecraft route, not AnimusForge. |
| “参考 DSH 设计插件系统” outside AF | No | Inspiration project is not AF identity. |
| Explicit “use the AnimusForge skill on this copied source” | Manual route, read-only until copy role is confirmed | Invocation does not authorize ambiguous writes. |

## Precedence

1. Latest user instruction.
2. Current repository/Git/build/test evidence.
3. Current AF execution ledger.
4. Repository-local authoritative docs and module manifests.
5. This skill's durable references.
6. Historical audit notes and old handoffs.

When levels disagree, report and reconcile the mismatch. Never silently overwrite newer on-disk changes with bundled skill assumptions.
