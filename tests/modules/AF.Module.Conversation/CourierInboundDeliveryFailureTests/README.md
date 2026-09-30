# Default inbound Courier delivered-memory recovery

`run.py` extracts the current production `DeliverInboundLetterToPlayer` and
compiles the actual `CourierDeliveryBehavior.DeliveredMemory.cs` owner and the
root storage type with Newtonsoft JSON roundtrips, plus synthetic Memory,
inventory, and UI dependencies. It checks immediate success,
pending/loaded completion, missing owner, stale instance, old-save missing field,
malformed identity quarantine, bounded backpressure, and no physical redelivery.
`--mutate skip-history` compiles but fails the named immediate-memory assertion.

The harness also compiles the production Memory origin-date helper, checks its
current/origin/empty/throwing calendar branches with a synthetic campaign calendar,
and verifies frozen letter/fact/day/hour/scene values reaching the Memory seam.
`--mutate current-date` must fail the cross-day assertion. Three loaded intents
exercise one-intent-per-tick selection and stable round-robin order without any
inventory, notice, or party-cleanup replay. Outputs are isolated with `--run-root`
under workspace `artifacts`; no inherited credentials are passed to dotnet.

This source-derived test does not execute Bannerlord SaveSystem or prove actual
disk persistence, and it does not cover the separate `AFCI1:` generation receipt.
