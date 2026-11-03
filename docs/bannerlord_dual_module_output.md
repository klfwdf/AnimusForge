# Bannerlord Unified Module / Dual Runtime Output

AnimusForge uses one source tree, builds two Bannerlord-compatible implementation DLLs, and publishes one launcher module. The historical filename is retained so existing repository references do not break; the old two-module output contract is retired.

## Default Level-9 LZMA2 7z Output (2026-11-04)

The one-click package entry defaults to two level-9 LZMA2 7z archives:
AnimusForge_vVERSION_CN_MMdd_HHmm.7z and AnimusForge_vVERSION_EN_MMdd_HHmm.7z.
The pair shares one version, timestamp and collision suffix; existing packages are never overwritten.
The shared encoder is scripts/build/package_sevenzip.ps1. Its fixed profile is LZMA2, level 9, a 16 MiB dictionary, fb273, solid sorting, two threads and no file timestamps (`-mx=9 -m0=LZMA2:d=16m:fb=273 -ms=on -mqs=on -mmt=2 -mtm=off -mtc=off -mta=off`). Do not reintroduce the legacy LZMA literal-context/position overrides: their sum exceeded the liblzma decoder limit. The archive contains the AnimusForge/ tree directly.

The 16 MiB profile was selected after a same-content comparison: 169.45 MiB versus 169.37 MiB with a 64 MiB dictionary on the tested 1.8.0 CN package. These are historical measurements, not future package-size or RAM guarantees. Dictionary size is an encoder setting, not archive size, total decoder memory or in-game RAM.

Compression-method support, Unicode filename support and content integrity are separate acceptance checks. A Windows tar readback previously skipped 21 AD 1259 persona files whose names could not be represented in the local ANSI code page; changing the compression profile does not repair that filename-conversion issue. Keep the original names, hero IDs and content. Successful 7-Zip readback does not prove Windows Explorer compatibility; use independent 7-Zip for the verified full extraction path.

ArchiveFormat defaults to 7z; an explicit -ArchiveFormat zip preserves the legacy writer for compatibility. The existing ZIP projection, layout, DLL and version-transaction checks remain an internal step for the normal verified flow; 7z readback is compared with the locked projection before publishing either final archive. References to ZIP streams/checks below describe that internal or explicitly selected legacy format, not the default delivery extension.

Models are included by default from AnimusForge/ONNX/bekko. To select another original or quantized bekko payload, pass -OnnxModelDir with the directory containing model.onnx, tokenizer.json and config.json. Only these three files are projected to ONNX/bekko in both editions; vector caches and retired rerankers are not bundled. No model is copied into the game or source by packaging.

This changes archive creation only. The existing build, Bootstrap/dual-implementation layout, version increments, rollback and game-directory protection remain. Session-specific requests to omit tests or hashes do not remove the normal future release gates; such a one-off delivery must record the omitted evidence separately.

## Output Contract

All build, deploy, overwrite, package, and push flows must target exactly one Bannerlord module:

- Folder: `Modules/AnimusForge`
- Module id: `<Id value="AnimusForge" />`
- Module name: `<Name value="AnimusForge" />`
- Launcher entry: one AnimusForge entry only

The complete runtime layout is:

```text
Modules/AnimusForge/
├─ SubModule.xml
├─ bin/Win64_Shipping_Client/
│  ├─ AnimusForge.Bootstrap.dll
│  ├─ allowlisted private runtime dependencies
│  └─ versions/
│     ├─ 1.3/AnimusForge.dll
│     └─ 1.4/AnimusForge.dll
├─ ModuleData/
└─ other shared module assets
```

`SubModule.xml` must list only `AnimusForge.Bootstrap.dll`. It must not list either implementation DLL. Both implementation assemblies retain the assembly name `AnimusForge`, but only the implementation selected for the running game version may be loaded into the process.

The Bootstrap must:

