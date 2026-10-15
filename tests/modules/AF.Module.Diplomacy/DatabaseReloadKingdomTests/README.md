# Database reload: unmatched dynamic kingdoms

Run `python tests/modules/AF.Module.Diplomacy/DatabaseReloadKingdomTests/run.py`.
`--source-file` accepts the pre-fix production file for a negative control.

The runner extracts the actual planning, matching, apply, candidate-clone/reset methods
and record/plan declarations verbatim. World enumeration, import reader, profile
normalization and localization are fixtures. The preservation test makes normalization
observable and compares every stored field, including overrides, generation and dates.

Checks cover mixed/all-skipped plans, all dynamic prefixes, missing saved profiles,
normal dynamic imports, unchanged hard validation, failed apply without partial state,
detached package inputs, original resets for other kingdoms and eight-entry UI bounds.
No actual Campaign, player save, renderer or network request is used.
