# Bannerlord compatibility and unified module contract

Use this reference for TaleWorlds APIs, Harmony, Campaign/Mission/Encounter/UI behavior, Bootstrap, dual API implementations, build, stage, deploy and package changes.

Repository-local compatibility and output docs remain the detailed source of truth. Read them before implementation, especially:

```text
docs/bannerlord_1_3_to_1_4_5_compatibility_diff.md
docs/bannerlord_dual_module_output.md
```

## Unified module invariant

```text
Modules/AnimusForge/
  SubModule.xml
  bin/Win64_Shipping_Client/
    AnimusForge.Bootstrap.dll
    versions/1.3/AnimusForge.dll
    versions/1.4/AnimusForge.dll
    <allowlisted private runtime dependencies/module assemblies>
```

- `SubModule.xml` declares only Bootstrap.
- Bootstrap detects a supported API line and loads exactly one implementation.
- Both implementation files keep the simple assembly/save identity required by existing code and saves.
- Retired `AnimusForge_1_3_x` / `AnimusForge_1_4_5` launcher modules are not valid outputs.
- Never copy TaleWorlds assemblies into the module/package.
- Build, package and deploy remain separate; compilation never implicitly overwrites the game.

## Module packaging under the plugin architecture

Foundation/modules/bridges may become controlled DLLs, but they remain within one validated AnimusForge launcher module. The profile manifest must identify the complete DLL/content closure for each API implementation.

Before approving a physical assembly split, prove:

- its dependencies resolve under net472/Bannerlord runtime;
- Bootstrap/implementation assembly resolution remains unambiguous;
- 1.3 and 1.4 each receive compatible builds;
- save types remain available under expected assembly/type identity or have a migration;
- package allowlist and hashes include the assembly;
- no implementation DLL is declared directly in `SubModule.xml`;
- the module lifecycle does not rely on unsupported runtime unload.

Logical module boundaries may precede physical DLL splits.

## API difference strategy

Prefer in order:

1. Existing compatibility helper.
2. Narrow GameAdapter around the changed domain.
3. Compile-time `BANNERLORD_1_4_OR_GREATER` only for signatures/members that cannot compile in both lines.
4. Cached, null-safe reflection only when a stable public seam is unavailable.

Keep common gameplay outside version branches. Business modules consume GameAdapter contracts, not direct version tests or private TaleWorlds reflection.

Every Harmony patch records:

```text
Owning module/GameAdapter
Target type/method/signature per API line
Patch ID
Lifecycle class
Conflict/arbitration rule
Failure/degradation behavior
Focused test or in-game scenario
```

Profile resolution must catch known patch conflicts before campaign load when possible. Do not depend on patch order by accident.

## Build evidence

A compatibility-sensitive completion normally requires:

- verified 1.3 reference provenance/pinned overlay;
- 1.3 implementation build;
- verified 1.4 game root/reference line;
- 1.4 implementation build;
- Bootstrap build;
- implementation markers/hash checks;
- profile/module/bridge closure checks;
- staged single-module validation;
- package allowlist/no forbidden entries;
- focused in-game validation on each affected API line when runtime behavior changed.

A compile symbol alone does not prove reference provenance. A successful compile does not prove Harmony target/runtime behavior.

## Deploy safety

- Use same-volume complete staging and transactional replacement.
- Preserve/merge user PlayerExports and logs according to repository policy.
- Never delete unknown module persistence during profile changes or SafeMode.
- Validate package before final publication.
- Keep source and artifact planes separate.
- Record module inventory, version, API markers and SHA-256 with a release.
