# Meeting escort roster regression

Run `python -B tests/AF.GameAdapter.Bannerlord/MeetingEscortRosterRegressionTests/run.py`
from the repository root. The source methods are extracted verbatim and compiled against
small scene, spawn-spy, supplier and roster fixtures with the installed .NET 10 SDK.
`--source-file` replays a historical source; `--baseline-source` checks unchanged damage,
removal, escalation-release and placement methods. Every run allocates fresh local evidence.

Both sides, Agent/Origin identity, last-troop duplicate/crash, two-troop casualties,
candidate eligibility, priorities, delayed spawn, timeout, cached selection and tick throttling
are covered. `--native-api 1.3` / `--native-api 1.4` choose native
`PartyAgentOrigin`/`PartyGroupAgentOrigin` casualty methods, which are also
extracted unchanged, but supplier and roster effects remain fixtures, not native engine code.
The old spawn edge is a spy that produces the old extra party-bound entity; production spawn
internals and actual Mission/Campaign event ordering are NOT exercised. Source wiring rejects
all synthetic escort spawn entrances. This is offline control-flow evidence, not a live-game
or player-save acceptance result. The regular unified build validates both actual API lines.
