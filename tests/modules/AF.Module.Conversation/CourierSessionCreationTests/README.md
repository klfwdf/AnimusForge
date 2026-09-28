# Courier session creation checks

`run.py` checks production outbound and inbound session publication order,
runtime-index registration, generation start, and the prewritten external
letter no-LLM path. Party creation and game UI remain `LIVE=NOT-RUN`.

`run_admission.py --dotnet <absolute SDK executable>` compiles the actual draft,
letter-window callbacks, confirmation and reset methods against explicit game/UI
fixtures. It checks old-window/new-draft and same-draft/new-step races, replaced
or sealed owners, save generation, main-thread admission, partial dispatch and
reentrant replacement, prepared-draft tickets (main-thread capture, client scope,
128-ticket owner limit, revocation and UI competition), and send-time recipient,
crew, mode and source-qualified aggregate cargo admission. All six payload modes
and the unknown-recipient reply exception retain positive cases.

The same runner also source-links `CoreDialogueClient`, `CoreDialogueOperation`,
the existing Courier main-thread phase dispatcher/retirement registry and the
internal module admission. Cases cover concurrent retries, cross-channel ID
conflicts/capacity, queued cancellation (including during validation), Dispose,
UI-first submission, partial failure, 128 outstanding operations per owner,
retirement without a future tick and release of transient associations. Cancelled
but physically undrained callbacks still occupy a bounded slot until dequeue or
owner reset, preventing cancel/recreate loops from flooding the existing queue. Successful
dispatch must remain **Running**, not Completed: b2 transport receipts are a separate
gate and Courier remains unavailable in V1 until that gate passes.

`--ref fb4af2d8` reproduces the pre-J14b defects (the baseline excludes new ticket
methods); `--mutate ignore-revision|ignore-generation|ignore-owner|ignore-client|ignore-ticket-capacity|ignore-stock|ignore-cancel|ignore-operation-capacity|release-undrained`
must compile and fail a named behavior assertion. Game stock snapshots and dispatch
effects here are fixtures, not full transport or public Courier API acceptance.
