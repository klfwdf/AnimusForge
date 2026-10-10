# Shared error UI display localization

Phase: VERIFY; local main slice, no build or deployment.
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
Rollback: focused inverse of this slice's product commit.
