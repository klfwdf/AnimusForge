# SETS and Hostile-Castle Localization Contract

## Ownership

- Reusable English and Simplified Chinese strings live in `ModuleData/Languages/sets_hostile_meeting_strings.xml` and `ModuleData/Languages/CNs/sets_hostile_meeting_strings-zh-CN.xml`.
- The two resource files are mirrored byte-for-byte into the fused module under `AnimusForge/ModuleData/Languages`.
- AF host code remains a thin consumer. It must use a localization token with an English fallback and must not embed the Chinese text in C#.

## Runtime identifiers

| Identifier | Intended host use |
| --- | --- |
| `sets_owned_target_supported_armed_fact` | Target-facing scene fact when a target fights the player in the player's domain and local guards plus the player's escort protect the player. Set the `PLAYER` variable. |
| `sets_owned_target_armed_fact` | Target-facing scene fact when the player's escort supports the player during conflict in the player's domain. Set the `PLAYER` variable. |
| `sets_owned_guard_reaction_fact` | Guard-facing scene fact for an armed conflict in the player's domain. Set the `PLAYER` variable. |
| `sets_owned_armed_conflict_notice` | On-screen armed-conflict notice in a player-controlled settlement. |
| `sets_foreign_armed_conflict_notice` | On-screen armed-conflict notice in a foreign settlement. |
| `af_hostile_castle_native_meeting_disabled_reason` | Settlement-access denial reason when the protected native-meeting context is unavailable. |

## Registration and validation

- Register the English resource in `ModuleData/Languages/language_data.xml`.
- Register the Simplified Chinese resource in `ModuleData/Languages/CNs/language_data.xml`.
- `tools/verify_gccz_town_refactor.ps1` verifies both mirrors, both registrations, all identifiers, and removal of the corresponding hard-coded Chinese strings from the AF host files.
