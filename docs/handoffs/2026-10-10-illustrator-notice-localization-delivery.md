# Illustrator notification and menu localization slice

Status: implemented, static checks PASS; compiler/game acceptance NOT-RUN.
Baseline: `37467703ea5b8a6b77564e1e0035d711faefdd14`, workspace `F:/AnimusForge-main`, branch `main`. No commit created for this slice.

## Changed files

- `extensions/AnimusForge.Illustrator/src/Core/IllustratorRuntime.cs`: generation success/failure notification wrappers and scope-expired/timeout/busy UI callbacks.
- `extensions/AnimusForge.Illustrator/src/Core/MissionScreenshotIllustration.cs`: screenshot input labels and fixed progress/cancellation/error wrappers; original API/scene exception messages retained.
- `extensions/AnimusForge.Illustrator/src/SubModule.cs`: gallery menu label; existing menu ID and actions retained.
- `extensions/AnimusForge.Illustrator/src/UI/IllustratorUiText.cs`: 24 new English fallback helpers.
- `content/modules/AnimusForge.Illustrator/ModuleData/Languages/illustrator_strings.xml`: 24 new English keys, 256 total.
- `content/modules/AnimusForge.Illustrator/ModuleData/Languages/CNs/illustrator_strings-zh-CN.xml`: matching 24 Chinese keys, 256 total.
- `tests/content/J15ContentContractTests/IllustratorNoticeLocalizationContractTests.py`: static resource, fallback, fixed UI and callback-boundary checks.

No changes to language detection, content registration, persistence identities, workers, scheduling, prompts, reference descriptions or diagnostics. Existing localized resource registrations were verified through the content map and Foundation Localization indexes.

Resolution occurs at menu registration, popup open, status change and main-thread result delivery. No localization scan/reflection was added to Tick. TickCapture only resolves messages in the existing completion branch. Worker progress callbacks resolve inside `IllustratorRuntime.Post`.

## Verification

- `python tests/content/J15ContentContractTests/IllustratorNoticeLocalizationContractTests.py`: PASS, 24 entry/notification keys, 256 resource keys; XML keys, placeholders, fallback text, mapping/LanguageFile registration, fixed UI text, retained original validation/worker errors and static main-thread witnesses.
- `python tests/content/J15ContentContractTests/LocalizationResourceContractTests.py`: PASS, all 26 registered resource pairs.
- Scoped `git diff --check`: PASS.
- Existing 1.3 and 1.4 localization DLLs: each PASS, 1,024 resource resolutions (English, Chinese, untranslated French, switch back). No compiler or Stage tree was invoked.
- `BuildOutputFrameRequirement` and `CaptureFacts` compared against baseline: byte-equivalent after normalizing CRLF to LF.
- Manual diff review: screenshot reference text, metadata title fallback, worker exception messages, cancellation reasons, diagnostics and actions retained.

These are static contracts, not compiler or runtime thread proofs.

## Generic fail callback consumer evidence

The three scope-expired/timeout/busy helpers are UI callback messages, not rewritten exceptions or persisted diagnostics:

- `IllustratorRuntime.cs:473,536,552`: UI `fail` calls. The original scope/admission diagnostic fields remain; `diagnostics.Finish("failed", ex.Message)` and `IllustrationGenerationUpdate.Error = error?.Message` retain raw errors. `Start:371-388` delivers through `PostCritical`.
- `MissionScreenshotIllustration.cs:196,200`: `Finish:235` updates status and emits a notification.
- `IllustrationCardPopup.cs:633,1017`: failure handlers call `IllustrationCardVM.SetReady:203`, which only sets UI properties.
- `IllustrationCardPopup.cs:727,778`: probe failure routes to `SetSceneProbeFailure:783`, which updates UI; its catch logs only a new presentation exception, not the supplied failure string.
- `WeeklyReportPopupIllustrationPatch.cs:611`: `sink.StatusText`. `StartGeneration:639,699` passes the same `fail` callback through.
- `BulletinIllustrationPreloader.cs:161,193`: `Finish:201` keeps `job.Status` in memory, consumed by `WeeklyReportPopupIllustrationPatch.cs:423` for UI. It does not serialize or log this text; `ReleaseWaiters` does not receive status.
- `CurrentImageRedraw.cs:25,46`: forwards `fail` to RunGeneration; callers in `IllustrationCardPopup.cs:240`, `WeeklyReportPopupIllustrationPatch.cs:524`, and `BulletinIllustrationPreloader.cs:182` update UI status.

## Remaining content: not English-complete

Known paths in this slice that can still display Chinese dynamic data:

- `MissionScreenshotIllustration.cs:111,114`: original scene-unavailable and API-required exception diagnostics (restored during review; unchanged).
- `IllustratorRuntime.cs:524`: raw result/exception error or Chinese default save error; `GenerationFailed` keeps the diagnostic body.
- `MissionScreenshotIllustration.cs:157`: duplicate-image exception (worker diagnostic).
- `MissionScreenshotIllustration.cs:177`: upstream image error or Chinese missing-image default.
- `MissionScreenshotIllustration.cs:181`: saved metadata title fallback is Chinese; generated `direction.Title` may also be Chinese.
- `MissionScreenshotIllustration.cs:187`: worker save exception.
- `MissionScreenshotIllustration.cs:196`: result-error callback retains Chinese save-failure diagnostic.
- Capture engine, visual director, upstream API and existing cache can supply other raw error/title strings. No inventory count for those wider files is asserted here.

Prompt/fact/reference Chinese remains deliberately outside this UI slice: `BuildOutputFrameRequirement`, `CaptureFacts`, screenshot A/B descriptions and `MissionScreenshotRules.Contract`. Internal cancellation strings remain too. This slice does not satisfy the whole-mod no-Chinese runtime/package requirement.

A later diagnostic projection and English prompt asset slice must address these producers without translating old saved facts or replacing diagnostic evidence. The original dynamic `{ERROR}`, `{TITLE}` and `{STATUS}` values are preserved here.

## NOT-RUN and rollback

NOT-RUN: Bannerlord 1.3/1.4/Bootstrap compilation, game/screenshots, old saves, live API requests, Stage, deployment, package generation, push.

Rollback: apply a focused inverse of this slice against the baseline for the seven files listed above, plus removal of this delivery note. Review intervening author changes first. Do not reset the workspace or overwrite concurrent edits. No git commit was made.
