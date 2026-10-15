# Dedicated TTS settings regression

Source-links the complete production `SpeechSettings` class. MCM, native localization, information messages and legacy settings are explicit test hosts, not a live game.

```powershell
dotnet run --project tests/modules/AF.Module.Llm/TtsSettingsTests -c Release
```

Covers legacy credential/selection migration, persistent one-time migration, unchanged-read IO bounds, provider exclusivity, reserved custom mode, value clamps, manual tone projection, unsaved clone isolation, live save compatibility and reload precedence. Provider selections use the MCM dropdown's in-place change event, including preset assignment without replacing localized options.

After the unified build, run `verify_mcm_dropdown.ps1` in Windows PowerShell with `-ImplementationPath`, `-ReferenceDirectory`, `-GameReferenceDirectory` and `-ProjectRoot`. Run once per API implementation with its matching `.tmp/build_check/1.3` or `1.4` references and the corresponding verified game reference directory used by the build. It loads the compiled AF and actual MCM assemblies, replays legacy integer `ProviderMode` JSON through the real dropdown converter, verifies integer output, change notifications, selection/cancel actions and paired resources. No player configuration is read or changed.

No official-site button, cloud API, local model or native audio is invoked. Full MCM provider lifecycle/rendering, encyclopedia clicks, game save loading and speech quality still require live acceptance; the managed JSON/action checks do not substitute for those.
