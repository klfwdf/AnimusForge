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

## Dynamic display projection update

The follow-up dynamic projection adds 48 keys (71 total in `CastleAftermathUiText`):

- Every `SiegeCastleActionKind` disposition is projected from its enum value at the notification boundary; the shared profile's Chinese `Describe` remains unchanged.
- Every `SiegeCastleLordRecruitmentBranch` is projected from its enum value; branch selection and matching terms remain unchanged.
- Deferred reason codes are projected for the five known constants, `removed_for_*`, `exception:*`, and unknown fallback. The reason code itself is unchanged in the action result and logs.
- Lord-sale failure reason codes are projected for the seven known codes plus empty/unknown fallback; `BuildLordSaleFailedMessage` is no longer used for the player notification.
- Recruitment status uses exact bridge-produced fixed messages, queued-letter format, cleanup suffixes, the existing `EconomyUiTexts.FactionIdentityStatusForDisplay` for clan-join statuses, and `SocialUiLocalization.RecruitmentStatusForDisplay` for companion/other forms. Complex fief/transition tails and unknown status text remain authoritative through existing fallback behavior.
- Null letter sender/recipient display fallbacks are localized without changing the saved sender/recipient IDs or letter body.

`tests/content/J15ContentContractTests/CastleAftermathDynamicLocalizationContractTests.py` passed: 71 key fallbacks, 48 new dynamic keys, 21 notification consumers, enum/reason/status producer coverage, and unchanged source outside notification blocks. The English fallback source remains `CastleAftermathUiText.cs`; Chinese additions are in `docs/handoffs/2026-10-10-castle-aftermath-dynamic-ui-keys.json` for mainline XML registration.

The dynamic key table now contains 48 complete `id/en/zh` rows under `strings`. Each `en` value is verified against the actual helper fallback; no row overlaps the original 23-key table, and no changes to those original keys are included. The follow-up static test pins its source comparison to `c8b6c652ebeaf43e0f503298ec3a76f9b60505f1`, not the moving HEAD. This delivery-format update changes no production C# or XML.

## Explicit residual Chinese

This slice is fixed-wrapper localization, not a no-Chinese runtime guarantee:

- Complex fief/transition tails in successful clan-join `STATUS` values are passed through the existing Economy display projector; any unrecognized tail remains raw. This preserves facts/logs and avoids guessing translations for generated summaries.
- Unknown `STATUS` or unrecognized detail fragments are retained through existing display fallback. Unknown reason codes and action/branch enum values use the same generic display semantics as the original profiles; their underlying codes/values remain in the result/log/Prompt channels, not inserted into the notification.
- Names from game objects remain original display names; only null/blank helper fallbacks are localized.
- Shared profile wording, Chinese role comparisons, Prompt witness text, stored facts, raw errors and reason codes remain unchanged. Other non-castle siege notifications remain outside this slice.
- Other non-castle siege notifications from external profiles/presentation data remain outside this slice. All Chinese role comparisons, Prompt witness text, stored facts, raw errors and reason codes remain unchanged.

## NOT-RUN and rollback

PASS: all 26 registered resource pairs, plus existing 1.3 and 1.4 localization DLLs each resolved 12,320 UI texts across English, Chinese, untranslated French and switch back. This also includes the three mainline Persona title keys added alongside this slice; it does not execute the current C# helper.

NOT-RUN: compiler dual implementation/Bootstrap, current helper execution, UI/game/old-save acceptance.

Rollback with a focused inverse of only the two modified notification blocks/calls plus the new helper/key table/note. Review concurrent changes first; do not reset the shared worktree. No commit exists for this slice.
