# Town scene memory store bridge

- `TownSceneMemoryStore` is the reusable source of truth for the bounded GCCZ scene event timeline.
- The store owns sequence numbers, consecutive duplicate suppression, capacity trimming, immutable snapshots, and reset behavior.
- The AF adapter supplies formatted live event text, logging side effects, and the exact session reset timing.
- `ResetSessionCounters` clears the store and restarts its sequence, so ordinary soldiers and civilians cannot retain GCCZ scene memory after exit.
- The store is intentionally not serialized. Persistent personal history for named nobles, prisoners, companions, and settlement notables remains an AF responsibility and is not duplicated here.
