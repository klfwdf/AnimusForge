## Scene fixed-prefix repair (2026-10-27)

Scene requests now place one shared public system first, then the current NPC's stable persona, selected chronological history/facts, and latest-only task/action/runtime context. Scene leases are keyed by actor; switching speakers does not overwrite another actor's retained prefix. Native/courier entry signatures and history owners remain unchanged. Profile/rule edits refresh the affected content; old dynamic snapshots are not replayed.

`scene_prefix_probe.py --api 1.3|1.4 --edition CN|EN --candidate <DLL> --run-root <fresh artifacts directory>` runs the compiled production composers, cache store and final provider serialization. It covers cross-NPC identical system, stable persona ordering, current-state replacement, literal placeholders, edited history/profile/rules and Gemini/DeepSeek/Claude wire shapes; it also retains the existing four-channel latest-state regression. This probe projects current resources without hash gates, makes no network requests and does not deploy. Actual provider cache tokens and billing require a separate live check.

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

Covered: authoritative current messages with original roles and order, latest state
replacement, one copy of unchanged background, vanished/empty state, AFEF and quoted
old values, failed/stale/superseded requests, TTL/provider/generation scopes,
compression/edit without resurrecting removed history, bounded retained storage,
lossless oversized fallback and final Gemini/Anthropic wire behavior.

The cache no longer replays previously optimized requests or appends dynamic-state
invalidation manifests. The selected history owner supplies accepted dialogue and
compressed memory for every request. A reply absent from that selected context is
not resurrected by the transport cache. Character/token targets remain diagnostics;
the separate 150000-character retained-storage cap never removes selected history.

Ordering is system, selected chronological speech/facts, current recall/runtime/rules,
then current input. Native/scene use the same scene owner; both courier directions
use the courier owner. Stable history precedes variable state. State replacement can
break the prefix at its changed position; cache fingerprints are diagnostics, not
provider usage or a guarantee of billed cache hits. Changed system text also replaces
its old version; unchanged system text is reused byte for byte.

The actual-DLL probe runs production channel owners, store and final wire serialization
without starting a game, HTTP request or save. Run on each API and content edition:

```powershell
python -B tests/modules/AF.Module.Prompt/ConversationCache/latest_state_probe.py --api 1.3 --edition CN --candidate bin/Debug/single_module_artifacts/versions/1.3/AnimusForge.dll --run-root artifacts/latest-state-13-cn
```

Use a new run directory for each invocation and repeat for 1.3/1.4 and CN/EN.
The unrelated legacy source-projection runners currently fail at a PolicySystem
review hash and missing fixture dependencies; their failures remain recorded.
The focused actual-DLL probe tests this task without relaxing those old gates.

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
