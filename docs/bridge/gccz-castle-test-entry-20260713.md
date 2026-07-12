# GCCZ castle test entry (2026-07-13)

- GCCZ `亲自进城决定` now accepts towns and castles.
- Castles enter their native courtyard location (`center`) through the existing `CastleEncounter` controller.
- Villages remain rejected by `SiegeInterventionEntryProfile.IsSupportedSettlementKind`.
- This does not enable SETS riots in castles or villages; SETS remains town-only.
- Castle scenes may contain fewer civilians than towns. Existing GCCZ runtime code must tolerate an empty civilian set and continue to allow TAB exit/outcome resolution.
