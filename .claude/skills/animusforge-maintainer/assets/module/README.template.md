# AF.Module.__NAME__

## Purpose and non-goals

- Purpose: __PURPOSE__
- Non-goals: __NON_GOALS__

## Ownership

- Team: __TEAM__
- Maintainers: __MAINTAINERS__
- Manifest ID: `af.module.__MODULE_ID__`

## Public capabilities and events

| ID | Version | Role | Contract owner | Notes |
| --- | --- | --- | --- | --- |
| __CAPABILITY__ | 1 | provider/consumer | `AF.Contracts` | __NOTES__ |

Do not expose private Behavior classes, Harmony targets, UI VMs, static state or raw save dictionaries.

## Dependencies and profiles

- Required modules/capabilities: __REQUIRED__
- Optional modules/capabilities: __OPTIONAL__
- Profiles: __PROFILES__
- Bannerlord API lines: 1.3 / 1.4 / __LIMITATION__

## Lifecycle and runtime effects

- Activation class: `boot-only` / `save-load-boundary` / `runtime-toggle-safe`
- Harmony patches: __PATCHES__
- Campaign/Mission events: __EVENTS__
- Tick/background tasks: __TASKS__
- UI contributions: __UI__
- Stop/restart constraints: __CONSTRAINTS__

## Persistence and user data

- Namespace: `__NAMESPACE__`
- Current schema: `1`
- Legacy types/keys: __LEGACY__
- Migration and corruption behavior: __MIGRATION__
- PlayerExports/content ownership: __CONTENT__

## Interaction impact

- Channels: scene shout / native conversation / courier / none
- Rule/capability/action/history effect: __INTERACTION__
- Explicit exclusions: __EXCLUSIONS__

## Diagnostics and health

- Health relationship: __HEALTH__
- Trace/metric names: __DIAGNOSTICS__
- Failure/degradation behavior: __FAILURE__

## Validation

- Pure/focused tests: __TESTS__
- Real profile composition: __COMPOSITION__
- 1.3/1.4 build/in-game scenarios: __GAME_TESTS__
- Save migration scenarios: __SAVE_TESTS__

## Extension rules

- Add behavior through: __EXTENSION_POINT__
- Cross-module behavior belongs in a co-owned `AF.Bridge.*`, not here.

## Known limitations and deferred work

- __LIMITATION__
