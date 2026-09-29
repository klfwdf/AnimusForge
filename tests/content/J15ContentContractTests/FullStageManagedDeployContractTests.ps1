[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$StageModuleDir,
    [Parameter(Mandatory = $true)][string]$RunRoot,
    [ValidateSet('Fault', 'Abrupt', 'AbruptChild')][string]$Mode = 'Fault'
)

$ErrorActionPreference = 'Stop'
$project = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
$stage = [System.IO.Path]::GetFullPath($StageModuleDir).TrimEnd('\')
$run = [System.IO.Path]::GetFullPath($RunRoot).TrimEnd('\')
$allowed = Join-Path $project 'artifacts\j15-content'
$expectedStage = Join-Path $project 'bin\Release\single_module_stage\AnimusForge'
if (-not $run.StartsWith($allowed + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
    -not $stage.Equals($expectedStage, [System.StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $stage -PathType Container)) {
    throw 'Full Stage fixture requires the project-local Release Stage and an artifact run root.'
}
if ($Mode -eq 'AbruptChild') {
    if (-not (Test-Path -LiteralPath $run -PathType Container)) { throw 'Abrupt child fixture root is missing.' }
}
elseif (Test-Path -LiteralPath $run) {
    throw 'Full Stage fixture root must be new; existing evidence is never cleared.'
}
foreach ($path in @($stage, (Split-Path -Parent $run))) {
    for ($directory = [System.IO.DirectoryInfo]::new($path); $null -ne $directory; $directory = $directory.Parent) {
        if ($directory.Exists -and ($directory.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Full Stage fixture path crosses a reparse point: $path"
        }
    }
}

$stageFiles = @(Get-ChildItem -LiteralPath $stage -File -Recurse -Force | Sort-Object FullName)
if ($stageFiles.Count -ne 124) { throw "Expected the current 124-file Stage, found $($stageFiles.Count)." }
$firstRelative = $stageFiles[0].FullName.Substring($stage.Length).TrimStart('\', '/')
$secondRelative = $stageFiles[1].FullName.Substring($stage.Length).TrimStart('\', '/')
$modules = Join-Path $run 'game\Modules'
$target = Join-Path $modules 'AnimusForge'
$local = Join-Path $run 'localappdata'
$recovery = Join-Path $local 'AnimusForge\Recovery\deploy'
$oldManaged = 'synthetic-previous-managed-bytes'
$sentinels = @(
    'ONNX\synthetic.bin',
    'PlayerExports\synthetic.txt',
    'Logs\synthetic.log'
)

if ($Mode -ne 'AbruptChild') {
    New-Item -ItemType Directory -Path $run, $target, $local | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $run '.j15-full-stage-fixture'), 'j15-full-stage-124')
    $firstTarget = Join-Path $target $firstRelative
    New-Item -ItemType Directory -Path (Split-Path -Parent $firstTarget) -Force | Out-Null
    [System.IO.File]::WriteAllText($firstTarget, $oldManaged)
    foreach ($relative in $sentinels) {
        $path = Join-Path $target $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        [System.IO.File]::WriteAllText($path, 'unmanaged-synthetic')
    }
}
elseif (-not (Test-Path -LiteralPath (Join-Path $run '.j15-full-stage-fixture') -PathType Leaf) -or
    [System.IO.File]::ReadAllText((Join-Path $run '.j15-full-stage-fixture')) -ne 'j15-full-stage-124' -or
    [System.IO.File]::ReadAllText((Join-Path $target $firstRelative)) -ne $oldManaged -or
    (Test-Path -LiteralPath $recovery)) {
    throw 'Abrupt child refuses an unprepared fixture root.'
}
$env:LOCALAPPDATA = $local

$astErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $project 'scripts\build\deploy_module.ps1'), [ref]$null, [ref]$astErrors)
if ($astErrors.Count -ne 0) { throw 'Deploy script has parse errors.' }
foreach ($name in @('Get-FullPathSafe', 'Assert-PathUnderRoot', 'Assert-NotReparsePoint',
        'Assert-NoReparseAncestors', 'Assert-DeploymentPath', 'Write-DeploymentMarker',
        'Assert-FeatureBridgesDeploymentBaseline', 'Invoke-ManagedStageDeployment')) {
    $definitions = @($ast.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    if ($definitions.Count -ne 1) { throw "Missing or duplicate deployment function: $name" }
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}

$script:activeStageHashes = 0
$script:faultInjected = $false
function Get-FileSha256 {
    param([string]$LiteralPath)

    $full = [System.IO.Path]::GetFullPath($LiteralPath)
    if ($full.StartsWith($stage + '\', [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $recovery -PathType Container)) {
        $active = @(Get-ChildItem -LiteralPath $recovery -Directory -Filter 'deploy-*' | Where-Object {
            (Test-Path -LiteralPath (Join-Path $_.FullName 'activating')) -and
            -not (Test-Path -LiteralPath (Join-Path $_.FullName 'complete')) -and
            -not (Test-Path -LiteralPath (Join-Path $_.FullName 'rolled-back'))
        })
        if ($active.Count -gt 0) {
            $script:activeStageHashes++
            if ($Mode -eq 'Fault' -and -not $script:faultInjected -and $script:activeStageHashes -eq 61) {
                $script:faultInjected = $true
                throw 'synthetic full-stage activation fault'
            }
            if ($Mode -eq 'AbruptChild' -and $script:activeStageHashes -eq 3) {
                [Environment]::Exit(77)
            }
        }
    }
    return (Get-FileHash -LiteralPath $LiteralPath -Algorithm SHA256).Hash
}

function Assert-Sentinels {
    foreach ($relative in $sentinels) {
        if ([System.IO.File]::ReadAllText((Join-Path $target $relative)) -ne 'unmanaged-synthetic') {
            throw "Unmanaged synthetic file changed: $relative"
        }
    }
}

function Assert-OldManagedState {
    foreach ($file in $stageFiles) {
        $relative = $file.FullName.Substring($stage.Length).TrimStart('\', '/')
        $path = Join-Path $target $relative
        if ($relative -eq $firstRelative) {
            if ([System.IO.File]::ReadAllText($path) -ne $oldManaged) { throw 'Old managed bytes were not restored.' }
        }
        elseif (Test-Path -LiteralPath $path) { throw "Unexpected managed file after rollback: $relative" }
    }
    Assert-Sentinels
}

function Assert-InstalledState {
    foreach ($file in $stageFiles) {
        $relative = $file.FullName.Substring($stage.Length).TrimStart('\', '/')
        $path = Join-Path $target $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) {
            throw "Installed file does not match the Stage: $relative"
        }
    }
    Assert-Sentinels
}

function Get-OnlyRecoveryRecord {
    $records = @(Get-ChildItem -LiteralPath $recovery -Directory -Filter 'deploy-*')
    if ($records.Count -ne 1) { throw "Expected one recovery record, found $($records.Count)." }
    $record = $records[0]
    $manifest = Get-Content -LiteralPath (Join-Path $record.FullName 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.files.Count -ne 124 -or $manifest.target -ne $target) { throw 'Full Stage manifest is incomplete.' }
    return $record
}

if ($Mode -eq 'AbruptChild') {
    Assert-Sentinels
    Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
    throw 'Abrupt child unexpectedly completed.'
}

if ($Mode -eq 'Fault') {
    try {
        Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
        throw 'Expected a synthetic full-stage activation fault.'
    }
    catch {
        if (-not $_.Exception.Message.Contains('all touched files were restored: synthetic full-stage activation fault')) { throw }
    }
    if (-not $script:faultInjected -or $script:activeStageHashes -ne 61) { throw 'Fault was not injected at Stage item 61.' }
    $record = Get-OnlyRecoveryRecord
    if (-not (Test-Path -LiteralPath (Join-Path $record.FullName 'rolled-back')) -or
        (Test-Path -LiteralPath (Join-Path $record.FullName 'complete'))) { throw 'Fault recovery marker is wrong.' }
    $backup = Join-Path (Join-Path $record.FullName 'files') $firstRelative
    if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $target $firstRelative) -Algorithm SHA256).Hash) {
        throw 'Restored old managed file does not match its private backup.'
    }
    Assert-OldManagedState
    Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
    Assert-InstalledState
    $recordCount = @(Get-ChildItem -LiteralPath $recovery -Directory -Filter 'deploy-*').Count
    if ($recordCount -ne 2) { throw 'Normal retry lacked its completed recovery record.' }
    Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
    if (@(Get-ChildItem -LiteralPath $recovery -Directory -Filter 'deploy-*').Count -ne $recordCount) {
        throw 'No-op deployment created another recovery record.'
    }
    [System.IO.File]::WriteAllText((Join-Path $run 'summary.txt'), 'fullStage=124 faultAt=61 rolledBack=124 retry=124 noOp=1 unmanaged=3 PASS')
    Write-Output 'fullStage=124 faultAt=61 rolledBack=124 retry=124 noOp=1 unmanaged=3 PASS'
    return
}

$pwsh = Join-Path $PSHOME 'pwsh.exe'
if (-not (Test-Path -LiteralPath $pwsh -PathType Leaf)) { throw 'PowerShell child executable is missing.' }
$childOutput = & $pwsh -NoLogo -NoProfile -File $PSCommandPath -ProjectRoot $project -StageModuleDir $stage -RunRoot $run -Mode AbruptChild 2>&1
$childCode = $LASTEXITCODE
$childOutput | Out-File -LiteralPath (Join-Path $run 'abrupt-child.log') -Encoding utf8
if ($childCode -ne 77) { throw "Abrupt child exited $childCode instead of 77." }
$record = Get-OnlyRecoveryRecord
if (-not (Test-Path -LiteralPath (Join-Path $record.FullName 'activating')) -or
    (Test-Path -LiteralPath (Join-Path $record.FullName 'complete')) -or
    (Test-Path -LiteralPath (Join-Path $record.FullName 'rolled-back'))) {
    throw 'Abrupt child did not leave an interrupted recovery record.'
}
foreach ($file in $stageFiles) {
    $relative = $file.FullName.Substring($stage.Length).TrimStart('\', '/')
    $path = Join-Path $target $relative
    if ($relative -eq $firstRelative -or $relative -eq $secondRelative) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) {
            throw "Abrupt child did not install the expected first two files: $relative"
        }
    }
    elseif (Test-Path -LiteralPath $path) { throw "Abrupt child installed an unexpected file: $relative" }
}
Assert-Sentinels
try {
    Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
    throw 'Interrupted deployment was not rejected.'
}
catch {
    if (-not $_.Exception.Message.Contains('interrupted deployment')) { throw }
}
foreach ($item in (Get-Content -LiteralPath (Join-Path $record.FullName 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json).files) {
    $path = Join-Path $target $item.relative
    Assert-DeploymentPath -Path $path -Root $modules
    if ($item.oldSha256) {
        $backup = Join-Path (Join-Path $record.FullName 'files') $item.relative
        if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $item.oldSha256 -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $item.newSha256) {
            throw "Manual recovery source changed: $($item.relative)"
        }
        [System.IO.File]::Copy($backup, $path, $true)
    }
    elseif (Test-Path -LiteralPath $path -PathType Leaf) {
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $item.newSha256) {
            throw "Manual recovery refuses an unknown target: $($item.relative)"
        }
        [System.IO.File]::Delete($path)
    }
}
Assert-OldManagedState
Write-DeploymentMarker -Directory $record.FullName -Name 'rolled-back'
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
Assert-InstalledState
$recordCount = @(Get-ChildItem -LiteralPath $recovery -Directory -Filter 'deploy-*').Count
if ($recordCount -ne 2) { throw 'Recovered retry lacked its completed recovery record.' }
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if (@(Get-ChildItem -LiteralPath $recovery -Directory -Filter 'deploy-*').Count -ne $recordCount) {
    throw 'No-op deployment created another recovery record.'
}
[System.IO.File]::WriteAllText((Join-Path $run 'summary.txt'), 'fullStage=124 abruptAfter=2 refused=1 manualRollback=124 retry=124 noOp=1 unmanaged=3 PASS')
Write-Output 'fullStage=124 abruptAfter=2 refused=1 manualRollback=124 retry=124 noOp=1 unmanaged=3 PASS'