1. Detect the supported Bannerlord API line from loaded TaleWorlds assemblies or an equally authoritative runtime signal.
2. Select `bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll` for supported 1.3.x builds or `bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll` for supported 1.4.x builds.
3. Load exactly one implementation assembly and forward the required submodule lifecycle callbacks.
4. Log the detected game version, selected implementation path, and implementation file version.
5. Fail closed with a clear diagnostic when the runtime version is unsupported or ambiguous. It must never guess and load the other implementation.

The Bootstrap must stay minimal and compile against API surface shared by both game lines. Harmony scanning, UIExtender registration, campaign behaviors, save types, and gameplay code belong to the selected implementation rather than the Bootstrap.

## Player Package Debug Symbols

PDB files remain in local implementation/Bootstrap build artifacts and the clean Stage for debugging. Client CN/EN archives exclude every .pdb; ZIP layout and Stage-to-package comparisons use that release projection. Required DLLs and build markers stay in the package. This does not change the one-click build or game-overwrite flow.

Player archives also omit the Chinese low-spec Calradia Chronicles worldbook (the folder beginning with low-spec Calradia Chronicles and mentioning 12400f) and its English counterpart, Calradia Chronicles (Lite Edition). Both source editions and local Stage originals remain available; the full Calradia Chronicles and other worldbooks remain in the player packages. The shared player-package omission predicate applies before language projection and when comparing archive contents with Stage.

## Build Rules

Both implementation DLLs are still required and must be built sequentially through the verified unified build entry:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build\build_single_module.ps1 `
  -ProjectRoot . `
  -BannerlordRoot "<Bannerlord root>" `
  -Configuration Debug `
  -Stage
```

Direct `BannerlordApi=1.3` project builds are intentionally fail-closed because the compile symbol alone cannot prove reference provenance. The unified script validates the pinned 1.3 `BuildInfo` and required reference overlay before enabling that build.

The build pipeline must capture the 1.3 output before starting the 1.4 build, or use isolated output/intermediate directories. The two builds must never race through shared `bin` or `obj` paths.

The unified build must validate that the pinned 1.3 reference overlay really reports a 1.3.x `BuildInfo` version and that the selected Bannerlord root reports a 1.4.x version. Missing or wrong-line references are fatal; a DLL must never receive a 1.3/1.4 marker merely because the corresponding compile symbol was set. Build metadata records the exact reference game version and SHA-256 for traceability.

The 1.4 build defines `BANNERLORD_1_4_OR_GREATER`. Prefer shared compatibility helpers; use `#if BANNERLORD_1_4_OR_GREATER` only for compile-time API or signature differences.

The Bootstrap must be built independently from the implementation DLLs. Building either implementation must not replace the Bootstrap, and building the Bootstrap must not change the implementation assembly name.

## One-click Script Rules

Every function under `一键编译覆盖推送` (the `.bat` entry points) and `scripts/build` (the PowerShell implementations they call) must use the unified contract:

- Build: build Bootstrap plus both implementation variants, then assemble one staged `AnimusForge` module.
- Deploy/overwrite: assemble and validate the project-local Stage against current build artifacts, then replace only Stage-listed files in `Modules/AnimusForge` through same-volume per-file candidates. Deployment creates no Recovery directory, backup, manifest, completion marker, or automatic rollback; a failure stops and reports the target state for manual inspection before retry. Preserve existing `CustomPrompts` and `ModuleData/TerminalSettings.json`, installing only missing files. Merge `PlayerExports` by modification time (newer wins; installed files win ties). Unlisted installed files are never mirrored or deleted.
- Build and overwrite: run the unified build, deploy the one module, and retain the existing optional launch behavior.
- Package: project the one staged module into CN and EN level-9 LZMA2 7z archives, each containing both implementation DLLs; ZIP is an explicit compatibility option.
- Push: publish the same unified source/output contract; no step may recreate or publish the legacy two modules.
- Validation: verify the module id, Bootstrap declaration, both implementation paths, and absence of conflicting implementation declarations before deployment or packaging.

