# Town low-attention prompt bridge

- `GcczTownPrompt.zh-CN.json` is the localized source for the ordered town main-prompt sections, six-role matrix, runtime state wording, semantic decision examples, and final machine-output contract.
- `TownPromptComposer` is the reusable source of truth for prompt ordering. The main reply always emits scene, role, memory, state, candidate boundary, forbidden actions, reply requirements, and output protocol in that order.
- `SiegeRuntimePromptProfile` no longer owns the former oversized mixed-responsibility main prompt. It delegates to the composer and retains only the separate commander and immediate-reaction compatibility helpers.
- `SiegePostprocessContextBuilder` now delegates its first four runtime sections to the same composer instead of maintaining a second long prompt layout.
- `GcczTownPromptResourceProvider` loads the localized catalog once. It prefers the active module file, falls back to the embedded copy, and uses an English fail-safe only when both are unavailable.
- `AfGcczShoutBridge.AppendTownPostprocessDecisionContract` appends the eligible candidate list, short positive and negative examples, and strict zero-or-one action protocol at the end of both unified scene postprocess request paths.
- `SiegePostprocessTagNormalizer.Validate` remains deterministic: it accepts legacy aliases only as compatibility input, rejects unlisted actions, collapses malformed multi-action town output to one conservative action, preserves one mood tag, and never infers an action from dialogue words.
- All runtime hooks remain guarded by the active GCCZ town route. Normal AF prompts and postprocessing are unchanged when the exclusive town route is inactive.
