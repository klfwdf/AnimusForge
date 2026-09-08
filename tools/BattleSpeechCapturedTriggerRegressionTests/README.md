# BattleSpeech captured trigger lifetime regression

Fixes the successful natural-trigger path that previously bypassed the opaque AF input identity check.

```powershell
python -B tools/BattleSpeechCapturedTriggerRegressionTests/run.py
# Intentional red baseline: same host/bridge and assertions, old trigger consumer only.
python -B tools/BattleSpeechCapturedTriggerRegressionTests/run.py --source-ref 5ce8767a --output-name red-5ce8767a
python -B tools/BattleSpeechCapturedTriggerRegressionTests/run.py --mutation omit-completion-check --output-name mutant-completion
python -B tools/BattleSpeechCapturedTriggerRegressionTests/run.py --mutation omit-final-check --output-name mutant-final
```

Current **18 PASS / 0 FAIL**. Old trigger source `5ce8767a`: **9 PASS / 9 FAIL**. Both mutations fail a behavioral assertion (**17 PASS / 1 FAIL**), not compilation.

## Reproduction

1. Capture natural input A and hold the real extracted async classifier method at a deterministic provider task.
2. Open new AF input UI B: the host input sequence changes, while BattleSpeech's trigger generation and Mission remain unchanged.
3. Resolve A as `PLAYER_SPEECH`.
4. Old `ProcessV2ClassifierCompletions` / `ApplyClassifiedTrigger` starts and prepares A. Current code rejects A through the non-consuming AF request checker.

The suite also covers epoch/session/save generation/player identity, consumed request, host main-thread rejection, phase-resolution invalidation immediately before `StartSession`, stale classifier failure, legitimate ordinary fallback, old-host observer/no-capture inputs, missing or malformed optional APIs, and wrong/throwing original owners. Valid success preserves the speech text and does not consume `Started` or call the replay API.

## Actual code and test boundaries

- Real source: complete `RunTriggerClassificationAsync`, the trigger-completion loop through its decisive branch, complete `ApplyClassifiedTrigger`, optional reflection binding/current validation, and the actual Shout capture/current-check methods including its new read-only wrapper.
- Only the unrelated plan-completion loop is excluded from the trigger consumer. `StartSession`, `PrepareSpeech`, ordinary replay, classifier result parser, Mission/Agent, phase lookup and the Bannerlord main-thread signal are controlled test boundaries.
- This proves the reviewed control-flow bug and its request identity fix, **not real game acceptance, parser behavior, movement, morale or audio effects**. The full extension and host still require the normal dual-version build and gameplay regression.
- `--source-ref` switches only the trigger source so the old control-flow defect remains independently observable with the same current checker. It does not claim the entire old host was built.
- No HTTP, LLM, real game APIs or save access. Generated fixtures/logs stay in `.generated/`.
