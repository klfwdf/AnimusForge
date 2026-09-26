# Diplomacy module ports

Run `python tools/DiplomacyModulePortTests/run.py --dotnet <dotnet-path>` from the repository.

Compiles the production contracts, bridge, game adapters and timeline adapters against recording game/owner stubs. Checks identity precedence, current indexed lookup, missing Campaign/targets, result/ref/out forwarding, exception propagation, read statuses, owner replacement, policy snapshots and zero-allocation tick forwarding. Four deliberate mutations must fail runtime assertions.

The source inverse compares all 29 migrated conversation/social/tribute call sites against DPL-090, preserving complete caller bodies including guards, arguments, commit order and channel history. It also compares policy cadence, producer values/cache logic, and tick/patch lifecycle routing. CampaignCompositionTests checks registration order; ModuleFrameworkApiTests checks the new internal capability catalog and feature-gate isolation. The existing WorldDiplomacy smoke suites cover actual diplomacy rules and workflows.

Generated projects/logs stay under ignored `.generated/`. This is offline contract evidence, not live engine, thread scheduling, save-load or gameplay acceptance.
