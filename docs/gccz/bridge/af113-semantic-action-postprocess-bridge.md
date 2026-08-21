# AF 1.1.3 semantic action postprocess bridge

- Town outcome actions are selected only by the guarded AF postprocess chain and are executed through `TryProcessAiActionTags`.
- Free-form player text is never converted directly into a GCCZ outcome tag. The retired `GCCZ test` keyword mapper and its immediate execution bridge were removed.
- Legacy canonical and alias tag formats remain accepted by `SiegeActionTagCatalog` and `SiegePostprocessTagNormalizer` after the AI postprocessor selects an eligible action.
- The direct SETS civilian gather command remains a separate scene-control compatibility path. It can only enqueue the existing gather operation while its settlement-entry runtime is active and cannot select a town outcome.
- Native conversation and scene-queue callers disable GCCZ postprocessing only when that direct scene-control command was actually handled. Other AF postprocess selections remain unchanged.
- The root and packaged `RuleBehaviorPrompts.json` copies must retain one canonical `siege_intervention_aftermath` entry with empty trigger keywords so the rule remains runtime-gated.
