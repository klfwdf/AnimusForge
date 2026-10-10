# Memory localization review fixes

Phase: intent. Owner: Codex Memory slice; local commits only.

## Scope

- Memory recall failure producers, summary queue notifications and failure notice owner.
- Narrow display wiring in `MyBehavior.MemorySummaryQueueRun.cs`, `MemoryRecallInputCaptureAdapter.cs`, and `MyBehavior.MemoryRecall.cs`.
- `afmemory_ui_strings` EN/CN resources and Memory failure/recall/queue replay tests.
- Preserve original diagnostic strings, queue state, Prompt content, persistence types and saved values.

## Implementation boundary

Carry a transient display description beside the original diagnostic title/message.
Resolve Bannerlord TextObject only at the existing main-thread UI publication boundary.
Summary failure details retain their original ordered diagnostic aggregation and an
independent localized display aggregation. Localization runs only when a summary
run starts/completes or a failure is presented; no Tick scans or reflection.

## Exit checks

1. EN/CN keys and placeholders match; native DLL text resolution where available.
2. Failure publication retains generation, main-thread, campaign, coalescing and callback revision guards.
3. Recall Prompt and summary Apply/Mark/source predicates remain unchanged.
4. Maintain actual-source replay inputs and assertions; do not execute compilation harnesses or product builds during concurrent work.

NOT-RUN: product builds, compiled harness execution, live game and existing save loading.
Rollback: focused inverse of the implementation commit; never reset shared changes.
