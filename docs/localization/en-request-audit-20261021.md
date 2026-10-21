# EN requests: diplomacy, policy and fixed fact markers (2026-10-21)

Scope: local source and installed EN resources, the frozen local log, and detached actual-DLL request assembly. This is not a deployment or a claim that every provider response is English. Private request bodies and player logs remain in local artifacts and are not part of this document.

## Findings

- The frozen log contains 56 diplomacy requests / 133 messages, all parsed. Its first declaration-generation request has zero Han characters across all three messages; the interface's raw response contains 111. The later declaration shown in the screenshot quotes previously generated Chinese titles and bodies. A fixed-template leak does not explain that first response.
- The installed NPC policy draft and policy evaluator editable defaults are English. The policy lifecycle log confirms generation of the policy shown in the screenshot, but does not capture its complete request body. Therefore that particular policy's entire dynamic context cannot be certified as English.
- 80 mapped English JSON assets were compared against source. 79 match byte for byte. The remaining player custom-rule file contains two legacy AFEF markers; the existing final-wire projection already converts these. All 82 checked installed fixed release resources match the previous EN release manifest.
- Actual defects found and fixed: known Chinese game-date metadata in declaration reading windows and canonical history; the inner `[AFEF内战事实]` annotation; a Chinese full stop after the NPC draft technical JSON example.
- Fixed English resources still contain 63 East Asian punctuation characters in four catalogs. These are not Han-language sentences. Some MODE delimiters also deliberately retain their existing full-width protocol grammar. This audit does not claim all Unicode punctuation or all dynamic request content is ASCII.

## Assembly and transport boundaries

| Request family | Content / consumer boundary | Sending boundary checked |
| --- | --- | --- |
| Scene, native conversation, courier; routing and action postprocessing | AIConfigHandler, shared message composers, channel history readers and selected rule resources | ShoutNetwork / LlmApiCompat |
| NPC policy draft, player policy evaluation, effect planning and policy history | NpcRulerPolicyBehavior.Generation, policy prompt readers, PolicyHistoryRetrievalService | PolicyLlmClient / LlmApiCompat |
| Diplomacy generation, response, oral drafting, analysis, compression and privacy work | WorldDiplomacyPromptContractRules, WorldDiplomacyPromptComposer, WorldDiplomacyLlmMessageApplication, WorldDiplomacyRequestHistoryApplication | WorldDiplomacyLlmClient / LlmApiCompat |
| Memory summarization, NPC persona / active contact, weekly reports, bulletin and opening / strategic context | Package-selected resources and their owning composers; retained original history remains dynamic input | AIConfigHandler / LegacyConfiguredChatGateway or policy-owned calls, according to the caller |
| Image director and generation | VisualDirectorEngine / UniversalOpenAiImageClient | Their configured image/chat protocol; not a conversation action-tag parser |
| Speech translation and synthesis | ConversationContextSettingsCapture / TTS gateways | OpenAI-compatible, Volc and GPT-SoVITS boundaries; speech/audio data is not a fixed creative system prompt |
| Model listing, embeddings and onboarding probe | Model catalog / retrieval / probe owners | Listed separately from creative-prompt language checks |

The local transport inventory covers 20 source files. Lexical scanning covers 1,990 C# files; its 79 Han-containing literal candidates are in an excluded tooling verifier, not runtime product sources. Neither count certifies generated history, user overrides or live provider behavior.

## AFEF contract

Final wire text uses `[AFEF NPC_ACTION]` and `[AFEF PLAYER_ACTION]`. The nested civil-war annotation becomes `[Civil war fact]`. Both policy and diplomacy use `LlmApiCompat.PrepareChatRequestJson`; its shared projection executes before OpenAI / Anthropic serialization, including text blocks. Saved records, actor identity, action protocol and opaque prose are not rewritten. Existing readers continue accepting legacy and ASCII markers.

No output-language system rule, reply rejection or automatic translation was added. An English fixed prompt can receive a Chinese provider response. Existing Chinese generated policies, declarations, player text and unknown historical prose remain verbatim; zero Han in every real request is consequently not a claim this implementation can make.

## Validation

- Unified Debug build: both Bannerlord API implementations, Bootstrap, both Coup seams and managed registration passed.
- Four actual-DLL combinations (1.3 / 1.4 × CN / EN): 33 request-audit assertions each passed. These exercise the real NPC draft composer, common diplomacy contract, three-message declaration assembly and final JSON serialization for both provider protocols, using detached English facts plus explicit legacy-marker fixtures.
- Each combination also passed the existing 74 AFEF reader checks, 170 message/history checks and 4,553 terminal-settings checks.
- Baseline evidence preserves the original failing draft-punctuation and inner-marker checks. The enhanced final-wire fixtures are synthetic; no new paid model request was sent.
- NOT RUN: installed-game acceptance, actual provider response-language acceptance, screenshot policy's missing complete payload, formal Stage / ZIP / deployment / push, and a full semantic review of every possible dynamic history combination.

Local evidence: `artifacts/en-request-audit-20261021/`, including frozen logs, asset hashes, `final13/result.json`, `final14/result.json`, and synthetic `request-prompt-audit/*-wire.json` under each package fixture. These private artifacts are not release content.
