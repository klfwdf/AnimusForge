# AnimusForge v1.6.3 Update

This update fixes remaining Chinese template text in the English content pack and improves compatibility between current and legacy fact records.

## English content pack

- Completed English projections for fixed rule headings and default player prompts used in conversation requests.
- Fixed remaining template text in declaration, policy and world bulletin category headings and dates, plus fixed fragments in recent events and battle reports.
- Corrected initial summary headings while preserving existing content and record order.

## Fact record compatibility

- Both content packs now use English AFEF fact markers, while continuing to recognize legacy Chinese markers and full-width variants.
- Fixed cases where memory recovery, history recall or scene conversation could miss facts, wrap them twice, or mistake them for character speech.
- Preserved the distinction between player speech, NPC speech and events that have actually occurred.

## Update notes

- The CN pack uses Chinese fixed prompts and the EN pack uses English fixed prompts. No additional rule forces the model to reply in a particular language; replies still depend on context and your custom instructions.
- Existing generated dialogue, declarations, policy bodies and player-authored text are not automatically translated. These fixes cover fixed templates and recognized legacy formats at display and request boundaries.
- Each pack remains a single module with implementations for Bannerlord 1.3.x and 1.4.x. Choose CN or EN according to the prompt content you want.

These notes cover changes since the previous remote update.
