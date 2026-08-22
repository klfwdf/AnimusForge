# Town Hidden Resident Spawn Bridge

## Runtime contract

- The feature is active only inside the current GCCZ town-center aftermath mission.
- A semantic `GatherCivilians` action is the only spawn trigger. Civic actions that merely reuse the gathering choreography do not reveal hidden residents.
- `TownHiddenResidentSpawnLedger` owns the dependency-free per-request, per-scene, visible-population, operation-snapshot, and scene-cap decisions.
- `TownHiddenResidentSpawnPositionPolicy` owns deterministic candidate ordering and group offsets. The AF bridge resolves those candidates against the live navmesh and active-agent clearance.
- `InterventionHiddenResidentSpawnMissionBehavior` owns Bannerlord-only civilian creation, location registration, spawning, safe-corner placement, and diagnostics.
- `GcczTownHiddenResidentResourceProvider` owns localized feedback. The Chinese catalog lives in `ModuleData/GcczTownHiddenResidents.zh-CN.json` and is also embedded as a fail-safe resource.

## Limits and accounting

- One request can add at most six residents.
- One scene can add at most twelve residents.
- No request adds residents when twenty-four or more ordinary civilians are already visible.
- No request adds residents after a plunder target snapshot or massacre victim snapshot is sealed.
- Successfully spawned residents are registered by the existing scene civilian tracker before any later operation snapshot is captured.
- The spawn ledger and its ordinary-civilian memory are mission-local and disappear on scene exit.

## Isolation

- No mission-start or periodic population refill exists.
- No raw agent origin is created.
- No merchant, notable, workshop, or market spawn tag is consumed.
- No village, castle, ordinary AF scene, or world-map path calls the behavior.
- Failed placement never falls back to spawning in front of the player.
