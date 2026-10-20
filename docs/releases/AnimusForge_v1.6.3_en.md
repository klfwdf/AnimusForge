# AnimusForge v1.6.3 Cumulative Update

This update covers changes since the **official v1.6.2 release on October 17**, including speech services, conversation context and caching, diplomacy, scene and aftermath fixes, and bilingual content improvements.

## 1. Speech services and custom voices

- Alongside Volcengine, added OpenAI-compatible speech, automatically managed AF-GPTSOVITS-L / U, and a manual GPT-SoVITS API option. MCM and the terminal show settings for the selected service while retaining the other services' configuration.
- OpenAI-compatible speech has separate endpoint, key, model, voice and speed settings. Supported WAV services can stream audio into playback and lip-sync; disabling streaming waits for the full utterance. Streaming support and first-audio latency depend on the service.
- All four services share a speech output language setting: Chinese, Cantonese, English or Japanese; blank/auto follows the reply. A separate spoken translation is prepared when needed. **Subtitles and saved conversation remain in their original language.** The selected voice and service must support the requested language.
- Added local runtime detection and service management for installed L/U packages, preferring L when both are present. The manual option can connect to a service you run yourself.
- Added local voice selection in character encyclopedia pages. Named characters keep their assigned random voice; Male/Female folders provide default pools, while Custom voices require manual assignment.
- Local speech follows the primary conversation partner; other speakers retain text. Local synthesis is disabled during actual battles. Failed synthesis leaves the text available.
- Added bilingual in-game speech help and an L installation, download, hardware and custom voice-pack guide. Reference recordings, accurate transcripts and a manifest can define custom voices without training inside AF.

**Local speech packages are separate downloads and are not bundled in the AF main ZIP.** This release links the L package and guide; U needs its own download, guide and hardware information. [Read the L guide](../tts/AF-GPTSOVITS-L.en.md).

## 2. Long conversation context

- Added automatic context management across scene shout, free conversation and courier: recent original turns, summaries of earlier intraday segments, and relevant earlier quotations.
- Defaults retain the latest **12 dialogue groups** within a **20,000-character** recent-history budget. Each group includes a player turn and its replies; the current group is not silently cut mid-sentence to fit.
- Added configurable request-character and estimated-context targets in MCM and the terminal. Older optional history is reduced first, while current input, required rules and confirmed facts remain. Required content can exceed these management targets and still proceed instead of being stopped by a local budget error.
- This changes request context, **not the saved original conversation**. Recall continues to respect the character's existing visibility boundaries.

## 3. Prompt caching and proactive contact performance

- Free conversation and scene shout now organize requests around a stable prefix and accepted transcript additions, reducing repeated rearrangement of the same content.
- Expired or changed sessions rebuild a complete context. Canceled, stale or unaccepted replies do not become accepted cached turns. Character identity, rules, facts and private context retain their boundaries.
- Added cache enable, capacity and lifetime controls to MCM and the terminal. **Actual cache hits and pricing depend on your provider**; enabling this feature does not guarantee a discount.
- Reused validated prompt-directory information to reduce repeated checks.
- Map NPC proactive contact now queries nearby parties first, with a fixed range of **twice the player's sight radius**. Existing contact eligibility rules still apply.

## 4. Diplomacy and alliances

- Fixed cases where a kingdom name resolved successfully but later scope validation still rejected it. Display names, IDs and compatible aliases are canonicalized before eligibility and duplicate-target checks.
- Improved compatible reading of model result fields, objects and intent aliases while preserving explicit action meaning.
- Improved player declaration response scheduling so the absence of a response-request flag alone does not drop declarations that should enter further processing. Eligibility and pause conditions still apply; every kingdom is not guaranteed to reply.
- Fixed event-list indexing failures when events close or are removed during processing. Background requests and retries use captured settings consistently.
- Improved bilingual failure, truncation, re-analysis and legacy receipt text, distinguishing a published declaration from an executed diplomatic action.
- Permanent alliances now display **Indefinite** instead of a misleading countdown of roughly 84 days. Existing authorized diplomacy actions can still end an alliance under their normal conditions.
- Completed more fixed diplomacy dates, prompts, treaty explanations and vassal war notifications in both languages.

## 5. Scenes, surrender and aftermath

- Fixed accepted NPC surrender being canceled during native farewell. The captured target party is retained until dialogue teardown; a genuinely changed encounter still cancels the pending action.
- Visible force context now includes the assembled army rather than only its leader's own party.
- Fixed early scene-action catalog initialization disabling actions for an entire launch, and improved the link between action selection and execution.
- Corrected postprocess handling of commitment actors, mood and duel targets while preserving speaker identity and execution permissions.
- Tightened intimacy decisions to require a current explicit affirmative answer and matching participants. Refusal, negation, speculation and past events are not current consent.
- Repaired civilian escape behavior in relevant peaceful-scene conflicts without overriding the owners of duel, conflict or aftermath behavior.
- Consolidated aftermath plunder relationship changes by notable, preserving the intended relationship change while reducing repeated relation events in one batch.
- Guarded castle aftermath exit against prisoner mutations during mission teardown, and repaired the interaction between damage-patch registration and court mechanics.
- Corrected speech-bubble screen-coordinate mapping and package-specific fixed civilian role labels.

## 6. Policy re-review and character facts

- Policy re-review replacement now verifies publication, scope and lineage before stopping the explicitly superseded predecessor, avoiding unrelated policy replacement.
- Persona generation includes current close-relative life states, with current facts taking precedence over outdated background.
- Improved projections of known social, economic and execution facts. Ambiguous character or clan delimiters preserve the original record rather than splitting an identity incorrectly.

## 7. Bilingual content and fact markers

- Completed more English fixed headings, default player prompts and mechanism instructions across conversation and memory requests.
- Fixed declaration, policy and bulletin headings and dates, initial summary titles, and fixed casualty, party-type and field-location fragments in recent events and battle reports.
- Both packs use English **AFEF fact markers**, while recognizing legacy Chinese and full-width variants. Fixed missed facts, double wrapping and facts mistaken for speech in recovery, recall and scene conversation.
- Preserved the distinction between player speech, NPC speech and confirmed events, reducing duplicate context and facts lost because of marker language.
- Restored shipped starting files for the original **10 editable prompt files**, alongside built-in defaults. Existing player edits are preserved.
- **CN uses Chinese fixed prompts; EN uses English fixed prompts. UI follows the game language.** No additional rule forces a reply language. Existing generated dialogue, declaration or policy bodies, dynamic names and player-authored text are not automatically translated.

## 8. Ambient chatter retirement and images

- Removed the ambient AI's separate API, automatic chatter execution, settings and built-in ambient lines. Legacy settings can no longer enable it.
- Player-initiated shout, free conversation and existing action reactions remain available.
- Generated images now save under **`AnimusForge/image save/`**, outside Logs.
- Image model names containing **Gemini**, case-insensitively, use the Chat Completions endpoint, including custom aliases such as `gemini-nano-banana-2.1`.

## 9. Download and update

- Choose one complete CN or EN pack. Each contains a single AnimusForge module with Bannerlord 1.3.x and 1.4.x implementations.
- **ONNX models and local speech packages are separate downloads.** Retain your existing models and custom voice data.
- Back up saves, custom prompts, worldbooks and voices before updating. Existing custom language instructions remain effective.
- Dual-runtime builds, relevant offline regression checks and both formal package validations passed. Actual model behavior, long sessions, old saves and third-party mod combinations still require in-game validation.
