# EndMissionInternal protected-cleanup contract

## Scope

This is an AF host integration contract for mission shutdown. The compile-ready
implementation lives in `EndMissionInternalSafePatch.cs` in the fused tree. The
standalone GCCZ project does not copy the Harmony patch; this document records
the boundary that must survive future AF merges.

The native-settlement branch is deliberately narrow: only the mission started
by an actual vanilla request to meet someone at a hostile castle may be
registered as `NativeHostileCastleMeeting`. Merely opening or observing a
settlement menu, remaining in the same encounter, or starting another mission
nearby is not sufficient. Existing explicitly identified AF meeting and AF duel
missions retain their separate protected kinds.

## Registration contract

1. The vanilla conversation-start path arms a one-shot context containing the
   expected hostile castle and selected hero. The first mission-start event
   consumes it regardless of whether validation succeeds, so slow scene loading
   cannot expire the correct mission and an unrelated mission cannot reuse it.
2. Mission start validates and consumes that context. A missing, expired,
   mismatched, or already consumed context must fail closed and must not protect
   the mission.
3. Protected missions are keyed by `Mission` reference identity. Protection is
   never inferred from a global scene flag after registration.
4. Normal missions execute the original shutdown path directly and retain the
   original exception behavior.

## Cleanup contract

The transpiler must match exactly one occurrence of each validated cleanup call
in `Mission.EndMissionInternal()` and therefore exactly ten replacements:

1. `IMissionListener.OnEndMission`
2. `Mission.StopSoundEvents`
3. `MissionBehavior.OnEndMissionInternal`
4. `Agent.OnRemove`
5. `Agent.OnDelete`
6. `Agent.Clear`
7. `MissionFocusableObjectInformationProvider.OnFinalize`
8. `MissionObject.OnEndMission`
9. `Mission.FreeResources`
10. `Mission.FinalizeMission`

If the IL shape or any required private cleanup delegate does not match, native
hostile-castle meeting protection is unavailable and the access model must keep
that request-meeting path disabled.

For a registered protected mission, each wrapper catches only a
`NullReferenceException` thrown by that one cleanup operation, records the
failed step, and allows the later cleanup operations to run. Any other exception
is propagated unchanged. No prefix or finalizer may convert an arbitrary
mission exception to `null`.

The Harmony finalizer is transparent: it always unregisters the exact mission
reference and returns the original exception unchanged. This prevents a strong
reference from surviving an early non-null-reference failure without weakening
the game's exception semantics. `SafeFinalizeMission` also unregisters on the
ordinary completion path, so removal is idempotent.

## Required verification

- Opening the hostile-castle menu without starting a conversation cannot arm a
  later unrelated mission.
- Selecting hero A registers only the resulting vanilla meeting with hero A;
  selecting hero B on the next attempt cannot reuse A's context.
- Each protected cleanup step may ignore only its own null reference and later
  steps still execute.
- A non-null-reference exception from a protected cleanup step propagates.
- Exceptions from unprotected missions propagate exactly as before.
- Protected mission registrations are removed after both successful and
  exceptional shutdown.
- Bannerlord API 1.3 and 1.4 builds both validate the same ten-call IL contract.
