# Scene execution terminal proof

`run.py --run-root <fresh-path>` compiles the actual session state, immediate
completion/publisher, player request owner, complete ModuleSceneContext, complete
postprocess gates and synchronization context against game/network leaf stubs.
22 assertions cover once-only claims, epoch/generation/mission/target rejection,
reset without queue draining, original cooldown, effect/history/speech order,
nontransactional failure without retry, frozen module identities and gate races.
Twelve compiled mutations must fail those assertions, not compilation.

`source_review.py` performs complete-body inverses against fixed Git `84f428cd`:
deferred postprocess, directive rules, four gates, module context, compact prompt
and normalization, 705-line group chain, passive/fallback, both player capture
methods and both player execution entries. Explicit reviewed deltas are detached
five-phase prompt scheduling, scalar history capture, main-thread entry/note and
scope guards. The queue-admission failure check is static: the same pending
request remains reachable until main-thread retirement, without worker callbacks
or effects and without retry. It does not claim dispatch remains usable after a
queue provider throws.

SceneContinuationLifecycleTests separately executes the actual synchronization
context with the actual sole PendingOperationRegistry (5 assertions, 3 mutations).
SceneGroupReceiptTests executes actual receipt entry and B's actual two history
capture/build methods with actual history assembly owners (31 assertions).

SceneRequestLifetimeRegressionTests retains every original UI/replay assertion:
34 plus 2 J14 completion assertions. Its default explicitly uses fixed Git
`84f428cd` as the pre-migration UI/game-stub oracle, and requires the current
complete-body inverse on every run. Nine original mutations remain executable.
It is not represented as executing the complete current game runtime. Actual
current request/gate/context execution is covered by the 22 assertions above.

All synthetic output requires fresh isolated run roots. Source reviews are
read-only and require no output argument. No live Bannerlord, pathfinding,
real AI/TTS service or player save acceptance is claimed. Product dual-API and
Bootstrap compile is a separate integrator gate bound to its frozen manifest.
