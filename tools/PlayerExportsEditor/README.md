# AnimusForge PlayerExports Editor

Standalone Windows editor for `AnimusForge/PlayerExports` data packages.

This tool is intentionally independent from Bannerlord and the AnimusForge mod runtime. It does not reference `TaleWorlds.*`, does not load `AnimusForge.dll`, and only edits JSON files in a selected `PlayerExports/<package>` folder.

## Projects

```text
PlayerExportsEditor.slnx
src/PlayerExportsEditor.Core
  Data models, UTF-8 JSON IO, package scanning, backup, validation.

src/PlayerExportsEditor.App
  WinForms desktop UI.

tests/PlayerExportsEditor.SmokeTests
  Console smoke test against local sample PlayerExports packages.
```

## Build

```powershell
dotnet build tools\PlayerExportsEditor\PlayerExportsEditor.slnx
```

## Run The App

```powershell
dotnet run --project tools\PlayerExportsEditor\src\PlayerExportsEditor.App\PlayerExportsEditor.App.csproj
```

## Publish EXE

Self-contained build, recommended for sharing with users who may not have .NET installed:

```powershell
powershell -ExecutionPolicy Bypass -File tools\PlayerExportsEditor\publish-win-x64.ps1
```

Output:

```text
tools/PlayerExportsEditor/dist/win-x64-self-contained/AnimusForgePlayerExportsEditor.exe
tools/PlayerExportsEditor/dist/win-x64-self-contained/Data/VanillaConditionCatalog.json
tools/PlayerExportsEditor/dist/packages/AnimusForgePlayerExportsEditor-win-x64-self-contained-<timestamp>.zip
```

The publish script also exports an offline vanilla condition catalog. The catalog contains IDs, Chinese display labels, categories, and condition candidates used by the editor dropdowns. It does not copy or redistribute TaleWorlds original XML/resources.

If Bannerlord is not installed in a default Steam path on the build machine, pass the game `Modules` directory explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File tools\PlayerExportsEditor\publish-win-x64.ps1 -ModulesRoot "F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules"
```

Use `-SkipVanillaCatalog` only for local debugging builds that do not need the offline user data package.

Lightweight framework-dependent build, only for machines with the matching .NET Desktop Runtime:

```powershell
powershell -ExecutionPolicy Bypass -File tools\PlayerExportsEditor\publish-win-x64.ps1 -FrameworkDependent
```

Output:

```text
tools/PlayerExportsEditor/dist/win-x64-framework-dependent/AnimusForgePlayerExportsEditor.exe
```

## Run Smoke Test

```powershell
dotnet run --project tools\PlayerExportsEditor\tests\PlayerExportsEditor.SmokeTests\PlayerExportsEditor.SmokeTests.csproj
```

## Current Features

- Open a `PlayerExports` root folder.
- List data packages.
- Create a new package with the expected folder structure.
- Soft-delete a package by moving it into `.deleted_packages`.
- Load package sections:
  - `knowledge/rules/*.json`
  - `personality_background/*.json`
  - `voice_mapping/VoiceMapping.json`
  - `event_data/*.json`
  - `unnamed_persona/UnnamedNpcProfiles.json`
- Show package counts and validation issues.
- Edit selected JSON as raw text when needed.
- Format selected JSON.
- Save selected JSON with UTF-8 encoding and an automatic backup under `.backups/<timestamp>/`.
- Structured knowledge editor:
  - RuleId
  - keywords
  - RAG short texts
  - prompt variants
  - variant conditions in a visible selected-condition table
  - editable `SkillMin` thresholds
  - text mappings
- Variant condition candidate catalog:
  - reads packaged offline vanilla metadata from `Data/VanillaConditionCatalog.json`
  - scans Bannerlord `Modules/*/ModuleData` XML without loading game DLLs when local game data is available
  - reads `Languages/CNs` and shows Chinese names before raw IDs when available
  - extracts hero, culture, kingdom/faction/clan, settlement, identity, role, and skill IDs
  - merges existing IDs from the current PlayerExports package
  - writes raw IDs back to JSON while keeping the editor display friendly
- Structured persona editor:
  - Personality
  - Background
  - VoiceId
- Structured VoiceMapping editor.
- Structured WorldOpeningSummary editor.
- Structured KingdomOpeningSummaries editor.
- Structured UnnamedNpcProfiles editor.
- Create and soft-delete knowledge rule files.

## Current Limitations

- VoiceId candidates are still text-only; voice grouping can be edited structurally.
- `DialogueHistory` and `Debt` exports are not covered yet.
- Specialized form editing writes the known schema for that file. Use raw JSON editing for files that contain custom unknown fields you need to preserve.
