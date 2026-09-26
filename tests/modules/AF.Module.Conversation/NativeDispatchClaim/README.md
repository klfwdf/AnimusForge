# Native queued-operation claim (J07b)

The production value type owns the atomic `Queued -> Started` / `Queued -> ExpiredBeforeStart` choice used by admission capture and final game-action dispatch. The same captured value is used by callback, timeout and retirement. Never copy it after enqueue. It is not an action receipt, cancellation token or retry permission.

- `run.py`: actual source, deterministic yield/acknowledgment and concurrent contenders sharing one closure; no sleep-based acceptance.
- `run_mutations.py`: four mutants must compile and fail a named assertion.
- `test_source_review.py` / `source-review.json`: three exact host edits, active real consumers and preserved CRLF/no BOM. The prior ticket-owner inverse remains a separate packet.
- Native five suites and action dispatch/admission negative controls cover the actual queued host methods, unknown-after-start propagation and no duplicate effects. Pending-history/general scheduling still have their own existing claims; this slice does not migrate them or assert the full 484-line sequencer is complete.

Run these Python entry points from the repository root with `AF_DOTNET` set to the local SDK. No game or paid provider calls are made. See the current main ledger for exact validation revisions and remaining work.
