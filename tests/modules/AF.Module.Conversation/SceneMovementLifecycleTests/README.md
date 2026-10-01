# Scene movement ownership and lifecycle proof

Run `python -B tests/modules/AF.Module.Conversation/SceneMovementLifecycleTests/run.py --run-root <fresh workspace-local path>`; the output root is required and cannot reuse an existing directory.

19 executable cases compile real controller declarations/types/state, mission reset and escort completion, location-path/return logic, plus the complete detached compact reaction runtime. Game primitives/navigation/ports/model/formatting are deterministic substitutes. Tests do not call a live model or game. Three formatter calls are checked for captured options and tag invisibility; full formatter rule coverage is separate.

`source_review.py` compares 165 actual moved declarations, 14 types, and 52 fields/constants/compiled regex/reflection bindings to Git `42db3ea2`, reversing only precise ports and reviewed mission/cancellation guards. The compact prompt/history/AFEF construction is token-exact to the same baseline. No source hashes are refreshed.

`run_mutations.py --run-root <fresh path>` requires four actual compiled mutations to reach their specific assertion failures; source lookup/compiler failures are not acceptance.

Covered: invalid admission, follow-return origin, relay LocationCharacter identity, preparation rollback without retrying prior effects, batch cancellation ownership, guide identity, return replacement, door graph/next-door LocationCharacter return, distance-only wait, missing target, reset cleanup once, cancellation/late arrival, same-index replacement mission, arrival once, detached model input/options/limits and failure no retry.

Not covered: running Bannerlord navigation meshes, real agent/proxy spawning, actual TTS timing, full guide route/arrival/return in a game mission, or live player saves. Both API product builds are handled by the integrator, not this fixture.
