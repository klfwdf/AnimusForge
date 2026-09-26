[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:assertions = 0

function Assert-Contract {
    param([bool]$Condition, [string]$Message)
    $script:assertions++
    if (-not $Condition) { throw $Message }
}

# This standalone test never cleans an existing run or reads real player exports.
. (Join-Path $ProjectRoot "一键编译覆盖推送\content_layout.ps1")
$RunRoot = Get-AnimusForgeContentFullPath -Path $RunRoot
Assert-AnimusForgePathUnderRoot -Path $RunRoot -Root (Join-Path $ProjectRoot "artifacts\j15-content") -Label "PlayerExports fixture"
Assert-AnimusForgeNoReparsePoint -Path $RunRoot -Label "PlayerExports fixture"
Assert-Contract (-not (Test-Path -LiteralPath $RunRoot)) "PlayerExports fixture root must not already exist"

# Evaluate only named production helpers and the bounded source-selection block.
# Never dot-source deploy_module.ps1 or execute deployment, replacement or cleanup.
$parseErrors = $null
$deployAst = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $ProjectRoot "一键编译覆盖推送\deploy_module.ps1"), [ref]$null, [ref]$parseErrors)
Assert-Contract ($parseErrors.Count -eq 0) "deploy script parse errors"
foreach ($name in @("Get-FullPathSafe", "Assert-PathUnderRoot", "Get-RelativePathUnderRoot", "Invoke-Robocopy", "Merge-PlayerExports", "Sync-PlayerExportsBackToSource")) {
    $definitions = @($deployAst.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    Assert-Contract ($definitions.Count -eq 1) "PlayerExports dependency missing or duplicated: $name"
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
$sourceAssignments = @($deployAst.FindAll({ param($node)
    $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left.Extent.Text -eq '$playerExportSources'
}, $false) | Sort-Object { $_.Extent.StartOffset })
$mergeCalls = @($deployAst.FindAll({ param($node)
    $node -is [System.Management.Automation.Language.CommandAst] -and $node.GetCommandName() -eq "Merge-PlayerExports"
}, $false))
Assert-Contract ($sourceAssignments.Count -eq 3 -and $mergeCalls.Count -eq 1) "PlayerExports source-selection boundary changed"
$start = $sourceAssignments[0].Extent.StartOffset
$selectionText = $deployAst.Extent.Text.Substring($start, $mergeCalls[0].Extent.EndOffset - $start)
$selection = [scriptblock]::Create($selectionText)
$selectionCommands = @($selection.Ast.FindAll({ param($node)
    $node -is [System.Management.Automation.Language.CommandAst]
}, $true) | ForEach-Object { $_.GetCommandName() } | Sort-Object -Unique)
Assert-Contract (($selectionCommands -join ",") -eq "Join-Path,Merge-PlayerExports,Test-Path,Write-Host") "unexpected command in isolated source-selection block"
New-Item -ItemType Directory -Path $RunRoot | Out-Null

function Write-Fixture {
    param([string]$Root, [string]$Relative, [string]$Text, [int]$Day = 1)
    $path = Join-Path $Root $Relative
    Assert-AnimusForgePathUnderRoot -Path $path -Root $RunRoot -Label "PlayerExports synthetic file"
    Assert-AnimusForgeNoReparsePoint -Path $path -Label "PlayerExports synthetic file"
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [System.IO.File]::WriteAllText($path, $Text, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::SetLastWriteTimeUtc($path, [DateTime]::new(2030, 1, $Day, 0, 0, 0, [DateTimeKind]::Utc))
}

function Get-FixtureSnapshot {
    param([string]$Root)
    Assert-AnimusForgePathUnderRoot -Path $Root -Root $RunRoot -Label "PlayerExports snapshot"
    $rows = @(if (Test-Path -LiteralPath $Root -PathType Container) {
        foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -Force -File | Sort-Object FullName) {
            $relative = Get-RelativePathUnderRoot -Path $file.FullName -Root $Root
            "{0}|{1}|{2}" -f $relative, (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash, $file.LastWriteTimeUtc.Ticks
        }
    })
    return ($rows -join "`n")
}

function Assert-FileText {
    param([string]$Root, [string]$Relative, [string]$Expected)
    Assert-Contract ([System.IO.File]::ReadAllText((Join-Path $Root $Relative)) -ceq $Expected) "unexpected winner: $Relative"
}

function Invoke-FixtureSelection {
    param([string]$Stage)
    $stagingModuleDir = $Stage
    & $selection
}

$sourceModuleDir = Join-Path $RunRoot "merge\source"
$legacy13ModuleDir = Join-Path $RunRoot "merge\legacy13"
$legacy14ModuleDir = Join-Path $RunRoot "merge\legacy14"
$targetModuleDir = Join-Path $RunRoot "merge\unified"
$source = Join-Path $sourceModuleDir "PlayerExports"
$legacy13 = Join-Path $legacy13ModuleDir "PlayerExports"
$legacy14 = Join-Path $legacy14ModuleDir "PlayerExports"
$unified = Join-Path $targetModuleDir "PlayerExports"
Write-Fixture $source "tie.json" "source-tie"
Write-Fixture $legacy13 "tie.json" "legacy13-tie"
Write-Fixture $legacy14 "tie.json" "legacy14-tie"
Write-Fixture $source "tie13.json" "source-tie13"
Write-Fixture $legacy13 "tie13.json" "legacy13-tie13"
Write-Fixture $source "newest.json" "source-newer" 3
Write-Fixture $legacy14 "newest.json" "legacy14-older"
Write-Fixture $source "source-only.json" "source-only"
Write-Fixture $legacy13 "legacy13-only.json" "legacy13-only"
Write-Fixture $legacy14 "unknown\opaque.dat" "unknown-legacy14"
$sourceRoots = @($source, $legacy13, $legacy14)
$before = @($sourceRoots | ForEach-Object { Get-FixtureSnapshot $_ })

$firstStage = Join-Path $RunRoot "first-stage"
Invoke-FixtureSelection $firstStage
$firstOutput = Join-Path $firstStage "PlayerExports"
Assert-FileText $firstOutput "tie.json" "legacy14-tie"
Assert-FileText $firstOutput "tie13.json" "legacy13-tie13"
Assert-FileText $firstOutput "newest.json" "source-newer"
Assert-FileText $firstOutput "source-only.json" "source-only"
Assert-FileText $firstOutput "legacy13-only.json" "legacy13-only"
Assert-FileText $firstOutput "unknown\opaque.dat" "unknown-legacy14"
Assert-Contract (@(Get-ChildItem -LiteralPath $firstOutput -Recurse -File).Count -eq 6) "first-install exact file set"
Assert-Contract ((Get-Item -LiteralPath (Join-Path $firstOutput "newest.json")).LastWriteTimeUtc.Ticks -eq
    (Get-Item -LiteralPath (Join-Path $source "newest.json")).LastWriteTimeUtc.Ticks) "winner timestamp not preserved"
$repeatStage = Join-Path $RunRoot "first-stage-repeat"
Invoke-FixtureSelection $repeatStage
Assert-Contract ((Get-FixtureSnapshot $firstOutput) -ceq (Get-FixtureSnapshot (Join-Path $repeatStage "PlayerExports"))) "first-install replay drift"
for ($i = 0; $i -lt $sourceRoots.Count; $i++) {
    Assert-Contract ($before[$i] -ceq (Get-FixtureSnapshot $sourceRoots[$i])) "merge modified a read-only source"
}
Assert-Contract (-not (Test-Path -LiteralPath $targetModuleDir)) "first-install merge created the unified installation"

# An existing unified directory excludes both legacy roots, even when it is empty.
New-Item -ItemType Directory -Path $targetModuleDir | Out-Null
$emptyInstalledStage = Join-Path $RunRoot "empty-installed-stage"
Invoke-FixtureSelection $emptyInstalledStage
Assert-Contract ((Get-FixtureSnapshot $source) -ceq (Get-FixtureSnapshot (Join-Path $emptyInstalledStage "PlayerExports"))) "empty unified directory must suppress legacy imports"
Write-Fixture $unified "tie.json" "unified-tie"
Write-Fixture $unified "newest.json" "unified-older" 2
Write-Fixture $unified "unknown\installed.extra" "unknown-unified"
Write-Fixture $legacy14 "tie.json" "legacy14-must-not-win" 4
$sourceRoots += $unified
$before = @($sourceRoots | ForEach-Object { Get-FixtureSnapshot $_ })
$installedStage = Join-Path $RunRoot "installed-stage"
Invoke-FixtureSelection $installedStage
$installedOutput = Join-Path $installedStage "PlayerExports"
Assert-FileText $installedOutput "tie.json" "unified-tie"
Assert-FileText $installedOutput "newest.json" "source-newer"
Assert-FileText $installedOutput "source-only.json" "source-only"
Assert-FileText $installedOutput "unknown\installed.extra" "unknown-unified"
Assert-Contract (@(Get-ChildItem -LiteralPath $installedOutput -Recurse -File).Count -eq 5) "installed exact file set (legacy must be excluded)"
$installedRepeat = Join-Path $RunRoot "installed-stage-repeat"
Invoke-FixtureSelection $installedRepeat
Assert-Contract ((Get-FixtureSnapshot $installedOutput) -ceq (Get-FixtureSnapshot (Join-Path $installedRepeat "PlayerExports"))) "installed replay drift"
for ($i = 0; $i -lt $sourceRoots.Count; $i++) {
    Assert-Contract ($before[$i] -ceq (Get-FixtureSnapshot $sourceRoots[$i])) "installed merge modified a read-only source"
}

$beforeDestination = Get-FixtureSnapshot $installedOutput
$rejected = $false
try { Invoke-FixtureSelection $installedStage } catch {
    $rejected = $_.Exception.Message.StartsWith("PlayerExports staging destination must not already exist:")
}
Assert-Contract $rejected "existing destination was not refused"
Assert-Contract ($beforeDestination -ceq (Get-FixtureSnapshot $installedOutput)) "refused merge changed destination"
$emptyDestination = Join-Path $RunRoot "missing-sources-output"
Merge-PlayerExports -DestinationDir $emptyDestination -Sources @(
    [PSCustomObject]@{ Path = (Join-Path $RunRoot "missing-source"); Priority = 10; Label = "missing" }
)
Assert-Contract ((Test-Path -LiteralPath $emptyDestination -PathType Container) -and (Get-FixtureSnapshot $emptyDestination) -eq "") "missing sources must produce an empty staging directory"

# Back-sync uses the real robocopy /E /XO helper, but both ends are synthetic.
$syncSourceModule = Join-Path $RunRoot "sync\source"
$syncTargetModule = Join-Path $RunRoot "sync\target"
$syncSource = Join-Path $syncSourceModule "PlayerExports"
$syncTarget = Join-Path $syncTargetModule "PlayerExports"
Write-Fixture $syncSource "source-only.json" "keep-source-only"
Write-Fixture $syncSource "source-newer.json" "keep-newer-source" 3
Write-Fixture $syncTarget "source-newer.json" "ignore-older-target"
Write-Fixture $syncSource "target-newer.json" "replace-older-source"
Write-Fixture $syncTarget "target-newer.json" "copy-newer-target" 3
Write-Fixture $syncTarget "unknown\target-only.dat" "copy-unknown-target"
$targetBefore = Get-FixtureSnapshot $syncTarget
Sync-PlayerExportsBackToSource -SourceModuleDir $syncSourceModule -TargetModuleDir $syncTargetModule
Assert-FileText $syncSource "source-only.json" "keep-source-only"
Assert-FileText $syncSource "source-newer.json" "keep-newer-source"
Assert-FileText $syncSource "target-newer.json" "copy-newer-target"
Assert-FileText $syncSource "unknown\target-only.dat" "copy-unknown-target"
Assert-Contract (@(Get-ChildItem -LiteralPath $syncSource -Recurse -File).Count -eq 4) "back-sync exact file set"
Assert-Contract ($targetBefore -ceq (Get-FixtureSnapshot $syncTarget)) "back-sync modified installed exports"
$syncBeforeRepeat = Get-FixtureSnapshot $syncSource
Sync-PlayerExportsBackToSource -SourceModuleDir $syncSourceModule -TargetModuleDir $syncTargetModule
Assert-Contract ($syncBeforeRepeat -ceq (Get-FixtureSnapshot $syncSource)) "back-sync replay drift"
$missingSyncSource = Join-Path $RunRoot "missing-sync-source"
Sync-PlayerExportsBackToSource -SourceModuleDir $missingSyncSource -TargetModuleDir (Join-Path $RunRoot "missing-sync-target")
Assert-Contract (-not (Test-Path -LiteralPath $missingSyncSource)) "missing installed exports must not create source data"

Write-Host "playerExportsContracts assertions=$script:assertions firstInstall=PASS unifiedInstalled=PASS existingDestination=REFUSED missingSources=PASS nonDeletingBackSync=PASS realPlayerData=NOT_READ cleanup=NONE PASS"
