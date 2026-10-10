# NPC action editor localization integration

Phase: static/resource verified; C# compilation and game NOT-RUN.
Product `45b0e18a5` adds 110 paired keys and a separate UI mapping for all 37
known action kinds. Existing EventEditorUiText resolves at editor display boundaries;
no Tick scan or reflection. Raw Text/GameDate/LocationText and unknown kind values
are retained; these can still contain Chinese, so runtime English is not complete.

The old preview was also used by CampaignCharacterRecordCaptureAdapter for weekly
material SnapshotText. Its complete algorithm moved, byte-equivalent after method
rename, to NpcActionMaterialPreviewProjection.Build. The main integrator changed
the single capture callback in MyBehavior.cs to this factual projection, keeping
the localized preview solely in UI. Original Prompt and saved material semantics
are preserved; retained Chinese fact literals still need later externalization.

Main integration registers both LanguageFiles and content-map entries. It also
fixes the subagent resource headers from base to base type=string after the shared
contract caught the omission. The static NPC contract now verifies the native
header and actual host callback, in addition to original-method equivalence.

PASS: 26 registered resource pairs; NPC static contract; existing 1.3/1.4 localization
DLL snapshots each resolve 440 NPC-editor texts, including Chinese and French
English fallback; focused diff check. This is native resource execution, not current
production assembly or renderer execution. New optional actor narrative display
port is not yet supplied: raw original actor text remains visible, not guessed.

NOT-RUN: lifecycle fixture execution, dual product/Bootstrap compilation, game,
old-save/long-campaign acceptance, formal package/deployment/push. Rollback uses
focused inverses of the integration slice and `45b0e18a5`, preserving other agents.
