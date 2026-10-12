# Memory localization review fixes

Phase: verified static/resource slice. Owner: Codex Memory slice; local commits only.

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

## Delivery

- Added 28 EN/CN keys; Memory resources now have 270 paired keys.
- Seven recall failure producers keep original diagnostics and carry a detached display
  description. Failure notice owner resolves it only after existing main-thread,
  generation, campaign and coalescing checks.
- Summary start/completion notifications and ordered failure details are localized.
  The original diagnostic aggregation still runs off-thread and is logged unchanged.
- Four execution result DTOs gain `JsonIgnore` ErrorDisplay fields. These DTOs are
  transient request receipts; the saved job/draft error fields still use original Error.
- Local summary parse failures receive a separate display translation. Raw provider
  replies and unexpected external exception messages are preserved verbatim.
- Maintained failure/recall/queue harness inputs, English/Chinese display assertions,
  actual-source fixture dependencies and prepare-only modes. Historical business,
  captured and terminal fixtures keep their original diagnostic oracles.

## Verification

- PASS `verify_display_static.py`: exact Recall source inverse (only paired display
  statements/fields added), seven unchanged diagnostic producers, four JsonIgnore
  fields, lifecycle/queue guards, EN fallback equality, test Python syntax.
- PASS real Bannerlord localization DLL 1.3 and 1.4: 1080 resolutions each,
  English/Chinese/French fallback/switch-back.
- PASS all 25 localization resource pairs and focused `git diff --check`.
- PASS Recall and actual Queue runtime/factory prepare-only extraction.
- BLOCKED FailureUi prepare: shared `verify_bindings()` first rejects the existing
  PolicySystem/Core/CustomPolicyBehavior.Lifecycle.cs J17 binding. No shared gate bypass.

## Integration Follow-up

The main integrator must review `tests/j17_terminal_migration_review.json` and
`tests/af2_terminal_migration_review.py` bindings for the approved Memory display
deltas plus other concurrent localization changes. Our Memory owner/adapter/DTO
files and modified evidence runners appear in those shared historical bindings.
Do not update hashes blindly or drop the main-thread/generation/source assertions.
After that integration, run FailureUi, Recall, QueueRun, Business/Captured/Terminal
compiled replays and source parity. We updated their inputs but did not execute them.

NOT-RUN: C# compilation/product builds, compiled harness execution, live game and
existing save loading. Static/XML/native resource success does not replace these.
Known scope boundary: shared LLM/network diagnostic producers, externally returned
messages, stored/user-written memory and Prompt content may contain Chinese. This
slice translates Memory-owned UI shells and known Memory parser failures only.
Rollback: focused inverse of the implementation commit; intent checkpoint
`88f8a7104`. Never reset shared changes.
