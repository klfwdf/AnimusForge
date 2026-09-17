# __COMPONENT_NAME__

This template documents a real owner; a README or runtime manifest is not mandatory for every helper.

## Required: responsibility and connection

- Responsibility / non-goals: __RESPONSIBILITY__
- Owner: __OWNER__
- Actual source and consumers: __PATHS_AND_CALLERS__
- State/algorithm/resource moved; remaining old responsibility: __COVERAGE__
- Preserved behavior/identity and approved changes: __CONTRACT__
- Verification, source revision, NOT-RUN and rollback: __EVIDENCE__

## Conditional: include only affected surfaces

- Internal contract or separately versioned public API; dependencies and failure semantics.
- Registration, tick frequency/work budget, background inputs, cancellation and disposal/restart.
- Existing save keys/types/schema, migration and user data; omit for stateless components.
- Content loader paths, configuration defaults/overrides and affected interaction channels.
- Actual manifest/profile consumer and schema; omit if no runtime declaration is consumed.
- Both Bannerlord 1.3/1.4 builds and applicable runtime scenarios, reported separately.

Do not invent a manifest ID, persistence namespace, entry type or Foundation dependency to fill this template.
