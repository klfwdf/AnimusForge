# Conversation prompt cache verification

`ConversationCacheTests.csproj` links the production cache store, request adapter,
message reader, CN/EN catalog readers and final protocol serializer. Game identity,
settings and logging are fixtures. No game, credentials or HTTP are required.

```powershell
dotnet run --project tests/modules/AF.Module.Prompt/ConversationCache/ConversationCacheTests.csproj -c Release
$env:AF_TEST_EDITION = 'EN'
dotnet run --project tests/modules/AF.Module.Prompt/ConversationCache/ConversationCacheTests.csproj -c Release
Remove-Item Env:AF_TEST_EDITION
dotnet run --project tests/modules/AF.Module.Prompt/ConversationCache/ConversationCacheTests.csproj -c Release -p:ProtocolRegression=true
```

The last command runs the existing 13 protocol regression groups against the real
production sources and catalogs, independently of the old historical source-hash
projection runner. It does not edit or bypass that runner's validation.

Covered: complete message sections and original roles/order, AFEF/memory/relay
retention, removal manifests, accepted/failed/cancelled/superseded requests,
scope/provider/generation isolation, TTL, bounded retained storage, whole-snapshot
budget reset and lossless fallback, native/auxiliary isolation, private actor
isolation with a common public prefix, and final Gemini/Anthropic wire shape.

Final character/token targets govern diagnostics only. Above-target
requests preserve all history already selected by the shared recent-window and intraday
summary pipeline, including a just-agreed prisoner purchase followed by payment with a
50,000-character system prompt and a 64,000-token output allowance. These targets never
bypass prefix caching: consecutive requests retain the same single system prefix and
accepted reply. The separate built-in 150000-character retained-storage cap still rebuilds
or falls back to the complete selected request without removing dialogue groups.

The runtime deliberately does not put different NPCs' private context into a
shared transcript. Scene visibility and authoritative history still select the
public dialogue each NPC can receive. Characters share the stable public prefix;
each character's accepted transcript appends independently.

For actual compiled MCM settings, terminal delegates and disk/JSON round trips,
use a new workspace output directory for each invocation:

```powershell
python -B tests/modules/AF.Module.Prompt/ConversationCache/settings_probe.py --api 1.3 --candidate bin/Debug/single_module_artifacts/versions/1.3/AnimusForge.dll --run-root artifacts/cache-settings-13
python -B tests/modules/AF.Module.Prompt/ConversationCache/settings_probe.py --api 1.4 --candidate bin/Debug/single_module_artifacts/versions/1.4/AnimusForge.dll --run-root artifacts/cache-settings-14
```

The probe checks build-marker hashes, uses the production content selectors and
the existing managed MCM-provider fixture, and isolates all writes in its supplied
workspace directory. It does not touch real player settings. It intentionally
runs only this feature's assertions; the old general terminal suite has a separate
persona-constructor parameter mismatch. Logs retain that attempted suite failure.

These checks do not establish real provider cache hits, prices, model compliance,
native MCM rendering, in-game conversation acceptance or old-save compatibility.


The cache settings group was retired on 2026-10-21. Conversation caching is a built-in mechanism with the existing 150000-character storage cap and 10-minute lifetime. Native MCM and terminal metadata no longer expose the three cache fields; old JSON/preset fields cannot change these defaults. Context-management settings remain independent. The settings probe checks retirement with the actual MCM formatter as well as the remaining context options.
