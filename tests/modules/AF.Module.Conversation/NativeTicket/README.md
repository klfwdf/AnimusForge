# Native admission owner contract (J07b1)

`NativeConversationAdmissionOwner<TTicket>` is the one process-local reservation/epoch/revision owner. It does not resolve game targets or own save generation. The host still captures and checks those facts on the game thread.

- `run.py`: actual owner compiled with a value-equal ticket double; reference identity, independent clocks, late release after a forced asynchronous yield, and separate host isolation.
- `run_mutations.py`: seven compiled mutants must fail their named runtime assertions; compilation/path failures do not count.
- `test_source_review.py` + `source-review.json`: exact four-host-file transformation from the recorded baseline. The monolith retains CRLF/no BOM. No whole-file/hash-only acceptance refresh.
- Existing Native Admission / Presentation / Pending / Completion / public submission runners consume this same owner. Historical runs keep their prior fixture setup. Pending rejection retains the captured ticket after the real ConversationEnded transition clears the active slot.

Run from the repository root with `AF_DOTNET` pointing at an available SDK:

```text
python -B tests/modules/AF.Module.Conversation/NativeTicket/run.py
python -B tests/modules/AF.Module.Conversation/NativeTicket/run_mutations.py
python -B tests/modules/AF.Module.Conversation/NativeTicket/test_source_review.py
```

These tests do not certify the complete Native stage sequencer, game loading, save loading, real frames, TTS or provider behavior. The current main ledger owns the acceptance record.
