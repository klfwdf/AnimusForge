[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($RunRoot)
$allowed = [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot 'artifacts\j15-content'))
if (-not $root.StartsWith($allowed + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
    (Test-Path -LiteralPath $root)) { throw 'Unsafe or reused deploy fixture root.' }

$astErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $ProjectRoot 'scripts\build\deploy_module.ps1'), [ref]$null, [ref]$astErrors)
if ($astErrors.Count -ne 0) { throw 'Deploy script has parse errors.' }
foreach ($name in @('Get-FullPathSafe', 'Assert-PathUnderRoot', 'Assert-NotReparsePoint',
        'Assert-NoReparseAncestors', 'Assert-DeploymentPath', 'Write-DeploymentMarker',
        'Assert-FeatureBridgesDeploymentBaseline', 'Invoke-ManagedStageDeployment')) {
    $definition = @($ast.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $false))
    if ($definition.Count -ne 1) { throw "Missing or duplicate deployment function: $name" }
    . ([scriptblock]::Create($definition[0].Extent.Text))
}

$stage = Join-Path $root 'stage'
$modules = Join-Path $root 'game\Modules'
$target = Join-Path $modules 'AnimusForge'
$local = Join-Path $root 'localappdata'
New-Item -ItemType Directory -Path $stage,$target,$local | Out-Null
$env:LOCALAPPDATA = $local
foreach ($name in @('a.txt', 'b.txt')) {
    [System.IO.File]::WriteAllText((Join-Path $stage $name), "new-$name")
    [System.IO.File]::WriteAllText((Join-Path $target $name), "old-$name")
}
$unmanaged = @(
    (Join-Path $target 'PlayerExports\synthetic.txt'),
    (Join-Path $target 'CustomPrompts\synthetic.json'),
    (Join-Path $target 'Logs\synthetic.log'),
    (Join-Path $target 'ONNX\synthetic.bin'),
    (Join-Path $target 'bin\Win64_Shipping_Client\unknown.dll'),
    (Join-Path $modules 'AnimusForge_1_3_x\PlayerExports\synthetic.txt'),
    (Join-Path $modules 'AnimusForge_1_4_5\PlayerExports\synthetic.txt')
)
foreach ($path in $unmanaged) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [System.IO.File]::WriteAllText($path, 'private-synthetic')
}
function Assert-UnmanagedUnchanged {
    foreach ($path in $unmanaged) {
        if ([System.IO.File]::ReadAllText($path) -ne 'private-synthetic') { throw 'Unmanaged file changed.' }
    }
}
$script:failAtSecond = $true

function Get-FileSha256 {
    param([string]$LiteralPath)
    $active = Join-Path $env:LOCALAPPDATA 'AnimusForge\Recovery\deploy'
    if ($script:failAtSecond -and $LiteralPath -eq (Join-Path $stage 'b.txt') -and
        @(Get-ChildItem -LiteralPath $active -Recurse -Filter 'activating' -File -ErrorAction SilentlyContinue).Count -gt 0) {
        $script:failAtSecond = $false
        throw 'synthetic activation fault'
    }
    return (Get-FileHash -LiteralPath $LiteralPath -Algorithm SHA256).Hash
}

try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules; throw 'Expected activation failure.' }
catch {
    if (-not $_.Exception.Message.Contains('all touched files were restored')) { throw }
}
foreach ($name in @('a.txt', 'b.txt')) {
    if ([System.IO.File]::ReadAllText((Join-Path $target $name)) -ne "old-$name") { throw 'Rollback did not restore managed bytes.' }
}
Assert-UnmanagedUnchanged
$recovery = Join-Path $local 'AnimusForge\Recovery\deploy'
$failed = @(Get-ChildItem -LiteralPath $recovery -Directory)
if ($failed.Count -ne 1 -or -not (Test-Path -LiteralPath (Join-Path $failed[0].FullName 'rolled-back'))) {
    throw 'Failed deployment lacks a rolled-back recovery record.'
}
foreach ($name in @('a.txt', 'b.txt')) {
    if ((Get-FileHash -LiteralPath (Join-Path $failed[0].FullName "files\$name") -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $target $name) -Algorithm SHA256).Hash) {
        throw 'Private rollback backup differs from restored target.'
    }
}

Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
foreach ($name in @('a.txt', 'b.txt')) {
    if ([System.IO.File]::ReadAllText((Join-Path $target $name)) -ne "new-$name") { throw 'Managed file was not installed.' }
}
$completedCount = @(Get-ChildItem -LiteralPath $recovery -Directory).Count
if ($completedCount -ne 2) {
    throw 'Successful deployment altered an unmanaged file or lacked a private record.'
}
Assert-UnmanagedUnchanged
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if (@(Get-ChildItem -LiteralPath $recovery -Directory).Count -ne $completedCount) {
    throw 'No-op deployment created another recovery record.'
}

