# Player captivity conversation

Run from the repository root:

```text
python -B tests/AF.GameAdapter.Bannerlord/PlayerCaptivityConversationTests/run.py
```

The runner links the production session adapter, independent release owner,
CN/EN readers and memory projection. It extracts the existing release-tag
normalizer verbatim and verifies both production RAW/FINAL call sites.
Native game objects/actions, resource paths, JSON parsing and history storage are
fixtures, not a game simulation or live LLM. Outputs stay under
`artifacts/player-captivity-opening-20261013`.

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
