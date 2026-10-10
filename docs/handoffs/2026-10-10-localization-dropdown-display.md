# MCM / Terminal Dropdown Display Slice

Phase: VERIFY. Workspace: F:/AnimusForge-main, branch main. Baseline: dc679cea8.

Scope: localize fixed dropdown labels and courier stationery names through native
TextObject IDs. Keep selection indices, legacy settings values, model names,
configuration keys and API routing intact. Resolve terminal display labels only
on existing UI events, with a per-item cache; do not add Tick work.

Files: DuelSettings.cs, DuelSettingsUiLocalization.cs, CourierLetterThemes.cs,
AnimusForgeTerminalUiModels.cs and the registered af_ui_strings resource pair.
Add a narrow display/selection contract check where existing tests permit.
Existing policy option-list contracts keep their internal Chinese values.

Validation: registered resource-pair contract, real localization DLL parsing on
both reference lines, source/selection boundary checks and focused diff review.
Builds, Stage, deployment, push and live-game acceptance are deferred by the user.

Parallel Economy work and external B/C changes remain outside this slice.
Rollback: focused inverse of this slice's eventual product commit.

2026-10-10 validation: added 34 option keys; all 25 registered resource pairs
pass XML/key/placeholder contracts. Existing 1.3 and 1.4 localization reference
DLLs each resolve 39,992 strings correctly for English, Chinese, untranslated
French and switch-back. This is resource parsing, not a new product build.
Terminal constructor, selector change, previous/next, inquiry callback and
refresh now resolve labels at their existing UI boundaries. Only localizable
labels are cached. Model dropdown caches still contain legacy normalized values;
the native MCM dropdown receives display markers and normalizes them on read.
Legacy manual presets map to the existing marker without appending a second
sentinel. Added model replay assertions; execution is deferred with builds.
NOT-RUN: model replay, product compilation, live MCM/terminal rendering, save/UI
interaction. Internal Chinese configuration values remain a later extraction
task under the expanded full-English release target.
