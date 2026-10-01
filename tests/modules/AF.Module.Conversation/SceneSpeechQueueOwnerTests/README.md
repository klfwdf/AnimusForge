# Scene speech queue owner contract

Compiles the production generic queue owner. It verifies FIFO, one worker-start lease under sequential and concurrent enqueue, atomic empty retirement, conversation clear versus full reset, and diagnostic snapshot state.

`run_mutations.py` proves duplicate-worker, never-retire, and reset-without-clear defects fail named runtime assertions. Payload execution, Bannerlord dispatch, TTS/audio, history, actions, and real timing remain outside this deterministic owner test.


## Historical runner repair (2026-10-01)

run_mutations.py now resolves the SDK locally and supports --run-root with a minimal compilation environment. The reset-keeps-queue mutation targets the current T[] Reset declaration and preserves its returned dropped-item array; all three named behavioral failures must compile and be observed.
