# AnimusForge 1.8.1 — Bringing the Camp to Life

Release date: November 5, 2026

Version 1.8 introduced field camps. Version 1.8.1 focuses on what happens after you step inside: fewer idle crowds, clearer companion duties, more natural rest and conversation, and more reliable movement. It also addresses diplomacy, armed coups, couriers and scene dialogue.

## A livelier, more believable camp

- Reworked resident activity allocation, strolls, pauses and returns to duty. Idle residents can turn to nearby companions or rest facing a campfire instead of merely cycling standing poses.
- Made companion assignments more visible: scouts look out, engineers repair, quartermasters write and surgeons wash.
- Expanded ground-sitting, lying, conversation and leaning poses, plus standing drinks near fires. Residents stand up before moving or changing sitting poses, reducing abrupt rest interruptions.
- Added more life around fires, tent entrances and work areas, with crates, firewood, worktables, handheld tools and food on grills.
- Fixed archery-post residents being reassigned to conversation spots, leaving the range inactive.

## More reliable movement, including larger camps

- Improved routes around fences and props, arrival checks and stuck-target handling. Fixed residents stopping near waypoints or continuing along an old direction after settling.
- Prefer stroll points near each resident's home post and abandon repeatedly unreachable targets.
- Scale the route-processing allowance with camp population and settle arrivals promptly, reducing delays between reaching a destination and starting an activity.
- Preserve individual post/current facing instead of turning everyone in the same direction.
- Improve ownership handoffs between camp life, following, conversations and duels.
- Halve ambient conversation waiting time and intervals inside field camps relative to the configured values. Other scenes retain their existing settings.

## Steadier diplomacy with clearer privacy boundaries

- Improve retrieval of diplomatic events and history; use lexical fallback when vector retrieval times out.
- Keep aid costs, payers and other private details from being directly exposed to unrelated realms.
- Fix rule registration, action-tag parsing and execution checks, and strengthen tribute caps, alliance limits, vassal matching and event-state recovery.
- Bound planning retries and account for request budgets more consistently.
- Remove the local declaration-length rejection ceiling. Model context and provider limits still apply.

## Practical fixes for coups, couriers and dialogue

- Enter armed-coup streets and halls in battle equipment, allow charge orders and suppress ordinary ambient chat during those fights.
- Improve RTS-hotkey blocking while typing in AF text popups.
- Add scrolling, clipping and caret following to long scene-shout input in both scroll and side-panel layouts.
- Preserve courier return-recovery information across interrupted dispatches or party transfers, with recovery handling after loading.
- Stop notables from initiating proactive letters. Normal replies to player-initiated correspondence are unaffected by this change.
- Update scene speech history incrementally and reduce repeated lip-sync scans. Fix speech/lip-sync cleanup ordering when a speaker dies or a mission ends.

## Illustrations, navigation and installation

- Separate director-only instructions from visual descriptions, unify prompt conflict resolution and revise vivid/natural style presets.
- Fix mission retention during free-camera screenshots.
- Preserve the native Kingdom button and append a separate AF terminal entry.
- Continue bundling the bekko retrieval model and its supporting files; use a lower-memory LZMA2 archive profile.
- Keep Bootstrap initialization alive when file logging is unavailable and improve mapped installation-path recognition.

## Updating

This is a refinement and stability update to 1.8, not the first release of camps, formations, duels or execution sites.

This batch delivers the CN edition only; EN packaging is deferred. The CN edition contains one `AnimusForge` module with Bannerlord 1.3 and 1.4 implementations. This batch is **not** a dedicated 1.5.6 build. Editions mainly select editable prompt defaults and worldbooks; UI and fixed context follow game language, without forcing the model's reply language.

Back up your saves, settings, custom prompts and worldbooks before updating. Use standalone 7-Zip: readable compression does not establish full Windows built-in extractor compatibility with every Unicode filename.

See the delivery record for completed builds and package checks stopped at the user’s request. Live gameplay, long-running saves and real model providers have not been accepted as tested; this release does not promise to repair already-corrupted saves.
