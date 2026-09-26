# Diplomacy dependency and retirement checks

Run `python tools/DiplomacyArchitectureTests/run.py --dotnet <dotnet-path>`.
Optionally pass `--baseline-dll <DPL-100 DLL> --candidate-dll <current DLL>` to compare public diplomacy metadata without loading the game.

The runner evaluates the real production MSBuild Compile list. The SDK Roslyn parser checks both Bannerlord preprocessor variants, ignores comments/string contents when checking type references, rejects external concrete behavior coupling, and restricts diplomacy bridges to stateless mapping/routing. Six injected violations must fail. The entire pure diplomacy layer compiles using framework and JSON references, with no game assemblies or host stubs. The check includes contracts, domain, persistence, application, rules and detached job coordinators.

Deletion evidence is pinned to DPL-100: 17 private, unannotated wrappers/unused algorithms are absent; surviving behavior text is identical. Retired local references and reflection-name literals are forbidden. The canonical storage field/save key remain uniquely owned. These are slice parity assertions, to be deliberately updated with behavioral evidence when an authorized future change affects this scope.

The retired queue DTO/selector had no production caller. Its three ordering/readiness/empty cases now run through the real `SelectAndPrepareLlmJob` in WorldDiplomacyRoundLifecycle.SmokeTests; J12 retains request lease/completion/routing tests. Current scheduler and persistence algorithms are unchanged.

Retained compatibility responsibilities:
- Public behavior, rule, DTO and gateway type/member identities remain for external binaries. Internal callers use typed module ports; public compatibility wrappers do not duplicate state.
- `LegacyWorldDiplomacyLlmGateway` remains the live AF transport adapter, with existing cancellation, retry metadata and route selection.
- Instance/Campaign owner resolution remains the existing lifecycle lookup, not a channel migration fallback.
- Old-save normalization, canonical history recovery, propagation recovery and service-failure narrative fallback remain active product behavior.

No real save, game, deployment, network service or LIVE acceptance is exercised. Generated files remain under ignored `.generated/`.
