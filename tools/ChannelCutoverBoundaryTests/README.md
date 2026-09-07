# Channel cutover boundary regression

This offline test executes the **actual contiguous production outer control flow**, not a copied implementation of the desired behavior.

## What is executed

- Scene: the block starting at string output = ""; after group_turn_prompt_ready, ending immediately before apiSw.Stop(), from ShoutBehavior.cs.
- Courier: the complete cutover condition through the final await GenerateNpcReplyAsync(request), from PrepareAndGenerateCourierReplyOffMainThreadAsync in CourierDeliveryBehavior.cs. The fallback callback's await is deliberately included as well.
- The production InteractionStatus enum and DetachedInteractionHostResult declaration.
- Courier's real FailDetachedCourierReplyOnMainThread (when present), FailCourierReplyGenerationOnMainThread, and FinalizeCourierReplyGenerationOnMainThread.
- Scene's actual public/private port factories, `CreateChatMessage` anonymous-object constructor, configured-chat `BuildPromptPackage`, immutable prompt/envelope contracts, main/postprocess composers and ports. These are extracted production declarations, not a substitute factory or adapter.

The extractor inserts these unchanged source regions into a temporary net8.0 harness. The scene region runs inside one loop iteration, preserving the real difference between terminal break (tail cleanup) and stale return (do not touch a new conversation epoch). Source fingerprints are included in each run log.

External dependencies are stubs: capture/configuration, detached Host outcomes, network send, logger/UI, normalization, and main-thread queue scheduling. The simulated Host also invokes the real port delegates with a deliberately different captured envelope, so the test detects accidental use of recomposed fragments instead of the already-prepared messages. The simulated ProcessSessionById asserts that action consumption is reserved and both pending text fields are cleared **before** terminal failure advances the session. Legacy completion stays queued until explicit drain.

## Commands

Run from G:\AFMOD\AF-REFACTOR:

    # Baseline red test. Exit code 1 is expected for this known-bug revision.
    python tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --source-ref aefa02ad --output-name baseline-aefa02ad

    # Prepared Scene main-prompt baseline: 46 PASS / 6 FAIL, including all original 44 boundary cases passing.
    python tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --source-ref 92ad625a --output-name prompt-red-92ad625a

    # Working-tree regression. Exit code 0 is required.
    python tools/ChannelCutoverBoundaryTests/run.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe --output-name current

    # Focused extraction checks.
    python tools/ChannelCutoverBoundaryTests/test_extraction.py

Use --dotnet <full-path-to-dotnet> to specify the SDK path. When omitted, the script checks DOTNET_ROOT and PATH; it does not assume a machine-specific drive. Python 3.10+ and .NET SDK 8 are sufficient. The production anonymous-message adapter requires an existing Newtonsoft.Json DLL: by default the runner uses `.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll`; otherwise pass `--newtonsoft <full-path-to-existing-dll>`. No game assemblies or package downloads are needed. NuGet package sources are cleared for the generated project. This does not invoke the repository's build/stage/deploy scripts.

Generated files and logs stay under:

G:\AFMOD\AF-REFACTOR\.tmp\channel-cutover-boundary\<output-name>

## Covered failure boundaries

- Bridge disabled, null capture/facade, or preparation exception before Host submission: exactly one old request.
- Host non-success statuses, missing result, and thrown exception after submission: no second old request.
- Empty successful replies and empty internal fallback replies: no duplicate request.
- Scene terminal failures reach existing loop tail; stale status returns without new-epoch mutation.
- Scene successful result followed by disposal exception stops downstream processing without issuing a second request.
- Courier pre-delivery generation retains the old deferred action path; post-delivery terminal failure reserves PostprocessConsumed, clears pending text, and releases generation wait.
- Queued successful finalization followed by disposal exception is preserved.
- Two failure actions caused by terminal result plus disposal exception finalize failure once.
- Started legacy fallback retains queued completion after disposal/Host exceptions.
- Generation change before queued failure processing causes no state mutation.
- A failed scene internal fallback stops cleanly; courier fallback retains its already-started completion ownership.

## Evidence boundary

The original 44 failure-boundary cases are retained. Eight additional main-prompt cases cover:

- Production anonymous message shape (not a dictionary-only fixture), exact system/user/assistant order, all content and whitespace, persona/rules/trust/current and historical AFEF markers.
- The actual caller's 5000-token budget and `legacy-scene-shout` model routing.
- No extra current input appended from the independently captured envelope.
- A source list and mutable message changed after factory creation cannot change the frozen prompt.
- Different target requests keep separate prepared messages; composing the first request after creating the second cannot borrow the second prompt.
- The public two-parameter/default-argument factory retains its old 4096-token detached composition for callers that provide no prepared prompt.
- Postprocess remains envelope-owned, with its existing history, sections, latest-reply composition, 4096-token budget and model unchanged.

This proves the extracted default-entry routing and failure-state ordering against deterministic outcomes, plus the **already-prepared main-message handoff** through the real factory and adapter. It does **not** execute `BuildStrictSceneMessagesForNpc` or prove that the prepared content itself is correct: complete prepared-message fixtures are supplied at that boundary. It does not execute the real detached pipeline, game state machine, Bannerlord thread scheduler, action executors, API, save/load, or player UI. In particular, the ProcessSessionById stub checks state **on entry**; it does not prove real arrival/return execution. The unchanged postprocess test is not evidence of full Scene postprocess parity. Existing production-DLL Host replay and dual-version builds remain separate checks; live-game acceptance is NOT_RUN.
