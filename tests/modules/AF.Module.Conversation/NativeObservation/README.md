# Native raw-reply game-thread boundary

This fixture executes the actual Native raw-taunt/observation/early-TTS call site and the actual `SubmitNativeConversationSceneActionObservation` method. The controlled `Mission/Agents` port refuses worker-thread reads. The TTS entry is an explicit thread-affinity spy, **not the real audio/provider engine**.

At `00574541` the raw-action callback returns to a worker before observation and early TTS:

- `run.py --baseline` reproduces a refused Mission read caught by the real observer, so the observation is silently lost.
- The tableau case reproduces entry into the TTS thread-affinity spy on the worker; the real TTS method separately contains Hero/Character/voice/Mission capture and is unchanged by this fix. This is a source-call-site/port proof, not a claim of measured in-game audio failure.
- The normal acceptance test was run **before** each placement fix and failed its expected named assertion.

The fix keeps raw taunts → natural-action observation → display cleanup → early TTS in the **existing validated game-thread callback**, before its continuation is released. No extra queue, network path, default toggle or save state is added. The second postprocess-start validation remains after this phase. The same rejection/rollback, non-Hero/tableau, fallback prose, no-speech exclusion and best-effort parser behavior remain.

`run.py` covers 37 checks across normal, fallback, stale target, disabled observer, missing Mission/Agent, tableau, silent reply and parser failure. `run_mutations.py` moves observation/TTS back to the worker, duplicates observation or drops target validation: all four must compile and fail their named assertions. `test_source_review.py` proves the exact reviewed placement changes, helper bodies unchanged, order/single call and CRLF/no BOM.

Game-thread cost is one existing Mission-agent lookup per accepted Native reply plus the existing early voice/TTS capture on tableau replies; there is no new per-frame poll. Actual frame cost, Bannerlord execution and real TTS/provider behavior remain NOT-RUN. Main ledger owns the candidate-specific results.
