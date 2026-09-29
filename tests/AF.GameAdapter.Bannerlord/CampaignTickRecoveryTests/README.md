# Campaign tick prior-crash recovery bridge

`run.py` extracts the current `ConsumePriorCrashSuspectPartySkip` and
`PartyHourlyAiTickPrefix` methods and runs them with a synthetic party. The
checkpoint loader is source-checked, not executed against a real log. The
replay confirms that the prior-crash ID gate suppresses 18 native hourly AI
calls, including when successive calls use distinct objects with the same ID.

This is a diagnostic-to-gameplay responsibility boundary, **not** evidence of
a real crash, an affected save, or correct/incorrect recovery policy. The
`--mutate ignore-skip` variant must fail the named skip assertion.
