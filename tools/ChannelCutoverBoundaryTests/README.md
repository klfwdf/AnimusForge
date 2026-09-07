# Channel cutover boundary regression

This offline suite executes extracted production control flow against deterministic dependency stubs. It does not deploy a module, access a live API, modify player settings, or load a game save.

## Production code executed

- Scene's contiguous default-entry block, from `string output = "";` after `group_turn_prompt_ready` to immediately before `apiSw.Stop()` in `ShoutBehavior.cs`. It runs inside one loop iteration, preserving terminal `break` versus stale `return` semantics.
- Scene's actual `GenerateSceneShoutMainReplyAsync`, `CreateSceneShoutMainReplyPorts`, `RunDetachedRefactorFallbackAsync`, and `RecordSceneReplyHistoryOnMainThreadAsync` declarations.
- Scene's actual `PrepareSceneMainReplySpeechText`, `QueueSceneMainReplyOnMainThreadAsync`, all four end/relay/action stripping helpers, the full `GiveAssetTagCodec`, and the contiguous tail sanitization statements. This extends the check beyond an empty main ActionPlan to the text actually passed to the speech queue.
- Scene's exact relay/queue decision expressions, `replyIsDirectPlayerResponse = firstTurn`, and the production battle-postprocess suppression predicate. Only their input bindings are synthetic.
- Five complete production main-thread lifecycle delegates: primary-target failure release, participant hold, battle follow-up suppression, normal idle-timeout arming, and failure release. Their bodies remain verbatim, including the request-start generation/session/epoch guards.
- Courier's complete cutover condition through the final `await GenerateNpcReplyAsync(request)`, plus its actual terminal failure and finalization methods.
- The real interaction status/result, prompt/envelope, ActionPlan, and capability contracts; actual public Scene opt-in factory, prompt composers, action parser, `CreateChatMessage`, and configured-chat `BuildPromptPackage`.

The runner inserts these source regions unchanged into a temporary .NET 8 harness and records their SHA-256 fingerprints. Optional declarations are absent when testing an older Git revision; the harness does not insert a synthetic production fix to make that baseline pass.

## What the stubs mean

The Scene facade stub invokes the **real production port delegates** with a deliberately different captured envelope, then supplies a deterministic generation result. Calling its commit method is a test failure. This checks the prepared-message boundary, zero executable actions from main text, and the actual generation helper's cancellation/fallback/result handling; it does not execute the full real coordinator or a network API.

History tests execute the actual dispatching helper. The dispatcher and history owners are simulated: callbacks assert that they run inside the dispatch boundary, record call order, and observe the audience passed by production. Three simulated listeners start with one already-recorded player line. This proves the helper calls the full-audience owners once without appending the player again, **not** that real Hero/non-Hero memory storage succeeds.

Main-speech tests execute the actual sanitizer and enqueue helper. The enqueue boundary records text, audience, action context, timeout, commit-history setting, and the real captured publication predicate. Tests invoke that predicate after generation/session/epoch/target changes; simulated entity resolution throws if attempted outside the main-thread dispatch boundary. The speech worker's actual gameplay executors remain outside this harness.

Lifecycle tests compile those five actual delegate bodies as `Func<bool>` callbacks. Only captured input objects and final game-side owners are stubbed; current requests must call the correct owners in order, while stale generation/session/epoch requests must call none. This covers the failure/cleanup tail after a history or speech enqueue rejection, not merely the generation-success path.

Courier Host outcomes and `ProcessSessionById` are simulated. Its original 24 cases retain their assertions: pending action text must be cleared and action consumption reserved before terminal session processing. The stub checks state at the real caller boundary, not the actual downstream arrival/return implementation.

## Commands

Run from `G:\AFMOD\AF-REFACTOR`:

```powershell
# Known buggy early-commit baseline: nonzero exit is expected.
python tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --source-ref d40808b3 --output-name scene-main-staging-red-d408

# Working-tree regression: zero exit is required.
python tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --output-name scene-main-staging-current

# Additional immutable baseline containing the main-tag-to-speech bypass.
python tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --source-ref cec3877a --output-name scene-speech-red-cec3877a

# Source extraction and actual tail wiring checks.
python tools/ChannelCutoverBoundaryTests/test_extraction.py
```

