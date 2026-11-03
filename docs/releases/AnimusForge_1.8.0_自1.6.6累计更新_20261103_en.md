# AnimusForge 1.8.0 Cumulative Update (from 1.6.6)

2026-11-03

From 1.6.6 to 1.8.0, diplomacy gains connected events and negotiations, and field camps become places you can visit. Soldiers report problems, noble prisoners appear in camp, and you choose where to deploy an execution site. Character profiles, ambient conversations, historical worldbooks, voice, and knowledge retrieval also receive updates.

## 1. Diplomacy rebuilt around connected events

- Includes the 1.7.0 diplomacy overhaul: rulers’ ambitions, personal rivalries, family ties, historical disputes, and current campaign conditions can shape diplomatic stories.
- Illustrated opening and closing announcements present an event’s background, national demands, responses, actions, and outcome.
- An event may involve several countries with different demands, limits, and conditions. Negotiations can produce agreements, concessions, threats, war, or unresolved differences.
- Wars and active agreements persist after events close. Eligible events can also affect ruler relations and town loyalty, security, prosperity, and food, with limits on town effects and duration.

## 2. Declarations, agreements, and continued negotiations

- The kingdom diplomacy page gains Write Diplomatic Declaration. With AI diplomacy enabled and ruler authority, you can write a declaration and decide your country’s position.
- Couriers, free conversation, and scene shouting use the actual exchange, participants, and confirmed terms to distinguish continued negotiations from new matters and reduce duplicate events.
- Explicitly accepted verbal agreements follow the formal process with their agreed terms, reducing renegotiation, lost conditions, and duplicate execution across channels and documents.
- Further fixes cover diplomacy entry handling for player vassals, ultimatum response matching, cross-country routing, and persuasion commitment notifications.
- Confirmed bilateral ruler agreements apply their actions before generating outcome announcements. Unpublished responses, load recovery, and retries receive fixes to reduce duplicate publications and mismatches with campaign state.

## 3. Diplomatic pacing, news, and memory

- New settings control event intervals, participant counts, and war tendency. Autonomous events default to a three-day interval and NPC contact to five days; ongoing negotiations and accepted commitments are handled separately.
- Low, standard, and high settings influence participant counts. War tendency affects threats, agreements, and deadlock; it is not a direct war probability and does not decide for the player.
- Announcements are public and declarations spread with distance. NPC awareness depends on delivery, publicity, and personal experience; private conversations do not become universal knowledge.
- Starting new campaigns at peace is enabled by default and affects new campaigns only. Disabling AI diplomacy pauses negotiations and restores native decisions; re-enabling it first checks the current situation.
- Research better reuses character and worldbook material, reduces repeated documents, and preserves question–answer pairs where possible. Notification timing, window input, and announcement pacing are improved.

## 4. Field camps and rest

- Press Z on the campaign map by default to camp, then again for rest, inspection, or breaking camp. You can camp with your party or organize an army camp as its leader.
- Setting up and breaking camp each take one hour by default and pause movement. Campaign-map tents can be shown or disabled independently.
- Defaults are double wounded recovery and one morale restored per hour up to 70 while resting. Setup and teardown times, recovery multipliers, and morale values are configurable.
- Settings include hotkeys, inspection time cost, visible soldier counts, and tent limits so you can tune them for your machine.

## 5. Camp facilities, inhabitants, and daily life

- Camp inspection retains an outdoor map while sharing town-style entry, interaction, and character lifecycle handling. Tents, fires, supplies, sentries, and prisoner areas adapt to terrain and party size.
- Your soldiers, companions, accompanying lords, and noble prisoners can appear. Regular troops are sampled by type and count; all noble prisoners held by your party are included independently of that sample limit.
- Soldiers can patrol, stand watch, rest, eat, or perform chores. Conversation commands assemble or dismiss them, forming up by troop type and returning them to camp activities afterwards.
- Regular troops share conversation memory by troop type within your party, while companion events belong to the individual. Camp duels stay in the current scene and restore character control afterwards.
- Fixes address automatic exits, missing tents or inhabitants, and prematurely invalidated character caches. Placement continues on imperfect terrain, and individual prop failures no longer directly exit the scene.
- Placement and activity fixes cover grounded tents, joined fences, floating seated characters, routes around props, and returns after activities.

## 6. Soldiers report camp problems in person

- Reports reflect actual conditions: unpaid wages, food shortages, many wounded, recent defeats, lack of rest, low morale, cultural friction, and companion disputes. Healthy camps may produce requests for action or rewards.
- The relevant soldier or companion approaches you. Depending on the situation, you can reassure them, agree, refuse, or enforce discipline, potentially spending denars, changing morale, or dismissing troublemakers.
- Outcomes enter the relevant character or troop-type memory so later conversations can refer to your choices.

## 7. Manual execution sites and successive executions

- With eligible noble prisoners and the relevant feature enabled, press B during inspection to deploy an execution site. Move to choose the location and confirm it; the site is no longer generated on entry.
- Deployment uses the site’s physical footprint. Fixes address empty sites still waiting for safe ground, unresponsive confirmation, and circular waits between movement and construction.
- The executioner and ceremony soldiers come from existing camp inhabitants. The executioner and prisoner arrive directly at their marks while spectators walk over without blocking construction.
- Select the next prisoner at the same site and reuse its personnel. Fixes cover prisoner conversations, F interaction prompts, wandering, stuck characters, and return behavior afterwards.
- Formal executions and directly killing prisoners who have not entered the ceremony are settled separately. Direct camp killings use scene-kill handling rather than incorrectly applying formal execution relation penalties.

