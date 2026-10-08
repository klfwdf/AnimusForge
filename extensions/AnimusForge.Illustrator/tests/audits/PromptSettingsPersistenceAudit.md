# Prompt settings persistence audit

Run in Windows PowerShell with an actual integrated AnimusForge DLL:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File extensions/AnimusForge.Illustrator/tests/audits/run_prompt_settings_persistence_audit.ps1 -AssemblyPath <candidate>/AnimusForge.dll -OutputDirectory <workspace>/artifacts/prompt-settings-test
```

The audit uses real MCM attribute discovery, BaseJsonSettingsFormat, settings copying,
JSON conversion and real Harmony patches. It invokes all three production editor
callbacks and SaveCurrentSettings. The editor surface and settings provider file
boundary are fixtures; output goes only to the explicitly supplied directory.
Storage initialization is intercepted to prevent writes to the installed module.
No game, save, network, image provider or GPU is started.

Checks cover copying/reopening, editor text, immediate save, live/page consistency,
ordinary settings, disk JSON reload into a fresh object, legacy missing keys, empty
text, invalid types, Unicode/newlines/quotes, long text, MCM apply/default copy and
unrelated settings isolation. Runtime MCM UI acceptance still requires a game test.
The extension sources are integrated into AnimusForge.csproj; validate both official
implementation outputs rather than treating the historical standalone extension
project as the shipped artifact.
