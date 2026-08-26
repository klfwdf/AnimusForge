# AF087 range shout and ambient reaction bridge

## Scope

- Applies only while `SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal()` is true.
- GCCZ reusable policy lives in `src/AnimusForge.SiegeAftermathIntervention/SiegeAmbientReactionProfile.cs`.
- AF fused bridge code in `ShoutBehavior.cs` must stay limited to live agent selection, timing, prompt assembly calls, and speech queueing.

## Range shout bridge

- In GCCZ stage, range shout no longer relies only on model-emitted `[RELAY:id]`.
- If the first direct responder does not relay, the AF bridge selects up to `SiegeAmbientReactionProfile.RangeShoutAutoFollowupSpeakers` non-hero NPC units.
- Selection excludes the direct responder and heroes/notables/headmen, then mixes ordinary civilians and allied soldiers when possible.
- Follow-up speakers are spaced by `SiegeAmbientReactionProfile.RangeShoutAutoReplySpacingSeconds`.
- Follow-up speech still uses the existing full `AppendSiegeInterventionRuntimePromptForScene` path through `GenerateGroupConversationTurnLineAsync`.
- Follow-up speech does not run GCCZ postprocess tags again; only the first direct response can settle the player command.

## Ambient semantic reaction bridge

- `TownPromptComposer.BuildAmbientReactionFact(...)` is the single prompt source for town action witnesses.
- The fact starts with a deterministic internal action/audience marker, followed by localized scene, six-role authority, personality, and reply instructions.
- `SiegeAiInterventionBehavior` creates separate allied and civilian event ids, so each side receives its own MCM response allowance.
- Immediate reaction generation passes the fact explicitly to the auxiliary request and runs the shared GCCZ postprocessor as an indirect reply.
- The postprocessor receives only currently eligible non-mutating suggestion tags plus the allied ordinary-soldier discontent tag. It never infers intent from fixed dialogue words.
- Suggestion tags display advice but do not execute an action. Soldier discontent opens the existing one-time town appeasement consequence.
- Castle witness cleanup is supplied through the generic immediate-reaction completion callback; the shared `ShoutBehavior` path no longer hardcodes castle cleanup for town reactions.
