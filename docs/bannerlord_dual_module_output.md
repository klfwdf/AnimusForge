# Bannerlord Unified Module / Dual Runtime Output

AnimusForge uses one source tree, builds two Bannerlord-compatible implementation DLLs, and publishes one launcher module. The historical filename is retained so existing repository references do not break; the old two-module output contract is retired.

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

## Build Rules

Both implementation DLLs are still required and must be built sequentially through the verified unified build entry:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 `
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

Every function under `一键编译覆盖推送` must use the unified contract:

- Build: build Bootstrap plus both implementation variants, then assemble one staged `AnimusForge` module.
- Deploy/overwrite: assemble and validate a complete same-volume staging module, then transactionally replace only `Modules/AnimusForge`; restore the previous unified module if replacement fails.
- Build and overwrite: run the unified build, deploy the one module, and retain the existing optional launch behavior.
- Package: package the one staged module into one ZIP containing both implementation DLLs.
- Push: publish the same unified source/output contract; no step may recreate or publish the legacy two modules.
- Validation: verify the module id, Bootstrap declaration, both implementation paths, and absence of conflicting implementation declarations before deployment or packaging.

Scripts must consume explicitly captured 1.3 and 1.4 build artifacts. A successful file-existence check is insufficient: validation should also reject identical/misrouted version artifacts when build metadata can identify them.

Generated client output must exclude generated `Logs` from source/package content and must not recursively copy earlier generated module folders. Deployment preserves existing unified logs. `PlayerExports` are merged without deletion; retired module folders participate only during the first unified deployment, while later deployments use the source and existing unified module so stale legacy data cannot be re-imported indefinitely.

## Legacy Module Rules

The retired folders are:

- `Modules/AnimusForge_1_3_x`
- `Modules/AnimusForge_1_4_5`

They are never valid runtime roots, build outputs, deployment targets, package roots, or launcher modules after this migration. Code must not fall back to either folder when resolving the active module root.

When preserving existing user data is necessary, a script or runtime migration may inspect the retired folders only as read-only, one-time migration candidates. Migrated data must be written to `Modules/AnimusForge`; the legacy folders must not be modified or refreshed. Prefer existing unified-module data when both unified and legacy copies exist, and record or mark completed migrations so stale legacy data is not repeatedly imported.

The unified installer or script should warn when an enabled legacy launcher module is detected because loading a legacy copy together with the unified module can duplicate Harmony patches and campaign registrations. Automated cleanup must not delete user data without an explicit, scoped cleanup action.

Because the launcher module id changes from a retired versioned id to `AnimusForge`, opening an old save may show a missing-module or module-version warning. The implementation assembly simple name and saveable type identities remain `AnimusForge`, which preserves the structural deserialization path, but users should back up old saves and this migration must not be described as warning-free until tested in game.

## Package Rules

One-click packaging creates one client package, for example:

```text
AnimusForge_<version>_bannerlord_1.3.x-1.4.x_<timestamp>.zip
```

Its root folder must be `AnimusForge`. The package must contain:

- `AnimusForge/bin/Win64_Shipping_Client/AnimusForge.Bootstrap.dll`
- `AnimusForge/bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll`
- `AnimusForge/bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll`

Packaging bumps the module version once and writes the same version to the source and project-local staged `SubModule.xml`; it does not deploy. `-NoBump` keeps the current version; an explicit version parameter overrides it when supported by the script. XML changes are rolled back byte-for-byte if packaging fails.

The ZIP is first written to a temporary file in the package directory. It becomes the final ZIP only after validating the Bootstrap-only XML, strict DLL allowlist, both implementation markers, and marker-to-DLL hashes; failed temporary archives are removed.

The client ZIP must not include the `ONNX` folder. Package validation must fail if any ONNX entry appears, if either implementation is missing, if more than one module root exists, or if `SubModule.xml` declares an implementation DLL directly.

## Safety Rules

- Never overwrite game files or TaleWorlds DLLs.
- Never copy TaleWorlds assemblies into the module or package.
- Never load both implementation DLLs in one process.
- Never emit `AnimusForge_1_3_x` or `AnimusForge_1_4_5` as an output module.
- Never maintain separate 1.3.x and 1.4.x source trees.
- Always keep the verified pipeline builds for both `BannerlordApi=1.3` and `BannerlordApi=1.4` passing.
- Deployment/overwrite actions must remain distinct from build-only validation; do not deploy as an implicit side effect of compiling.