## 8. Optional ambient scene conversations

- In peaceful taverns, lord’s halls, towns, and villages, nearby characters can chat or approach you about relationships, war, local affairs, and the player.
- Unnamed characters can participate. Improvements cover reply targets, mutual attention, audience gaze, conversations across rooms, and seated responses at suitable distances.
- Off by default. Settings control idle time, intervals, pair cooldowns, per-scene limits, length, hearing range, and topics; voice playback can be disabled independently.
- With voice disabled, bubbles and messages remain. Generating each line still uses model requests, so tune frequency for performance and cost.

## 9. Character profiles and evolving experience

- Encyclopedia profile controls receive layout and interaction fixes. Regenerate with instructions lets you specify a character’s background direction, personality requirements, and details to retain.
- Instructions apply to the current request only. Regeneration retains the selected voice, failures preserve the existing profile, and edits made while generation is pending take priority.
- Expanded profiles control traits, motivations, values, fears, and speech style. Disabling them hides displays and editors and stops injecting saved expanded fields into model context, including cached scene context.
- Switching off does not mass-delete saved profiles. Successful regeneration while disabled clears that character’s old expanded fields while preserving the description, voice, and evolution state.
- Event-driven updates are controlled separately and use events, campaign facts, and worldbook material. With expanded generation disabled, evolution updates no longer restore expanded fields.

## 10. Three new historical worldbooks

- Adds the Burning Empires, Eastern Europe 1259, and AD 1259 chronicles with national backgrounds, character profiles, knowledge entries, opening summaries, and historical events.
- Burning Empires distinguishes political legitimacy, internal tensions, economies, military strength, and neighboring relations to give countries different backgrounds and motives.
- Eastern Europe 1259 expands opening states for 20 factions and material on the Rus’ lands, Mongol pressure, the Baltic, institutions, military supply, religion, households, and everyday costs.
- AD 1259 improves opening states and shared context with corrections to laws, roles, and dates. Historical revisions reduce future events being presented as knowledge already available at the start.
- Punic War material is updated, English counterparts are completed for new worldbooks, and superseded placeholders, case-only duplicates, and ambiguous numbered names are cleaned up.

## 11. bekko retrieval and first-conversation indexing

- Retrieval uses bekko multilingual embeddings, replacing the old embedding model and reranker and removing their CPU/GPU selection settings.
- Name retrieval and compound keyword cues are adjusted to reduce irrelevant matches from similar names, alongside semantic retrieval conditions for world news and policy history.
- Indexing uses batches and thread limits with model-specific disk vector caches. Compatible caches are reused, while new models build their own caches.
- Fixes an entity-free first conversation incorrectly marking an unpublished snapshot as a completed empty index and leaving later queries without candidates. The first normal conversation now prepares rules and builds indexes before selection.
- Entity-free greetings can trigger initial indexing, but no knowledge is invented without imported material. Retrieval switches, rule eligibility, and name thresholds remain; indexing does not guarantee matches for every question.

## 12. Voice, quests, and scene outcomes

- OpenAI-compatible TTS supports per-character voices and emotional variants, with model-list voice discovery for some services. You can disable waiting for postprocessed emotion to start neutral speech earlier while action postprocessing continues.
- Fixes spoken lines incorrectly removed near bold markers, continues filtering stage directions and emoji, and makes campaign-map meeting audio follow the main voice volume.
- Horse-delivery quests use the horses actually delivered and strengthen participant binding. Private debt contracts no longer enter unrelated NPCs’ public context.
- Improves gender cues for anonymous players, removes duplicate noble-prisoner context, and excludes imprisoned nobles from inappropriate scene provocation targets.
- Scene crime settlement no longer retains overflow from the same incident after reaching the native cap. Payment, punishment, or pardon clears matching pending crime to reduce old balances restoring settled penalties.

## 13. Factions, interface, language, and compatibility

- Fixes inapplicable cooldowns blocking faction creation, unreadable dates, and families failing to leave peacefully with their fiefs after autonomy is granted. Existing relation costs for founding factions remain.
- Adjusts noble gathering schedules, domestic page headings, and tables; removes obsolete terminal footer entry controls and hints and improves diplomatic window input handling.
- Fixed prompts, model context, and mod-generated text follow the game language in Chinese or English. CN/EN editions select editable prompt defaults and worldbooks; both retain bilingual UI.
- Language changes do not rewrite old history, player input, or proper names or add a forced response-language rule. Known legacy records support recognition in both languages.
- Adjusts reasoning parameters for some YJ Gemini services and maps retired minimal effort to low. Fixes address periodic stalls involving SETS and control restoration after some duels end or are cancelled.

## 14. Smaller packages and upgrade notes

- CN and EN extreme-compression 7Z packages are about 171.47 MiB and 171.03 MiB. Both include the quantized bekko model, tokenizer, and configuration without a separate download for those three files.
- The model shrinks from about 190.07 MiB to 119.18 MiB, around 37.3% smaller. Quantization targets size reduction, and some retrieval rankings may differ slightly from the original.
- Unused artwork is removed, diplomatic illustrations are resized, and PDB symbols and the Lite Calradia Chronicles edition are omitted. The full edition and required runtime dependencies remain.
- Install either CN or EN. Each remains one AnimusForge module containing Bannerlord 1.3 and 1.4 implementations selected by the bootstrap. Local voice runtimes are still installed separately when needed.
- Defaults are Z for camping and B for execution-site placement, with ambient conversations off. Initial indexing may add cold-start delay before cache reuse; tune population, tents, and chat frequency for performance and model costs.
