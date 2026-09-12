# Execution ledger and handoff

The AF execution ledger is the rolling source of current task truth. This skill owns durable method; the ledger owns live state.

## Locate the ledger

Preferred filename:

```text
animusforge-refactoring-and-repository-reorganization-plan.md
```

It may temporarily live outside the canonical Git worktree while the repository is being reconciled. Do not create a second independent ledger. If moving it into the repository, record the move, old-location disposition, and synchronization rule in the ledger first.

## One current status entry

When the project has accumulated a root HANDOFF, the original execution plan and newer phase ledgers:

1. Identify the current canonical status entry using the latest user request, checked Git revision and explicit supersession links. Do not assume the newest filename or an old machine path is authoritative.
2. Keep one current summary of scope, revision, task state and next gate. Other entry documents link to it rather than maintaining competing copies.
3. After an authorized status update, mark superseded “current/latest/not started” sections as historical and point to their replacement. Preserve the original audit/results instead of rewriting history as if later fixes had already existed.
4. Map any scope change back to the original checklist: implemented, partial, deferred/HOLD, owner-transferred or explicitly out of scope. State which original acceptance requirements remain open.
5. If the request is read-only, report contradictory status entries and the evidence-based reading; do not fix documentation or resume work without authorization.

A handoff's historical authorization is context, not authorization for this session. `PAUSED`, `VERIFY`, `OFFLINE_COMPLETE` and `TRANSFER_READY` are not synonyms for product `DONE`.

## Before any repository or source write

1. Read current status, active tasks, phase gates, change records and handoff snapshot.
2. Verify canonical worktree, branch, HEAD and dirty status.
3. Select an existing task ID or add a scoped task with dependencies and acceptance criteria.
4. Change its state to `ACTIVE`.
5. Add an intent row containing:
   - executor;
   - purpose and non-goals;
   - paths expected to change;
   - module/foundation/bridge owner;
   - save/profile/channel/1.3-1.4/user-data risks;
   - validation plan.
6. Only then write files.

## Meaningful checkpoints

Update the ledger when:

- a vertical slice or module extraction completes;
- a manifest, capability, profile, bridge or persistence decision changes;
- validation passes or fails;
- scope expands or splits;
- a blocker appears;
- work is rolled back or abandoned;
- another person/model/session will continue;
- the requested task completes.

Tiny edits within the same active slice do not need one ledger row each. The ledger should remain an execution ledger, not a keystroke diary.

## Completion evidence

A completed row must state:

- actual paths changed, including old → new moves; key code locations include verified one-based line ranges, symbols, source revision, actual caller/owner and covered versus uncovered behavior;
- exact checks run and results;
- `NOT-RUN` checks and concrete reasons;
- logs/artifacts/commit SHA where available;
- save/profile/channel/1.3-1.4/user-data impact;
- remaining risk and rollback;
- next exact task.

A task remains `VERIFY` or `BLOCKED` when an acceptance-required check cannot run; use `ACTIVE` only while work is actually executing. On a user-requested pause, preserve unresolved acceptance and record the pause rather than leaving a false active task. Do not mark `DONE` from reasoning alone.

## Handoff snapshot

Before stopping, leave:

```text
Current task and state
Canonical worktree + branch + HEAD
Files actually changed
Git status summary
Validation run + result
Validation not run + reason
Current blocker/risk
One exact next action
Actions that remain unsafe
```

If no work remains active, remove stale active intent and point to the next ledger task.

## Conflict handling

The latest user request and current disk/Git state outrank stale ledger text. When a file changed outside the current session:

- read the current file;
- preserve deliberate changes;
- do not restore an older skill/ledger snapshot over it;
- reconcile task status and intent;
- note the mismatch if it changes scope or safety.

## Division of durable knowledge

| Knowledge | Home |
| --- | --- |
| Current objective, progress, blocker, validation, next step | Execution ledger |
| Stable AF workflow and architecture rules | This skill/reference set |
| User-requested historical review checklist | Dated, revision-bound skill reference; revalidate before use, never a competing live ledger |
| Irreversible design decision and alternatives | Repository ADR |
| Module-specific API, ownership, config, lifecycle, save schema | Module/bridge README and manifest |
| Human installation/use/release behavior | Repository docs/README/release notes |
| Full logs, builds, packages, diagnostics | Ignored artifact plane |