Scripts must consume explicitly captured 1.3 and 1.4 build artifacts. A successful file-existence check is insufficient: validation should also reject identical/misrouted version artifacts when build metadata can identify them.

Generated client output contains the reviewed clean defaults, including the four built-in `PlayerExports` worldbooks. It excludes player-edited data, terminal settings, `Logs`, caches and runtime Recovery. The selected bekko model/tokenizer/config are added to both client archives during package projection. Source/package assembly never copies player directories or earlier generated module folders. Deployment preserves unlisted installed files, including `Modules/AnimusForge/ONNX`, and never syncs player data back to source or Stage. Runtime exports, cache, terminal settings and export Recovery belong to the current module; deployment Recovery belongs to `Modules`. None use AppData or `ANIMUSFORGE_DATA_ROOT`. Old C-drive files remain untouched and inactive; automatic migration is not provided.

## Legacy Module Rules

The retired folders are:

- `Modules/AnimusForge_1_3_x`
- `Modules/AnimusForge_1_4_5`

They are never valid runtime roots, build outputs, deployment targets, package roots, or launcher modules after this migration. Code must not fall back to either folder when resolving the active module root.

The historical migration implementation and evidence are retained, but its CLI activation/inventory entry points are retired before any AppData lookup. Legacy module folders and old C-drive records do not participate in runtime reads or new deployment recovery. Do not run a migration or modify those folders without a separate explicit request.

The unified installer or script should warn when an enabled legacy launcher module is detected because loading a legacy copy together with the unified module can duplicate Harmony patches and campaign registrations. Automated cleanup must not delete user data without an explicit, scoped cleanup action.

Because the launcher module id changes from a retired versioned id to `AnimusForge`, opening an old save may show a missing-module or module-version warning. The implementation assembly simple name and saveable type identities remain `AnimusForge`, which preserves the structural deserialization path, but users should back up old saves and this migration must not be described as warning-free until tested in game.

## Package Rules

One-click packaging creates two level-9 LZMA2 7z client archives from the same Stage and runtime DLLs:

```text
AnimusForge_v1.X.X_CN_MMdd_HHmm.7z
AnimusForge_v1.X.X_EN_MMdd_HHmm.7z
```

The version prefix is always `v`; `MMdd_HHmm` is the local month/day and hour/minute captured once for the pair. For example: `AnimusForge_v1.5.8_CN_1007_0802.7z` and `AnimusForge_v1.5.8_EN_1007_0802.7z`. If either filename already exists, both new archives receive the same next `_1`, `_2`, etc. suffix after the time. Existing files are preserved. `-PackageLabel` is rejected before version selection/build, since release names must follow this contract. Explicit four-part versions retain their micro component.

Each root folder must be `AnimusForge`. Each package must contain:

- `AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.dll`
- `AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll`
- `AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll`

Both one-click package entries increment the patch component by one, carrying at 10: `1.5.5 -> 1.5.6`, `1.5.9 -> 1.6.0`, `1.9.9 -> 2.0.0`. The source `AnimusForge/SubModule.xml` records the last successful package version; the initial baseline for this flow is `v1.5.5`, so the first package is `v1.5.6`.

The one-click entry passes `-Build` to `package_mod.ps1`: select the next version before invoking the existing unified build/Stage, stamp `Properties/AssemblyInfo.cs` with the matching four-part `AssemblyFileVersion` (for example `1.5.6.0`), and require both implementation DLL file versions to match before writing the package candidates. `AssemblyVersion` stays `0.0.0.0`; Bootstrap selection, hashes, clean-Stage validation and the single-module layout remain unchanged. Packaging does not deploy.

