# Interaction request lifetime checks

Pinned main: `437925b856fae76b4e9ee207e96ba048f35d5a67`.

```powershell
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --main --case common
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --main --case supersede
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --main --case dispose
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --main --case token
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --main --case race
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --main --case precancel
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --mutate dispose_early
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --mutate propagate_callback
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/run.py --mutate ignore_active_cancel
python -X utf8 -B tests/modules/AF.Module.Conversation/InteractionRequestLifetimeTests/verify_compat.py
```

The common main cases must pass; the five pinned defect cases must compile and fail their intended assertions. The current all-cases run must pass; three deliberate mutations must compile and fail behavioral assertions. Cancellation/completion races use explicit synchronization, not timing guesses.

`verify_compat.py` compares public signatures, compiles `LegacyClient.cs` against the main source-built library, then executes unchanged consumer IL with the current source-built replacement. This is a real compiled-client ABI check for the existing coordinator, not a claim that Api.V1 request APIs are implemented. Both libraries contain actual contracts/coordinator source; the pipeline is a deterministic test double. No game/LLM calls occur.

Generated main/current/mutated artifacts remain separate under ignored `.generated/`. Production contains only the new internal lease and the original coordinator entrypoints. No copied main implementation is shipped.

Remaining limits: cooperative cancellation does not forcibly abort network operations or roll back already-started actions; a blocked plugin callback can still block the caller cancelling it. Callback exceptions are contained/diagnosed, but arbitrary foreign code cannot be made nonblocking by this lease. Campaign/Mission owner cleanup, all main features and versioned three-channel sub-MOD submission need their own acceptance.

## Current channel consumers (J17)

`run_native_transport.py` executes the extracted production Native API consumer with real
non-stream/stream transports. Its deadline test uses 60ms; caller-cancel tests use 5000ms
to avoid racing first-use streaming JIT against the deadline. Cancellation is still
asserted immediately after `caller.Cancel()` (no delay before checking the send token).
The three mutations remove the send deadline, caller linkage, or late-chunk guard;
each must compile and fail its corresponding behavior assertion.

`run_stream_fallback.py` executes the actual stream method, transports, message policy
and SaveRuntimeGuard with a recording non-stream sender. It covers caller/owner token
inheritance, cancelled fallback/empty retry, ignored-token late success suppression,
pre-cancel and no cancellation retry popup (19 checks). Its four token/publication
mutations must compile and fail behavior assertions; the sender/UI remain stubs.

`run_courier_retirement.py` executes the current generation finalizer, reply failure
and inbound failure methods with actual registry, request lifetime and lease sources.
It checks active worker cancellation, registry release before state-machine progression,
terminal flags, and stale/terminal rejection (18 checks). Three `--mutate` choices omit
the corresponding retirement call. Domain effects are stubs; the other generation
branches and actual game state-machine execution are not covered by this fixture.

All three runners accept `--run-root` only for a new repository-local directory and
use isolated SDK/cache paths and a minimal subprocess environment. Current DLL,
dual-API build and live-game/provider acceptance remain separate gates.
