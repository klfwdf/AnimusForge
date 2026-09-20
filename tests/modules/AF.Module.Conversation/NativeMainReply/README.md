# Native main-reply acceptance phase

The real `SubmitNativeConversationTextInternalAsync` now invokes `NativeConversationMainReplyStage` once. The stage owns completed-response normalization, post-provider generation checking, main-thread target validation, scoped pending-input revocation and empty/provider-error stop decisions. The existing provider helper, text cleanup helpers and later raw actions/TTS/postprocessing are not replaced.

`INativeConversationMainReplyHost` is a same-DLL internal port. The private host captures the original admission/key/sequence and routes target validation and pending revocation through existing main-thread operations. No public API, persistence identity or parallel provider pipeline is added. A default result is NotStarted, never permission to continue.

## Executable evidence

- `run.py`: current stage, current private adapter, current caller's six-line stop gate and production `LlmVisibleReplyNormalizer` versus the exact 36-line source phase at `dabee763`.
- 19 scenarios / 179 checks: normal and Unicode JSON replies, cleaned-empty-but-continuing reply, raw empty replies, all four failure prefixes, stale generation/owner/epoch/ticket/target, empty rejection reason and provider/validation/rollback exceptions. Both paths cross a real provider gate and controlled main-thread queue; result, call order, pending identity, popup, single provider invocation and exception identity are compared.
- `presentation-only` deliberately does **not** invent a presentation-revision test inside backend admission validation. Presentation/UI authority is separately covered by the existing Native presentation suite. This fixture's target predicate and queue are controlled ports, not a replacement for that suite or live game tests.
- `run_mutations.py`: ten mutants must compile, reach the named scenario and fail its specified assertion. Mutations preserve diagnostic calls when needed so an earlier trace mismatch cannot masquerade as a later stale-generation rejection.
- `test_source_review.py` / `source-review.json`: exact caller/added-file binding, no second active old phase, strict inverse, CRLF/no BOM and no unrelated source drift. The older Native inverse packets compose with this one rather than refreshing their digests.

The PendingHistory suite's first failure branch now executes the actual extracted branch plus the source-derived captured rollback port; the other four branches and original mode remain intact. Text prefix/leak helpers and game side effects are explicitly recording stubs here; a fake provider is used and no paid service or game installation is touched.

Run these Python entry points from the repository root with `AF_DOTNET` and `AF_NEWTONSOFT` set. `.generated/` is test output and not delivery content. Actual acceptance results and remaining stages live in the main refactoring ledger.
