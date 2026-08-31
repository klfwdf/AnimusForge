# ProductionConfiguredHostReplayTests

This replay loads the project-local Bannerlord 1.4 staged `AnimusForge.dll` and
instantiates the production `LegacyConfiguredChatGateway`,
`LegacyChannelInteractionFacade`, and `DetachedInteractionHost` through reflection.
A loopback TCP provider returns deterministic main/postprocess replies.

The replay covers NativeConversation, SceneShout, and Courier channel identities;
main/postprocess HTTP exchanges; credential boundary; commit/history roles;
provider failure fallback; and caller cancellation. It is an equivalent controlled
host fixture, not a real Bannerlord campaign or mission host. It never deploys to the
game directory.

The commit-failure matrix additionally checks four faults across all three
channels (12 cases): memory append throws after an action, `afterCommit` throws,
and the dispatcher throws or returns null after invoking the callback. These
must return `NonRetryableFailure`, retain an available action receipt, and never
invoke legacy fallback. Action/memory ports remain controlled fixtures, not live
Hero inventory or AFEF storage. The wider 48-case callback/cancellation matrix
runs in `InteractionPipelineContractTests` against linked production sources.

Run:

```powershell
dotnet run --project tools/ProductionConfiguredHostReplayTests/ProductionConfiguredHostReplayTests.csproj
```
