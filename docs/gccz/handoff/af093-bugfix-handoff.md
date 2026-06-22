# AF093 GCCZ bugfix handoff

## Scope

- Upstream source: `G:\AFMOD\YM0.9.3\Mount-Blade-Bannerlord-AnimusForge-mod-main` (read-only source snapshot).
- Fixed worktree: `G:\AFMOD\new-0.9.3\Mount-Blade-Bannerlord-AnimusForge-mod-main`.
- Standalone GCCZ source remains `G:\AFMOD\GCCZ\src\AnimusForge.SiegeAftermathIntervention`; it was mirrored into the AF093 worktree after excluding `bin/obj`.

## Fixed contracts

- `MenuContext.OpenTroopSelection` must be called through runtime-compatible reflection. Do not compile-bind to the 1.4/Naval overload; current player runtimes may only expose the 6-parameter overload.
- GCCZ “亲自进城/攻城处置” is town-only. Castles have no civilian assembly target and should not expose the personal-entry option.
- AF front preprocessing must append GCCZ runtime prompt/memory into `MyBehavior.BuildShoutPromptContextForExternalInternal` and mark `siege_intervention_aftermath` in `PreprocessRuleIds`.
- Shout postprocess must provide GCCZ runtime context, normalize GCCZ tags, and dispatch them through `SiegeAiInterventionBehavior.TryProcessAiActionTags`.
- `RuleBehaviorPrompts.json` must contain `siege_intervention_aftermath` in both the root copy and `AnimusForge\ModuleData` copy for packaging/runtime consistency.

## AF093 bridge touch points

- `SiegeAiInterventionBehavior.cs`
  - `OpenTroopSelection` reflection bridge.
  - town-only entry condition/location resolution.
  - town civilian assembly cap only.
- `MyBehavior.cs`
  - `AppendSiegeInterventionRuntimePromptForShoutContext` bridge.
- `ShoutBehavior.cs`
  - native and deferred scene postprocess bridge for GCCZ runtime tags.
- `AnimusForge\ModuleData\RuleBehaviorPrompts.json`
  - added `siege_intervention_aftermath` rule.

## Verification notes

- Passed: `dotnet build ... AnimusForge.csproj -c Debug /p:BannerlordApi=1.3` with 0 warnings and 0 errors.
- Passed: `dotnet test G:\AFMOD\GCCZ\GCCZ.sln --no-restore`.
- Attempted `BannerlordApi=1.4` against the current local reference DLLs; it failed in upstream AF files using APIs absent from this local runtime (`HasTradeAgreement` 3-arg overload, raid/naval encounter APIs, `CharacterObject` constructor shape). Treat this as the known local DLL/API mismatch, not as a GCCZ regression.

## Safety

- No hard reset or force push was used.
- No game directory overwrite was performed in this handoff.
- Keep `YM0.9.3` read-only unless the user explicitly authorizes editing the upstream snapshot.
