# Castle aftermath fixed notification localization

Local source and shared resource integration complete. Mainline added all 23 EN/CN keys to the registered `af_ui_strings` pair. No compilation, Stage, deployment, package or push performed. No J17 work performed.

## Files

- `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathLordRecruitmentRuntimeBridge.cs`: one introduction-letter notification only.
- `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.cs`: 19 castle-only notification calls; six formerly direct Chinese messages and 13 profile-produced notices/calls.
- `src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/CastleAftermathUiText.cs`: host-only notification resolver, 23 English fallback IDs; no other consumers.
- `docs/handoffs/2026-10-10-castle-aftermath-ui-keys.json`: corresponding Chinese translations; `englishFallbackSource` identifies the canonical English fallback source. All 23 key sets/placeholder occurrences checked equal.

No external GCCZ profile, shared XML, language detection, world book, C/D/E package or stored fact was changed. Existing colors, numerical clamping, positive trust sign, whitespace handling, actions and message conditions remain. The English/CN XML entries must be added by the resource owner; without that step Chinese clients see English fallback for these new keys.

## Static evidence

Executed against disk and current HEAD (HEAD observed during work: `14ac606ed6ac186fe0f5260c5a7718a5b614d68b`; branch `main`):

- Key-table JSON parses; all 23 IDs exist once in helper and Chinese table, and have equal placeholder occurrences. No Chinese in helper.
- `git diff --check -- src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath`: PASS.
- For both modified source files, replace each `InformationManager.DisplayMessage(new InformationMessage(... Color.FromUint(...)));` block with a marker, normalize CRLF and compare to HEAD: identical. Thus source outside notification blocks, including memory, status, reason, matching, log and scheduling paths, is unchanged.
- All 20 `CastleAftermathUiText` call sites are inside those existing notification blocks. Helper contains no Tick/SyncData/logger/record/worker/reflection path.
- Diff manually reviewed against `SiegeCastleActionOutcomeTextProfile` and `SiegeCastlePrisonerDispositionProfile`. The original profiles are unchanged.

These are static checks, not compiler acceptance, localization runtime acceptance or game-thread proofs.

## Consumer and frequency evidence

- `SiegeAiInterventionBehavior.cs:442` registers `DailyTickTownEvent`; `OnDailyTickTown:566-569` invokes `ProcessDueLetters`. The new letter notification is resolved only for an actually due, deliverable letter (`CastleAftermathLordRecruitmentRuntimeBridge.cs:198`). Existing queue scans/frequency are unchanged.
- Castle care, armaments, staging, recruitment, sale, duel and execution code already modifies game state and emits messages synchronously. New helper calls replace text expressions at precisely those message boundaries; no dispatch or ownership change.
- Slaughter death notice retains the existing first/every-tenth-kill gate (`SiegeAiInterventionBehavior.cs:5310-5315`). No per-frame localization parsing was added.
- Recruitment `result.StatusText` is still passed into `RecordInterventionMemory` (`:5018`) and the original outcome logger (`:5024`), while only `InformationMessage` uses the helper (`:5005,:5020`).

## Explicit residual Chinese

This slice is fixed-wrapper localization, not a no-Chinese runtime guarantee:

- Staging/deferred notifications keep `SiegeCastleRegularDispositionStagingProfile.Describe(action)` as `{ACTION}`. The profile is also a fact/Prompt producer; unchanged.
- Deferred failure uses `SiegeCastlePrisonerDispositionProfile.DescribeDeferredFailureReason(result.ReasonCode)` as `{REASON}`. Original reason code and its description remain.
- Lord recruitment keeps `SiegeCastleLordRecruitmentBranchProfile.Describe(branch)` as `{BRANCH}` and original trimmed `result.StatusText` as `{STATUS}`. The latter is shared with facts/logs/political memory and cannot be translated in place.
- Names already prepared for shared facts (`senderName`, `recipientName`, `lordName`) keep original Chinese null fallbacks. Helper only provides its own localized captured-lord fallback when it directly receives null/blank; it does not reinterpret existing name values.
- Lord-sale failure remains the original `SiegeCastleActionOutcomeTextProfile.BuildLordSaleFailedMessage` (`SiegeAiInterventionBehavior.cs:4914`), which contains Chinese reason descriptions in a private profile helper. No substring extraction or duplicated reason mapping was introduced.
- Other non-castle siege notifications from external profiles/presentation data remain outside this slice. All Chinese role comparisons, Prompt witness text, stored facts, raw errors and reason codes remain unchanged.

## NOT-RUN and rollback

PASS: all 26 registered resource pairs, plus existing 1.3 and 1.4 localization DLLs each resolved 12,320 UI texts across English, Chinese, untranslated French and switch back. This also includes the three mainline Persona title keys added alongside this slice; it does not execute the current C# helper.

NOT-RUN: compiler dual implementation/Bootstrap, current helper execution, UI/game/old-save acceptance.

Rollback with a focused inverse of only the two modified notification blocks/calls plus the new helper/key table/note. Review concurrent changes first; do not reset the shared worktree. No commit exists for this slice.
