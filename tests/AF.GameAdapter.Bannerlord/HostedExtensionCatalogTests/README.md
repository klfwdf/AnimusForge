# Hosted extension catalog contract checks

Scope: Illustrator, DialogueUI, Coup and Vengeance **host lifecycle metadata** in the existing internal directory. This is not new gameplay, a dynamic plugin loader or an externally callable execution API.

`run.py` compiles the production directory, registration, lifecycle owner, host and public snapshot projector. DialogueUI/Coup SubModules are source-linked; Illustrator's complete SubModule class and Vengeance's actual Initialize/RegisterCampaign/Shutdown methods are extracted unchanged. Only the engine, resource, Harmony and unrelated gameplay/event leaves are fakes. Source hashes and the fake boundary are written to each run receipt.

Checks cover: no auto-Ready on declaration; startup success and swallowed failure; UI installation deferral/missing resources/optional-wheel fallback; Vengeance claim, rollback and registration failure; campaign failures isolated to their host; startup failures cannot be hidden by campaign callbacks; real later campaign registration can recover a campaign-only failure; shutdown including cleanup errors; stopped-state late reports; reload and duplicate declarations; original bridge gate isolation; read-only V1 projection and parallel queries. `--defer-coup` is only for the explicitly requested three-extension intermediate slice.

Frequency: state reports occur only at startup/installation/campaign registration/shutdown. Production Tick remains byte-for-byte unchanged. There is no polling, game reference in directory state or new save key.

Run from the repository:

```powershell
python tests/AF.GameAdapter.Bannerlord/HostedExtensionCatalogTests/run.py
```

Actual engine patch installation, rendering, game loading, old saves and service behavior remain NOT_RUN. The separate public API suite retains its pre-extension team-port oracle and validates the additive hosted entries; it does not silently auto-start fake hosts.
