# Illustrator card fixed UI slice

## Delivery

Uncommitted slice based on `14ac606ed`, affecting only:

- `extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs`: 27 displayed-expression lines changed.
- `extensions/AnimusForge.Illustrator/src/UI/IllustratorUiText.cs`: 11 new one-line catalog methods; existing methods preserved.
- `extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/keys.json`: machine-readable EN/CN table and baseline.
- `extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/verify_static.py`: read-only static closure/inverse checks.

Main-agent integration added all 11 resource pairs to the existing registered Illustrator XML files. No J17, build, Stage, deployment, push, paid API, existing offline audit script or compiled test was executed.

## New resource keys

Add these 11 pairs to `content/modules/AnimusForge.Illustrator/ModuleData/Languages/illustrator_strings.xml` and its `CNs/illustrator_strings-zh-CN.xml` sibling. Exact machine strings are in `keys.json`; `{ERROR}` must remain identical.

| ID | English | Chinese |
| --- | --- | --- |
| AF_ILL_UI_CONVERSATION_SESSION_CHANGED | The previous conversation illustration was archived; the current conversation has changed. | 原会话画卷已存入画廊，当前会话已变化。 |
| AF_ILL_UI_CONVERSATION_FIRST_IMAGE_REQUIRED | Click Redraw to create the first scene illustration for this conversation. | 请点击“重新绘制”生成本次对话的第一张场景插画。 |
| AF_ILL_UI_CONVERSATION_PREPARING | The AI artist is analyzing the conversation and body language... | AI画师正在分析现场交谈与肢体姿势... |
| AF_ILL_UI_CONVERSATION_EQUIPMENT_UNAVAILABLE | The complete equipment snapshot for both characters was unavailable. Generation stopped without substituting a troop template or another outfit. | 未能取得双方完整装备快照，已停止生成；不会用兵种模板或另一套服装替代。 |
| AF_ILL_UI_CONVERSATION_REFERENCE_REUSE | Reusing the environment and character references captured at the start of this conversation | 复用本次会话首次环境与人物参考 |
| AF_ILL_UI_PORTRAIT_EQUIPMENT_UNAVAILABLE | The encyclopedia's current displayed equipment could not be read. Generation stopped without guessing civilian or battle clothing. | 无法读取百科当前展示装备，已停止生成；不会改用猜测的便服或战斗服。 |
| AF_ILL_UI_CARD_DRAW_FAILED | Drawing failed: {ERROR} | 绘制失败: {ERROR} |
| AF_ILL_UI_CARD_GENERATION_EXCEPTION | Generation error: {ERROR} | 生成异常: {ERROR} |
| AF_ILL_UI_CARD_NOT_SAVED_OR_DISPLAYED | The image could not be saved or displayed. | 未能保存或显示图片 |
| AF_ILL_UI_SCENE_PROBE_LOADING | Rebuilding the nearby environment and capturing a complete panorama... | 正在重建附近环境并采集完整全景... |
| AF_ILL_UI_SCENE_PROBE_DISPLAY_FAILED | The exported preview could not be displayed. | 未能显示导出的预览 |

## Existing resource reuse

Ten existing methods were reused by the changed expressions. Their keys were verified in both resource files:

| Method | ID |
| --- | --- |
| GalleryLoading | AF_ILL_UI_GALLERY_LOADING |
| WeeklyBackground | AF_ILL_UI_WEEKLY_BACKGROUND |
| WeeklyRedrawLoading | AF_ILL_UI_WEEKLY_REDRAW_LOADING |
| WeeklyRedrawSuccess | AF_ILL_UI_WEEKLY_REDRAW_SUCCESS |
| WeeklyRedrawFailed | AF_ILL_UI_WEEKLY_REDRAW_FAILED |
| WeeklyDefaultChanged | AF_ILL_UI_WEEKLY_DEFAULT_CHANGED |
| CardPrepareFailed | AF_ILL_UI_CARD_PREPARE_FAILED |
| CardProbeFailed | AF_ILL_UI_CARD_PROBE_FAILED |
| PortraitPreparing | AF_ILL_UI_PORTRAIT_PREPARING |
| WeeklyNotSaved | AF_ILL_UI_WEEKLY_NOT_SAVED |

