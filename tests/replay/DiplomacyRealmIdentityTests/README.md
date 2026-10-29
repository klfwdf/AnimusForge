# Sourced diplomatic realm identity replay

Run from the repository root:

```powershell
python tests/replay/DiplomacyRealmIdentityTests/run.py
```

The runner compiles exact production method spans for title selection, institutional
context/cache, source precedence and title validation, using the real package JSON
readers. Game objects, `GameTexts` registrations and knowledge retrieval are fixture
leaves. Both CN and EN packages run in separate processes. The output includes
source hashes and one-based method coordinates.

Coverage includes reused `empire_n`/`empire_w`/`empire_s` IDs with a custom consul and
council; current native male/female titles; missing title/library/lore; failed
retrieval; world-book revision and same-version library replacement; ruler/name,
case-only display and title changes; bounded cache; and CN/EN personal-title
contradictions without inferring a government. Static checks trace the shared
generation, oral-validation, retry and prompt-version consumers.

This does not test real Bannerlord loading, a total-conversion MOD, world-book
retrieval quality or model compliance. Build both APIs using the existing unified
build entry point. The broader bilingual parser suite retains its independent
tribute, cession and topic assertions; this focused replay does not waive them.
