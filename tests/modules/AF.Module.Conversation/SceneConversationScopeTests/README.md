# Scene conversation scope contract

Compiles the production `Channels/Scene/SceneShoutConversationScope.cs` with small Bannerlord type stubs. It verifies stable audience merge/order, Mission/epoch and Agent identity revalidation, liveness, and fail-closed capture.

`run_mutations.py` creates ignored copies and proves the epoch, Agent-reference, and origin-merge guards are observed by named behavioral cases. This is deterministic offline evidence, not a live Mission, LOS, audio, performance, or save test.
