# Illustrator status display projection

## Delivery

Second narrow slice based on `c8b6c652e`. No commit, compiler, compiled replay, model/API request, Stage, deployment, push, or J17 change.

- New `extensions/AnimusForge.Illustrator/src/UI/IllustrationStatusUiText.cs` owns detached status display projections.
- `IllustratorUiText.cs` has 23 new catalog methods. Exact EN/CN table: `extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/status-keys.json`.
- `IllustrationCardPopup.cs` has 8 changed display lines, preserving underlying producer metadata, Prompt, logs, save inputs, generation and callback behavior.
- `verify_status_static.py` checks the second inverse; the first `verify_static.py` composes it before validating the earlier card slice.

## Producers and consumers

| Producer | Authoritative fields | Popup consumers |
| --- | --- | --- |
| Core/IllustrationDirection.cs:34-48 | DirectionStatus, UsedLocalFallback, VisionUnsupported, FinishReason, FallbackReason | Popup.cs:609/984 progress callbacks |
| Engine/DiskImageCacheManager.cs:34-40 | Theme, DirectorStatus, DirectorStatusText, DirectorFallbackReason | Popup.cs:281/627/1010 complete states |
| Engine/DiskImageCacheManager.cs:164-176 | StyleFingerprint compatibility plus cache display | Popup.cs:349/482 cache completion |
| Engine/MapConversationSceneCapture.cs:145 | Exact terrain-facts status | Popup.cs:912/984 progress callbacks |
| Engine/SceneReferenceCapture.cs:78/255/269 | Exact disabled/panorama status, complete budget template with numeric Limit | Popup.cs:912/984 progress callbacks |

Four Core/Engine producer files are untouched and statically compared to the second-slice baseline. No current/old cache migration or language-detection changes were made.

## Display contracts

- Live director structured states: failed wrapper with raw reason; truncated; local fallback; vision unsupported; complete with/without finish marker. Unknown states return original StatusText.
- Cached DirectorStatusText is mapped only when both its structured status and exact producer text agree; unknown/custom stored status text remains intact.
- Local fallback recognizes all seven exact bounded producer reason outputs, optionally preceded by the exact vision-unsupported prefix. It does not infer a reason from arbitrary provider prose or discard additional text.
- Theme wrappers become localized while Theme itself remains unchanged. Empty Theme retains the original Chronicle illustration behavior. Style compatibility uses the existing `IsStyleCompatible` predicate.
- Scene status recognizes five complete fixed producer strings. Numeric budget matching is anchored to the entire producer template; no partial keyword translation. Unknown text and details pass through unchanged.
- Raw FallbackReason, Theme, director/user/provider content and saved Title are not translated. Known fallback theme content `人物与情境` remains the raw Theme value rather than being guessed from string equality.

## Thread/performance

Direction/Scene display methods are invoked only within the existing Popup `IllustratorRuntime.Post` callbacks. Cached display is resolved only in existing Start/scope completion/main-thread publication callbacks. Existing Runtime Start -> PostCritical -> OnApplicationTick/Tick dispatch and scope revision/cancellation guards remain unchanged.

One static Regex is constructed for the complete budget template. It runs only for unknown scene strings at progress updates, not per-frame binding getters. No new scans, reflection, locks, worker tasks, queues or language state were introduced.

## Remaining candidate paths

Same known cache producer remains raw in consumers outside this exclusive popup slice:

- UI/Gallery/IllustratorGalleryPopupVM.cs:398 -> SelectedTheme and :411 -> StatusText.
- UI/Patches/WeeklyReportPopupIllustrationPatch.cs:442/:468/:578/:609 -> status sink.
- Core/BulletinIllustrationPreloader.cs:159/:191 -> Finish status -> notification/state consumers. This owner must remain raw until its UI sink is reviewed; do not replace its internal result with localized text blindly.

Untranslated content in the current popup can still originate from raw provider/exception messages, saved/generated titles, Theme, failed FallbackReason, custom/unknown status strings, and Prompt preview/copy text. Accurate input producers include `VisualDirectorEngine.CreateDirectionAsync` (Title/Theme/StatusText), `UniversalOpenAiImageClient` (ErrorMessage), `DiskImageCacheManager` (stored metadata) and `SceneReferenceCapture` exceptions. The projection localizes provable system structure while preserving those details.

## Validation and rollback

PASS: `python -B extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/verify_status_static.py` and composed `verify_static.py`; exact popup/catalog inverse, untouched producers, 23 EN/CN placeholder pairs, unknown/dynamic value retention, no metadata setters, and Post-only direction/scene resolution.

PASS: scoped `git diff --check` with repository CRLF conversion notices.

NOT-RUN: compiled logic/replay, production 1.3/1.4/Bootstrap, current helper native language resolution, live-game UI/input/layout, Campaign/old saves and real image APIs. Main agent owns resource registration/validation and may append that evidence separately.

No commit was created. The 8 popup inverse pairs are in `verify_status_static.py`; its removal of the 23 newly added catalog lines is exact and baseline-bound. Use a focused inverse after main-agent commit; retain other authors' changes.
