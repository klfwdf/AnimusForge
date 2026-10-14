# Party transfer owner regression

Run from the repository root with a new output directory:

```powershell
python tests/modules/AF.Module.Economy/PartyTransferOwnerTests/run.py --out artifacts/party-transfer-check
```

The runner links production projection, authorization and execution owners and
extracts the current roster transfer methods. `CarriedSourceCases.cs.txt` also
exercises the actual candidate builder and courier outgoing source admission:
player carried prisoners are listed, player dungeons are not scanned, foreign
parties and stale dungeon/party selections are rejected, and carried regular and
hero prisoners can be transferred. NPC dungeon candidate routing is retained.

Campaign objects, candidate roster projection and native prisoner actions use
boundary doubles. The courier downstream transfer is a recording double: its
checks prove source admission/routing, not a complete courier journey. These
checks do not establish live army UI, save-load or native-game acceptance.
