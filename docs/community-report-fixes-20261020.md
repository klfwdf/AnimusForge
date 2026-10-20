# Community report fixes (2026-10-20)

## Scope

- Keep alliances permanent, show an explicit indefinite duration, and preserve the existing explicitly authorized alliance-break action.
- Project fixed scene role labels through the active CN/EN prompt package so compatibility values do not leak into EN visible text.
- Include authoritative direct-relative life-state facts in NPC persona generation.
- Preserve the captured encounter party while a native NPC surrender waits for dialogue teardown.

## Non-goals

- No deployment, packaging, push, game launch, or changes to unrelated dirty files.
- No change to dynamic names, player text, unknown historical text, or the existing alliance-break authorization allowlist.

## Exit gates

- Focused source/replay checks for each affected owner.
- Debug builds for Bannerlord API 1.3, 1.4, and Bootstrap through the existing build entry.
- Report any live-game, old-save, or provider validation as not run when not available.

## Implemented and verified

- `PermanentAllianceGuard` keeps the existing explicit break authorization and projects the native diplomacy row to `Indefinite/无限期` through a runtime-safe VM postfix.
- `ConversationActionPostprocessOwner` projects compatibility role values at the visible/prompt boundary; dynamic names remain unchanged.
- `PersonaGenerationFactCaptureAdapter` now includes bounded relative life states and the existing bilingual `FamilyStatusAuthority` instruction.
- `LordEncounterBehavior` keeps the captured surrender party across a transient null party read and cancels only when a readable different encounter is observed.
- Passed `CommunityReportFixesContractTests.py`, `ReleaseSurrenderBoundaryTests` (71 checks), and `WorldDiplomacyOralBreakAlliance` (28 assertions).
- Official `build_single_module.ps1 -Configuration Debug` passed for 1.3, 1.4, Bootstrap and both Coup seam gates with 0 errors; the build emitted the repository's existing warnings.

## Not run / pre-existing failures

- No game launch, live diplomacy UI, live persona provider, old-save replay, packaging, deployment, or push.
- The broader Persona localization script currently fails because its pinned key set omits the pre-existing `AF_PERSONA_UI_CONTEXT_RecentChange` key. The scene notice script also fails against its older pinned source baseline; neither failure involves files changed here.
