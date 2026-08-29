# AF repository structure and cleanup

Use this reference for inventory, cleanup, directory migration, large assets, tracked artifacts, docs organization and reproducible-build boundaries.

## Cleanup precedes broad decomposition

Do not broadly extract modules until the repository gate in the execution ledger is complete. Cleanup establishes:

- one canonical worktree;
- recoverable baseline and representative user/save data;
- tracked-asset inventory and ownership;
- license/distribution decisions;
- reproducible local dependency preparation;
- artifact/user-data separation;
- package/stage baseline;
- target directory and module-owner mapping.

## Target planes

```text
src/
  AF.Contracts/
  AF.Foundation.Runtime/
  AF.GameAdapter.Bannerlord/
  AF.Persistence/
  AF.Bootstrap/
  modules/AF.Module.<Name>/
  bridges/AF.Bridge.<A><B>/

content/
  foundation/
  modules/<module-id>/
  bridges/<bridge-id>/
  profiles/

tests/
  contracts/
  foundation/
  modules/<module-id>/
  bridges/<bridge-id>/
  composition/
  persistence/
  compatibility/
  fixtures/

tools/        source only
scripts/      build/deploy/package/development/verification
docs/         architecture/modules/operations/compatibility/cases/handoffs/reference/archive
references/   manifests and reproducible local-extraction/verification scripts
design/       licensed source assets
local/        ignored machine-local game refs/private settings/reference snapshots
artifacts/    ignored stage/packages/logs/test output/diagnostics/tool distributions
```

Directory diagrams are targets, not permission for bulk moves.

## Classification table

| Current content | Default decision | Must verify first |
| --- | --- | --- |
| Root production C# | Assign owner, then move by one module/facade slice | Save type identity, build include, reflection paths, 1.3/1.4, tests |
| `AnimusForge/ModuleData` and GUI | Move to owning foundation/module/bridge content | Runtime paths, package map, user-writable vs static |
| PlayerExports | Split curated shipped data from user-mutated data | Deploy merge, backup, ownership, save references |
| Original/decompiled game sources and TaleWorlds DLLs | Prefer ignored local extraction + version/hash manifest | License/distribution rights and pinned 1.3 build provenance |
| `_deps_auto` | Generate/validate locally | Unified build's fail-closed 1.3 reference requirements |
| ONNX models | Decide Release/LFS/local installer separately | License, runtime requirement, current package rule excluding ONNX |
| Tool `dist`, EXE, DLL, RAR/ZIP | Remove from source tracking; rebuild or publish artifact | Source reproducibility and release channel |
| `.tmp`, `tmp`, `.codex_tmp`, `.dotnet*`, browser profiles | Ignore and stop tracking in approved batches | Accidental build/runtime dependency |
| Logs, JSONL, TRN, crash archives | Artifact plane; inspect for secrets/privacy before removal | Player text, prompts, API endpoints/keys, personal paths |
| Design previews/generated images | Keep licensed source only; outputs to artifacts | Which files ship at runtime |

## Safe cleanup order

1. Confirm worktree and backups.
2. Inventory tracked/untracked files, sizes, extensions and consumers.
3. Mark user data and no-delete paths.
4. Audit source/license/distribution/reproducibility.
5. Add/merge ignore, `local/`, and `artifacts/` rules.
6. Prove build/stage does not rely on accidental caches.
7. Stop tracking one approved category at a time, preserving local backups.
8. Validate clean-clone preparation and package layout.
9. Reorganize docs/scripts/content in small mapped batches.
10. Consider Git-history rewriting only in a separate announced maintenance window.

`git rm --cached` changes the index, not local data. It still requires ledger intent, approved classification, backup and validation.

## Source and artifact planes

Static analysis, docs, manifests and pure tests should pass on a clean source tree. Checks that consume built DLLs or a staged module must explicitly depend on the artifact-producing step. Never allow a stale artifact to make a source check pass.

## Module content ownership

Every shipped resource must belong to exactly one of:

- foundation;
- one module;
- one bridge;
- one profile composition file.

Shared content is not automatically foundation content. Promote only stable, truly cross-module assets with an owner and current consumers. Modules must not overwrite another module's content path.

## Documentation organization

- `docs/architecture`: foundation/contracts/global decisions and ADRs.
- `docs/modules`: generated/current module catalog, owner map, capabilities, profiles and bridge matrix.
- `docs/operations`: environment, build, stage, deploy, package, recovery.
- `docs/compatibility`: Bannerlord API lines and game-specific adapters.
- `docs/cases`: reusable validated maintenance cases.
- `docs/handoffs`: time-bound handoffs, not authority for current architecture.
- `docs/archive`: frozen historical decisions/handoffs.

One fact should have one authoritative home. Other docs link to it instead of copying it.
