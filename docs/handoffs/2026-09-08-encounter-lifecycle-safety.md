# Encounter lifecycle safety repair

## Scope and ownership

Approved repair work following the GORK report audit. This is a partial delivery,
not completion of the entire AF cutover audit. The checkpoint is `77772637`.
Implementation commits: `4a239d95`, `cec3877a`.

Another task was actively editing ShoutBehavior, its postprocess partial, channel
boundary tests, the public ledger, and two existing handoff drafts. None of that
work was staged, modified, reverted, or included in this verification snapshot.
This is ordinary AF lifecycle/UI work: no GCCZ outcome, prompt or core changed.
No remote push, game deployment, LLM request, or player-save operation occurred.

## Implemented

- Replace the unscoped release boolean and scattered pending fields with one
  request binding the encounter instance, party, source Mission and save generation.
- Cancel expired/replaced/missing contexts before any native conversation or Mission
  exit. Cancellation clears only the authorization owned by that request.
- Revalidate after EndConversation because native callbacks can replace the context.
- Preserve manual early map exit and the existing 10-second delay, 120-second
  timeout, 0.25-second retry throttle, and existing native safe-passage effects.
- Wait through MissionState teardown when Mission.Current is already null; finish
  the same pending request when the map becomes active. Do not cancel valid release
  at that transitional boundary.
- Enforce the duel source-Mission deadline in ordinary scene mode as well as native
  conversation mode. Remove the now-unreachable second conversation check.
- Keep FocusTick safety installation independent of the runtime-game-adapter flag.
- Remove terminal search focus bindings to nonexistent methods; retain the existing
  editor and SearchText binding rather than inventing no-op VM handlers.
- Do not change duel keyword/level rules or the author's release-negotiation policy.

## Verification

- `python -B tools/EncounterLifecycleBoundaryTests/run.py`: 35 assertions passed.
  This executes extracted production methods with native effects stubbed, not a game.
- Final relevant sources match the fixed `cec3877a` verification worktree bytewise.
- Existing official unified build script: Debug and Release, each containing
  Bannerlord 1.3, 1.4 and Bootstrap, all zero warnings and errors. Stage only.
- Final diff/check, removed-symbol search, added-CJK-line check and prefab XML parse passed.
- Verification worktree: `.tmp/encounter-verify` at `cec3877a`. Keep it as a replay
  artifact, not a new development owner. It deliberately excludes other unfinished work.
- Logs and DLL SHA-256: `.tmp/encounter-lifecycle-boundary/` (`run.log`,
  `build-Debug.log`, `build-Release.log`, `artifact-hashes.json`).

Build recipe (run the repository script, unchanged):

```powershell
$root = 'G:\AFMOD\AF-REFACTOR\.tmp\encounter-verify'
$env:DOTNET_ROOT = 'G:\AFMOD\.dotnet-sdk'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:NUGET_PACKAGES = 'G:\AFMOD\AF-REFACTOR\.tmp\nuget-packages'
$env:DOTNET_CLI_HOME = 'G:\AFMOD\AF-REFACTOR\.tmp\dotnet-cli'
foreach ($configuration in @('Debug', 'Release')) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$root\一键编译覆盖推送\build_single_module.ps1" `
      -ProjectRoot $root `
      -BannerlordRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
      -Bannerlord13ReferenceDir 'G:\AFMOD\NEW-10\_deps_auto' `
      -Bannerlord14ReferenceDir 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
      -WorkshopContentDir 'E:\steam\steamapps\workshop\content\261550' `
      -RuntimeDependencyDir 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' `
      -Configuration $configuration -Stage
}
```

## Player acceptance: NOT_RUN

1. After an enemy accepts release, wait through the countdown; verify return to map
   and existing safe passage. Repeat by leaving manually before the deadline.
2. Cancel/replace the encounter, or load another save during the delay. Confirm the
   new scene is not closed, and it receives no release authorization or cooldown.
3. Start a duel through scene speech, then native conversation. Both must respect
   the displayed delay; no early scene exit and no duplicate duel start.
4. Open terminal search, type/filter/delete text and close the popup. Confirm the
   existing editor still behaves normally after removal of dead focus commands.

## Still outstanding

- Scene dynamic postprocess/action/relay/shared-memory parity: another task's
  unfinished work; re-audit its committed result and combined build before release.
- Courier (and relevant scene factory) main-thread snapshot boundaries. Moving the
  entire current builder onto the main thread is not an acceptable quick fix: its
  preparation also invokes synchronous rule preprocessing and can stall the game.
  Separate native capture from network/CPU work without changing channel semantics.
- Real Campaign/Mission event order, save/load and installed-mod acceptance are NOT_RUN.
- Do not claim the whole GORK list fixed or deploy this isolated intermediate build.