$stageConfig = Join-Path $stage 'ModuleData\FeatureBridges.json'
$targetConfig = Join-Path $target 'ModuleData\FeatureBridges.json'
New-Item -ItemType Directory -Path (Split-Path -Parent $stageConfig),(Split-Path -Parent $targetConfig) -Force | Out-Null
[System.IO.File]::Copy((Join-Path $ProjectRoot 'content\foundation\AF.Foundation.Runtime\ModuleData\FeatureBridges.json'), $stageConfig)
$oldDefault = @'
{
  "schemaVersion": 1,
  "contractVersion": 1,
  "enabled": [
    "conversation-gateway",
    "conversation-action",
    "action-memory",
    "action-economy",
    "policy-world-diplomacy",
    "conversation-siege",
    "conversation-courier",
    "memory-social-reports",
    "gateway-knowledge-profile",
    "ui-runtime-integration",
    "runtime-game-adapter",
    "scene-duel",
    "host-runtime"
  ]
}
'@
[System.IO.File]::WriteAllText($targetConfig, ($oldDefault.Replace("`r`n", "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
if ((Get-FileHash -LiteralPath $targetConfig -Algorithm SHA256).Hash -ne
    'C732B9034B1DEC21A81EBC5F74FDE19F4A9BF1DC05E98AAF3B4D875633A3F4E9') {
    throw 'Historical FeatureBridges default fixture does not match the checked-in revision.'
}
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if ((Get-FileHash -LiteralPath $targetConfig -Algorithm SHA256).Hash -ne
    (Get-FileHash -LiteralPath $stageConfig -Algorithm SHA256).Hash) { throw 'Known old default was not upgraded.' }
$completedCount = @(Get-ChildItem -LiteralPath $recovery -Directory).Count

[System.IO.File]::WriteAllText((Join-Path $stage 'a.txt'), 'newer-a.txt')
foreach ($unrecognized in @('{"schemaVersion":1,"contractVersion":1,"enabled":[]}', '{bad json')) {
    [System.IO.File]::WriteAllText($targetConfig, $unrecognized)
    try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules; throw 'Expected FeatureBridges conflict.' }
    catch {
        if (-not $_.Exception.Message.Contains('FeatureBridges deployment conflict')) { throw }
    }
    if ([System.IO.File]::ReadAllText((Join-Path $target 'a.txt')) -ne 'new-a.txt' -or
        [System.IO.File]::ReadAllText($targetConfig) -ne $unrecognized -or
        @(Get-ChildItem -LiteralPath $recovery -Directory).Count -ne $completedCount) {
        throw 'FeatureBridges conflict changed a managed target or private recovery.'
    }
}
Assert-UnmanagedUnchanged
[System.IO.File]::Copy($stageConfig, $targetConfig, $true)

$newTarget = Join-Path $modules 'AnimusForge-new-install'
New-Item -ItemType Directory -Path $newTarget | Out-Null
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $newTarget -ModulesDir $modules
if ((Get-FileHash -LiteralPath (Join-Path $newTarget 'ModuleData\FeatureBridges.json') -Algorithm SHA256).Hash -ne
    (Get-FileHash -LiteralPath $stageConfig -Algorithm SHA256).Hash) { throw 'New install did not receive the default.' }

$interrupted = Join-Path $recovery 'deploy-interrupted-fixture'
New-Item -ItemType Directory -Path $interrupted | Out-Null
[System.IO.File]::WriteAllText((Join-Path $interrupted 'manifest.json'), (@{schemaVersion=1; target=$target; files=@()} | ConvertTo-Json))
[System.IO.File]::WriteAllText((Join-Path $interrupted 'activating'), 'synthetic')
try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules; throw 'Expected interruption rejection.' }
catch {
    if (-not $_.Exception.Message.Contains('interrupted deployment')) { throw }
}
Assert-UnmanagedUnchanged
Write-Output 'managedDeployContract rollback=1 recovery=1 success=1 noop=1 interruption=1 unmanaged=7 featureBridges=known-old,new-install,hand-edit,bad-json PASS'
