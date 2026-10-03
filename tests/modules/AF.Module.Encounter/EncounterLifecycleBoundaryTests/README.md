# Encounter lifecycle boundary regression

Run `python -B tests/modules/AF.Module.Encounter/EncounterLifecycleBoundaryTests/run.py --dotnet local/dotnet/8.0.425/dotnet.exe` from this checkout.
The launcher accepts an explicit project-local .NET SDK and has no package sources.
Generated files and the result log stay under `.tmp/encounter-lifecycle-boundary`.

The harness extracts production methods verbatim using the existing boundary extractor
and compiles the actual Encounter target owner source. Only native API effects are stubbed.
Assertions cover selected army members, stale target fallback, the release deadline, manual map
exit, duplicate completion, expired authorization, encounter/party/Mission/save changes,
reentrant conversation callbacks, non-Hero parties, the duel deadline in both scene modes,
and unconditional one-time FocusTick safety installation.

The intercepted-meeting cases also replay the actual hard-suppression and village-activity
predicates: a new encounter cannot inherit an old attack lock, native battle/result/mission
cleanup remains protected, and raid AI orders alone do not exclude a field meeting.
Actual raid/supplies/volunteers events and the current raided village remain protected.
Use `--source-file <historical LordEncounterBehavior.cs> --audit hard` or `--audit raid`
to reproduce each old-source semantic failure independently.

This is control-flow evidence, not a Bannerlord Campaign/Mission playthrough or proof
that native encounter cleanup has the same event order on every supported game version.
No game files, save files, LLM service, or shared scene-postprocess source is touched.
