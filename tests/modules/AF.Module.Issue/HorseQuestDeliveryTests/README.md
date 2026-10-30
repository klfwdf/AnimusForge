# Horse quest delivery replay

Build Replay.csproj with GameReferences pointing to the matching 1.3/1.4 reference directory,
HarmonyCorePath pointing to the installed managed 0Harmony.dll, and isolated OutputPath /
BaseIntermediateOutputPath. Run a fresh process per API and content package:

Replay.exe "references|runtime-dependencies|Harmony|other-mod-dependencies" "candidate/AnimusForge.dll" "resource-directory"

The resource directory must contain the selected CN or EN IssuePromptTexts.json.
The actual candidate owner, runtime rules and RAW tag normalization are exercised.
The actual native LordNeedsHorses OnCompleteWithSuccess body executes with fixtures for
inventory movement, gold, renown, journal creation and outer quest finalization; this is
not live save/campaign/UI/provider acceptance. Production Harmony targets are applied.
Cases cover split delivery, wrong item/recipient, inventory plus handed-over amounts,
confirmed payment offset, unconfirmed native UI payment, repeated completion, exception
field restoration, and local person binding despite later global text changes.

Existing transfers from before this fix have no task journal observations and are not
inferred from arbitrary historic conversation text. The fix adds two bounded numeric
observations to existing native quest JournalLog records, without new SyncData keys.
