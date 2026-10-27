# Conversation history scope and captor cache bindings

Build `Replay.csproj` with `HarmonyCorePath` set to the installed Harmony DLL;
keep output/intermediate files in the task's artifacts directory. Run the .NET
Framework 4.7.2 executable in a fresh process for each actual API dependency line:

```text
Replay.exe "game-reference-directory|AF-dependencies|Harmony|MCM|ButterLib|UIExtenderEx" "candidate/AnimusForge.dll" 1.5
```

Repeat with the verified 1.3 and 1.4 references and matching implementation DLLs.
This exercises the actual player-facing history query (filtered) versus the
model-facing query (unfiltered), canonical CN/EN fact recognition,
utterance/interaction/unknown preservation, visible-tail limit, original edit
ordinals and unchanged NPC memory lists. For 1.5 it instantiates detached native
Campaign/cache objects and tests the actual previous/current visibility fields,
exact-party removal and idempotency; older APIs must not bind those fields.

Logs are disabled through Harmony. Game startup, native scene/UI, live save
files, provider calls and installed module/config writes are not performed.
Use `PlayerCaptivityConversationTests` for session/return/release lifecycle cases.
