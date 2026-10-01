# System NPC speech terminal closure

`run.py --run-root <fresh-path>` compiles the complete actual runtime with the
actual shared speech effect ports, dispatcher, PendingOperationRegistry, queued
speech effect and speech completion owners. Game/action/audio leaves are
substitutes in their real namespaces. It executes 15 system speech checks plus
the unchanged 11 queued speech/completion checks.

The returned task is the same dispatch outcome, not a new effect gate. The
strong-yield retirement case waits for this actual task to complete false
without executing any queued callback, then executes the old callbacks twice
and proves they remain inert. Other cases cover original deferred admission,
duplicate delivery, mission change retaining the same Agent reference, save
generation/session/epoch/owner retirement, index reuse, missing/inactive target,
hero and nonhero observed partial transfer facts/notifications, nontransactional
effect failure, TTS failure, and original history-before-exit order.

`run_mutations.py --run-root <fresh-path>` requires all nine mutations to compile
and reach their intended unchanged assertions. The mission fixture intentionally
retains the same Agent in the new Mission, isolating the Mission guard from the
Agent-reference guard.

`source_review.py` is read-only: complete original 287-line body inverse against
fixed Git `ab1d3e48`, with explicitly reviewed atomic ports, main-thread capture,
scope/Agent guards and queued-task result substitutions. It also checks the
actual original public producer/private facade/controller consumer. No new
state authority, queue, registry or retry policy is introduced.

No live Bannerlord, real AI/TTS or player-save acceptance is claimed. Performance
is event-driven; the old nearby capture and single target lookup are reused,
not supplemented by a new full-agent scan or polling loop. Product dual-API
compilation is a separate frozen-manifest integrator gate.
