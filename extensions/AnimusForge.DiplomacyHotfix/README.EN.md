# AnimusForge Diplomacy Hotfix 0.1.0

A standalone diplomacy patch for AnimusForge v1.6.2. Requires installed AnimusForge and Bannerlord.Harmony modules.

## Fixes

- Player diplomacy maintenance captures up to eight event references before processing them under the original rules. Closing events can no longer invalidate subsequent indexed reads, and the rotating cursor stays bounded.
- World diplomacy prepares and freezes request settings on the main thread. Asynchronous sends and retries reuse them while preserving the original HTTP admission budget, fallback, retry and cancellation rules.

The module does not replace AnimusForge.dll, change save formats or add settings. Harmony patches install at startup. Event snapshots retain the original game-hour gate; there is no new per-frame reflection or disk access.

## Installation

1. Exit the game. Extract the ZIP and place the `AnimusForge.DiplomacyHotfix` folder inside the game's `Modules` directory.
2. Enable it in the launcher after its dependencies: **Bannerlord.Harmony → AnimusForge → AnimusForge.DiplomacyHotfix**. Keep your other module ordering.
3. After startup, check `Modules/AnimusForge.DiplomacyHotfix/Logs/DiplomacyHotfix.log`.

`maintenance=applied;requests=applied` means both patch groups installed. `already_fixed` skips an existing equivalent fix. `unsupported_*` or `*_patch_rejected` means that group was not applied; retain the log for diagnosis.

CN and EN packages differ only in this documentation. They contain the same DLL and work with either AF content language. Install one package.

## Compatibility and verification

Patch installation, repeated installation and maintenance replay were tested against locally retained AF v1.6.2 implementations (file version 1.6.2.0) for both API lines. Both repaired implementations correctly skip patching. Other builds with the same version number are checked by actual method structure; their compatibility is not established by the version number alone.

The original out-of-range failure was reproduced. Each patched API implementation passed 84 maintenance assertions. Offline replay of the original request sources with the real Harmony patch passed settings freeze, retry, budget, cancellation and missing-configuration checks. Native game/MCM and HTTP sender leaves were substituted; no real provider was contacted.

Compiling against Bannerlord 1.3.15 and 1.4.6 references produced byte-identical DLLs. In-game acceptance, actual old saves and Bannerlord 1.4.8 have not been tested. `build-info.json` records reference versions and hashes. This package carries only these two fixes, not the main project's other diplomacy changes.

## Uninstall / rollback

Exit the game, disable the module and delete `Modules/AnimusForge.DiplomacyHotfix`. The original AnimusForge and save files remain untouched; no AF DLL restoration is needed.

## Build from source

From the repository root (Python 3, .NET SDK and your installed Harmony):

```powershell
python extensions/AnimusForge.DiplomacyHotfix/build.py --harmony "<Harmony directory>/0Harmony.dll" --package
```

Use `--refs13` / `--refs14` to select reference directories. Defaults are `_deps_auto` and `.tmp/build_check/1.4` inside the repository. The script verifies reference versions, builds both sequentially, checks identical DLL bytes and creates the CN/EN ZIP pair under this extension's `artifacts/packages`. It never deploys to the game or overwrites old packages. Tests are under `tests/`; the main project's existing build entry points are unchanged.
