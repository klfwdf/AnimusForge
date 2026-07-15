# Build Notes

AnimusForge now publishes one `Modules\AnimusForge` module with a Bootstrap and two version-specific implementation DLLs.

## Quick Build

Run `一键编译覆盖推送\一键编译.bat` to build both implementations plus Bootstrap and assemble a complete project-local module at `bin\Debug\single_module_stage\AnimusForge`. It does not write to the game directory.

For the repository's complete compile-only pipeline, which never deploys without `-Deploy`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 `
  -ProjectRoot . `
  -BannerlordRoot "F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" `
  -Configuration Debug `
  -Stage
```

Direct project checks are available for the current 1.4 implementation and Bootstrap:

```powershell
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4
dotnet build AnimusForge.Bootstrap\AnimusForge.Bootstrap.csproj -c Debug
```

The 1.3 implementation intentionally fails closed when built directly. Use `build_single_module.ps1`; it verifies that the pinned reference overlay reports a 1.3.x `BuildInfo` before enabling the 1.3 compile.

The unified build script uses isolated output/intermediate directories, validates the 1.3 and 1.4 reference lines, records the exact reference game version in each `build.json`, and keeps only DLL/PDB/build metadata artifacts.

## Runtime Layout

```text
Modules\AnimusForge\bin\Win64_Shipping_Client\
├─ AnimusForge.Bootstrap.dll
├─ AnimusForge.Bootstrap.pdb
├─ AnimusForge.Bootstrap.build.json
├─ private runtime dependencies
└─ versions\
   ├─ 1.3\AnimusForge.dll + PDB + build.json
   └─ 1.4\AnimusForge.dll + PDB + build.json
```

`SubModule.xml` loads only `AnimusForge.Bootstrap.dll`. The Bootstrap detects the game version and loads exactly one implementation.

## Optional MSBuild Properties

- `BannerlordApi=1.3|1.4` (`1.3` is enabled only by the verified unified build script)
- `Configuration=Debug|Release`
- `VersionedDepsDir=<1.3 dependency directory>`
- `Bannerlord13ReferenceDir=<verified 1.3 reference overlay>` (unified build script)
- `BannerlordRoot=<Bannerlord root>`
- `AnimusForgeBinDir=<runtime dependency directory>`
- `Mcmv5Path=<full path to MCMv5.dll>`

Deployment stages and validates a complete module beside `Modules\AnimusForge`, then swaps directories on the same volume and restores the old module if replacement fails. On the first unified deployment only, legacy module `PlayerExports` are read as migration candidates; legacy module folders are never modified or deleted.

Build-only commands never deploy. Only `一键编译并覆盖.bat` and `一键覆盖.bat` write `Modules\AnimusForge`. `一键编译.bat` and the packaging BAT use the project-local staged module; the development pull/build/push BAT files also do not deploy.
