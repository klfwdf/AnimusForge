# MCM dropdown refresh replay

Run with an explicitly selected installed SDK, e.g. PowerShell:

```powershell
$env:AF_DOTNET = 'C:/Program Files/dotnet/dotnet.exe'
python tests/AF.GameAdapter.Bannerlord/McmDropdownRefreshTests/run.py
```

The test compiles the production refresh helper against the current 1.4 references and loads the installed MCM 5.11.4 selector and edit action. The property host/configuration owner is a double. It checks no automatic writes, independent main/ambient/image models, nonempty lists, user selection after callback restoration, and constructor-failure preservation. It writes only a unique repository artifact directory. It does not fetch models, read credentials, alter game configuration, or prove rendered Gauntlet behavior.

`--baseline <revision>` binds the same assertions to an earlier helper; the pre-fix refresh must fail the no-automatic-write assertion. Source and MCM hashes are retained in receipt.json.
