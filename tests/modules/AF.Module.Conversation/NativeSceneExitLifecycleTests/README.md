# Native scene-action exit lifecycle regression

`python -B tests/modules/AF.Module.Conversation/NativeSceneExitLifecycleTests/run.py --run-root <new-local-directory>` compiles the actual production admission, exit event, begin event, arm/tick/execute/reset methods and pending DTO. Native game objects, movement execution and logging are deterministic substitutes. Actual MissionTick and mission/save reset wiring and line-edit scope are checked separately in the runner.

`--baseline` runs the same event test against checkpoint `4eaaeae37`; it must fail `native-end-event-does-not-execute-or-drain`, proving the old in-event execution rather than a compile failure.

Covers post-cleanup execution once, native flow/agent list/mission mode, immediate F reopen, same-index replacement Mission/Agent, inactive agent, ending mission, replaced Campaign/ConversationManager, reset, detached stale generation, no speech-queue retry, bounded tick and lords-hall exclusion. It does not prove live Bannerlord native memory/camera behavior or identify a player's missing crash stack.

Migration replay reads admission/arm/tick/execute/reset from the real SceneNativeMechanismController, pending DTO and begin/end wrappers from ShoutBehavior. It separately verifies the thin host Tick bridge, begin-only generation advance, history adapter partial-day flag and the MemoryHistoryCommit owner deletion consumer. The same event oracle and baseline failure remain unchanged.
