# Player starting history replay

Build `Replay.csproj` with `HarmonyCorePath` pointing to the installed managed
Harmony DLL. Keep build/intermediate outputs in an isolated task artifacts
directory. Run the resulting .NET Framework 4.7.2 executable once per package:

```text
Replay.exe "reference-directory|AF-dependency-directory|Harmony-directory|MCM-directory|ButterLib-directory|UIExtenderEx-directory" "candidate/AnimusForge.dll" "content/modules/AF.Module.Social/ModuleData" CN
Replay.exe "reference-directory|AF-dependency-directory|Harmony-directory|MCM-directory|ButterLib-directory|UIExtenderEx-directory" "candidate/AnimusForge.dll" "content/modules/AF.Module.Social/ModuleData" EN
```

The candidate must be the 1.5 implementation. The replay exercises its real
history owner, nine start identities, existing/unknown history, once-only flag,
JSON reload, manual clearing, typed `AdvancedStartOptionsData` session capture,
prompt reader and UI formatter, failed-summary compaction, proactive evidence
budget/cache/rename and observer gate. UI translations are loaded into the
managed localization dictionary as fixtures, independently of package choice.
The provider is replaced with a completed invalid response for the fallback
test. Game/current campaign getters and observer qualification are controlled
fixtures; this is not live campaign, save-file, provider or Gauntlet acceptance.
No installed module or player configuration is written.
