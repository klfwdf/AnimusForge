# Encounter lifecycle boundary regression

Run `python -B tools/EncounterLifecycleBoundaryTests/run.py` from this checkout.
The launcher uses the existing .NET SDK under the AFMOD root and has no package sources.
Generated files and the result log stay under `.tmp/encounter-lifecycle-boundary`.

The harness extracts production methods verbatim using the existing boundary extractor.
Only native API effects are stubbed. Assertions cover the release deadline, manual map
exit, duplicate completion, expired authorization, encounter/party/Mission/save changes,
reentrant conversation callbacks, non-Hero parties, the duel deadline in both scene modes,
and unconditional one-time FocusTick safety installation.

This is control-flow evidence, not a Bannerlord Campaign/Mission playthrough or proof
that native encounter cleanup has the same event order on every supported game version.
No game files, save files, LLM service, or shared scene-postprocess source is touched.
