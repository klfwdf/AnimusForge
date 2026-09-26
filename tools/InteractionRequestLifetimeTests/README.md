# Interaction request lifetime checks

Pinned main: `437925b856fae76b4e9ee207e96ba048f35d5a67`.

```powershell
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --main --case common
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --main --case supersede
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --main --case dispose
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --main --case token
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --main --case race
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --main --case precancel
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --mutate dispose_early
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --mutate propagate_callback
python -X utf8 -B tools/InteractionRequestLifetimeTests/run.py --mutate ignore_active_cancel
python -X utf8 -B tools/InteractionRequestLifetimeTests/verify_compat.py
```

The common main cases must pass; the five pinned defect cases must compile and fail their intended assertions. The current all-cases run must pass; three deliberate mutations must compile and fail behavioral assertions. Cancellation/completion races use explicit synchronization, not timing guesses.

`verify_compat.py` compares public signatures, compiles `LegacyClient.cs` against the main source-built library, then executes unchanged consumer IL with the current source-built replacement. This is a real compiled-client ABI check for the existing coordinator, not a claim that Api.V1 request APIs are implemented. Both libraries contain actual contracts/coordinator source; the pipeline is a deterministic test double. No game/LLM calls occur.

Generated main/current/mutated artifacts remain separate under ignored `.generated/`. Production contains only the new internal lease and the original coordinator entrypoints. No copied main implementation is shipped.

Remaining limits: cooperative cancellation does not forcibly abort network operations or roll back already-started actions; a blocked plugin callback can still block the caller cancelling it. Callback exceptions are contained/diagnosed, but arbitrary foreign code cannot be made nonblocking by this lease. Campaign/Mission owner cleanup, all main features and versioned three-channel sub-MOD submission need their own acceptance.
