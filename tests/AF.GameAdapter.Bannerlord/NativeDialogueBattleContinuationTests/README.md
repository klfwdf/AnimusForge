# Native combat dialogue continuation

Run `dotnet run --project NativeDialogueBattleContinuationTests.csproj -c Release`.
The production continuation, scope owner and save generation are compiled directly. Game/menu/Harmony
boundaries are fixtures. The menu boundary rejects an activation without the scoped redirect bypass,
then substitutes vanilla battle initialization. The test verifies delayed teardown, recognized native
fight endings, cancellation, reentry and exception cleanup; it does not run a real battle.

`native-hooks/NativeHookTests.csproj` targets net472 and takes explicit `GuardReferenceDir`,
`GuardSharedReferenceDir` and `GuardHarmonyDir`, with isolated outputs for each API. It installs the
production postfixes with real Harmony onto actual native ProcessSentence/EndConversation methods.
It verifies installation/signatures and inactive/skipped calls, not a live conversation.

Both native source references use the terminal NPC IDs `player_turns_down_surrender`,
`lord_attack_verify_commit` and `frivolous_surrender_demand_response`, each targeting `close_window`.
Variation text keeps the same ID. Merely showing demands or hostility is not an accepted ending.
The existing EncounterLifecycleBoundaryTests runner remains the regression gate for handoff/release.

Integration: the existing custom native-dialogue option calls Begin; session start/open failure cancels;
existing application tick consumes queued work; LordEncounterBehavior.TryGetCustomEncounterMenuDisableReason
honors the transient bypass while the native encounter menu initializes. No manual game DLL replacement,
Stage, deployment or automatic attack mission launch is part of these tests.
