# Courier authoritative postprocess owner regression

This tool executes the complete `CourierDeliveryBehavior.DetachedPostprocess.cs`, the actual Courier port factories, request-to-envelope mapping, domain qualification mapping, and Shout's real single-use `CourierActionPostprocessWorkItem`. Contracts, composition/coordinator, anonymous-message adapter, visible-letter/JSON cleaners, and `LegacyActionTagParser` compile directly from production sources.

## Run

```powershell
python -B tools/CourierPostprocessOwnerRegressionTests/run.py
python -B tools/CourierPostprocessOwnerRegressionTests/run_mutations.py
python -B -m unittest discover -s tools/CourierPostprocessOwnerRegressionTests -p test_extraction.py
```

The runner writes source fingerprints, generated source and logs under ignored `.generated/`. Only the generated owner-phase deadline changes from `Task.Delay(30000)` to `Task.Delay(180)` for a bounded timeout test. It does not edit production, deploy, call an API, or load a save.

## Tested boundaries

- Exact prepared main prompt, immutable role/content strings, 5000-token budget, no repeated builder inside the extracted prepared-envelope capture, and default-entry wiring that reuses that envelope.
- Actual input, history, raw reply (including role-play evidence), selected hits, entity context and rule flags reach the game-owned preparation. Player-visible text is separately cleaned; error/empty/internal-only replies cannot become successful coordinated results.
- Unbound synchronous/async parsing cannot grant actions. The raw unqualified model stream is normalized on a physical main thread before the real parser creates an ActionPlan.
- Generation, owner, missing/terminal session, generation-started/generated flags, dead/different recipient and same-ID recipient replacement are rejected. Context keys are request-local and weak; completions cannot replay or cross concurrent recipients.
- Pending cancellation/timeout retires an unstarted callback, duplicate invocation is inert, and normalization exceptions cannot reenter. **Cancellation after an action starts cannot roll back its effects**: the fixture explicitly retains one started effect while rejecting the result.
- Eight mutation controls must fail the intended runtime case rather than merely fail compilation. Seven extraction checks prevent replacing a production boundary with a mock implementation.

## Deliberate limits

Hero/session lookup and the domain-heavy `TryPrepareCourierActionPostprocessForExternal` implementation are stubbed. Its actual caller, complete argument list, real work item, owner dispatch, visible cleaners and parser execute; domain economics/policy actions do not. The snapshot adapter is a detached copying stub. A zero builder count proves only the extracted prepared capture does not rebuild, while a separate source check covers default-entry reuse. It does **not** prove all original request-builder internals run on the main thread; that pre-existing issue is tracked separately.

The shared ChannelCutoverBoundary suite still covers its original 24 Courier terminal/fallback/queued-completion cases. Its new owner-phase dependencies remain stubs, while this suite tests those production owners directly. `ProductionCourierHostReplayTests` separately tests the real built DLL without a Campaign: absence of a live owner must deny postprocess/ActionPlan rather than manufacture a second HTTP stage.
