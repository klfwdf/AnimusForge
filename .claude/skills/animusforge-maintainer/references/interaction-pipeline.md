# AF interaction pipeline

Use this reference for scene shout, native/free conversation, courier, preprocess routing, prompts, LLM calls, action postprocessing, visible replies, history and AFEF facts.

Read the repository's current channel-alignment and directive-tag case documents before editing. This reference defines the intended module seam, not every current method.

## Ownership

- `AF.Module.Conversation` owns the shared interaction pipeline and three channel adapters.
- `AF.Module.Memory` provides public history/fact storage capability.
- LLM providers expose a stable `llm.generate` capability.
- Action-owning modules register action handlers/capabilities.
- Cross-module action effects use a bridge when the behavior belongs to more than one module.
- Foundation supplies context, scheduler, diagnostics and GameAdapter ports but not prompt/action policy.

## Shared flow

```text
Channel adapter
→ [main thread] capture immutable GameInteractionSnapshot
→ EligibilityEvaluator: RuleSelection + CapabilitySet
→ PromptComposer
→ [background] LlmGateway
→ VisibleReplyNormalizer
→ ActionPostprocessor: ActionPlan
→ [main thread] validate current targets and execute once
→ Memory capability: dialogue + AFEF facts
```

Channel adapters own trigger/UI/timing only. They do not own private prompt grammars, tag regex copies, action execution or history semantics.

## Core DTO intent

```text
InteractionChannel
InteractionIdentity
GameInteractionSnapshot
InteractionEnvelope
RuleSelection
CapabilitySet
PromptPackage
PostprocessContext
ActionPlan
InteractionResult
TraceContext
```

Snapshots contain detached IDs/names/values/candidate lists, not live `Hero`, `Agent`, `MobileParty`, `Mission` or other TaleWorlds objects crossing background boundaries.

## Three-channel alignment

| Contract | Scene shout | Native/free conversation | Courier |
| --- | --- | --- | --- |
| Eligibility/preprocess | Shared | Shared | Shared with explicit courier exclusions |
| Rule hit propagation | Same IDs | Same IDs | Same IDs |
| Prompt block/role semantics | Shared | Shared | Shared; letter delivery facts fit the same fact/history model |
| History and memory | Shared capability | Shared capability | Shared capability; timing differs at delivery/reply |
| Postprocess rules/capabilities | Shared | Shared | Shared or explicit exclusion |
| Action parsing/execution | Shared action registry | Shared action registry | Same; execution may occur at delivery/return boundary |
| AFEF facts | Shared types | Shared types | Shared types |
| UI/TTS/group display | Channel-specific | Channel-specific | Channel-specific |

A non-applicable capability has a machine-readable exclusion reason. Silence or a shorter private pipeline is not an exclusion.

## Facts

Player/NPC speech is not proof that a transfer/action happened. Only game-confirmed execution results become AFEF facts. Use typed fact records internally; render legacy prefixes at compatibility boundaries while migrating.

- player speech: user dialogue;
- NPC speech: assistant dialogue;
- game-confirmed player action: player AFEF;
- game-confirmed NPC action: NPC AFEF;
- rejected/failed action: explicit result/diagnostic; do not write success fact.

## Action tags

Tags/structured output belong to postprocessing, not visible main-reply prompt text. For each parameterized tag distinguish:

1. rule/template family authorized this turn;
2. concrete output allowed by that family;
3. target/amount/state legal in the snapshot;
4. target/state still legal on the main thread;
5. execution happened exactly once;
6. tag removed from visible reply;
7. facts/notifications reflect actual result.

Direct tag execution tests only prove the executor. Real tests cover:

```text
selected rule → merged postprocess rules → RAW → FINAL
→ parsed ActionPlan → main-thread validation/execution
→ visible reply → AFEF/history
```

## Ambient context

Migrate hand-set/cleared AsyncLocal target fields toward explicit `InteractionContext`. During transition, use a scoped owner that restores the previous context in `finally`; do not set six fields in every caller and clear to global empty values.

## Errors and traces

Every stage preserves one trace ID and reports statuses such as:

```text
Succeeded
SkippedByEligibility
DegradedWithoutProvider
RetryableFailure
NonRetryableFailure
CancelledAsStale
RejectedByValidation
Executed
```

Default logs record bounded metadata, selected rule/capability/action summaries, timings and failure reason—not API keys or unrestricted full player/model content.
