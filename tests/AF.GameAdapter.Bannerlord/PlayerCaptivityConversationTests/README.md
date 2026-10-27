# Player captivity conversation

Run from the repository root:

```text
python -B tests/AF.GameAdapter.Bannerlord/PlayerCaptivityConversationTests/run.py
```

The runner links the production session adapter, independent release owner,
CN/EN readers and memory projection. It extracts the existing release-tag
normalizer verbatim and verifies both production RAW/FINAL call sites.
Native game objects/actions, resource paths, JSON parsing and history storage are
fixtures, not a game simulation or live LLM. Outputs use a fresh repository-local
directory; `--run-root` selects it and `--api15` enables the 1.5 branch.

The lifecycle fixture preserves the native `IsCaptive` getter's Campaign and
PlayerCaptivity owner dereferences. It covers the main menu, a partially
constructed campaign, teardown with pending release, and a subsequent campaign.
`NativeStartupReplay.cs.txt` is a separate .NET Framework 4.7.2 harness for actual
AF/TaleWorlds DLLs. It invokes the native getter and real AF Tick, using reflection
only to supply partial Campaign instances; no getters are replaced. A broken
candidate can be checked with `expect-crash`. This proves the startup guard,
not live game loading or save-file acceptance.

The automatic-opening extension links the real opening router and extracts the
unchanged-signature opening message/history composers verbatim. Other proactive
sources and scope-prefix stripping are fixture leaves. CN/EN checks cover the
new capture event, deferred stable UI, one opening per captivity, owner/governor
choice, no catch-up on load, ordinary exit/manual re-entry, stale pending/consumed
openings and canonical fact projection. Static checks verify event-to-overlay-to-
admission wiring, player-input suppression and the topic-free opening resource;
they are not a full provider request or end-to-end UI simulation.

Coverage: mobile lord; local owning-clan leader/governor and deduplication;
foreign/remote/inactive/busy targets; ordinary exit; time pause; player-controlled
release on leaving (no ten-second auto-close or two-minute reading expiry);
duplicate tags; exact no-amount tag; unselected and non-native
channels; transfer/ownership/save/campaign/manager changes; native exceptions;
post-close reentry; no stale history; confirmed AFEF projection in both editions;
UI/resource registration and canonical saved fact identity.

`python -B tests/AF.GameAdapter.Bannerlord/PlayerCaptivityConversationTests/run_trade_options.py`
also checks the verbatim production resource reader: a partyless captive can give
personal gold without accessing the captor's inventory. Owned inventory, show
allowances and troop/fief modes retain their existing behavior (game ports are fixtures).

Still requires live acceptance on both game APIs: both menu entries and governor
selection, UI language, normal return/time restoration, LLM conditional quote vs
confirmed payment/release, roster/main-party restoration without duplicate player,
save/load and transfer while a provider request is in flight. Provider semantics
are not proven by feeding a fixture tag through the normalizer.

The farewell regression also links the production `CaptivityMapVisibilityCompat`:
only the still-current mobile captor outside a settlement is revealed. Ordinary
farewell, former/replaced/inactive captors, dungeon menus and native release are
covered without changing rosters. After the runner prepares `PostprocessProbe.cs`,
run this project with `-p:DefineConstants=BANNERLORD_1_5_OR_GREATER` and the same
`CaptivityContractSource`/edition environment to exercise the real 1.5 cache
removal branch against field-shaped game fixtures (217 checks per edition).
`CaptivityAdapterSource` optionally selects a preserved pre-fix source for a
negative control; the unchanged assertions must fail on hidden-captor farewell.
Siege, economy, Social and experience fact projection leaves are fixtures here;
their behavior is outside this captivity test. Actual 1.5 cache field bindings
are tested separately by `ConversationHistoryScopeTests` against the built DLL.
