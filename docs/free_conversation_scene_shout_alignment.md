# Free Conversation / Scene Shout Alignment

This document records a hard maintenance rule for AnimusForge: free conversation and scene shout must share the same LLM request-body design. UI can be different, but prompt assembly, rule routing, history shape, and postprocess behavior must stay aligned.

## Scope

- "Free conversation" means the AnimusForge input box opened during vanilla one-to-one conversation.
- "Scene shout" means the existing scene shout / scene dialogue path driven by `ShoutBehavior`.
- This document covers the LLM chain only: preprocess, main reply generation, and action postprocess.
- This document does not require changing scene shout behavior to satisfy free conversation. Free conversation must follow scene shout.

## Core Rule

Every free-conversation request body must correspond to the same stage in scene shout.

- Free-conversation preprocess must match scene-shout preprocess.
- Free-conversation main reply request must match scene-shout main reply request.
- Free-conversation action postprocess must match scene-shout action postprocess.

Do not mix all context types into all three stages. Each stage only receives the context that the corresponding scene-shout stage receives.

## Preprocess Alignment

Free conversation must use the same topic-selection and rule-eligibility logic as scene shout.

Required:

- Use `MyBehavior.BuildShoutPromptContextForExternal(...)` as the entry point.
- Use the same current NPC / current scene / target-character conditions as scene shout.
- Use `BuildPreprocessExcludedRuleIdsForCurrentInteraction(...)` or its current scene-shout equivalent for selective rule injection.
- Use the same auxiliary dialogue-history entry path as scene shout.
- If free conversation has no valid scene agent index, it may use a native conversation history adapter, but the rendered text must still use scene-shout history wording.

Forbidden:

- Do not add a separate free-conversation preprocess request body.
- Do not narrow rule eligibility only because the request came from vanilla conversation.
- Do not bypass scene-shout runtime restrictions for Duel, Scene_Move, summon, follow, give/show, world-map party commands, or other action topics.

## Main Reply Alignment

Free conversation main reply generation must use scene-shout prompt blocks and scene-shout message conversion.

Required block order:

1. Recent dialogue history section from `SplitPersistedHeroHistorySections(...)`.
2. Memory overview and memory blocks from `MyBehavior.BuildHistoryContextForExternal(...)`.
3. Dynamic runtime context built from scene-shout helpers, including NPC identity, present NPC list, trust block, and miscellaneous runtime facts.
4. Knowledge and selected rule block from `BuildSceneSystemRuleBlock(...)`.
5. Conversation messages converted through `BuildStrictSceneMessagesForNpc(...)`.

Required helpers:

- `BuildSceneSystemTopPromptIntroForSingle(...)`
- `BuildSceneUserRuntimeContextForSingle(...)`
- `BuildSceneSystemRuleBlock(...)`
- `BuildSceneCompositeUserBlock(...)`
- `BuildStrictSceneMessagesForNpc(...)`
- `TryRenderSceneHistoryLine(...)` for any adapted free-conversation history lines

Forbidden:

- Do not hardcode native-only text such as "you are in a vanilla conversation box".
- Do not create headers such as `【原版对话当前台词】` or `【玩家输入】`.
- Do not create a free-conversation-only message role format.
- Do not add a second request-body implementation just to support the free-conversation UI.

## Postprocess Alignment

Free conversation action postprocess must use the same postprocess path and history shape as scene shout.

Required:

- Use `RunCourierActionPostprocessForExternal(...)` or the current unified scene-shout postprocess entry.
- Pass the same topic-hit result from preprocess into postprocess.
- Use the same runtime postprocess rule builders and tag normalizers as scene shout.
- Build postprocess history from:
  - `TrimPrivateRecentWindowForActionPostprocess(..., 5)`
  - `BuildScenePublicHistorySection(...)`
  - `BuildSceneCompositeUserBlock(...)`
- Keep postprocess history at the same effective recent-window depth as scene shout unless scene shout itself changes.

Forbidden:

- Do not pass only the current player line and current NPC line.
- Do not use native-only history labels.
- Do not write a separate free-conversation postprocess request body.
- Do not let free conversation use a shorter or richer postprocess context than scene shout for the same stage.

## History And Facts

Free conversation may keep its own temporary session history, but it must be rendered into scene-shout form before entering any LLM request.

Required:

- Current vanilla NPC text should be recorded as an NPC dialogue turn before the player input is submitted.
- Player input should be recorded as a player dialogue turn.
- AFEF behavior facts must keep the same factual boundary as scene shout:
  - `[AFEF玩家行为补充]`
  - `[AFEF NPC行为补充]`
- Claims inside normal dialogue history are not system facts.
- Only AFEF-prefixed behavior facts can be treated as confirmed actions.

Forbidden:

- Do not treat a normal dialogue sentence like "X gave Y money" as a confirmed transfer.
- Do not remove `role=user` style scene message conversion from adapted history.
- Do not use a private native history format in final LLM messages.

## New Feature Checklist

When adding any new scene-shout feature, check free conversation at the same time.

- If a new topic can be selected by scene shout preprocess, confirm free conversation uses the same selection conditions.
- If a new runtime instruction is injected into scene shout main reply, confirm free conversation receives it through the same helper path.
- If a new `PostprocessRules` tag is added, confirm free conversation passes the same postprocess flags, runtime target facts, candidate lists, and normalizers.
- If a feature is scene-only, gate it through the same eligibility/exclusion mechanism instead of adding free-conversation prompt text.
- If a feature must close the current dialogue window before execution, implement that in the action executor, not by changing the LLM request body.

## Verification

Before considering alignment work done:

- Search for native-only prompt labels:
  - `rg "原版对话当前台词|【玩家输入】|原版对话框" ShoutBehavior.cs AIConfigHandler.cs MyBehavior.cs`
- Build both versions:
  - `dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3`
  - `dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4`
- In game, compare logs for the three stages:
  - preprocess topic hits
  - main reply request shape
  - action postprocess history and selected tag rules

If scene shout and free conversation differ, treat it as a bug unless there is a documented hard gameplay reason.
