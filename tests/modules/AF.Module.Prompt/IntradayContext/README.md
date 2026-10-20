# Intraday context replay

Run `dotnet run --project tests/modules/AF.Module.Prompt/IntradayContext/IntradayContextTests.csproj -c Release`.

This runner links the real window selector, derived summary owner, budget calculator and actor cache. It simulates 300 user/assistant turns within one game day, keeps all 600 source messages, supplies a deterministic summary callback (no HTTP), and checks bounded final requests and early exact-quote recall. It also covers source chronology, separate facts, cancellation, provider failure cooldown, output allowance, required-only over-target continuation, and physical removal of evicted history from the cache.

Actual product DLL/consumer checks are in `../ConversationCache/SettingsProbe.cs`, invoked through `settings_probe.py`. They check all five new settings through native MCM discovery, terminal setters/ranges and actual MCM JSON round trips, then run the real shared history composer on 600 messages. Final Gemini merge and native Anthropic payloads go through the production budget guard and protocol converter.

Defaults: latest 12 user-led speech groups, at most 20000 recent original characters, up to three relevant earlier groups / 6000 characters, 100000 input-character management target, estimated context target 128000 tokens including configured output allowance. The token calculation is an estimate, not a provider tokenizer; character counts are measured after protocol conversion. Targets automatically reduce optional context; required-only inputs continue above them without a local budget error.

Intraday summaries are derived, not authoritative game facts. Closed source segments are at most 8000 characters, normally sealed at 6000. At most one new auxiliary summary runs per request, and synchronous main-thread compatibility captures never run a summary/embedding callback. Accepted summaries must fit 1200 characters. Up to four selected summaries are injected. Failures retain original archives, use bounded quote recall and cool down summary attempts for one minute. Keyword candidates are reranked with existing local ONNX embeddings when available; no claim of guaranteed recall.

The derived cache holds at most 32 owner/day/provider/edition scopes and 128 bounded segments per scope. Scope keys include generation, owner, game day, provider and fixed summary prompt. Source hashes identify complete segments; edits invalidate changed segments. Existing save/history data is unchanged; no additional save keys or disk cache are introduced. Summaries can be regenerated from saved daily drafts after loading. Normal daily compression remains the long-term memory owner.

All work is request-time detached text. There is no new Tick IO, scanning or polling. A new summary or history-window eviction rebuilds the actor transcript. Cache disable and context disable are separate settings; cache fallback still runs final primary-request budget diagnostics. Actual provider/transport failures remain visible; local management targets do not interrupt a conversation.

Not covered by offline replay: factual fidelity of a real model's summaries, provider tokenizer/cached-token billing, native UI rendering, live scene acceptance, representative old saves. Do not interpret these tests as deployment or live-game acceptance.
