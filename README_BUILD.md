# Build Notes

## Quick Build
Run in this directory:

```powershell
.\build.ps1
```

If `MCMv5.dll` is not auto-detected:

```powershell
.\build.ps1 -Mcmv5Path "F:\SteamLibrary\steamapps\workshop\content\261550\<modid>\bin\Win64_Shipping_Client\MCMv5.dll"
```

## Optional Parameters
- `-Configuration Debug|Release`
- `-DepsDir <path to _deps_261550_managed>`
- `-BannerlordRoot <path to Bannerlord root>`
- `-Mcmv5Path <full path to MCMv5.dll>`

## Notes
- `AnimusForge.csproj` now uses configurable MSBuild properties:
  - `DepsDir`
  - `BannerlordRoot`
  - `AnimusForgeBinDir`
  - `Mcmv5Path`
- Build fails early with clear errors if required DLLs are missing.
