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

Run:

```powershell
dotnet run --project tools/ProductionConfiguredHostReplayTests/ProductionConfiguredHostReplayTests.csproj
```
