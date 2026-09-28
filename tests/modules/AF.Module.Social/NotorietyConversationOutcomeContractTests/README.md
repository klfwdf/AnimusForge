# Notoriety conversation outcome contract tests

Red baseline before the runtime source existed:

```text
error CS2001: Source file 'Refactor/Runtime/NotorietyConversationOutcomeReceipt.cs' could not be found.
```

The project source-links the production-compatible data-only runtime file and
executes it on `net8.0` without TaleWorlds or AnimusForge host references.
