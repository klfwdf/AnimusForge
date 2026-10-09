# Persona startup regression replay

Scope: actual production `CampaignSaveExitController` and `SaveRuntimeGuard` source, plus an exact extracted `CompleteOnboardingAndOpenPlayerPersonaSetup` method/enum/UI-active property. UI, Campaign and model boundary objects are fakes; this is **not** Bannerlord acceptance or a reproduction of the player's hardware hang.

```powershell
python -B tests/modules/AF.Module.Onboarding/PersonaStartupTests/run.py --dotnet "<dotnet.exe>"
# Negative baseline 4ed61998: expected nonzero (synchronous initialization, early completion, stale UI).
python -B tests/modules/AF.Module.Onboarding/PersonaStartupTests/run.py --dotnet "<dotnet.exe>" --baseline
# Complete production engine source; block/release a fake file/model boundary to prove real asynchronous yield and single flight.
dotnet run --project tests/modules/AF.Module.Onboarding/PersonaStartupTests/EngineAsync/EngineAsync.csproj -c Release "-p:OnnxManagedDll=<verified Microsoft.ML.OnnxRuntime.dll>"
```

The engine probe uses the actual managed ONNX type reference but does not load a native model. It verifies off-caller-thread execution, immutable module-root capture, one shared task while loading/after completion, missing-model failure and error preservation. The controller replay covers deferred UI, no per-tick duplicate work, success/fault/cancellation/timeout, late completion, new campaign/generation, and save-before-exit. The exact onboarding callback covers import/skip, confirmation, final choice, duplicate completion, owner/generation changes and open failure.

Artifacts stay under the repository `artifacts/` directory. The 60-second timeout test advances the private timestamp, rather than waiting 60 seconds; it never claims native work was forcibly stopped. Full engine/model latency, actual Gauntlet rendering/input, `.sav` and live gameplay remain manual acceptance.
