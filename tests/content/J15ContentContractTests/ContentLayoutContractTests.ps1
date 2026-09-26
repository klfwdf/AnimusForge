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
    [ordered]@{ owner="Test"; source="content/modules/Test/ModuleData/two.json"; target="ModuleData/two.json" }
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

$realMapPath = Join-Path $ProjectRoot "content\content-map.json"
$realMap = Get-Content -LiteralPath $realMapPath -Raw -Encoding UTF8 | ConvertFrom-Json
$realDestination = Join-Path $RunRoot "real-projection"
$realResult = @(Invoke-AnimusForgeContentProjection -ProjectRoot $ProjectRoot -DestinationModuleDir $realDestination -MapPath $realMapPath)
Assert-Contract ($realResult.Count -eq $realMap.entries.Count) "real projection count"
foreach ($entry in $realMap.entries) {
    $source = Join-Path $ProjectRoot ([string]$entry.source)
    $target = Join-Path $realDestination ([string]$entry.target)
    Assert-Contract (Test-Path -LiteralPath $target -PathType Leaf) "real projection target missing: $($entry.target)"
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    Assert-Contract ($sourceHash -eq $targetHash) "real projection bytes drifted: $($entry.target)"
}
$realFiles = @(Get-ChildItem -LiteralPath $realDestination -Recurse -File -Force)
Assert-Contract ($realFiles.Count -eq $realMap.entries.Count) "real projection contains unexpected files"

Write-Output "contentLayoutContract valid=1 real=$($realMap.entries.Count) invalid=8 PASS"

# Load only the production merge and its dependencies, never deploy's top-level actions.
$parseErrors = $null
$deployAst = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $ProjectRoot "一键编译覆盖推送\deploy_module.ps1"), [ref]$null, [ref]$parseErrors)
Assert-Contract ($parseErrors.Count -eq 0) "deploy script parse errors"
foreach ($name in @("Get-FullPathSafe", "Assert-PathUnderRoot", "Assert-NotReparsePoint", "Invoke-Robocopy", "Merge-InstalledCustomPromptsIntoStaging")) {
    $definitions = @($deployAst.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    Assert-Contract ($definitions.Count -eq 1) "merge dependency missing or duplicated: $name"
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
$legacyAssignment = @($deployAst.FindAll({ param($node)
    $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left.Extent.Text -eq '$LegacyRootPolicyPromptFileNames'
}, $false))
Assert-Contract ($legacyAssignment.Count -eq 1) "legacy prompt name list missing"
. ([scriptblock]::Create($legacyAssignment[0].Extent.Text))

$promptEntries = @($realMap.entries | Where-Object { $_.target.StartsWith("CustomPrompts/") })
$policyEntries = @($promptEntries | Where-Object { $_.target.StartsWith("CustomPrompts/Policy/") })
Assert-Contract ($promptEntries.Count -eq 30 -and $policyEntries.Count -eq 22) "mapped prompt default counts"

function Write-PromptFixture {
    param([string]$Root, [string]$Relative, [string]$Text)
    $path = Join-Path $Root $Relative
    Assert-AnimusForgePathUnderRoot -Path $path -Root $RunRoot -Label "Prompt fixture"
    Assert-AnimusForgeNoReparsePoint -Path $path -Label "Prompt fixture"
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [System.IO.File]::WriteAllText($path, $Text, [System.Text.UTF8Encoding]::new($false))
}

foreach ($caseName in @("fresh", "empty", "split", "legacy")) {
    $caseRoot = Join-Path $RunRoot "custom-prompts\$caseName"
    $installed = Join-Path $caseRoot "installed"
    $staging = Join-Path $caseRoot "staging"
    Assert-AnimusForgePathUnderRoot -Path $caseRoot -Root $RunRoot -Label "Prompt fixture"
    Assert-AnimusForgeNoReparsePoint -Path $caseRoot -Label "Prompt fixture"
    Assert-Contract (-not (Test-Path -LiteralPath $caseRoot)) "prompt fixture must start empty"
    New-Item -ItemType Directory -Path $staging -Force | Out-Null
    $installedFiles = @{}
    if ($caseName -eq "empty") {
        New-Item -ItemType Directory -Path (Join-Path $installed "CustomPrompts") -Force | Out-Null
    } elseif ($caseName -eq "split") {
        $installedFiles = @{
            "CustomPrompts/PlayerCustomPromptRule.json" = "user override"
            "CustomPrompts/unknown.json" = "unknown root"
            "CustomPrompts/Policy/unknown.json" = "unknown policy"
            "CustomPrompts/Policy/Effects/heroGold.json" = "user effect"
            "CustomPrompts/Policy/Effects/unknown.json" = "unknown effect"
        }
    } elseif ($caseName -eq "legacy") {
        $installedFiles = @{
            "CustomPrompts/PlayerCustomPromptRule.json" = "user override"
            "CustomPrompts/unknown.json" = "unknown root"
            "CustomPrompts/Policy/unknown.json" = "legacy policy to replace"
        }
        foreach ($fileName in $LegacyRootPolicyPromptFileNames) {
            $installedFiles["CustomPrompts/$fileName"] = "legacy policy"
        }
    }
    foreach ($entry in $installedFiles.GetEnumerator()) {
        Write-PromptFixture -Root $installed -Relative $entry.Key -Text $entry.Value
    }
    foreach ($pass in 1..2) {
        # Each pass mirrors real assembly order: mapped defaults, then installed override.
        Copy-Item -LiteralPath (Join-Path $realDestination "CustomPrompts") -Destination $staging -Recurse -Force
        Merge-InstalledCustomPromptsIntoStaging -ProjectRoot $ProjectRoot -TargetModuleDir $installed -StagingModuleDir $staging
        $expectedDefaults = @()
        $expectedUserFiles = @{}
        switch ($caseName) {
            "fresh" { $expectedDefaults = $promptEntries }
            "empty" { $expectedDefaults = $policyEntries }
            "split" { $expectedUserFiles = $installedFiles }
            "legacy" {
                $expectedDefaults = $policyEntries
                $expectedUserFiles = @{
                    "CustomPrompts/PlayerCustomPromptRule.json" = "user override"
                    "CustomPrompts/unknown.json" = "unknown root"
                }
            }
        }
        foreach ($entry in $expectedDefaults) {
            $source = Join-Path $ProjectRoot $entry.source
            $target = Join-Path $staging $entry.target
            Assert-Contract ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $target).Hash) "$caseName default drift: $($entry.target)"
        }
        foreach ($entry in $expectedUserFiles.GetEnumerator()) {
            Assert-Contract ([System.IO.File]::ReadAllText((Join-Path $staging $entry.Key)) -eq $entry.Value) "$caseName override drift: $($entry.Key)"
        }
        $actualFiles = @(Get-ChildItem -LiteralPath $staging -Recurse -File)
        Assert-Contract ($actualFiles.Count -eq $expectedDefaults.Count + $expectedUserFiles.Count) "$caseName unexpected files, pass $pass"
        # Merging must never mutate the installed source, including old/unknown policy files.
        foreach ($entry in $installedFiles.GetEnumerator()) {
            Assert-Contract ([System.IO.File]::ReadAllText((Join-Path $installed $entry.Key)) -eq $entry.Value) "$caseName installed source changed"
        }
    }
}
Write-Output "customPromptMerge defaults=30 policy=22 scenarios=4 repeated=4 installedUnchanged=1 PASS"
