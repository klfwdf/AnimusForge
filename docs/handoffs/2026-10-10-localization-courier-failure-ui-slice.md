# Courier failure UI localization slice

## Status and ownership

Implemented as an uncommitted narrow slice. No build, compiled replay, deployment, push, shared XML, LlmRetryPrompt or J17 changes.

Production files:

- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.InboundCompletion.cs`
- `src/modules/AF.Module.Conversation/Channels/Courier/CourierFailureUiText.cs`

Tests:

- `tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/run_liveness.py`
- `tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/CourierLocalizationBoundary.cs.txt`
- `tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/verify_failure_ui_static.py`

## Resource table for main-agent integration

Seven new keys should be added to the existing EN/CN UI pair. Three shared Retry keys already exist in that pair. Literal `\n\n` below means two newlines; XML must use its existing newline encoding convention. The raw `DETAIL` variable is identical in both languages.

| ID | English fallback / EN resource | CN resource | Status |
| --- | --- | --- | --- |
| AF_CONV_UI_Courier_ReplyFailedTitle | Courier reply generation failed | 信使回信生成失败 | New |
| AF_CONV_UI_Courier_LetterFailedTitle | Courier letter generation failed | 信使来信生成失败 | New |
| AF_CONV_UI_Courier_ReplyFailedDetail | Courier reply generation failed:\n\n{DETAIL} | 信使回信正文生成失败：\n\n{DETAIL} | New |
| AF_CONV_UI_Courier_LetterFailedDetail | Courier letter generation failed:\n\n{DETAIL} | 信使来信正文生成失败：\n\n{DETAIL} | New |
| AF_CONV_UI_Courier_ReplyStoppedTitle | Courier reply stopped | 信使回信已停止 | New |
| AF_CONV_UI_Courier_ReplyStoppedDetail | This reply could not be completed safely. The system will not automatically retry actions that may already have taken effect. Check the log and verify the actual outcome. | 本次回信未能安全完成，系统不会自动重试可能已生效的动作。请查看日志并核对实际结果。 | New |
| AF_CONV_UI_Courier_InboundAborted | Courier letter processing failed. The courier was safely stopped and the letter was not delivered. | 信使来信处理失败，信使已安全终止，未交付信件。 | New |
| AF_UI_Retry_Retry | Retry | 重试 | Reused |
| AF_UI_Retry_Abort | Abort | 放弃 | Reused |
| AF_UI_Retry_UnknownError | Unknown error | 未知错误 | Reused |

Main-agent integration added all seven new keys to the registered `af_ui_strings` EN/CN pair. The full 26-pair resource contract passed. Existing 1.3 and 1.4 localization DLLs each passed 12,204 UI resource resolutions across English, Chinese, untranslated French and switching back. This is resource parsing evidence, not a build or game acceptance result.

## Actual main-thread consumers

- Reply retry: `GenerateNpcReplyAsync` enqueues `CompleteCourierReplyGenerationOnMainThread` at GenerationLifecycle line 338. Completion checks generation/current request before calling `ShowCourierReplyGenerationRetryPrompt` at line 382; its current-session guard precedes helper resolution at line 570.
- Inbound retry: `GenerateInboundNpcLetterAsync` enqueues `CompleteInboundLetterGenerationOnMainThread` at line 741. Completion checks generation/current request/inbound direction before calling `ShowInboundLetterGenerationRetryPrompt` at line 776; its current-session guard precedes helper resolution at line 814.
- Detached reply stopped: both callsites (lines 275-277 and 289-291) are callbacks passed to `EnqueueMainThreadActionForGeneration`, followed by current-request checks. `FailDetachedCourierReplyOnMainThread` retains its stale/session guards before line 625 resolution.
- Queue consumer: RuntimeTick `EnqueueMainThreadActionForGeneration` lines 109-120 wraps execution in `IsCurrentGeneration`. `OnEngineTick` lines 53-57 dequeues/invokes; `ApplicationTickComposition.cs:70` calls it from the existing engine tick composition.
- Inbound abort: `ProcessOneCourierInboundCompletionReceipt` calls abort at InboundCompletion lines 145/150 and is called from `OnCampaignTick` at RuntimeTick line 156. Courier host registers `CampaignEvents.TickEvent` at CourierDeliveryBehavior.cs line 877. The other abort path is `AbortCourierInboundCompletionBeforeMemory`, called only after `CommitCourierInboundMemoryWithCompletionReceipt` checks `TWParallel.IsMainThread()` at InboundCompletion lines 386-392. No background abort callsite was found.

No TextObject is constructed in the network workers or Tick scanning loops. Resolution happens only when one of the existing notices opens. There is no new scan, reflection, queue, cached-language state or frame allocation.

## Preserved boundaries

`error` is passed unchanged to retryability and `BuildRetryDescription`; DETAIL receives that same value. Null errors use localized Unknown error; empty strings keep their former empty behavior. Request generation, current-session guards, Interlocked retry choice, callback actions, raw logs, protocol strings, session fields, letter normalization, receipt fields, persistence, facts and history remain byte-equivalent after the reviewed UI inverse.

Preprocess failure titles, arbitrary provider/exception text and Chinese diagnostic payloads remain outside this slice.

## Verification and limits

- PASS: `python -B tests/modules/AF.Module.Conversation/CourierPromptPreparationTests/verify_failure_ui_static.py`. Exact inverse of the two production source files restores the slice baseline; 10 unique fallback references, English fallback content, raw DETAIL behavior, runner Python AST, linked production helper and scalar main-thread boundary checked.
- PASS: scoped `git diff --check` (only repository CRLF conversion notices).
- Current liveness closure maintained: full production helper is linked and hashed through the existing Compile manifest traversal; the new scalar TextObject boundary is included in test input hashes and invokes `NativeHost.Game()` to count incorrect-thread resolution. This scalar boundary supports English fallback, not native translations.
- NOT-RUN: `run_liveness.py --current-consumer`. Its initial `d.verify` compiles; it has no prepare-only flag and was deliberately not invoked.
- NOT-RUN: compiled fixture/mutations, game popup/notifications, both production builds/Bootstrap, old saves and complete Campaign paths.

## Rollback

No commit was created. Main agent should include this slice in its verified focused commit and use a focused inverse commit to roll it back. Until then, the inverse pairs in `verify_failure_ui_static.py` identify the exact production expression changes; retain all other authors' edits. Do not use whole-file checkout/reset.
