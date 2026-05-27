# Bannerlord Dual Module Output

AnimusForge uses one source tree and builds two Bannerlord client modules.

## Output Contract

One-click build scripts must always write both modules into the active Bannerlord `Modules` directory:

- `Modules/AnimusForge_1_3_x`
- `Modules/AnimusForge_1_4_5`

Each output folder is a complete Bannerlord module. Both folders keep the standard DLL name:

- `bin/Win64_Shipping_Client/AnimusForge.dll`

The two modules must not share a module id. The output `SubModule.xml` files must be rewritten as:

- 1.3.x: `<Id value="AnimusForge_1_3_x" />`, `<Name value="AnimusForge 1.3.x" />`
- 1.4.5: `<Id value="AnimusForge_1_4_5" />`, `<Name value="AnimusForge 1.4.5" />`

Only enable the module matching the current Bannerlord game version in the launcher.

## Build Rules

Build both versions sequentially. Do not run the two `dotnet build` commands in parallel because they share the same `bin` and `obj` paths.

```bat
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4
```

The 1.4 build defines `BANNERLORD_1_4_OR_GREATER`. API differences should be isolated in compatibility helpers where possible. Use `#if BANNERLORD_1_4_OR_GREATER` only when method signatures or missing types require compile-time separation.

## Script Rules

- The one-click build batch builds both versions and writes both module folders.
- The one-click build-and-overwrite batch builds both versions, overwrites both module folders, then launches Bannerlord by default.
- Pass `--no-launch` or `/no-launch` to the build-and-overwrite batch when script validation should not start the game.
- The one-click package batch first runs the dual-version build/output flow, then writes two ZIP packages from the versioned game `Modules` folders.
- `deploy_module.ps1 -DualClientOutput` is the source of truth for writing both versioned modules.
- `package_mod.ps1 -DualClientPackages` is the source of truth for packaging both versioned modules.
- Output must exclude `Logs` and must not copy generated client folders recursively.
- Before output, scripts may sync `PlayerExports` from an existing game module back into the source module.

## Package Rules

One-click packaging must create two client packages, one per supported Bannerlord line:

- `AnimusForge_1_3_x_<version>_bannerlord_1.3.x_<timestamp>.zip`
- `AnimusForge_1_4_5_<version>_bannerlord_1.4.5_<timestamp>.zip`

Each ZIP root folder must match its module folder name. The DLL inside each package must remain:

- `<versioned module>/bin/Win64_Shipping_Client/AnimusForge.dll`

One-click packaging must not include the `ONNX` folder. The package script should fail validation if an ONNX entry appears in either dual-client ZIP.

One-click dual-client packaging bumps the patch version by default and writes the same version back to the source `AnimusForge/SubModule.xml` plus both versioned output modules. Pass `-NoBump` to keep the current version, or `-Version x.y.z` when a release needs an explicit package version.

## Safety Rules

- Never overwrite game or TaleWorlds DLLs.
- Never place both versions into one `Modules/AnimusForge` folder.
- Never give both output modules the same `SubModule.xml` id.
- Do not maintain two separate source trees for 1.3.x and 1.4.5.
- After compatibility edits, verify both `BannerlordApi=1.3` and `BannerlordApi=1.4` builds.
