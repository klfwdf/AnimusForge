# AF.Bridge.__MODULE_A____MODULE_B__

## Cross-module behavior and non-goals

- Behavior owned by this bridge: __BEHAVIOR__
- Module A remains responsible for: __A_RESPONSIBILITY__
- Module B remains responsible for: __B_RESPONSIBILITY__
- This bridge does not: __NON_GOALS__

## Joint ownership

- Module A owner/reviewer: __A_OWNER__
- Module B owner/reviewer: __B_OWNER__
- Bridge manifest ID: `af.bridge.__BRIDGE_ID__`

Both owners review public capability, gameplay outcome, persistence and version-range changes.

## Public capability/event use

| Participant | Capability/event | Version range | Use |
| --- | --- | --- | --- |
| `af.module.__MODULE_A__` | __A_CAPABILITY__ | __A_VERSION__ | __A_USE__ |
| `af.module.__MODULE_B__` | __B_CAPABILITY__ | __B_VERSION__ | __B_USE__ |

No private implementation assembly, field, method, save key or UI VM may be referenced.

## Persistence

- Bridge namespace: `__BRIDGE_NAMESPACE__`
- Schema: `1`
- Data meaning: __DATA__
- Missing/disabled bridge behavior: preserve data; __DEGRADATION__

## Lifecycle, conflicts and failure

- Activation class: __ACTIVATION__
- Harmony/tick/UI effects: __EFFECTS__
- Arbitration/conflict policy: __CONFLICT__
- Start/runtime failure: A and B remain usable; __FAILURE__

## Required composition matrix

| Composition | Expected |
| --- | --- |
| Foundation + A | A works without B/bridge. |
| Foundation + B | B works without A/bridge. |
| Foundation + A + B | Both work; no hidden integration. |
| Foundation + A + B + bridge | __INTEGRATED_RESULT__ |
| Missing dependency | Bridge is `Blocked`; no entry invocation. |
| Incompatible version | Bridge is `Blocked` with exact reason. |
| Bridge failure | A/B remain usable; inventory reports bridge trace. |
| Bridge disabled / SafeMode | No new cross-state writes; bridge data is preserved. |

## Validation

- Contract/composition tests: __TESTS__
- 1.3/1.4 scenarios: __GAME_TESTS__
- Save/rollback scenarios: __SAVE_TESTS__

## Known limitations and deferred work

- __LIMITATION__
