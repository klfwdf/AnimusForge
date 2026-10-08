# Current Memory / Weekly persistence boundary replay

Run from the repository root:

```powershell
python -X utf8 -B tests/AF.Persistence/CurrentMemoryWeeklyOwnerTests/run.py --run-root artifacts/current-memory-weekly-owner
```

The replay links the current four persistence adapters, `CampaignSaveChunkHelper`, `OwnerJsonStorageCodec`, Newtonsoft, persisted DTOs and sanitizers. Authoritative Memory/Weekly/material fields are extracted verbatim from their current owner source declarations. WorldBulletin's actual export/import methods execute with a controlled log port. The in-memory `IDataStore` serializes each slot and restores into a new owner; it does not read or write a Bannerlord `.sav`.

The current positive result is **31 roundtrip/schema checks**: five long Unicode Memory dictionaries, three queues, 12000-byte chunk limits and no full raw duplication, normal old raw values, malformed owner JSON isolation, Weekly world/kingdom openings and record/material graph, source material normalization, and WorldBulletin unread/mode/corruptRaw state. Game, save-engine, HTTP/provider and UI leaves remain unexecuted.

Two printed observations are **not corruption-protection PASS**:

- A malformed Memory owner JSON value remains only in scratch after load and disappears on the next save.
- A missing Weekly record chunk produces no truncated live graph, but the current owner becomes empty and retains no original damaged payload.

Those observations must remain visible in review output. This suite does not repair the save, truncate data, certify the historical D0 catalog or replace the broad campaign-event/summary lifecycle harness. The memory adapter and the relevant Weekly/material static save/load methods are normalized identical to `c629e866c`; that distinguishes inherited behavior from a new migration regression without treating the risk as resolved.

Artifacts include exact source hashes, runner/fixture hashes, revision, log and scope. Output directories must be new and stay inside the workspace. The other current prompt replay lives at `tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/run.py`; its direct-file scope and effective negative controls are separate from this persistence boundary.
