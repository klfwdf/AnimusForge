# Native admission identity regression

Run `python -B tests/modules/AF.Module.Diplomacy/NativeAdmissionIdentityTests/run.py`
with explicit `--references`, `--shared-references`, `--framework` (.NET Framework
4.7.2 reference assemblies) and an optional new workspace `--run-root`.
Use the verified 1.3 reference directory and then the verified 1.4 directory.
No dependency installation, model call, game launch, save access or deployment occurs.

The runner compiles the complete production country-admission host partial,
shared resolver and admission domain. The identity contract is extracted verbatim
from the application partial. Actual TaleWorlds assemblies perform object lookup,
hero state reads and registry traversal. Reflection initializes a synthetic campaign
and native entities; it does not replace lookup methods or return expected identities.
Unrelated border/war/relation capture is outside this test and must not run.

Coverage: live/dead/missing/dynamic heroes, countries, allegiance changes, unknown
affiliation, subject-country and core-hero capture consumers, the separate settlement
registry, campaign replacement and unavailable campaign. Each of four wrong-store
mutations must compile and then fail behaviorally at its intended check. The reference
and production source hashes are recorded with build/run logs.

Application recovery/publication and controlled model responses remain covered by
`WorldDiplomacyRoundLifecycle.SmokeTests`. Neither suite is live-game acceptance or
proof that every provider response produces an event within three game days.
