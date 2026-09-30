# Default inbound Courier delivered-memory recovery

`run.py` extracts the current production `DeliverInboundLetterToPlayer` and
compiles the actual `CourierDeliveryBehavior.DeliveredMemory.cs` owner and the
root storage type with Newtonsoft JSON roundtrips, plus synthetic Memory,
inventory, and UI dependencies. It checks immediate success,
pending/loaded completion, missing owner, stale instance, old-save missing field,
malformed identity quarantine, bounded backpressure, and no physical redelivery.
`--mutate skip-history` compiles but fails the named immediate-memory assertion.

This source-derived test does not execute Bannerlord SaveSystem or prove actual
disk persistence, and it does not cover the separate `AFCI1:` generation receipt.
