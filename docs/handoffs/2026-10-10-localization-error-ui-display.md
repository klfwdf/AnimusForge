# Shared error UI display localization

Phase: static/resource verification completed; local main slice, no build or deployment.
Baseline `b98a7d970`; includes LlmRetryPrompt, NonBlockingErrorReport,
AiErrorAnalysisInquiry, registered af_ui_strings pair and courier replay closure.

Added 20 resource keys for retry stages/description/buttons, failure title/details,
error feed envelope and analysis-result/failure envelopes. Resolve at existing UI
publication/confirmation boundaries. Retry stage input remains compatible with
legacy callers; known stages receive native localized display text. Internal
matching tokens remain for a subsequent complete runtime-literal extraction.
Raw diagnostics, full model/API responses, IsRetryableLlmError and dispatch logic
remain intact. Courier replay extraction includes the new stage helper and its
controlled TextObject leaf supports variables; replay execution is deferred.

PASS: all 25 registered resource pairs; existing localization DLL snapshots 1.3
and 1.4 each resolve 11,956 af_ui_strings texts. Static contract checks unchanged
protocol/full failure detail/dispatcher methods, callback/retry wiring, newline
encoding and native fallback consistency. These are not compiled C# execution.

NOT-RUN: product builds, compiled courier replay, rendered error interactions,
game/old-save acceptance. Background analysis errors and the embedded Chinese
analysis prompt remain release gaps; provider/old/user error text is not guessed
or silently rewritten. This slice does not make the module EN-release-ready.
Rollback: focused inverse of product `3eadd643f` and the subsequent analysis-failure slice.

## Analysis failure follow-up

Baseline `3eadd643f`. Added six paired keys for configuration unavailable/incomplete,
HTTP status, provider error code, empty reply and cancellation timeout. The private
worker returns immutable AnalysisFailure kind/detail data; only the existing
main-thread failure publication resolves native text. Exception type/message,
status/code, callbacks, the network request, system/user Prompt, cache and two-action
Tick budget are preserved. Removed an unreachable repeated success normalization.
Fixed the existing confirmation resource's literal backslash-n in both languages.

PASS: static contract and independent read-only review; 25 resource pairs;
existing 1.3 and 1.4 localization DLL snapshots each resolve 11,980 UI texts;
focused diff check. NOT-RUN: C# compilation, HTTP execution, rendered UI and game.
The Chinese analysis Prompt, fixed full-diagnostic headings and internal matching
tokens remain extraction/release work; raw model/provider/old/user text is preserved.
