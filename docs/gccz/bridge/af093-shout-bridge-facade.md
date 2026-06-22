# AF093 GCCZ shout bridge facade

Date: 2026-06-23

## Purpose

AF updates frequently rewrite `MyBehavior.cs` and `ShoutBehavior.cs`. The GCCZ prompt/postprocess bridge should therefore be reattached through one small AF-side facade instead of scattering direct calls to `SiegeAiInterventionBehavior` across AF monolith files.

## Facade

Fused worktree file:

- `G:\AFMOD\new-0.9.3\Mount-Blade-Bannerlord-AnimusForge-mod-main\AfGcczShoutBridge.cs`

This file is intentionally **not** GCCZ core. It is a thin AF/Bannerlord adapter that may reference AF types such as `Hero`, `CharacterObject`, `PostprocessRuleEntry`, and `MyBehavior.ShoutPromptContext`.

## Stable hook contract

When porting GCCZ to a future AF version, prefer restoring only these hooks:

1. In `MyBehavior.BuildShoutPromptContextForExternalInternal(...)`, after `PreprocessRuleIds` is assigned:
   - call `AfGcczShoutBridge.AppendRuntimePromptToShoutContext(...)`.
2. In native/non-native conversation postprocess:
   - use `AfGcczShoutBridge.ShouldRunPostprocessFromPreprocessHits(...)`.
   - merge `AfGcczShoutBridge.BuildPostprocessRules(...)`.
   - append `AfGcczShoutBridge.BuildPostprocessContext(...)`.
   - normalize with `AfGcczShoutBridge.NormalizePostprocessTags(...)`.
3. In scene unified/deferred postprocess:
   - use `ShouldRunPostprocessFromPrompt(...)` or `ShouldContinuePostprocess(...)`.
   - dispatch final GCCZ tags through `AfGcczShoutBridge.TryProcessActionTags(...)`.
4. Do not hard-code `siege_intervention_aftermath` or `【附加规则:siege_intervention_aftermath】` in `MyBehavior.cs` or `ShoutBehavior.cs`; use facade/core constants.

## Boundary

Keep in GCCZ core:

- rule id and injected marker via `SiegePostprocessRuleCatalog`;
- runtime prompt wording;
- postprocess rules/context text;
- tag normalization and routing policy.

Keep in AF facade/monolith:

- live `Hero` / `CharacterObject` / `Agent` lookup;
- `AIConfigHandler` postprocess call wiring;
- `PostprocessRuleEntry` mapping;
- final Bannerlord side effects through `SiegeAiInterventionBehavior`.

This reduces AF update breakage without moving TaleWorlds/AF runtime dependencies into standalone GCCZ core.
