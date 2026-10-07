# Scene conversation scope contract

Compiles the production `Channels/Scene/SceneShoutConversationScope.cs` with small Bannerlord type stubs. It verifies stable audience merge/order, Mission/epoch and Agent identity revalidation, liveness, and fail-closed capture.

`run_mutations.py` creates ignored copies and proves the epoch, Agent-reference, and origin-merge guards are observed by named behavioral cases. This is deterministic offline evidence, not a live Mission, LOS, audio, performance, or save test.

`run_capture.py --run-root <new repository-local output>` also compiles the actual
`TryBuildSceneShoutConversationScope` method extracted from the scene host with the production scope.
It covers the default ambient audience, frame-only filtering before requests, late arrivals, selected
anchor/LOS metadata, manual exclusion, primary protection, far invited members and stale targets.
Game objects and LOS are fixtures; this does not verify a live Mission or MCM.
