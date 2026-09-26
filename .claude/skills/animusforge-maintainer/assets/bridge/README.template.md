# __BRIDGE_NAME__

Use for an actual cross-domain behavior, not a thin same-DLL adapter.

## Required: cross-domain contract

- Joint owners and participants: __OWNERS__
- Integration owned here / responsibilities retained by each participant: __BOUNDARY__
- Supported internal or public contracts and real callers: __CONTRACTS__
- Missing, incompatible, failed and disabled behavior: __FAILURE_SEMANTICS__
- Verification / source revision / NOT-RUN / rollback: __EVIDENCE__

## Composition evidence

| Composition | Expected and verified result |
| --- | --- |
| A alone / B alone | __INDEPENDENCE__ |
| A+B without bridge | __NO_HIDDEN_INTEGRATION__ |
| A+B with bridge | __INTEGRATION__ |
| Dependency missing/incompatible | __REJECTION__ |
| Bridge failure or disablement | __UNRELATED_BEHAVIOR_AND_DATA_PRESERVED__ |

## Conditional: include only affected surfaces

- Owned persistent state, existing namespace/schema/keys and compatibility; omit for stateless bridges.
- Actual activation/registrations/Harmony/tick/UI effects, conflicts and cleanup/restart.
- Real profile/SafeMode/manifest consumer, if implemented; do not invent one.
- Both Bannerlord 1.3/1.4 builds, runtime and save scenarios when relevant.

Do not reflect participant-private fields, write their raw save keys or copy their algorithms.