Successful packaging retains that version in both the source and project-local staged XML for the next increment. `-NoBump` keeps the current version; explicit `-Version` overrides it; explicit `-BumpMicro` remains available but neither one-click entry enables it by default. Captured build or archive failures restore version sources byte-for-byte without consuming a release number; concurrent source edits are preserved and reported instead of overwritten. A direct package-only invocation requires already-built DLLs matching the requested version; it cannot just relabel old DLLs through XML. Interrupted processes or system failures still require inspecting version sources and rebuilding before retrying.

`package_localization.ps1` builds the localized package projection for the archive writer. The CN archive retains the release projection of the clean Stage (excluding PDB and the Lite worldbook); the EN archive selects sibling `.en.json` / `.en-US.json` bodies under the original package filenames. Both archives retain the registered English and Chinese UI resources, so interface language switching remains available in either edition. Source Chinese assets and the clean Stage are never rewritten by localization. JSON field/type/array contracts, stable identities, variables and protocol examples must survive translation. The English prose check permits stable identity fields and preserved protocol tags; surrounding natural-language text must still be English. Missing English or invalid XML/JSON fails closed.

Worldbook selection first uses `PlayerExports/english-worldbook-identity-map.json` for the four mapped Chinese/English editions. Package directories and filenames retain the Chinese source paths. When an English knowledge asset has a mapped translated `Id`, projection verifies both mapped identities and writes the original Chinese source `Id` into the package bytes; the source English file is unchanged. Without an explicit mapping, the legacy Calradia index matches personality files by hero prefix, knowledge rules by `Id`, and shared files by relative path. Every selected body still passes the JSON and English-prose contracts; a mapping alone does not prove translation quality or runtime acceptance.

Both temporary archives must pass the Bootstrap-only XML, strict DLL allowlist, both implementation markers, marker-to-DLL hashes, and locked projection hashes before either final archive is published. Failure cleans up both candidates and follows the existing version rollback contract. Runtime readiness is checked before selecting a release version or building, then actual Bootstrap and both implementation DLLs are checked for managed user strings and embedded Chinese text. The source scan is a conservative release gate, stops after twelve failures, and is not a complete inventory. There is no language detection or runtime protocol rewrite in this packaging step.

The isolated `LocalizedPackageContractTests.ps1` covers both real ZIP writers, canonical pair names, minute collisions, source/Stage preservation, missing English, escaped Chinese, JSON/protocol/variable drift, actual tiny managed DLL strings, and second-archive failure cleanup. `PackageVersionContractTests.ps1` separately verifies the existing release version rollback behavior. These tests do not build the mod, assemble a production Stage, deploy, create release packages, or establish game acceptance. Release readiness still requires the actual source/DLL gates and complete CN/EN projection against current inputs.

Every client archive includes ONNX/bekko/model.onnx, tokenizer.json and config.json. The repository bekko directory is selected by default; -OnnxModelDir selects another complete bekko payload. Packaging rejects missing model files, missing implementations, multiple module roots or direct implementation DLL declarations in SubModule.xml.

Client archives install the bundled bekko payload into `Modules/AnimusForge/ONNX/bekko`. The missing-model campaign gate and actual model loaders use that same installed location; missing required models block campaign continuation through the existing pause/exit flow. No AppData model fallback, migration receipt, Recovery completion record, Python installation, or player-run migration command is required. Program upgrades preserve installed ONNX files. The corrected resolver and its acceptance cases are tracked in [J15 F4-M](plans/j15-content-profile-plan.md#j15-onnx-install-contract); offline verification does not replace two-version game and old-save acceptance.

## Safety Rules

- Never overwrite game files or TaleWorlds DLLs.
- Never copy TaleWorlds assemblies into the module or package.
- Never load both implementation DLLs in one process.
- Never emit `AnimusForge_1_3_x` or `AnimusForge_1_4_5` as an output module.
- Never maintain separate 1.3.x and 1.4.x source trees.
- Always keep the verified pipeline builds for both `BannerlordApi=1.3` and `BannerlordApi=1.4` passing.
- Deployment/overwrite actions must remain distinct from build-only validation; do not deploy as an implicit side effect of compiling.