Python 3.10+ and .NET SDK 8 suffice. `--dotnet` may be omitted when `DOTNET_ROOT` or `PATH` resolves an existing SDK. The actual anonymous-message adapter requires an existing Newtonsoft.Json DLL: default `.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll`, or pass `--newtonsoft <path>`.

No game assemblies or package downloads are needed. The generated project clears package sources. Files and logs stay under `G:\AFMOD\AF-REFACTOR\.tmp\channel-cutover-boundary\<output-name>`. The repository build/stage/deploy scripts are not invoked.

## Covered boundaries

- Bridge disabled or failed preparation before submission: exactly one legacy request.
- Scene main success, including empty text: no postprocess composition, action plan, memory transaction, or commit receipt before the authoritative tail.
- Main text containing real action-tag syntax remains an empty ActionPlan; END is retained for the Scene stage rather than executed during generation.
- Actual tail sanitization removes main-origin gold/Duel/Issue/asset/debt/recruitment/guide/summon/follow/mood/vassalage tags, including GIVE_ASSET names with embedded brackets/colons. A tag-only main result becomes empty rather than a hidden action. Only already-validated follow-stop/summon-end booleans can restore `[STP]`/`[END]` once; being followed without an END signal creates neither.
- Main speech enqueues once on the main thread, forwards full audience and original timing, does not commit history again, and does not grant player-directed context to relay replies. Empty/stale/unavailable text never enqueues; delayed generation/session/epoch invalidation is rejected on either thread, while entity checks run only on the main thread.
- All five lifecycle delegates preserve current-request behavior but reject stale generation/session/epoch before holding, releasing, suppressing or re-arming participants; this prevents an old cleanup tail from mutating a new scene that reused an epoch or agent index.
- Only known `RetryableFailure`, `DegradedWithoutProvider`, or `SkippedByEligibility` permits one internal, pre-effect fallback. Null, exception, validation failure, unexpected `Executed`, and nonretryable status stop without an outer retry.
- Unexpected nonempty generated actions are rejected before fallback eligibility, including a contradictory `RetryableFailure` carrying actions.
- Runtime generation/epoch changes before or after generation and during fallback suppress subsequent history/action processing.
- Disposal failure never re-enters legacy generation; Courier queued completion retains its existing ownership.
- Prepared anonymous messages preserve system/user/assistant order, whitespace, persona/rule/trust/AFEF markers, 5000-token budget, and model. No captured input is appended again; source mutation and another target cannot change the frozen prompt.
- Current default captures identity directly rather than recomputing reduced Prompt sections. The public full opt-in factory retains its 4096-token main/postprocess composition and capability contract.
- Real history helper rejects stale generation/session/epoch and unavailable target; successful calls preserve Scene/shared-history then persistent-audience-owner order exactly once.
- Extracted tail predicates preserve action-only processing, battle suppression, relay END/remaining-turn/candidate guards, and `firstTurn=false` on relay rounds.
- Separate source-bound assertions require exactly one awaited history helper call followed by exactly one authoritative deferred queue call, with original rule hits, reply context, and live relay candidate variables. Relay must release the processing flag through the existing bounded gate before awaiting a completed postprocess task. These assertions are wiring evidence, not gameplay execution.

## Architecture and evidence boundary

The former assertion that an accepted default Scene Host had already committed is intentionally replaced by **zero early effects**. This is the approved staging repair, not a weaker success criterion: the authoritative Scene tail must now own the one history/postprocess path. Full public opt-in APIs and Courier behavior remain covered separately. The original 82 behavior checks and 10 extraction checks remain; main-speech coverage adds 30 behavior checks and 3 extraction/wiring checks, and lifecycle delegates add 20 behavior checks and 1 extraction check: 132 behavior checks and 14 extraction checks in total.

This suite does not prove that `BuildStrictSceneMessagesForNpc` assembled correct content, that the complete deferred postprocess applies real game actions, or that persistence, AFEF, TTS, relay movement, save/load, and UI work in Bannerlord. Full production-DLL replay, official dual-version builds, and live-game acceptance remain distinct evidence. `live=NOT_RUN`, `apiNetwork=NOT_RUN`.
