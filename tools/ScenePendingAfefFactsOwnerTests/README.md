# Scene pending AFEF owner contract

Compiles the production `ScenePendingAfefFactsOwner` and `ConversationMessage`. It verifies per-Agent isolation, the existing 12-item oldest-first bound, one-shot transfer to the next prompt, mission/session clear behavior, and invalid-input rejection.

`run_mutations.py` proves that removing the consume release, the bound, or the agent key fails a named runtime assertion without a compile failure. This is process-local offline evidence, not a live Mission, save, memory-store, or provider test.
