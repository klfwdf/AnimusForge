# Default inbound Courier delivery failure reproduction

`run.py` extracts the current production `DeliverInboundLetterToPlayer` and
`MyBehavior.AppendExternalDialogueHistory` bodies, compiles them with synthetic
Campaign, memory-owner, inventory, and UI stubs, and runs accepted, rejected,
throwing, and missing-owner cases. It confirms that default inbound delivery
continues when the legacy void memory facade cannot confirm History/AFEF.

The test does **not** prove a real game/save loses a fact. It does not cover
the separate `AFCI1:` detached opt-in receipt path. `--mutate skip-history`
must fail the named history-attempt assertion.
