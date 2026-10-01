# Scene input lifetime / BattleSpeech frozen replay regression

This suite executes extracted production methods from `src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs`, `Channels/Scene/ScenePlayerShoutRequestOwner.cs`, and the AF compatibility bridge. It covers audit F4 (stale input after an await), F5 (BattleSpeech ordinary fallback loses the framed target), and overlapping gate waiter ownership.

## Run

From the repository root:

```powershell
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run.py
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run.py --source-ref 5ce8767a --core --output-name red-5ce8767a
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run_mutations.py
python -B -m unittest discover -s tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests -p test_extraction.py
python -B tests/modules/AF.Module.Conversation/ScenePostprocessParityTests/run_gate.py --output-name gate-scene-request
```

The red baseline command intentionally exits 1. Output uses a fresh repository-local `artifacts/tests/scene-request-lifetime/` directory; `--run-root` selects a new explicit directory; no source checkout or game deployment is performed. `--dotnet` overrides the local SDK executable.

## Deferred request cancellation

```powershell
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run_deferred_cancellation.py
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run_deferred_cancellation.py --mutate deadline-does-not-cancel
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run_deferred_cancellation.py --mutate scope-does-not-bind
python -B tests/modules/AF.Module.Conversation/SceneRequestLifetimeRegressionTests/run_deferred_cancellation.py --mutate identity-ignores-cancel
```

The current case must exit 0; each mutation must compile and exit 1 at its named assertion. Deadline and scope mutations reject `scene-deferred-deadline-aborts-actual-request`; the identity mutation rejects `scene-deferred-canceled-result-no-commit`. The fixture extracts the actual deadline, current-request predicate, transport scope, blocking request seam and cleanup. It runs production lifetime/transport sources with an in-memory sender, not a real provider. Deadline and owner retirement both abort the request and prevent the commit sentinel; downstream game actions are **NOT-RUN**. Each fresh output directory contains `run.log` and `result.json` with source/harness hashes, compilation presence and exit code. `--run-root` must name a new repository-local directory.

## Evidence and scope

- Current: 30 runtime cases, including a 20 m framed target, exact framed audience, live agent identity replacement, mission / generation / session / player / newer input rejection, one-shot replay before and after gate completion, and main-thread resume.
- The real compatibility pre-route captures an opaque request before UI resume; its real ordinary fallback and real accepted-message observer execute in the fixture. Ordinary replay suppresses a second classifier observation only during the actual synchronous main-thread callback. Authorized BattleSpeech observation remains enabled.
- Two waiters inherit the same gate's borrowed processing flag in either continuation order. `ResumeGame` retires the UI's busy lifetime without cancelling its already accepted input. Closing an old/new UI cannot resurrect a stale busy flag.
- Old `5ce8767a` core: 0 PASS / 4 FAIL (range, audience, stale session, overlapping waiters). Before the processing-lifetime correction, the expanded fixture reproduced 2 further stale-busy failures.
- Seven mutation controls must fail the named behavioral case, not merely fail compilation. Seven extraction checks guard the source boundary and opaque-request forwarding.
- The separate existing gate suite covers 6 additional race groups (late task, waiter, timeout, queued UI messages, same gate, fault/cancel).

## Deliberate limits

Bannerlord objects and the main-thread queue are stubs; the gate uses real task continuations with controlled synchronization contexts. Source is copied verbatim through the decisive target/current-request branch; the expensive game-owner tail after `TryBuildSceneShoutConversationScope` is replaced by an acceptance counter plus a call to the extracted accepted-message observer. This is **not** a live Campaign/Mission run, complete Scene-domain verification, or proof of save/economy side effects.

The opaque request is transient and single-use; its sequence/claim owner is process memory only, it is not serialized, and it never replaces `_activeShoutTargetingContext`. On older hosts without the optional capture/replay API, natural pre-routing falls through to AF and the normal scene observer rather than inventing a nearby audience. Existing reflection support for NPC reply replay remains in use and is not removed.
