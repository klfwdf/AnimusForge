# GCCZ village governance checks

- `verify_wiring.py`: bounded source/resource wiring checks. It does not simulate the game.
- `verify_runtime_prompts.ps1`: execute the actual built DLL's castle prompt renderer with CN/EN catalogs. Run each DLL in a fresh Windows PowerShell process. Pass `-AssemblyPath`, `-ReferenceDir` and `-HarmonyPath` using the same dependencies as the official build. No Campaign, native mission, provider request or configuration mutation is started; module initializers may install managed patches in this test process.
- Standalone rule tests live in `F:/AF/GCCZ/tests/AnimusForge.SiegeAftermathIntervention.Tests/Village/VillageGovernanceTests.cs`. The reusable village sources must match the AF mirror; do not copy the independently diverged Castle/Town localization files wholesale.

Acceptance still requires real saves/UI, permission changes, aid day progression, actual troop actors, culture questions and player confirmation, and SETS military rewards followed by village settlement. Known unfinished behavior is tracked in the main ledger, not silently treated as passing.

## Review regression (2026-10-15)

`../GcczVillageReviewTests` source-links production MCM settings and village economy. It checks draft/preset isolation, live-save-only writeback, undo/terminal coherence, value bounds and destruction's consumed remainder (24 checks). MCM hosts are fakes, not rendered UI or real configuration IO. Separate source wiring now covers the installed parade anchor, main/postprocess separation, daily food suppression and explicit village TAB bypass (29 checks).

Isolated pre-fix files from `9a4b9bdd` fail the named clone-mutation and destroyed-remainder assertions; the latter fixture excludes the newly introduced method's separate test so it compiles before failing behaviorally. Full outputs and initial fixture compilation failure are retained under `artifacts/gccz-review-game-20261015/`.

The user explicitly retained cross-visit resource refresh as an easter egg. No 10-day resource cooldown was added. Foreign concealment remains unavailable; native UI, real saves, actor navigation and player testing remain required.
