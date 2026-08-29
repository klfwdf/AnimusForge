# Persistence, save compatibility and user data

Use this reference for `SyncData`, save types, JSON/storage dictionaries, chunking, module schemas, migrations, PlayerExports, settings, content/user-data boundaries and cleanup/deploy safety.

## Preserve identity first

Existing saves may depend on:

- implementation assembly simple name;
- serialized namespace/type identity;
- CampaignBehavior presence;
- `SyncData` keys;
- JSON field names and defaults;
- chunk key conventions;
- module paths and PlayerExports merge behavior.

Directory or class cleanup is not permission to rename these. Keep legacy Behavior/API facades until representative old-save migration evidence exists.

## Per-module persistence ownership

Every module/bridge manifest declares a unique namespace and schema version:

```yaml
persistence:
  namespace: diplomacy
  schemaVersion: 3
```

The module/bridge owner maintains:

```text
Current schema
Legacy type/key/field mapping
Read-old path
Write-current path
Migration timing
Corruption/missing-field behavior
Size/chunk bounds
Representative fixture/saves
Rollback/disable behavior
```

Foundation/Persistence supplies namespace registration, conflict detection, chunking, migration catalog, diagnostics and size guardrails. It does not interpret or discard module business data.

Bridges use their own namespace. They never write into either participating module's private save keys.

## SafeMode and missing modules

When a module is disabled/missing/failed:

- preserve its unknown persisted records;
- list namespace/schema in inventory/recovery diagnostics;
- do not instantiate incompatible business types merely to erase them;
- do not claim identical gameplay state;
- only the module's registered migration may transform its data;
- a recovery tool may export/backup data but needs explicit destructive approval to delete it.

## Schema migration

A migration should be monotonic and idempotent where practical:

```text
read version
→ validate available migration path
→ backup/detach old representation
→ migrate in bounded steps
→ validate current representation
→ publish current state at one commit point
→ retain diagnostic/rollback metadata
```

Do not partially publish new derived state before all required migration steps succeed. Never silently default a malformed critical value to a successful empty state.

## Chunked strings and size

Use the existing `CampaignSaveChunkHelper` behavior as a compatibility seam while adding tests for:

- empty/small legacy inline values;
- UTF-8 multibyte and surrogate boundaries;
- exact and over-limit chunks;
- missing/corrupt count or chunk;
- maximum chunk-count refusal;
- dictionary flatten/restore;
- save/load round trip;
- per-module size diagnostics without full sensitive data.

Limits apply to complete stored values, including wrapper metadata where known.

## PlayerExports and module content

Classify every path as:

- curated immutable shipped content;
- user-writable runtime content;
- generated cache/index;
- diagnostic/log;
- staging/package artifact.

Do not place user-writable PlayerExports in a source replacement path that deploy deletes. Preserve existing merge-without-deletion behavior until a tested migration supersedes it.

Module content belongs under that module's content mapping. A module cannot overwrite another module's player data or static assets.

## Settings and credentials

- Module settings have an owned schema/namespace and immutable request snapshot.
- Reload affects future operations; in-flight operations keep their starting snapshot.
- Credential references/values never enter save files, module manifests, normal logs or public fixtures.
- Move HTTP clients/provider config out of `DuelSettings` into the LLM capability/provider layer without losing MCM migration behavior.

## Cleanup and deployment

Before moving/stopping tracking/deleting a data-looking path:

1. identify owner and readers/writers;
2. classify static/user/cache/artifact;
3. back up user data;
4. verify build/stage/deploy consumers;
5. choose copy/merge/migrate/ignore/stop-track/delete separately;
6. perform transactional staging/deploy;
7. validate old and new locations;
8. record evidence in the ledger.

`git rm --cached` is not a user-data deletion plan. Package cleanup and runtime-user-data cleanup are separate operations.
