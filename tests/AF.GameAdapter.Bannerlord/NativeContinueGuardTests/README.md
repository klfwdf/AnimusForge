# Native AI continue guard

Compile the production `ContinueConversationSafePatch`, overlay guard partial and
`SaveRuntimeGuard` against each verified native reference line. Pass `GuardReferenceDir`,
`GuardSharedReferenceDir` (for 1.3's missing SaveSystem reference if required), and
`GuardHarmonyDir` explicitly, with separate output/intermediate paths per API.

The executable installs real Harmony prefixes on the actual `MapConversationVM.ExecuteContinue`,
`MissionConversationVM.ExecuteContinue` and `ConversationManager.ContinueConversation` methods.
It invokes the real map callback in ordinary mode and checks that AI mode skips it, skips
the mission command's option-state side effect and never enters the manager. Native keyboard
paths converge on the same mission command in both supported source references.

The Campaign/manager/VM instances are uninitialized fixtures, with only the relevant native
fields set. The overlay shell, backend busy flag and stale-conversation recovery are substitutes;
the mode guard, save generation and Harmony patch bodies are production. Named lifecycle cases
exercise the invariant with busy true/false, not actual network/audio/temporary-screen rendering.
This does not prove live-game focus, physical input, GPU rendering or player save behavior.

Companion UI tests are the existing DialogueUI `continue-hit-tests` runner for each API and
`lifecycle-tests` executable (requires repository root as its first argument). These verify
the actual shield binding, hit method, default/ordinary mode and wrapper notifications.
