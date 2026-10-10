# MCM / Terminal Dropdown Display Slice

Phase: ACTIVE. Workspace: F:/AnimusForge-main, branch main. Baseline: dc679cea8.

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
