[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contract {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Write-TestMap {
    param([string]$Path, [object[]]$Entries)
    $payload = [ordered]@{ schemaVersion = 1; entries = $Entries }
    [System.IO.File]::WriteAllText($Path, ($payload | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))
}

function Assert-ThrowsWithoutOutput {
    param([string]$Label, [scriptblock]$Action, [string]$Destination)
    $threw = $false
    try { & $Action } catch { $threw = $true }
    Assert-Contract $threw "$Label must fail"
    $files = @()
    if (Test-Path -LiteralPath $Destination) {
        $files = @(Get-ChildItem -LiteralPath $Destination -Recurse -File -Force)
    }
    Assert-Contract ($files.Count -eq 0) "$Label wrote partial output"
}

$helper = Join-Path $ProjectRoot "一键编译覆盖推送\content_layout.ps1"
Assert-Contract (Test-Path -LiteralPath $helper -PathType Leaf) "content layout helper is missing"
. $helper

$fixtureRoot = Join-Path $RunRoot "powershell"
if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
$fixtureProject = Join-Path $fixtureRoot "project"
$sourceDir = Join-Path $fixtureProject "content\modules\Test\ModuleData"
New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $sourceDir "one.json"), "one", [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $sourceDir "two.json"), "two", [System.Text.UTF8Encoding]::new($false))
$mapPath = Join-Path $fixtureProject "content\content-map.json"
New-Item -ItemType Directory -Path (Split-Path -Parent $mapPath) -Force | Out-Null
$validEntries = @(
    [ordered]@{ owner="Test"; source="content/modules/Test/ModuleData/one.json"; target="ModuleData/one.json"; logicalName="Test.One" },
    [ordered]@{ owner="Test"; source="content/modules/Test/ModuleData/two.json"; target="ModuleData/two.json"; logicalName="Test.Two" }
)
Write-TestMap -Path $mapPath -Entries $validEntries
$validDestination = Join-Path $fixtureRoot "valid-output"
$result = @(Invoke-AnimusForgeContentProjection -ProjectRoot $fixtureProject -DestinationModuleDir $validDestination -MapPath $mapPath)
Assert-Contract ($result.Count -eq 2) "valid projection count"
Assert-Contract ([System.IO.File]::ReadAllText((Join-Path $validDestination "ModuleData\one.json")) -eq "one") "valid projection content"

$cases = @(
    [ordered]@{ Label="missing-source"; Entries=@($validEntries[0], [ordered]@{owner="Test";source="content/modules/Test/missing.json";target="ModuleData/missing.json";logicalName="Test.Missing"}) },
    [ordered]@{ Label="duplicate-target"; Entries=@($validEntries[0], [ordered]@{owner="Test";source="content/modules/Test/ModuleData/two.json";target="moduledata/ONE.json";logicalName="Test.Other"}) },
    [ordered]@{ Label="duplicate-logical"; Entries=@($validEntries[0], [ordered]@{owner="Test";source="content/modules/Test/ModuleData/two.json";target="ModuleData/two.json";logicalName="test.one"}) },
    [ordered]@{ Label="source-traversal"; Entries=@([ordered]@{owner="Test";source="../one.json";target="ModuleData/one.json";logicalName="Test.One"}) },
    [ordered]@{ Label="target-traversal"; Entries=@([ordered]@{owner="Test";source="content/modules/Test/ModuleData/one.json";target="../one.json";logicalName="Test.One"}) },
    [ordered]@{ Label="absolute-source"; Entries=@([ordered]@{owner="Test";source="C:/one.json";target="ModuleData/one.json";logicalName="Test.One"}) },
    [ordered]@{ Label="ads-source"; Entries=@([ordered]@{owner="Test";source="content/modules/Test/ModuleData/one.json:stream";target="ModuleData/one.json";logicalName="Test.One"}) }
)

foreach ($case in $cases) {
    $caseMap = Join-Path $fixtureProject ("content\{0}.json" -f $case.Label)
    Write-TestMap -Path $caseMap -Entries $case.Entries
    $destination = Join-Path $fixtureRoot ("invalid-" + $case.Label)
    Assert-ThrowsWithoutOutput -Label $case.Label -Destination $destination -Action {
        Invoke-AnimusForgeContentProjection -ProjectRoot $fixtureProject -DestinationModuleDir $destination -MapPath $caseMap | Out-Null
    }
}

$realDir = Join-Path $fixtureRoot "real-source"
$junction = Join-Path $fixtureProject "linked-source"
New-Item -ItemType Directory -Path $realDir -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $realDir "linked.json"), "linked", [System.Text.UTF8Encoding]::new($false))
New-Item -ItemType Junction -Path $junction -Target $realDir | Out-Null
$reparseMap = Join-Path $fixtureProject "content\reparse.json"
Write-TestMap -Path $reparseMap -Entries @([ordered]@{owner="Test";source="linked-source/linked.json";target="ModuleData/linked.json";logicalName="Test.Linked"})
$reparseDestination = Join-Path $fixtureRoot "invalid-reparse"
Assert-ThrowsWithoutOutput -Label "reparse-source" -Destination $reparseDestination -Action {
    Invoke-AnimusForgeContentProjection -ProjectRoot $fixtureProject -DestinationModuleDir $reparseDestination -MapPath $reparseMap | Out-Null
}

Write-Output "contentLayoutContract valid=1 invalid=8 PASS"
