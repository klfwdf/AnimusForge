[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$ProjectRoot, [Parameter(Mandatory = $true)][string]$RunRoot)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($RunRoot)
$allowed = [IO.Path]::GetFullPath((Join-Path $ProjectRoot 'artifacts\j15-content'))
if (-not $root.StartsWith($allowed + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $root)) {
    throw 'New workspace-only storage deploy fixture required.'
}
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $ProjectRoot 'scripts\build\deploy_module.ps1'), [ref]$null, [ref]$errors)
if ($errors.Count) { throw 'Deploy syntax errors.' }
foreach ($name in @('Get-FullPathSafe', 'Get-FileSha256', 'Assert-PathUnderRoot', 'Assert-NotReparsePoint', 'Assert-NoReparseAncestors', 'Assert-DeploymentPath', 'Write-DeploymentMarker', 'Assert-FeatureBridgesDeploymentBaseline', 'Invoke-ManagedStageDeployment')) {
    $node = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $false))
    if ($node.Count -ne 1) { throw "Missing function: $name" }
    . ([scriptblock]::Create($node[0].Extent.Text))
}
$stage = Join-Path $root 'stage'
$modules = Join-Path $root 'game\Modules'
$target = Join-Path $modules 'AnimusForge'
$recovery = Join-Path $modules '.AnimusForge.Recovery\deploy'
$fakeUser = Join-Path $root 'ignored-user-data'
New-Item -ItemType Directory -Path $stage,$target,$fakeUser | Out-Null
$env:LOCALAPPDATA = $null
$env:ANIMUSFORGE_DATA_ROOT = $fakeUser
function Put([string]$base, [string]$relative, [string]$text, [datetime]$time) {
    $path = Join-Path $base $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [IO.File]::WriteAllText($path, $text)
    [IO.File]::SetLastWriteTimeUtc($path, $time)
}
$old = ([datetime]'2020-01-01T00:00:00Z').ToUniversalTime()
$new = ([datetime]'2021-01-01T00:00:00Z').ToUniversalTime()
$relative = 'PlayerExports\worldbook\data.json'
Put $stage $relative 'default-new' $new
Put $target $relative 'player-old' $old
foreach ($name in @('CustomPrompts\keep.json', 'ModuleData\TerminalSettings.json', 'Logs\ModelDropdownCache.json', 'Recovery\private.json')) {
    Put $stage $name 'default' $new
    Put $target $name '{broken-player-content' $old
}
Put $target 'PlayerExports\extra\private.json' 'EXTRA' $new
$sourceHashes = @{}
foreach ($file in Get-ChildItem -LiteralPath $stage -File -Recurse) { $sourceHashes[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if ([IO.File]::ReadAllText((Join-Path $target $relative)) -ne 'default-new' -or [IO.File]::GetLastWriteTimeUtc((Join-Path $target $relative)) -ne $new) { throw 'Newer default or source timestamp not installed.' }
foreach ($name in @('CustomPrompts\keep.json', 'ModuleData\TerminalSettings.json', 'Logs\ModelDropdownCache.json', 'Recovery\private.json')) {
    if ([IO.File]::ReadAllText((Join-Path $target $name)) -ne '{broken-player-content') { throw "Player file overwritten: $name" }
}
if (-not (Test-Path -LiteralPath $recovery) -or (Get-ChildItem -LiteralPath $fakeUser -Force).Count -ne 0) { throw 'Recovery did not stay in Modules.' }
$count = @(Get-ChildItem -LiteralPath $recovery -Directory).Count
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if (@(Get-ChildItem -LiteralPath $recovery -Directory).Count -ne $count) { throw 'No-op created a recovery record.' }
Put $target $relative 'player-newer' ([datetime]'2022-01-01T00:00:00Z')
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if ([IO.File]::ReadAllText((Join-Path $target $relative)) -ne 'player-newer') { throw 'Newer player file lost.' }
Put $target $relative 'player-equal-time' $new
$env:LOCALAPPDATA = 'relative-invalid-root'
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if ([IO.File]::ReadAllText((Join-Path $target $relative)) -ne 'player-equal-time') { throw 'Equal timestamps must prefer installed module.' }
Put $stage 'PlayerExports\missing\data.json' 'missing-default' $new
Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules
if (-not (Test-Path -LiteralPath (Join-Path $target 'PlayerExports\missing\data.json')) -or [IO.File]::ReadAllText((Join-Path $target 'PlayerExports\extra\private.json')) -ne 'EXTRA') { throw 'Missing install or non-deleting merge failed.' }
Put $stage $relative 'fault-default' ([datetime]'2023-01-01T00:00:00Z')
Put $stage 'PlayerExports\worldbook\same-bytes.json' 'same-bytes' $new
Put $target 'PlayerExports\worldbook\same-bytes.json' 'same-bytes' $old
Put $stage 'z.txt' 'new-z' $new
Put $target 'z.txt' 'old-z' $old
$rawHash = ${function:Get-FileSha256}
$script:fault = $true
function Get-FileSha256 {
    param([string]$LiteralPath)
    if ($script:fault -and [IO.Path]::GetFullPath($LiteralPath).Equals([IO.Path]::GetFullPath((Join-Path $script:target 'z.txt')), [StringComparison]::OrdinalIgnoreCase) -and [IO.File]::ReadAllText($LiteralPath) -eq 'new-z') {
        $script:fault = $false
        throw 'Injected post-replacement failure.'
    }
    & $rawHash -LiteralPath $LiteralPath
}
try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules; throw 'Expected injected failure.' }
catch { if (-not $_.Exception.Message.Contains('all touched files were restored')) { throw } }
if ([IO.File]::ReadAllText((Join-Path $target $relative)) -ne 'player-equal-time' -or [IO.File]::GetLastWriteTimeUtc((Join-Path $target $relative)) -ne $new -or [IO.File]::GetLastWriteTimeUtc((Join-Path $target 'z.txt')) -ne $old) { throw 'Rollback failed to restore bytes and timestamps.' }
if ([IO.File]::GetLastWriteTimeUtc((Join-Path $target 'PlayerExports\worldbook\same-bytes.json')) -ne $old) { throw 'Same-byte timestamp-only merge did not roll back.' }
foreach ($path in $sourceHashes.Keys) {
    if ($path -ne (Join-Path $stage $relative) -and (Get-FileHash -LiteralPath $path).Hash -ne $sourceHashes[$path]) { throw 'Stage changed during merge.' }
}
$outside = Join-Path $root 'outside-recovery'
$badModules = Join-Path $root 'unsafe-game\Modules'
$badTarget = Join-Path $badModules 'AnimusForge'
New-Item -ItemType Directory -Path $outside,$badTarget | Out-Null
New-Item -ItemType Junction -Path (Join-Path $badModules '.AnimusForge.Recovery') -Target $outside | Out-Null
try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $badTarget -ModulesDir $badModules; throw 'Expected reparse rejection.' }
catch { if (-not $_.Exception.Message.Contains('reparse point')) { throw } }
if ((Get-ChildItem -LiteralPath $outside -Force).Count -ne 0 -or (Get-ChildItem -LiteralPath $badTarget -Force).Count -ne 0) { throw 'Reparse recovery modified an unintended directory.' }
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$icacls = Join-Path ([Environment]::GetFolderPath('System')) 'icacls.exe'
& $icacls $recovery '/deny' "*${sid}:(OI)(CI)(WD,AD)" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Readonly recovery fixture ACL failed.' }
try {
    $readonlyFailed = $false
    try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules }
    catch { $readonlyFailed = $true }
    if (-not $readonlyFailed -or [IO.File]::ReadAllText((Join-Path $target 'z.txt')) -ne 'old-z') { throw 'Readonly Recovery reported success or changed target.' }
}
finally {
    & $icacls $recovery '/remove:d' "*$sid" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Readonly recovery ACL restore failed.' }
}
$interrupted = Join-Path $recovery 'deploy-interrupted-fixture'
New-Item -ItemType Directory -Path $interrupted | Out-Null
[IO.File]::WriteAllText((Join-Path $interrupted 'manifest.json'), (@{schemaVersion=1; target=$target; files=@()} | ConvertTo-Json))
[IO.File]::WriteAllText((Join-Path $interrupted 'activating'), 'synthetic')
try { Invoke-ManagedStageDeployment -StageModuleDir $stage -TargetModuleDir $target -ModulesDir $modules; throw 'Expected interruption rejection.' }
catch { if (-not $_.Exception.Message.Contains('interrupted deployment')) { throw } }
Write-Output 'PASS module storage deploy newerDefault=1 newerPlayer=1 equalTime=1 missing=1 extra=1 protected=4 noop=1 missingAppData=1 invalidAppData=1 rollbackBytesTime=1 sameBytesTimeRollback=1 stageUnchanged=1 reparse=1 readonly=1 interruption=1'
