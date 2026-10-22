# Marriage target regression

- `run.py` extracts the current production enumeration, relationship, normalization, execution guards, both marriage executors and dispatch/cleanup methods verbatim. Game mutation, settings and memory boundaries are explicit test doubles. Resource key/placeholder contracts and main-prompt/tag separation are checked as well. A workspace-local populated NuGet cache can be supplied using `--nuget-cache`; outputs must be new workspace-local directories.
- `run_current.py` compiles a .NET Framework runner against an explicit current 1.4 DLL/marker and matching reference directory. It creates detached **real Hero/Clan instances**, including a brother, sister and companion, without starting a native campaign. It runs the production member/context capture and tag normalization. Mapped JSON resources and logs stay in its isolated `AnimusForge` module fixture, never the installed module.
- Optional `--live --settings <MCM JSON> --model-cache <ModelDropdownCache.json>` is a **billable opt-in**. It reads credentials in memory only and uses the production `LegacyConfiguredChatGateway`, production-captured candidate facts, current CN/EN postprocess resources and the actual DLL normalizer. Only synthetic dialogue is sent. Transport failure stops the run; semantic mismatches do not. The executable config targets net472 like the product (avoiding CLR 4.0 TLS defaults), without relaxing certificate validation or changing machine TLS. It does not rewrite settings, probe other providers, generate NPC main replies, start/save a campaign, or claim native marriage acceptance.
- `cases.json` defines exact expected partner IDs for personal elopement, the leader personally marrying, a daughter marrying, brother/companion/sister clan marriages, brief acceptance, duplicate names, ambiguous candidates, negotiation and unauthorized approval.

Example (replace paths with the explicitly verified inputs):

```text
python -X utf8 -B tests/modules/AF.Module.Social/MarriageTagTests/run.py --run-root artifacts/<new-run> --nuget-cache <workspace-cache>
python -X utf8 -B tests/modules/AF.Module.Social/MarriageTagTests/run_current.py --dll <current-1.4-DLL> --references <matching-1.4-refs> --game-root <game-root> --run-root artifacts/<new-run>
```

Native clan transfers, spouse effects, the full three-channel main-reply flow, actual save files and real-game UI remain separate acceptance gates. Never infer those outcomes from a normalized tag or a detached fixture.