Existing initial Gallery/PortraitTitle/ConversationTitle/SceneTitle, complete-state labels and scene-probe title/complete labels were already localized and remain intact.

## Thread and lifecycle evidence

- `IllustratorRuntime.cs:371-386`: Start asserts main thread, runs worker via Task.Run, then enqueues complete through PostCritical.
- `IllustratorRuntime.cs:273-295`: Tick asserts the captured main thread and drains Critical/Pending callbacks. `SubModule.cs:54-65` invokes Tick from OnApplicationTick.
- `IllustrationScope.RunCore` at Runtime line 468 asserts main thread on admission. Lines 512-536 revalidate scope, request revision and cancellation before calling complete/fail. CurrentImageRedraw uses this same RunGeneration route.
- Popup redraw/loading entry belongs to the existing view/editor action; completion/failure uses that guarded scope completion. Cached reads use Runtime.Start plus popup scope/active-instance/cache-version guard.
- DefaultImageChanged and GenerationUpdated are published through Runtime main-thread methods at lines 307-327; the popup preserves its active/scope/request/source/session guards.
- Scene progress text now resolves `ConversationReferenceReuse` and `PortraitPreparing` inside the existing Post callbacks at Popup lines 912/984. Previously `PortraitPreparing` was resolved in the asynchronous worker; that resolution was moved to the existing presentation callback without changing raw scene capture/status, Prompt or metadata values.

Catalog methods execute only on open, status change, failure or completion. No new Tick getter, cache scan, reflection, worker, queue, request or language probe was added.

## Residual Chinese and invariants

Dynamic `error`, provider `ErrorMessage`, exception `Message`, `direction.StatusText`, `sceneCapture.StatusText`, `SavedItem.DisplayStatusText`, cache display status and saved image `Title` are deliberately passed through. Any of those may still be Chinese; zero fixed UI literals does not imply zero Chinese rendered from dynamic content.

The worker `resultError` fallback (`未能保存图像`), sceneStatus raw reuse marker, generated portrait/meeting fallback titles, NPC/name fact fallbacks, `probeNote`, Prompt/fact markers and reference descriptions remain unchanged. Saved titles and director-status metadata remain raw. The new null-error fallback only changes the detached UI representation; raw result selectors keep their previous values.

The pure-English runtime Prompt/metadata loader and detector remain outside this slice. No provider diagnostics were rewritten, no saved values were translated and no callback or state transition changed.

## Validation

- PASS: `python -B extensions/AnimusForge.Illustrator/tests/LocalizationCardSlice/verify_static.py`.
- Exact reviewed UI inverse restores the entire popup to `14ac606ed`; removing the 11 new method lines restores the entire previous catalog. This checks unchanged Prompt, logs, metadata, saved-title inputs, callbacks and surrounding behavior.
- Direct fixed Chinese scan of SetLoading/SetReady/StatusText/TitleText: 0. New fallback/placeholder table: 11 pairs; reused methods found in EN/CN XML: 10.
- PASS: scoped git diff --check; only repository CRLF conversion notices.
- PASS: all 26 resource pairs; existing 1.3 and 1.4 localization DLLs each resolved 1,068 Illustrator texts across English, Chinese, untranslated French and switch back. This does not execute the current popup/helper C#.
- NOT-RUN: both production builds/Bootstrap; compiled replay; existing offline audit scripts; native window/layout/input acceptance; real model generation, complete Campaign and old saves.

## Rollback

No commit was made. Main agent should commit this verified slice and use a focused inverse commit when needed. Until then, `verify_static.py` contains exact reviewed popup inverse pairs tied to `14ac606ed`; remove only this slice's 11 catalog lines and owned test/handoff files when rolling back. Preserve other authors' changes and do not reset/checkout whole files.
